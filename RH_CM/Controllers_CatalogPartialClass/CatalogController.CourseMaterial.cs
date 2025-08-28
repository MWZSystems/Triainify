using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        // GET: CtCoursematerial
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public IActionResult IndexCourseMaterial()
        {
            var materials = _context.CtCoursematerials
                .AsNoTracking()
                //.Where(m => m.Available == 1)
                .OrderBy(m => m.NameMaterial)
                .Select(m => new
                {
                    m.PkCoursematerial,
                    MaterialName = m.NameMaterial,
                    m.MaterialType,
                    m.Available,
                    m.CreateUser,
                    m.CreateDate,
                    m.LastUpdateUser,
                    m.LastUpdateDate
                })
                .ToList();

            return View(materials);
        }

        // GET: CtCoursematerial/Create
        [HttpGet]
        [Authorize(Roles = "Administrador, RHGerente")]
        public IActionResult CreateCourseMaterial()
        {
            // Ya no cargamos listas ni ViewBags
            return View(new CtCoursematerial());
        }


        //Crear Material PDF
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePDFMaterial(List<IFormFile> uploadedFiles)
        {
            if (uploadedFiles == null || !uploadedFiles.Any())
            {
                TempData["ErrorMessage"] = "You must upload at least one PDF file.";
                return RedirectToAction(nameof(CreateCourseMaterial));
            }

            foreach (var uploadedFile in uploadedFiles)
            {
                if (uploadedFile.Length == 0) continue;

                var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? "";

                if (!fileName.EndsWith(".pdf"))
                {
                    TempData["ErrorMessage"] = "Only PDF files are allowed (.pdf).";
                    return RedirectToAction(nameof(CreateCourseMaterial));
                }

                // Verificar si ya existe un material con ese nombre (opcional)
                var existsName = await _context.CtCoursematerials
                    .AsNoTracking()
                    .AnyAsync(m => m.NameMaterial == fileName && m.Available == 1);

                if (existsName)
                {
                    TempData["ErrorMessage"] = $"A material with the name {fileName} already exists.";
                    return RedirectToAction(nameof(CreateCourseMaterial));
                }

                // Guardar archivo
                using var ms = new MemoryStream();
                await uploadedFile.CopyToAsync(ms);

                var material = new CtCoursematerial
                {
                    NameMaterial = fileName,   // nombre del archivo con extensión
                    MaterialType = "PDF",
                    File = ms.ToArray(),
                    CreateUser = User.Identity?.Name ?? "Unknown",
                    CreateDate = DateTime.Now,
                    LastUpdateUser = User.Identity?.Name ?? "Unknown",
                    LastUpdateDate = DateTime.Now,
                    Available = 1
                };

                _context.Add(material);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Materials uploaded successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }


        //Crear Material PDF
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVideoMaterial(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                TempData["ErrorMessage"] = "Please Choose a Video File";
                return RedirectToAction(nameof(CreateCourseMaterial));
            }

            // Lista de extensiones permitidas
            var allowedExtensions = new[] { ".exe", ".mp4", ".avi", ".mov", ".mkv", ".wmv" };

            // Obtener la extensión del archivo
            var fileExtension = Path.GetExtension(filePath)?.ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Not Valid Extension, must end with .exe, .mp4, .avi, .mov, .mkv, .wmv";
                return RedirectToAction(nameof(CreateCourseMaterial));
            }


            // Validar que contenga al menos un "/" o "\"
            if (!filePath.Contains("/") && !filePath.Contains("\\"))
            {
                TempData["ErrorMessage"] = "Please enter full path! use: '/' or '\\' to be valid.";
                return RedirectToAction(nameof(CreateCourseMaterial));
            }


            var fileNameOnly = Path.GetFileNameWithoutExtension(filePath);


            var existsName = await _context.CtCoursematerials
                .AsNoTracking()
                .AnyAsync(m => m.NameMaterial == fileNameOnly && m.Available == 1);
            if (existsName)
            {
                TempData["ErrorMessage"] = "A Video material with the same name already exists.";
                return RedirectToAction(nameof(CreateCourseMaterial));
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

        //    // POST: CtCoursematerial/Create
        //    [HttpPost]
        //[Authorize(Roles = "Administrador, RHGerente")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> CreateCourseMaterial(CtCoursematerial model, IFormFile uploadedFile)
        //{
        //    // Validar nombre
        //    if (string.IsNullOrWhiteSpace(model.NameMaterial))
        //    {
        //        TempData["ErrorMessage"] = "Material Name is required.";
        //        return View(model);
        //    }

        //    // (Opcional) Evitar duplicado por nombre (ajusta si quieres case-insensitive)
        //    var existsName = await _context.CtCoursematerials
        //        .AsNoTracking()
        //        .AnyAsync(m => m.NameMaterial == model.NameMaterial && m.Available == 1);
        //    if (existsName)
        //    {
        //        TempData["ErrorMessage"] = "A material with the same name already exists.";
        //        return View(model);
        //    }

        //    // Validar archivo
        //    if (uploadedFile == null || uploadedFile.Length == 0)
        //    {
        //        TempData["ErrorMessage"] = "You must upload a PDF file.";
        //        return View(model);
        //    }
        //    var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? "";
        //    if (!fileName.EndsWith(".pdf"))
        //    {
        //        TempData["ErrorMessage"] = "Only PDF files are allowed (.pdf).";
        //        return View(model);
        //    }
        //    // (Opcional) validar content-type reportado por el navegador
        //    // if (uploadedFile.ContentType != "application/pdf") { ... }

        //    // Guardar archivo en varbinary(max)
        //    using (var ms = new MemoryStream())
        //    {
        //        await uploadedFile.CopyToAsync(ms);
        //        model.File = ms.ToArray();
        //    }

        //    // Metadatos
        //    model.CreateUser = User.Identity?.Name ?? "Unknown";
        //    model.CreateDate = DateTime.Now;
        //    model.LastUpdateUser = User.Identity?.Name ?? "Unknown";
        //    model.LastUpdateDate = DateTime.Now;
        //    model.Available = 1;

        //    _context.Add(model);
        //    await _context.SaveChangesAsync();

        //    TempData["SuccessMessage"] = "Material created successfully.";
        //    return RedirectToAction(nameof(IndexCourseMaterial));
        //}



        // GET: CtCoursematerial/Edit/5
        [Authorize(Roles = "Administrador, RHGerente")]
        [HttpGet]
        public async Task<IActionResult> EditCourseMaterial(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null) return NotFound();

            return View(material); // ya no cargamos ViewBags
        }


        // GET: CtCoursematerial/Edit/5
        [Authorize(Roles = "Administrador, RHGerente")]
        [HttpGet]
        public async Task<IActionResult> EditPDFMaterial(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null) return NotFound();

            return View(material); // ya no cargamos ViewBags
        }


        // POST: CtCoursematerial/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPDFMaterial(/*int id,*/ CtCoursematerial model, IFormFile? uploadedFile)
        {
            //if (id != model.PkCoursematerial) return NotFound();

            int id = model.PkCoursematerial;


            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Choose a File.";
                return RedirectToAction(nameof(EditCourseMaterial), new { id });
            }


            var existing = await _context.CtCoursematerials.FindAsync(id);
            if (existing == null) return NotFound();


            //// Validaciones mínimas
            //if (string.IsNullOrWhiteSpace(model.NameMaterial))
            //{
            //    TempData["ErrorMessage"] = "Material Name is required.";
            //    return View(model);
            //}

            var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? "";

            // Actualizar campos editables
            existing.NameMaterial = fileName;
            existing.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            existing.LastUpdateDate = DateTime.Now;


            // Si subió nuevo archivo (PDF)
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                //var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? "";
                if (!fileName.EndsWith(".pdf"))
                {
                    TempData["ErrorMessage"] = "Only PDF files are allowed (.pdf).";
                    return RedirectToAction(nameof(EditCourseMaterial), new { id });
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


        // POST: CtCoursematerial/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVIDEOMaterial(/*int id,**/ CtCoursematerial model, string filePath)
        {
            int id = model.PkCoursematerial;

            var existing = await _context.CtCoursematerials.FindAsync(id);
            if (existing == null) return NotFound();


            if (string.IsNullOrEmpty(filePath))
            {
                TempData["ErrorMessage"] = "Please Choose a Video File";
                return RedirectToAction(nameof(EditCourseMaterial), new { id });
            }

            // Lista de extensiones permitidas
            var allowedExtensions = new[] { ".exe", ".mp4", ".avi", ".mov", ".mkv", ".wmv" };

            // Obtener la extensión del archivo
            var fileExtension = Path.GetExtension(filePath)?.ToLower();

            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "Not Valid Extension, must end with .exe, .mp4, .avi, .mov, .mkv, .wmv";
                return RedirectToAction(nameof(EditCourseMaterial), new { id });
            }


            // Validar que contenga al menos un "/" o "\"
            if (!filePath.Contains("/") && !filePath.Contains("\\"))
            {
                TempData["ErrorMessage"] = "Please enter full path! use: '/' or '\\' to be valid.";
                return RedirectToAction(nameof(EditCourseMaterial), new { id });
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
        [Authorize(Roles = "Administrador, RHGerente")]
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
        [Authorize(Roles = "Administrador, RHGerente")]
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
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseMaterial(int id)
        {

            var validation = await _context.CtCourseLevelMaterials
            .FirstOrDefaultAsync(x => x.FkCourseMaterial == id);

            if (validation != null)
            {
                TempData["ErrorMessage"] = "This material is linked to a Course-Level";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }


            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material != null)
            {

                _context.CtCoursematerials.Remove(material);
                await _context.SaveChangesAsync();
            }
            TempData["SuccessMessage"] = "Material has been deleted Successfully";
            return RedirectToAction(nameof(IndexCourseMaterial));
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
