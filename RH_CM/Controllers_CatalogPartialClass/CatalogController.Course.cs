using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        // GET: CtCourse
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> IndexCourse()
        {
            var courses = await _context.CtCourses.ToListAsync();
            return View(courses);
        }

        // GET: CtCourse/Create
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        public IActionResult CreateCourse()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CtCourse ctCourse)
        {
            // Validar campos requeridos manualmente
            if (string.IsNullOrWhiteSpace(ctCourse.ManagementSystem))
            {
                TempData["ErrorMessage"] = "The Management System field is required.";
                return View(ctCourse);
            }

            if (string.IsNullOrWhiteSpace(ctCourse.Idcourse))
            {
                TempData["ErrorMessage"] = "The Course ID field is required.";
                return View(ctCourse);
            }

            if (string.IsNullOrWhiteSpace(ctCourse.CourseName))
            {
                TempData["ErrorMessage"] = "The Course Name field is required.";
                return View(ctCourse);
            }

            if (ctCourse.Revision <= 0)
            {
                TempData["ErrorMessage"] = "The Revision must be greater than 0.";
                return View(ctCourse);
            }

            // Verificar si el Course ID ya existe
            bool idExists = await _context.CtCourses
                .AnyAsync(c => c.Idcourse == ctCourse.Idcourse);

            // Verificar si el Course Name ya existe
            bool nameExists = await _context.CtCourses
                .AnyAsync(c => c.CourseName == ctCourse.CourseName);

            if (idExists && nameExists)
            {
                TempData["ErrorMessage"] = "The Course ID and Course Name already exist. Please choose different values.";
                return View(ctCourse);
            }
            else if (idExists)
            {
                TempData["ErrorMessage"] = "The Course ID already exists. Please choose a different ID.";
                return View(ctCourse);
            }
            else if (nameExists)
            {
                TempData["ErrorMessage"] = "The Course Name already exists. Please choose a different name.";
                return View(ctCourse);
            }

            // Asignar valores automáticos
            ctCourse.CreateUser = User.Identity.Name ?? "Unknown";
            ctCourse.CreateDate = DateTime.Now;
            ctCourse.LastUpdateUser = User.Identity.Name ?? "Unknown";
            ctCourse.LastUpdateDate = DateTime.Now;
            ctCourse.Available = 1;

            try
            {
                _context.Add(ctCourse);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Course created successfully.";
                return RedirectToAction(nameof(IndexCourse));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the course: {ex.Message}";
                return View(ctCourse);
            }
        }

        // GET: CtCourse/Edit/5
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> EditCourse(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ctCourse = await _context.CtCourses.FindAsync(id);
            if (ctCourse == null)
            {
                return NotFound();
            }
            return View(ctCourse);
        }

        // POST: CtCourse/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, CtCourse ctCourse)
        {
            if (id != ctCourse.PkCourse)
            {
                TempData["ErrorMessage"] = "The specified course was not found.";
                return RedirectToAction(nameof(IndexCourse));
            }


            // Validar campos requeridos manualmente
            if (string.IsNullOrWhiteSpace(ctCourse.ManagementSystem))
            {
                TempData["ErrorMessage"] = "The Management System field is required.";
                return View(ctCourse);
            }

            if (string.IsNullOrWhiteSpace(ctCourse.Idcourse))
            {
                TempData["ErrorMessage"] = "The Course ID field is required.";
                return View(ctCourse);
            }

            if (string.IsNullOrWhiteSpace(ctCourse.CourseName))
            {
                TempData["ErrorMessage"] = "The Course Name field is required.";
                return View(ctCourse);
            }

            if (ctCourse.CourseValidityDays <= 0)
            {
                TempData["ErrorMessage"] = "The Course Validity Days must be greater than 0.";
                return View(ctCourse);
            }

            if (ctCourse.Revision <= 0)
            {
                TempData["ErrorMessage"] = "The Revision must be greater than 0.";
                return View(ctCourse);
            }

            // Verificar duplicados en la base de datos (excluyendo el curso actual)
            bool idExists = await _context.CtCourses
                .AnyAsync(c => c.Idcourse == ctCourse.Idcourse && c.PkCourse != ctCourse.PkCourse);

            bool nameExists = await _context.CtCourses
                .AnyAsync(c => c.CourseName == ctCourse.CourseName && c.PkCourse != ctCourse.PkCourse);

            if (idExists && nameExists)
            {
                TempData["ErrorMessage"] = "The Course ID and Course Name already exist. Please choose different values.";
                return View(ctCourse);
            }
            else if (idExists)
            {
                TempData["ErrorMessage"] = "The Course ID already exists. Please choose a different ID.";
                return View(ctCourse);
            }
            else if (nameExists)
            {
                TempData["ErrorMessage"] = "The Course Name already exists. Please choose a different name.";
                return View(ctCourse);
            }

            try
            {
                var existingCourse = await _context.CtCourses.FindAsync(id);
                if (existingCourse == null)
                {
                    TempData["ErrorMessage"] = "The course no longer exists in the database.";
                    return NotFound();
                }

                // Actualizar campos permitidos
                existingCourse.ManagementSystem = ctCourse.ManagementSystem;
                existingCourse.Idcourse = ctCourse.Idcourse;
                existingCourse.CourseName = ctCourse.CourseName;
                existingCourse.CourseValidityDays = ctCourse.CourseValidityDays;
                existingCourse.Revision = ctCourse.Revision;
                existingCourse.LastUpdateUser = User.Identity.Name ?? "Unknown";
                existingCourse.LastUpdateDate = DateTime.Now;

                _context.Update(existingCourse);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Course updated successfully.";
                return RedirectToAction(nameof(EditCourse));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "There was a concurrency error while updating the course.";
                return View(ctCourse);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the course: {ex.Message}";
                return View(ctCourse);
            }
        }


        // POST: /CtCourse/ToggleCourse/5
        [HttpPost]
        [Route("ToggleCourse")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourse(int id)
        {
            var ctCourse = await _context.CtCourses.FindAsync(id);
            if (ctCourse != null)
            {
                ctCourse.Available = ctCourse.Available == 1 ? 0 : 1;
                _context.Update(ctCourse);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexCourse));
        }

        // POST: /CtCourse/DeleteCourse/5
        [HttpPost]
        [Route("DeleteCourse")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            // 1) Validar si el curso está ligado a asignaciones
            bool hasAssignments = await _context.CtCourseassignments
                .AsNoTracking()
                .AnyAsync(a => a.FkCourse == id);

            if (hasAssignments)
            {
                TempData["ErrorMessage"] = "No se puede eliminar el curso porque está ligado a asignaciones. " +
                                           "Primero tiene que desligar el Course de las asignaciones.";
                return RedirectToAction(nameof(IndexCourse));
            }

            // 2) Buscar el curso
            var ctCourse = await _context.CtCourses.FindAsync(id);
            if (ctCourse == null)
            {
                TempData["ErrorMessage"] = "El curso no existe o ya fue eliminado.";
                return RedirectToAction(nameof(IndexCourse));
            }

            try
            {
                _context.CtCourses.Remove(ctCourse);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Curso eliminado correctamente.";
            }
            catch (DbUpdateException)
            {
                // En caso de que la BD tenga una FK restrictiva y alguien agregue asignaciones
                // después de la validación previa, caeríamos aquí.
                TempData["ErrorMessage"] = "No se puede eliminar el curso porque está ligado a asignaciones. " +
                                           "Primero tiene que desligar el Course de las asignaciones.";
            }

            return RedirectToAction(nameof(IndexCourse));
        }

        private bool CtCourseExists(int id)
        {
            return _context.CtCourses.Any(e => e.PkCourse == id);
        }
    }
}
