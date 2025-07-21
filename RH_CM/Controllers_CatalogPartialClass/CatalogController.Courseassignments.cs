using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Models;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public IActionResult IndexCourseassignments()
        {
            var courseAssignments = _context.CtCourseassignments
                .Join(_context.CtPositions,
                      ca => ca.FkPosition,
                      p => p.PkPosition,
                      (ca, p) => new { ca, p })
                .Join(_context.CtCourses,
                      temp => temp.ca.FkCourse,
                      c => c.PkCourse,
                      (temp, c) => new { temp.ca, temp.p, c })
                .Join(_context.CtLevelcourses,
                      temp => temp.ca.FkRequiredCourseLevels,
                      lc => lc.PkLevelcourse,
                      (temp, lc) => new
                      {
                          temp.ca.PkCourseAssignment,
                          PositionName = temp.p.NamePosition,
                          CourseName = temp.c.CourseName,
                          FkRequiredCourseLevels = temp.ca.FkRequiredCourseLevels,
                          Requiered = temp.ca.Requiered,
                          Available = temp.ca.Available,
                          RequiredCourseLevelDescription = lc.DescripctionLevel
                      })
                .ToList();

            return View(courseAssignments);
        }

        // GET: CourseAssignments/Create
        [Authorize(Roles = "Administrador")]
        public IActionResult CreateCourseAssignment()
        {
            ViewBag.Positions = _context.CtPositions.Where(p => p.Available == 1).ToList();
            ViewBag.Courses = _context.CtCourses.Where(c => c.Available == 1).ToList();
            ViewBag.LevelCourses = _context.CtLevelcourses.Where(c => c.Available == 1).ToList();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCourseAssignment(CtCourseassignment courseAssignment)
        {
            // Validar campos vacíos
            if (courseAssignment.FkPosition <= 0)
            {
                TempData["ErrorMessage"] = "Position is required. Please select a valid position.";
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            if (courseAssignment.FkCourse <= 0)
            {
                TempData["ErrorMessage"] = "Course is required. Please select a valid course.";
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            if (courseAssignment.FkRequiredCourseLevels <= 0)
            {
                TempData["ErrorMessage"] = "Required Course Level is required. Please select a valid level.";
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            // Validar duplicados
            bool exists = _context.CtCourseassignments.Any(ca =>
                ca.FkPosition == courseAssignment.FkPosition &&
                ca.FkCourse == courseAssignment.FkCourse &&
                ca.FkRequiredCourseLevels == courseAssignment.FkRequiredCourseLevels);

            if (exists)
            {
                TempData["ErrorMessage"] = "The combination of Position, Course, and Required Course Level already exists.";
                return RedirectToAction(nameof(CreateCourseAssignment));
            }

            // Guardar si todo está correcto
            courseAssignment.CreateUser = User.Identity.Name ?? "Unknown";
            courseAssignment.CreateDate = DateTime.Now;
            courseAssignment.LastUpdateUser = User.Identity.Name ?? "Unknown";
            courseAssignment.LastUpdateDate = DateTime.Now;
            courseAssignment.Available = 1;
            _context.CtCourseassignments.Add(courseAssignment);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Course assignment created successfully.";
            return RedirectToAction(nameof(CreateCourseAssignment));
        }


        // GET: CourseAssignments/Edit/5
        [Authorize(Roles = "Administrador")]
        public IActionResult EditCourseAssignment(int id)
        {
            var courseAssignment = _context.CtCourseassignments.Find(id);
            if (courseAssignment == null) return NotFound();

            ViewBag.Positions = _context.CtPositions.Where(p => p.Available == 1).ToList();
            ViewBag.Courses = _context.CtCourses.Where(c => c.Available == 1).ToList();
            ViewBag.LevelCourses = _context.CtLevelcourses.Where(c => c.Available == 1).ToList();
            return View(courseAssignment);
        }

        // POST: CourseAssignments/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult EditCourseAssignment(int id, CtCourseassignment courseAssignment)
        {
            if (id != courseAssignment.PkCourseAssignment) return NotFound();

            // Validar campos vacíos
            if (courseAssignment.FkPosition <= 0)
            {
                TempData["ErrorMessage"] = "Position is required. Please select a valid position.";
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            if (courseAssignment.FkCourse <= 0)
            {
                TempData["ErrorMessage"] = "Course is required. Please select a valid course.";
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            if (courseAssignment.FkRequiredCourseLevels <= 0)
            {
                TempData["ErrorMessage"] = "Required Course Level is required. Please select a valid level.";
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            // Validar duplicados
            bool exists = _context.CtCourseassignments.Any(ca =>
                ca.FkPosition == courseAssignment.FkPosition &&
                ca.FkCourse == courseAssignment.FkCourse &&
                ca.FkRequiredCourseLevels == courseAssignment.FkRequiredCourseLevels &&
                ca.PkCourseAssignment != id); // Excluir el registro actual

            if (exists)
            {
                TempData["ErrorMessage"] = "The combination of Position, Course, and Required Course Level already exists.";
                return RedirectToAction(nameof(EditCourseAssignment), new { id });
            }

            // Actualizar si todo está correcto
            courseAssignment.CreateUser = User.Identity.Name ?? "Unknown";
            courseAssignment.CreateDate = DateTime.Now;
            courseAssignment.LastUpdateUser = User.Identity.Name ?? "Unknown";
            courseAssignment.LastUpdateDate = DateTime.Now;

            _context.CtCourseassignments.Update(courseAssignment);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Course assignment updated successfully.";
            return RedirectToAction(nameof(EditCourseAssignment));
        }


        // POST: CourseAssignments/Delete/5
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCourseAssignment(int id)
        {
            var courseAssignment = _context.CtCourseassignments.Find(id);
            if (courseAssignment != null)
            {
                _context.CtCourseassignments.Remove(courseAssignment);
                _context.SaveChanges();
                TempData["SuccessMessage"] = "Course assignment deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Course assignment not found.";
            }
            return RedirectToAction(nameof(IndexCourseassignments));
        }

        // POST: /CtCourseassignment/ToggleCourseAssignment/5
        [HttpPost]
        [Route("ToggleCourseAssignment")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourseAssignment(int id)
        {
            var courseAssignment = await _context.CtCourseassignments.FindAsync(id);
            if (courseAssignment != null)
            {
                // Alternar el valor de Available
                courseAssignment.Available = courseAssignment.Available == 1 ? 0 : 1;
                courseAssignment.LastUpdateUser = User.Identity.Name ?? "Unknown";
                courseAssignment.LastUpdateDate = DateTime.Now;

                _context.CtCourseassignments.Update(courseAssignment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "The course assignment availability status was successfully updated.";
            }
            else
            {
                TempData["ErrorMessage"] = "Course assignment not found.";
            }
            return RedirectToAction(nameof(IndexCourseassignments));
        }

    }
}
