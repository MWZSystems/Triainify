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
                .Where(m => m.Available == 1)
                .Join(
                    _context.CtCourses.Where(c => c.Available == 1),
                    m => m.FkCourse,
                    c => c.PkCourse,
                    (m, c) => new { m, c }
                )
                .Select(temp => new
                {
                    temp.m.PkCoursematerial,
                    MaterialName = temp.m.NameMaterial,  // NOMBRE CONSISTENTE
                    temp.m.Available,
                    CourseName = temp.c.CourseName,
                    temp.c.ManagementSystem,
                    //temp.c.Idcourse,
                    //temp.c.Revision,
                    //temp.c.CourseValidityDays
                })
                .ToList();

            return View(materials);
        }

        // GET: CtCoursematerial/Create
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> CreateCourseMaterial()
        {
            // Cargar cursos activos
            ViewBag.Courses = await _context.CtCourses
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            return View();
        }
        // POST: CtCoursematerial/Create
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourseMaterial(CtCoursematerial model, IFormFile uploadedFile)
        {
            // Validar archivo obligatorio
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                TempData["ErrorMessage"] = "You must upload a PDF file.";

                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(model);
            }

            // Validar Level no negativo
            if (model.Level.HasValue && model.Level < 0)
            {
                TempData["ErrorMessage"] = "Level cannot be negative.";

                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(model);
            }

            // Obtener el FkCourse desde el radio button
            var selectedFkCourse = Request.Form["FkCourse"].FirstOrDefault();
            if (string.IsNullOrEmpty(selectedFkCourse))
            {
                TempData["ErrorMessage"] = "You must select a course.";

                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(model);
            }

            if (!int.TryParse(selectedFkCourse, out var fkCourseValue))
            {
                TempData["ErrorMessage"] = "Invalid course selection.";

                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(model);
            }

            using var ms = new MemoryStream();
            await uploadedFile.CopyToAsync(ms);
            model.File = ms.ToArray();

            model.FkCourse = fkCourseValue;
            model.Createuser = User.Identity?.Name ?? "Unknown";
            model.Createdate = DateTime.Now;
            model.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            model.Lastupdatedate = DateTime.Now;
            model.Available = 1;

            _context.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material created successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }


        // GET: CtCoursematerial/Edit/5
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditCourseMaterial(int? id)
        {
            if (id == null)
                return NotFound();

            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
                return NotFound();

            // Cargar cursos activos para la tabla
            ViewBag.Courses = await _context.CtCourses
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            return View(material);
        }

        // POST: CtCoursematerial/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourseMaterial(int id, CtCoursematerial model, IFormFile? uploadedFile)
        {
            if (id != model.PkCoursematerial)
                return NotFound();

            var existing = await _context.CtCoursematerials.FindAsync(id);
            if (existing == null)
                return NotFound();

            // Obtener el FkCourse desde el radio button
            var selectedFkCourse = Request.Form["FkCourse"].FirstOrDefault();

            if (string.IsNullOrEmpty(selectedFkCourse))
            {
                TempData["ErrorMessage"] = "You must select a course.";

                // Recargar cursos y devolver vista
                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(model);
            }

            if (!int.TryParse(selectedFkCourse, out var fkCourseValue))
            {
                TempData["ErrorMessage"] = "Invalid course selection.";

                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                return View(model);
            }

            // Actualizar campos
            existing.NameMaterial = model.NameMaterial;
            existing.FkCourse = fkCourseValue;
            existing.Level = model.Level;
            existing.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            existing.Lastupdatedate = DateTime.Now;

            // Si subió nuevo archivo
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
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
            material.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            material.Lastupdatedate = DateTime.Now;

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
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material != null)
            {
                _context.CtCoursematerials.Remove(material);
                await _context.SaveChangesAsync();
            }

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
