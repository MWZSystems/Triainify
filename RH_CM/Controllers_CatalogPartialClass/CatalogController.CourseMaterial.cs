using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        private const string PdfMaterialType = "PDF";
        private const string VideoMaterialType = "VIDEO";
        private const int AvailableStatus = 1;

        private void LoadCourseAndLevelViewBags()
        {
            ViewBag.Courses = _context.CtCourses
                .AsNoTracking()
                .Where(c => c.Available == AvailableStatus)
                .OrderBy(c => c.CourseName)
                .ToList();

            ViewBag.LevelCourses = _context.CtLevelcourses
                .AsNoTracking()
                .Where(l => l.Available == AvailableStatus)
                .OrderBy(l => l.PkLevelcourse)
                .ToList();
        }

        private RedirectToActionResult RedirectToMaterialCreation(string materialType, string errorMessage)
        {
            TempData["ErrorMessage"] = errorMessage;
            return RedirectToAction(nameof(CreateCourseMaterial), new { type = materialType });
        }

        private async Task<string?> ValidatePdfCourseSelectionAsync(
            int fkCourse,
            int fkLevelCourse,
            int uploadedFileCount)
        {
            if (fkCourse > 0 && uploadedFileCount > 1)
            {
                return "Only one PDF can be assigned per course. Please upload a single PDF.";
            }

            if (fkCourse > 0 && await CourseAlreadyHasPdfAsync(fkCourse))
            {
                return "This course already has a PDF assigned. Only one PDF is allowed per course.";
            }

            if (fkCourse > 0 &&
                fkLevelCourse > 0 &&
                !await CourseLevelCombinationExistsAsync(fkCourse, fkLevelCourse))
            {
                return "This Course-Level combination doesn't exist in Courseassignments.";
            }

            return null;
        }

        private Task<bool> CourseAlreadyHasPdfAsync(int fkCourse, int? excludedMaterialId = null)
        {
            return (
                from clm in _context.CtCourseLevelMaterials.AsNoTracking()
                join cm in _context.CtCoursematerials.AsNoTracking()
                    on clm.FkCourseMaterial equals cm.PkCoursematerial
                where clm.FkCourse == fkCourse
                      && clm.Available == AvailableStatus
                      && cm.Available == AvailableStatus
                      && cm.MaterialType == PdfMaterialType
                      && (!excludedMaterialId.HasValue ||
                          cm.PkCoursematerial != excludedMaterialId.Value)
                select clm.PkCourseLevelMaterial
            ).AnyAsync();
        }

        private Task<bool> CourseLevelCombinationExistsAsync(int fkCourse, int fkLevelCourse)
        {
            return _context.CtCourseassignments
                .AsNoTracking()
                .AnyAsync(ca =>
                    ca.FkCourse == fkCourse &&
                    ca.FkRequiredCourseLevels == fkLevelCourse);
        }

        private async Task<(List<CtCoursematerial> Materials, string? ErrorMessage)>
            PreparePdfMaterialsAsync(IEnumerable<IFormFile> uploadedFiles)
        {
            var materials = new List<CtCoursematerial>();

            foreach (var uploadedFile in uploadedFiles.Where(file => file.Length > 0))
            {
                var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? string.Empty;

                if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return (materials, "Only PDF files are allowed (.pdf).");
                }

                var existsName = await _context.CtCoursematerials
                    .AsNoTracking()
                    .AnyAsync(m =>
                        m.NameMaterial == fileName &&
                        m.Available == AvailableStatus);

                if (existsName)
                {
                    return (materials, $"A material with the name {fileName} already exists.");
                }

                using var memoryStream = new MemoryStream();
                await uploadedFile.CopyToAsync(memoryStream);

                materials.Add(new CtCoursematerial
                {
                    NameMaterial = fileName,
                    MaterialType = PdfMaterialType,
                    File = memoryStream.ToArray(),
                    CreateUser = User.Identity?.Name ?? "Unknown",
                    CreateDate = DateTime.Now,
                    LastUpdateUser = User.Identity?.Name ?? "Unknown",
                    LastUpdateDate = DateTime.Now,
                    Available = AvailableStatus
                });
            }

            return (materials, null);
        }

        private async Task AssignMaterialsToCourseLevelAsync(
            IEnumerable<CtCoursematerial> materials,
            int fkCourse,
            int fkLevelCourse)
        {
            foreach (var material in materials)
            {
                var alreadyLinked = await _context.CtCourseLevelMaterials
                    .AsNoTracking()
                    .AnyAsync(x =>
                        x.FkCourse == fkCourse &&
                        x.FkLevelCourse == fkLevelCourse &&
                        x.FkCourseMaterial == material.PkCoursematerial);

                if (alreadyLinked)
                {
                    continue;
                }

                _context.CtCourseLevelMaterials.Add(new CtCourseLevelMaterial
                {
                    FkCourse = fkCourse,
                    FkLevelCourse = fkLevelCourse,
                    FkCourseMaterial = material.PkCoursematerial,
                    CreateUser = User.Identity?.Name ?? "Unknown",
                    CreateDate = DateTime.Now,
                    LastUpdateUser = User.Identity?.Name ?? "Unknown",
                    LastUpdateDate = DateTime.Now,
                    Available = AvailableStatus
                });
            }

            await _context.SaveChangesAsync();
        }


        private async Task<List<string>> GetAssignedCourseNamesAsync(int materialId)
        {
            return await (
                from courseLevelMaterial in _context.CtCourseLevelMaterials.AsNoTracking()
                join course in _context.CtCourses.AsNoTracking()
                    on courseLevelMaterial.FkCourse equals course.PkCourse
                where courseLevelMaterial.FkCourseMaterial == materialId
                      && courseLevelMaterial.Available == AvailableStatus
                      && course.Available == AvailableStatus
                orderby course.CourseName
                select course.CourseName
            )
            .Distinct()
            .ToListAsync();
        }

        [Authorize(Policy = "ViewAccess")]
        public IActionResult IndexCourseMaterial()
        {
            var materials = (
                from material in _context.CtCoursematerials.AsNoTracking()
                join courseLevelMaterial in _context.CtCourseLevelMaterials.AsNoTracking()
                    on material.PkCoursematerial equals courseLevelMaterial.FkCourseMaterial
                    into courseLevelMaterialJoin
                from courseLevelMaterial in courseLevelMaterialJoin.DefaultIfEmpty()
                join course in _context.CtCourses.AsNoTracking()
                    on courseLevelMaterial.FkCourse equals course.PkCourse
                    into courseJoin
                from course in courseJoin.DefaultIfEmpty()
                join level in _context.CtLevelcourses.AsNoTracking()
                    on courseLevelMaterial.FkLevelCourse equals level.PkLevelcourse
                    into levelJoin
                from level in levelJoin.DefaultIfEmpty()
                orderby material.NameMaterial
                select new
                {
                    material.PkCoursematerial,
                    MaterialName = material.NameMaterial,
                    material.MaterialType,
                    material.Available,
                    material.CreateUser,
                    material.CreateDate,
                    material.LastUpdateUser,
                    material.LastUpdateDate,
                    CourseName = course != null ? course.CourseName : "Not Assigned",
                    LevelName = level != null ? level.DescripctionLevel : "Not Assigned"
                }
            ).ToList();

            return View(materials);
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public IActionResult CreateCourseMaterial(string type)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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
            int fkLevelCourse)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToMaterialCreation(
                    PdfMaterialType,
                    "The submitted information is not valid.");
            }

            if (uploadedFiles == null || uploadedFiles.Count == 0)
            {
                return RedirectToMaterialCreation(
                    PdfMaterialType,
                    "You must upload at least one PDF file.");
            }

            var courseValidationError = await ValidatePdfCourseSelectionAsync(
                fkCourse,
                fkLevelCourse,
                uploadedFiles.Count);

            if (courseValidationError != null)
            {
                return RedirectToMaterialCreation(PdfMaterialType, courseValidationError);
            }

            var preparationResult = await PreparePdfMaterialsAsync(uploadedFiles);

            if (preparationResult.ErrorMessage != null)
            {
                return RedirectToMaterialCreation(
                    PdfMaterialType,
                    preparationResult.ErrorMessage);
            }

            if (preparationResult.Materials.Count == 0)
            {
                return RedirectToMaterialCreation(
                    PdfMaterialType,
                    "You must upload at least one non-empty PDF file.");
            }

            _context.CtCoursematerials.AddRange(preparationResult.Materials);
            await _context.SaveChangesAsync();

            if (fkCourse > 0 && fkLevelCourse > 0)
            {
                await AssignMaterialsToCourseLevelAsync(
                    preparationResult.Materials,
                    fkCourse,
                    fkLevelCourse);
            }

            TempData["SuccessMessage"] = "Materials uploaded successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateVideoMaterial(
            string materialName,
            string filePath,
            int fkCourse,
            int fkLevelCourse)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToMaterialCreation(
                    VideoMaterialType,
                    "The submitted information is not valid.");
            }

            materialName = materialName?.Trim() ?? string.Empty;
            filePath = filePath?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(materialName))
            {
                return RedirectToMaterialCreation(
                    VideoMaterialType,
                    "Material Name is required.");
            }

            if (fkCourse <= 0)
            {
                return RedirectToMaterialCreation(
                    VideoMaterialType,
                    "Course is required.");
            }

            if (fkLevelCourse <= 0)
            {
                return RedirectToMaterialCreation(
                    VideoMaterialType,
                    "Level is required.");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return RedirectToMaterialCreation(
                    VideoMaterialType,
                    "Video Path is required.");
            }

            if (!await CourseLevelCombinationExistsAsync(fkCourse, fkLevelCourse))
            {
                return RedirectToMaterialCreation(
                    VideoMaterialType,
                    "The selected Course-Level combination is not valid.");
            }

            var existsName = await _context.CtCoursematerials
                .AsNoTracking()
                .AnyAsync(m =>
                    m.NameMaterial == materialName &&
                    m.Available == AvailableStatus);

            if (existsName)
            {
                return RedirectToMaterialCreation(
                    VideoMaterialType,
                    "A material with the same name already exists.");
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync();

            try
            {
                var material = new CtCoursematerial
                {
                    NameMaterial = materialName,
                    MaterialType = VideoMaterialType,
                    UrlPath = filePath,
                    CreateUser = User.Identity?.Name ?? "Unknown",
                    CreateDate = DateTime.Now,
                    LastUpdateUser = User.Identity?.Name ?? "Unknown",
                    LastUpdateDate = DateTime.Now,
                    Available = AvailableStatus
                };

                _context.CtCoursematerials.Add(material);
                await _context.SaveChangesAsync();

                _context.CtCourseLevelMaterials.Add(
                    new CtCourseLevelMaterial
                    {
                        FkCourse = fkCourse,
                        FkLevelCourse = fkLevelCourse,
                        FkCourseMaterial = material.PkCoursematerial,
                        CreateUser = User.Identity?.Name ?? "Unknown",
                        CreateDate = DateTime.Now,
                        LastUpdateUser = User.Identity?.Name ?? "Unknown",
                        LastUpdateDate = DateTime.Now,
                        Available = AvailableStatus
                    });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Video material and course assignment created successfully.";

                return RedirectToAction(nameof(IndexCourseMaterial));
            }
            catch
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "The video material could not be created.";

                return RedirectToAction(
                    nameof(CreateCourseMaterial),
                    new { type = VideoMaterialType });
            }
        }


        [Authorize(Policy = "ViewAccess")]
        [HttpGet]
        public async Task<IActionResult> EditPDFMaterial(int? id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!id.HasValue)
            {
                return NotFound();
            }

            var material = await _context.CtCoursematerials.FindAsync(id.Value);

            if (material == null)
            {
                return NotFound();
            }

            LoadCourseAndLevelViewBags();

            var link = await _context.CtCourseLevelMaterials
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.FkCourseMaterial == id.Value &&
                    x.Available == AvailableStatus);

            ViewBag.SelectedCourseId = link?.FkCourse ?? 0;
            ViewBag.SelectedLevelId = link?.FkLevelCourse ?? 0;

            return View(material);
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPDFMaterial(
            CtCoursematerial model,
            IFormFile? uploadedFile,
            int fkCourse,
            int fkLevelCourse)
        {
            var id = model.PkCoursematerial;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "The submitted information is not valid.";
                return RedirectToAction(nameof(EditPDFMaterial), new { id });
            }

            var existing = await _context.CtCoursematerials.FindAsync(id);

            if (existing == null)
            {
                return NotFound();
            }

            if (fkCourse > 0 && fkLevelCourse > 0)
            {
                var validCombination = await CourseLevelCombinationExistsAsync(
                    fkCourse,
                    fkLevelCourse);

                if (!validCombination)
                {
                    TempData["ErrorMessage"] =
                        "This Course-Level combination doesn't exist in Courseassignments.";

                    return RedirectToAction(nameof(EditPDFMaterial), new { id });
                }

                if (await CourseAlreadyHasPdfAsync(fkCourse, id))
                {
                    TempData["ErrorMessage"] =
                        "This course already has a PDF assigned. Only one PDF is allowed per course.";

                    return RedirectToAction(nameof(EditPDFMaterial), new { id });
                }
            }

            if (uploadedFile is { Length: > 0 })
            {
                var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? string.Empty;

                if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Only PDF files are allowed (.pdf).";
                    return RedirectToAction(nameof(EditPDFMaterial), new { id });
                }

                using var memoryStream = new MemoryStream();
                await uploadedFile.CopyToAsync(memoryStream);

                existing.File = memoryStream.ToArray();
                existing.NameMaterial = fileName;
            }

            existing.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            existing.LastUpdateDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }


        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> ViewVideoMaterial(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var material = await _context.CtCoursematerials
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.PkCoursematerial == id &&
                    m.MaterialType == VideoMaterialType);

            if (material == null)
            {
                return NotFound();
            }

            var assignedCourses = await GetAssignedCourseNamesAsync(id);

            if (assignedCourses.Count == 0)
            {
                TempData["ErrorMessage"] =
                    "This video material does not have a valid course assignment.";

                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            ViewBag.AssignedCourses = assignedCourses;

            return View(material);
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditVIDEOMaterial(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var material = await _context.CtCoursematerials
                .AsNoTracking()
                .FirstOrDefaultAsync(m =>
                    m.PkCoursematerial == id &&
                    m.MaterialType == VideoMaterialType);

            if (material == null)
            {
                return NotFound();
            }

            var assignedCourses = await GetAssignedCourseNamesAsync(id);

            if (assignedCourses.Count == 0)
            {
                TempData["ErrorMessage"] =
                    "This video material does not have a valid course assignment.";

                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            ViewBag.AssignedCourses = assignedCourses;

            return View("EditCourseMaterial", material);
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditVIDEOMaterial(
            CtCoursematerial model,
            string filePath)
        {
            var id = model.PkCoursematerial;

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "The submitted information is not valid.";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            var existing = await _context.CtCoursematerials.FindAsync(id);

            if (existing == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                TempData["ErrorMessage"] = "Please enter a Video file path or URL.";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            existing.UrlPath = filePath.Trim();
            existing.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            existing.LastUpdateDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Video Material updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourseMaterial(
            int id,
            CtCoursematerial model,
            IFormFile? uploadedFile)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "The submitted information is not valid.";
                return View(model);
            }

            if (id != model.PkCoursematerial)
            {
                return NotFound();
            }

            var existing = await _context.CtCoursematerials.FindAsync(id);

            if (existing == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(model.NameMaterial))
            {
                TempData["ErrorMessage"] = "Material Name is required.";
                return View(model);
            }

            existing.NameMaterial = model.NameMaterial;
            existing.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            existing.LastUpdateDate = DateTime.Now;

            if (uploadedFile is { Length: > 0 })
            {
                var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? string.Empty;

                if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    TempData["ErrorMessage"] = "Only PDF files are allowed (.pdf).";
                    return View(model);
                }

                using var memoryStream = new MemoryStream();
                await uploadedFile.CopyToAsync(memoryStream);
                existing.File = memoryStream.ToArray();
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourseMaterial(int id)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "The submitted information is not valid.";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            var material = await _context.CtCoursematerials.FindAsync(id);

            if (material == null)
            {
                TempData["ErrorMessage"] = "Material not found.";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            material.Available = material.Available == AvailableStatus ? 0 : AvailableStatus;
            material.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            material.LastUpdateDate = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material status updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseMaterial(int id)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "The submitted information is not valid.";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            var links = await _context.CtCourseLevelMaterials
                .Where(x => x.FkCourseMaterial == id)
                .ToListAsync();

            var material = await _context.CtCoursematerials.FindAsync(id);

            if (material == null)
            {
                TempData["ErrorMessage"] = "Material not found.";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (links.Count > 0)
                {
                    _context.CtCourseLevelMaterials.RemoveRange(links);
                }

                _context.CtCoursematerials.Remove(material);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Material and its assignments were deleted successfully.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error deleting material: {ex.Message}";
            }

            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> DownloadCourseMaterial(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var material = await _context.CtCoursematerials
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PkCoursematerial == id);

            if (material?.File is not { Length: > 0 } fileContents)
            {
                return NotFound();
            }

            var materialName = string.IsNullOrWhiteSpace(material.NameMaterial)
                ? $"material-{material.PkCoursematerial}"
                : material.NameMaterial;

            var downloadName = materialName.EndsWith(
                ".pdf",
                StringComparison.OrdinalIgnoreCase)
                ? materialName
                : $"{materialName}.pdf";

            return File(fileContents, "application/pdf", downloadName);
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> ViewPdfCourseMaterial(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var material = await _context.CtCoursematerials
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PkCoursematerial == id);

            if (material?.File is not { Length: > 0 } fileContents)
            {
                return NotFound();
            }

            return File(fileContents, "application/pdf");
        }
    }
}
