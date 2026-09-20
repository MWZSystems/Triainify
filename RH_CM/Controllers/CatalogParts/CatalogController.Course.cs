using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;
using RH_CM.Messages.Catalog;
using RH_CM.Service.Export;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        /// <summary>
        /// Displays the list of courses.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> IndexCourse()
        {
            var courses = await _context.CtCourses
                .OrderByDescending(c => c.Available)
                .ToListAsync();

            return View("Course/IndexCourse", courses);
        }

        /// <summary>
        /// Raw export of every column in CtCourses, with no joins or translations,
        /// so staff can cross-check the data behind the Courses catalog.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> ExportCourseFullData()
        {
            var data = await _context.CtCourses.AsNoTracking().ToListAsync();
            var bytes = RawExcelExportHelper.ExportFullData(data, "Courses");
            return File(bytes, RawExcelExportHelper.ExcelContentType, RawExcelExportHelper.BuildFileName("Courses"));
        }

        /// <summary>
        /// Displays the form to create a new course.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public IActionResult CreateCourse()
        {
            return View("Course/CreateCourse");
        }

        /// <summary>
        /// Creates a new course.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourse(CtCourse ctCourse)
        {
            // Validate required fields manually
            if (string.IsNullOrWhiteSpace(ctCourse.ManagementSystem))
            {
                TempData["ErrorMessage"] = CourseMessages.ManagementSystemFieldIsRequired;
                return View("Course/CreateCourse", ctCourse);
            }

            if (string.IsNullOrWhiteSpace(ctCourse.Idcourse))
            {
                TempData["ErrorMessage"] = CourseMessages.CourseIdFieldIsRequired;
                return View("Course/CreateCourse", ctCourse);
            }

            if (string.IsNullOrWhiteSpace(ctCourse.CourseName))
            {
                TempData["ErrorMessage"] = CourseMessages.CourseNameFieldIsRequired;
                return View("Course/CreateCourse", ctCourse);
            }

            if (ctCourse.Revision <= 0)
            {
                TempData["ErrorMessage"] = CourseMessages.RevisionMustBeGreaterThan0;
                return View("Course/CreateCourse", ctCourse);
            }

            // Check if Course ID already exists
            bool idExists = await _context.CtCourses
                .AnyAsync(c => c.Idcourse == ctCourse.Idcourse);

            // Check if Course Name already exists
            bool nameExists = await _context.CtCourses
                .AnyAsync(c => c.CourseName == ctCourse.CourseName);

            if (idExists && nameExists)
            {
                TempData["ErrorMessage"] = CourseMessages.CourseIdAndCourseNameAlreadyExist;
                return View("Course/CreateCourse", ctCourse);
            }
            else if (idExists)
            {
                TempData["ErrorMessage"] = CourseMessages.CourseIdAlreadyExistsPleaseChooseA;
                return View("Course/CreateCourse", ctCourse);
            }
            else if (nameExists)
            {
                TempData["ErrorMessage"] = CourseMessages.CourseNameAlreadyExistsPleaseChooseA;
                return View("Course/CreateCourse", ctCourse);
            }

            // Assign automatic values
            ctCourse.CreateUser = User.Identity?.Name ?? "Unknown";
            ctCourse.CreateDate = DateTime.Now;
            ctCourse.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            ctCourse.LastUpdateDate = DateTime.Now;
            ctCourse.Available = 1;

            try
            {
                _context.Add(ctCourse);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = CourseMessages.CourseCreatedSuccessfully;
                return RedirectToAction(nameof(IndexCourse));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(CourseMessages.ErrorOccurredWhileCreatingTheCourseFormat, ex.Message);
                return View("Course/CreateCourse", ctCourse);
            }
        }

        /// <summary>
        /// Displays the form to edit an existing course.
        /// </summary>
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

            return View("Course/EditCourse", ctCourse);
        }

        /// <summary>
        /// Updates an existing course.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourse(int id, CtCourse ctCourse)
        {
            if (id != ctCourse.PkCourse)
            {
                TempData["ErrorMessage"] = CourseMessages.SpecifiedCourseWasNotFound;
                return RedirectToAction(nameof(IndexCourse));
            }

            // Validate required fields manually
            if (string.IsNullOrWhiteSpace(ctCourse.ManagementSystem))
            {
                TempData["ErrorMessage"] = CourseMessages.ManagementSystemFieldIsRequired;
                return View("Course/EditCourse", ctCourse);
            }

            if (string.IsNullOrWhiteSpace(ctCourse.Idcourse))
            {
                TempData["ErrorMessage"] = CourseMessages.CourseIdFieldIsRequired;
                return View("Course/EditCourse", ctCourse);
            }

            if (string.IsNullOrWhiteSpace(ctCourse.CourseName))
            {
                TempData["ErrorMessage"] = CourseMessages.CourseNameFieldIsRequired;
                return View("Course/EditCourse", ctCourse);
            }

            if (ctCourse.CourseValidityDays <= 0)
            {
                TempData["ErrorMessage"] = CourseMessages.CourseValidityDaysMustBeGreaterThan;
                return View("Course/EditCourse", ctCourse);
            }

            if (ctCourse.Revision <= 0)
            {
                TempData["ErrorMessage"] = CourseMessages.RevisionMustBeGreaterThan0;
                return View("Course/EditCourse", ctCourse);
            }

            // Check for duplicates excluding current course
            bool idExists = await _context.CtCourses
                .AnyAsync(c => c.Idcourse == ctCourse.Idcourse && c.PkCourse != ctCourse.PkCourse);

            bool nameExists = await _context.CtCourses
                .AnyAsync(c => c.CourseName == ctCourse.CourseName && c.PkCourse != ctCourse.PkCourse);

            if (idExists && nameExists)
            {
                TempData["ErrorMessage"] = CourseMessages.CourseIdAndCourseNameAlreadyExist;
                return View("Course/EditCourse", ctCourse);
            }
            else if (idExists)
            {
                TempData["ErrorMessage"] = CourseMessages.CourseIdAlreadyExistsPleaseChooseA;
                return View("Course/EditCourse", ctCourse);
            }
            else if (nameExists)
            {
                TempData["ErrorMessage"] = CourseMessages.CourseNameAlreadyExistsPleaseChooseA;
                return View("Course/EditCourse", ctCourse);
            }

            try
            {
                var existingCourse = await _context.CtCourses.FindAsync(id);
                if (existingCourse == null)
                {
                    TempData["ErrorMessage"] = CourseMessages.CourseNoLongerExistsInTheDatabase;
                    return NotFound();
                }

                // Update allowed fields
                existingCourse.ManagementSystem = ctCourse.ManagementSystem;
                existingCourse.Idcourse = ctCourse.Idcourse;
                existingCourse.CourseName = ctCourse.CourseName;
                existingCourse.CourseValidityDays = ctCourse.CourseValidityDays;
                existingCourse.Revision = ctCourse.Revision;
                existingCourse.LastUpdateUser = User.Identity?.Name ?? "Unknown";
                existingCourse.LastUpdateDate = DateTime.Now;

                _context.Update(existingCourse);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = CourseMessages.CourseUpdatedSuccessfully;
                return RedirectToAction(nameof(EditCourse));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = CourseMessages.ConcurrencyErrorOccurredWhileUpdatingTheCourse;
                return View("Course/EditCourse", ctCourse);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(CourseMessages.ErrorOccurredWhileUpdatingTheCourseFormat, ex.Message);
                return View("Course/EditCourse", ctCourse);
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

        /// <summary>
        /// Toggles a course's availability, optionally applying the change to its assignments.
        /// </summary>
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
                    TempData["ErrorMessage"] = CourseMessages.SelectedCourseWasNotFound;
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
                    TempData["SuccessMessage"] = CourseMessages.CourseAndAllRelatedCourseAssignmentsWere;
                }
                else if (available == 1 && !applyToAssignments)
                {
                    TempData["SuccessMessage"] = CourseMessages.CourseWasEnabledSuccessfully;
                }
                else if (available == 0 && applyToAssignments)
                {
                    TempData["SuccessMessage"] = CourseMessages.CourseAndAllRelatedCourseAssignmentsWereDisabled;
                }
                else
                {
                    TempData["SuccessMessage"] = CourseMessages.CourseWasDisabledSuccessfully;
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(CourseMessages.ErrorOccurredWhileUpdatingTheCourseAvailabilityFormat, ex.Message);
            }

            return RedirectToAction(nameof(IndexCourse));
        }

        /// <summary>
        /// Deletes a course.
        /// </summary>
        [HttpPost]
        [Route("DeleteCourse")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            // Validate if the course is linked to assignments
            var dependency = await _catalogIntegrityService.CourseDependencyAsync(id);
            if (dependency != null)
            {
                TempData["ErrorMessage"] = dependency;
                return RedirectToAction(nameof(IndexCourse));
            }

            // Find the course
            var ctCourse = await _context.CtCourses.FindAsync(id);
            if (ctCourse == null)
            {
                TempData["ErrorMessage"] = CourseMessages.CourseDoesNotExistOrHasAlready;
                return RedirectToAction(nameof(IndexCourse));
            }

            try
            {
                _context.CtCourses.Remove(ctCourse);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = CourseMessages.CourseDeletedSuccessfully;
            }
            catch (DbUpdateException)
            {
                // In case of FK restriction at database level
                TempData["ErrorMessage"] = CourseMessages.CourseCannotBeDeletedBecauseItIs;
            }

            return RedirectToAction(nameof(IndexCourse));
        }

        private bool CtCourseExists(int id)
        {
            return _context.CtCourses.Any(e => e.PkCourse == id);
        }
    }
}
