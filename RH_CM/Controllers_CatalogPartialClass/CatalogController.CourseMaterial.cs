using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {

        // ==========================================================
        // HELPERS PARA COMBOS (Course y Level)
        // ==========================================================
        private void LoadCourseAndLevelViewBags()
        {
            ViewBag.Courses = _context.CtCourses
                .AsNoTracking()
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToList();

            ViewBag.LevelCourses = _context.CtLevelcourses
                .AsNoTracking()
                .Where(l => l.Available == 1)
                .OrderBy(l => l.PkLevelcourse)
                .ToList();
        }

        // GET: CtCoursematerial
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [Authorize(Policy = "ViewAccess")]
        public IActionResult IndexCourseMaterial()
        {
            var materials = (
                from m in _context.CtCoursematerials.AsNoTracking()

                    // LEFT JOIN a CourseLevelMaterial
                join clm in _context.CtCourseLevelMaterials.AsNoTracking()
                    on m.PkCoursematerial equals clm.FkCourseMaterial into clmJoin
                from clm in clmJoin.DefaultIfEmpty()

                    // LEFT JOIN a Courses
                join c in _context.CtCourses.AsNoTracking()
                    on clm.FkCourse equals c.PkCourse into courseJoin
                from c in courseJoin.DefaultIfEmpty()

                    // LEFT JOIN a LevelCourses
                join lvl in _context.CtLevelcourses.AsNoTracking()
                    on clm.FkLevelCourse equals lvl.PkLevelcourse into lvlJoin
                from lvl in lvlJoin.DefaultIfEmpty()

                orderby m.NameMaterial
                select new
                {
                    m.PkCoursematerial,
                    MaterialName = m.NameMaterial,
                    m.MaterialType,
                    m.Available,
                    m.CreateUser,
                    m.CreateDate,
                    m.LastUpdateUser,
                    m.LastUpdateDate,

                    // 🔽 Nuevos campos solicitados
                    CourseName = c != null ? c.CourseName : "Not Assigned",
                    LevelName = lvl != null ? lvl.DescripctionLevel : "Not Assigned"
                }
            ).ToList();

            return View(materials);
        }


        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public IActionResult CreateCourseMaterial(string type)
        {
            LoadCourseAndLevelViewBags(); 
            ViewBag.Type = type;
            return View(new CtCoursematerial());
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePDFMaterial(
            List<IFormFile> uploadedFiles,
            int fkCourse,
            int fkLevelCourse
        )
        {
            if (uploadedFiles == null || !uploadedFiles.Any())
            {
                TempData["ErrorMessage"] = "You must upload at least one PDF file.";
                return RedirectToAction(nameof(CreateCourseMaterial), new { type = "PDF" });
            }

            if (fkCourse > 0)
            {
                // 1) Si intenta subir más de 1 PDF al mismo curso en un solo post
                if (uploadedFiles.Count > 1)
                {
                    TempData["ErrorMessage"] = "Only one PDF can be assigned per course. Please upload a single PDF.";
                    return RedirectToAction(nameof(CreateCourseMaterial), new { type = "PDF" });
                }

                // 2) Si el curso ya tiene un PDF asignado previamente
                bool courseAlreadyHasPdf = await (
                    from clm in _context.CtCourseLevelMaterials.AsNoTracking()
                    join cm in _context.CtCoursematerials.AsNoTracking()
                        on clm.FkCourseMaterial equals cm.PkCoursematerial
                    where clm.FkCourse == fkCourse
                          && clm.Available == 1
                          && cm.Available == 1
                          && cm.MaterialType == "PDF"
                    select clm.PkCourseLevelMaterial
                ).AnyAsync();

                if (courseAlreadyHasPdf)
                {
                    TempData["ErrorMessage"] = "This course already has a PDF assigned. Only one PDF is allowed per course.";
                    return RedirectToAction(nameof(CreateCourseMaterial), new { type = "PDF" });
                }
            }

            if (fkCourse > 0 && fkLevelCourse > 0)
            {
                bool comboExists = await _context.CtCourseassignments
                    .AnyAsync(ca => ca.FkCourse == fkCourse && ca.FkRequiredCourseLevels == fkLevelCourse);

                if (!comboExists)
                {
                    TempData["ErrorMessage"] = "This Course-Level combination doesn't exist in Courseassignments.";
                    return RedirectToAction(nameof(CreateCourseMaterial), new { type = "PDF" });
                }
            }

            var createdMaterials = new List<CtCoursematerial>();

            foreach (var uploadedFile in uploadedFiles)
            {
                if (uploadedFile.Length == 0) continue;

                var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? "";

                if (!fileName.EndsWith(".pdf"))
                {
                    TempData["ErrorMessage"] = "Only PDF files are allowed (.pdf).";
                    return RedirectToAction(nameof(CreateCourseMaterial), new { type = "PDF" });
                }

                bool existsName = await _context.CtCoursematerials
                    .AsNoTracking()
                    .AnyAsync(m => m.NameMaterial == fileName && m.Available == 1);

                if (existsName)
                {
                    TempData["ErrorMessage"] = $"A material with the name {fileName} already exists.";
                    return RedirectToAction(nameof(CreateCourseMaterial), new { type = "PDF" });
                }

                using var ms = new MemoryStream();
                await uploadedFile.CopyToAsync(ms);

                var material = new CtCoursematerial
                {
                    NameMaterial = fileName,
                    MaterialType = "PDF",
                    File = ms.ToArray(),
                    CreateUser = User.Identity?.Name ?? "Unknown",
                    CreateDate = DateTime.Now,
                    LastUpdateUser = User.Identity?.Name ?? "Unknown",
                    LastUpdateDate = DateTime.Now,
                    Available = 1
                };

                _context.Add(material);
                createdMaterials.Add(material);
            }

            // Guardar para obtener PKs
            await _context.SaveChangesAsync();

            // ✅ ASIGNAR AUTOMÁTICO (si seleccionaron Course y Level)
            if (fkCourse > 0 && fkLevelCourse > 0)
            {
                foreach (var mat in createdMaterials)
                {
                    bool alreadyLinked = await _context.CtCourseLevelMaterials.AnyAsync(x =>
                        x.FkCourse == fkCourse &&
                        x.FkLevelCourse == fkLevelCourse &&
                        x.FkCourseMaterial == mat.PkCoursematerial
                    );

                    if (!alreadyLinked)
                    {
                        var link = new CtCourseLevelMaterial
                        {
                            FkCourse = fkCourse,
                            FkLevelCourse = fkLevelCourse,
                            FkCourseMaterial = mat.PkCoursematerial,
                            CreateUser = User.Identity?.Name ?? "Unknown",
                            CreateDate = DateTime.Now,
                            LastUpdateUser = User.Identity?.Name ?? "Unknown",
                            LastUpdateDate = DateTime.Now,
                            Available = 1
                        };

                        _context.Add(link);
                    }
                }

                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Materials uploaded successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVideoMaterial(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                TempData["ErrorMessage"] = "Please choose a Video File or valid URL.";
                return RedirectToAction(nameof(CreateCourseMaterial), new { type = "VIDEO" });
            }

            //// Lista de extensiones permitidas
            //var allowedExtensions = new[] { ".exe", ".mp4", ".avi", ".mov", ".mkv", ".wmv" };

            //// Obtener la extensión del archivo
            //var fileExtension = Path.GetExtension(filePath)?.ToLower();

            //if (!allowedExtensions.Contains(fileExtension))
            //{
            //    TempData["ErrorMessage"] = "Not Valid Extension, must end with .exe, .mp4, .avi, .mov, .mkv, .wmv";
            //    return RedirectToAction(nameof(CreateCourseMaterial), new { type = "VIDEO" });
            //}


            // Validar que contenga al menos un "/" o "\"
            if (!filePath.Contains("https://"))
            {
                TempData["ErrorMessage"] = "Please enter full path! use: https:// to be valid.";
                return RedirectToAction(nameof(CreateCourseMaterial), new { type = "VIDEO" });
            }

            var fileNameOnly = Path.GetFileNameWithoutExtension(filePath);


            var existsName = await _context.CtCoursematerials
                .AsNoTracking()
                .AnyAsync(m => m.NameMaterial == fileNameOnly && m.Available == 1);

            if (existsName)
            {
                TempData["ErrorMessage"] = "A Video material with the same name already exists.";
                return RedirectToAction(nameof(CreateCourseMaterial), new { type = "VIDEO" });
            }

            CtCoursematerial model = new CtCoursematerial();

            // Metadatos
            model.NameMaterial = fileNameOnly;
            model.MaterialType = "VIDEO";
            model.UrlPath = filePath;
            model.CreateUser = User.Identity?.Name ?? "Unknown";
            model.CreateDate = DateTime.Now;
            model.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            model.LastUpdateDate = DateTime.Now;
            model.Available = 1;

            _context.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material created successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        // ==========================
        // GET: EditPDFMaterial  
        // ==========================
        [Authorize(Policy = "ViewAccess")]
        [HttpGet]
        public async Task<IActionResult> EditPDFMaterial(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null) return NotFound();

            LoadCourseAndLevelViewBags();

            var link = await _context.CtCourseLevelMaterials
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.FkCourseMaterial == id && x.Available == 1);

            ViewBag.SelectedCourseId = link?.FkCourse ?? 0;
            ViewBag.SelectedLevelId = link?.FkLevelCourse ?? 0;

            return View(material);
        }


        // ==========================
        // POST: EditPDFMaterial  (ÚNICO POST)
        // ==========================
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPDFMaterial(
            CtCoursematerial model,
            IFormFile? uploadedFile,
            int fkCourse,
            int fkLevelCourse
        )
        {
            int id = model.PkCoursematerial;

            var existing = await _context.CtCoursematerials.FindAsync(id);
            if (existing == null) return NotFound();

            // ==============================
            // VALIDAR Y ACTUALIZAR PDF
            // ==============================
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? "";

                if (!fileName.EndsWith(".pdf"))
                {
                    TempData["ErrorMessage"] = "Only PDF files are allowed (.pdf).";
                    return RedirectToAction(nameof(EditPDFMaterial), new { id });
                }

                using var ms = new MemoryStream();
                await uploadedFile.CopyToAsync(ms);

                existing.File = ms.ToArray();
                existing.NameMaterial = fileName;
            }

            existing.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            existing.LastUpdateDate = DateTime.Now;

            _context.Update(existing);
            await _context.SaveChangesAsync();

            // ======================================================
            // VALIDACIONES DE CURSO (aunque no se pueda cambiar)
            // ======================================================
            if (fkCourse > 0 && fkLevelCourse > 0)
            {
                bool validCombo = await _context.CtCourseassignments
                    .AnyAsync(ca => ca.FkCourse == fkCourse &&
                                    ca.FkRequiredCourseLevels == fkLevelCourse);

                if (!validCombo)
                {
                    TempData["ErrorMessage"] = "This Course-Level combination doesn't exist in Courseassignments.";
                    return RedirectToAction(nameof(EditPDFMaterial), new { id });
                }

                bool courseHasOtherPdf = await (
                    from clm in _context.CtCourseLevelMaterials.AsNoTracking()
                    join cm in _context.CtCoursematerials.AsNoTracking()
                        on clm.FkCourseMaterial equals cm.PkCoursematerial
                    where clm.FkCourse == fkCourse
                          && clm.Available == 1
                          && cm.Available == 1
                          && cm.MaterialType == "PDF"
                          && cm.PkCoursematerial != id
                    select clm
                ).AnyAsync();

                if (courseHasOtherPdf)
                {
                    TempData["ErrorMessage"] = "This course already has a PDF assigned. Only one PDF is allowed per course.";
                    return RedirectToAction(nameof(EditPDFMaterial), new { id });
                }
            }

            TempData["SuccessMessage"] = "Material updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        // POST: CtCoursematerial/Edit/5
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVIDEOMaterial(/*int id,**/ CtCoursematerial model, string filePath)
        {
            int id = model.PkCoursematerial;

            var existing = await _context.CtCoursematerials.FindAsync(id);
            if (existing == null) return NotFound();


            if (string.IsNullOrEmpty(filePath))
            {
                TempData["ErrorMessage"] = "Please Choose a Video File";
                return RedirectToAction(nameof(EditPDFMaterial), new { id });
            }

            //// Lista de extensiones permitidas
            //var allowedExtensions = new[] { ".exe", ".mp4", ".avi", ".mov", ".mkv", ".wmv" };

            //// Obtener la extensión del archivo
            //var fileExtension = Path.GetExtension(filePath)?.ToLower();

            //if (!allowedExtensions.Contains(fileExtension))
            //{
            //    TempData["ErrorMessage"] = "Not Valid Extension, must end with .exe, .mp4, .avi, .mov, .mkv, .wmv";
            //    return RedirectToAction(nameof(CreateCourseMaterial), new { type = "VIDEO" });
            //}

            // Validar que contenga al menos un "/" o "\"
            if (!filePath.Contains("https://"))
            {
                TempData["ErrorMessage"] = "Please enter full path! use: https:// to be valid.";
                return RedirectToAction(nameof(CreateCourseMaterial), new { type = "VIDEO" });
            }

            var fileNameOnly = Path.GetFileNameWithoutExtension(filePath);

            // Actualizar campos editables
            existing.NameMaterial = fileNameOnly;
            existing.UrlPath = filePath;
            existing.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            existing.LastUpdateDate = DateTime.Now;


            _context.Update(existing);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Video Material updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }


        // POST: CtCoursematerial/Edit/5
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourseMaterial(int id, CtCoursematerial model, IFormFile? uploadedFile)
        {
            if (id != model.PkCoursematerial) return NotFound();

            var existing = await _context.CtCoursematerials.FindAsync(id);
            if (existing == null) return NotFound();

            // Validaciones mínimas
            if (string.IsNullOrWhiteSpace(model.NameMaterial))
            {
                TempData["ErrorMessage"] = "Material Name is required.";
                return View(model);
            }

            // Actualizar campos editables
            existing.NameMaterial = model.NameMaterial;
            existing.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            existing.LastUpdateDate = DateTime.Now;

            // Si subió nuevo archivo (PDF)
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? "";
                if (!fileName.EndsWith(".pdf"))
                {
                    TempData["ErrorMessage"] = "Only PDF files are allowed (.pdf).";
                    return View(model);
                }

                using var ms = new MemoryStream();
                await uploadedFile.CopyToAsync(ms);
                existing.File = ms.ToArray();
            }

            _context.Update(existing);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        // POST: CtCoursematerial/Toggle/5
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourseMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
            {
                TempData["ErrorMessage"] = "Material not found.";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            material.Available = material.Available == 1 ? 0 : 1;
            material.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            material.LastUpdateDate = DateTime.Now;

            _context.Update(material);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material status updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        // POST: CtCoursematerial/Delete/5
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseMaterial(int id)
        {
            // 1) Buscar todos los vínculos Course–Level–Material de ese material
            var links = await _context.CtCourseLevelMaterials
                .Where(x => x.FkCourseMaterial == id)
                .ToListAsync();

            // 2) Buscar el material
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
            {
                TempData["ErrorMessage"] = "Material not found.";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            // 3) Borrar en ambas tablas dentro de una transacción
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                if (links.Any())
                {
                    _context.CtCourseLevelMaterials.RemoveRange(links);
                    await _context.SaveChangesAsync();
                }

                _context.CtCoursematerials.Remove(material);
                await _context.SaveChangesAsync();

                await tx.CommitAsync();

                TempData["SuccessMessage"] = "Material and its assignments were deleted successfully.";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                TempData["ErrorMessage"] = $"Error deleting material: {ex.Message}";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }
        }


        // GET: CtCoursematerial/Download/5
        public async Task<IActionResult> DownloadCourseMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
                return NotFound();

            return File(material.File, "application/pdf", $"{material.NameMaterial}.pdf");
        }

        // GET: CtCoursematerial/ViewPdf/5
        public async Task<IActionResult> ViewPdfCourseMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
                return NotFound();

            return File(material.File, "application/pdf");
        }
    }
}
