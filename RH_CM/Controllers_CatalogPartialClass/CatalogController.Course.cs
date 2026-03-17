using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        // GET: CtCourse
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> IndexCourse()
        {
            var courses = await _context.CtCourses
                .OrderByDescending(c => c.Available)
                .ToListAsync();

            return View(courses);
        }

        // GET: CtCourse/Create
        [Authorize(Policy = "ViewAccess")]
        public IActionResult CreateCourse()
        {
            return View();
        }

        // POST: CtCourse/Create
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CtCourse ctCourse)
        {
            // Validate required fields manually
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

            // Check if Course ID already exists
            bool idExists = await _context.CtCourses
                .AnyAsync(c => c.Idcourse == ctCourse.Idcourse);

            // Check if Course Name already exists
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

            // Assign automatic values
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
        [Authorize(Policy = "ViewAccess")]
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
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, CtCourse ctCourse)
        {
            if (id != ctCourse.PkCourse)
            {
                TempData["ErrorMessage"] = "The specified course was not found.";
                return RedirectToAction(nameof(IndexCourse));
            }

            // Validate required fields manually
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

            // Check for duplicates excluding current course
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

                // Update allowed fields
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
                TempData["ErrorMessage"] = "A concurrency error occurred while updating the course.";
                return View(ctCourse);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the course: {ex.Message}";
                return View(ctCourse);
            }
        }

        //// POST: Toggle Course Availability
        //[HttpPost]
        //[Route("ToggleCourse")]
        //[Authorize(Policy = "ViewAccess")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> ToggleCourse(int id)
        //{
        //    var ctCourse = await _context.CtCourses.FindAsync(id);
        //    if (ctCourse != null)
        //    {
        //        ctCourse.Available = ctCourse.Available == 1 ? 0 : 1;
        //        _context.Update(ctCourse);
        //        await _context.SaveChangesAsync();
        //    }

        //    return RedirectToAction(nameof(IndexCourse));
        //}

        // POST: Update Course Availability
        [HttpPost]
        [Route("ToggleCourse")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourse(int id, int available, bool applyToAssignments)
        {
            try
            {
                var courseExists = await _context.CtCourses
                    .AsNoTracking()
                    .AnyAsync(c => c.PkCourse == id);

                if (!courseExists)
                {
                    TempData["ErrorMessage"] = "The selected course was not found.";
                    return RedirectToAction(nameof(IndexCourse));
                }

                var updateUser = User.Identity?.Name ?? "Unknown";

                var pkCourseParam = new SqlParameter("@PkCourse", id);
                var availableParam = new SqlParameter("@Available", available);
                var applyToAssignmentsParam = new SqlParameter("@ApplyToAssignments", applyToAssignments);
                var updateUserParam = new SqlParameter("@UpdateUser", updateUser);

                await _context.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.sp_UpdateCourseAvailability @PkCourse, @Available, @ApplyToAssignments, @UpdateUser",
                    pkCourseParam,
                    availableParam,
                    applyToAssignmentsParam,
                    updateUserParam
                );

                if (available == 1 && applyToAssignments)
                {
                    TempData["SuccessMessage"] = "The course and all related course assignments were enabled successfully.";
                }
                else if (available == 1 && !applyToAssignments)
                {
                    TempData["SuccessMessage"] = "The course was enabled successfully.";
                }
                else if (available == 0 && applyToAssignments)
                {
                    TempData["SuccessMessage"] = "The course and all related course assignments were disabled successfully.";
                }
                else
                {
                    TempData["SuccessMessage"] = "The course was disabled successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the course availability: {ex.Message}";
            }

            return RedirectToAction(nameof(IndexCourse));
        }

        // POST: Delete Course
        [HttpPost]
        [Route("DeleteCourse")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            // Validate if the course is linked to assignments
            bool hasAssignments = await _context.CtCourseassignments
                .AsNoTracking()
                .AnyAsync(a => a.FkCourse == id);

            if (hasAssignments)
            {
                TempData["ErrorMessage"] = "The course cannot be deleted because it is linked to assignments. Please unlink it first.";
                return RedirectToAction(nameof(IndexCourse));
            }

            // Find the course
            var ctCourse = await _context.CtCourses.FindAsync(id);
            if (ctCourse == null)
            {
                TempData["ErrorMessage"] = "The course does not exist or has already been deleted.";
                return RedirectToAction(nameof(IndexCourse));
            }

            try
            {
                _context.CtCourses.Remove(ctCourse);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Course deleted successfully.";
            }
            catch (DbUpdateException)
            {
                // In case of FK restriction at database level
                TempData["ErrorMessage"] = "The course cannot be deleted because it is linked to assignments. Please unlink it first.";
            }

            return RedirectToAction(nameof(IndexCourse));
        }

        private bool CtCourseExists(int id)
        {
            return _context.CtCourses.Any(e => e.PkCourse == id);
        }
    }
}