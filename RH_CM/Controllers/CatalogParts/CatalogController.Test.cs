using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.ViewModels;
using RH_CM.Messages.Catalog;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        /// <summary>
        /// Displays the list of tests.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> IndexTest()
        {
            var tests = await _context.CtTests
                .AsNoTracking()
                .Join(_context.CtCourses,
                      test => test.FkCourse,
                      course => course.PkCourse,
                      (test, course) => new { test, course })
                .Join(_context.CtLevelcourses,
                      temp => temp.test.FkLevelcourse,
                      level => level.PkLevelcourse,
                      (temp, level) => new
                      {
                          Test = temp.test,
                          CourseName = temp.course.CourseName,
                          LevelDescription = level.DescripctionLevel
                      })
                .ToListAsync();

            return View("Test/IndexTest", tests);
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTest(int id)
        {
            var result = await _testCatalogService.DeleteTestAsync(id);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(IndexTest));
        }

        /// <summary>
        /// Displays the form to edit an existing test.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditTest(int? id)
        {
            var result = await _testCatalogService.GetEditTestViewModelAsync(id);
            if (result.Test == null)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(IndexTest));
            }

            ViewBag.Courses = result.Courses;
            ViewBag.AvailableLevels = result.Levels;
            return View("Test/EditTest", result.Test);
        }

        /// <summary>
        /// Toggles a test's availability.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTest(int id)
        {
            var result = await _testCatalogService.ToggleTestAsync(id);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(IndexTest));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditTest(int id, RH_CM.Models.CtTest model)
        {
            var outcome = await _testCatalogService.EditTestAsync(id, model);

            if (outcome.RedirectToIndex)
            {
                TempData[outcome.IsSuccess ? "SuccessMessage" : "ErrorMessage"] = outcome.Message;
                return RedirectToAction(nameof(IndexTest));
            }

            if (outcome.RedirectToEditForm)
            {
                TempData["ErrorMessage"] = outcome.Message;
                return RedirectToAction(nameof(EditTest), new { id });
            }

            TempData["ErrorMessage"] = outcome.Message;
            ViewBag.Courses = outcome.Courses;
            ViewBag.AvailableLevels = outcome.Levels;
            return View("Test/EditTest", model);
        }

        /// <summary>
        /// Displays the form to create a test's questions.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> CreateQuestions()
        {
            var vm = new TestCreateViewModel();
            await _testCatalogService.PopulateTestFormOptionsAsync(vm);

            // CHANGED: this used to pre-load a "dummy" question with 2 empty options, but the view
            // never used it (the questions-container was built 100% by JS). Now that the view DOES
            // render Model.Questions (to restore data after a validation error), we leave the list
            // empty on initial load so we don't show a blank card.
            vm.Questions = new List<QuestionCreateViewModel>();

            return View("Test/CreateQuestions", vm);
        }

        /// <summary>
        /// Creates a test's questions.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestions(TestCreateViewModel model)
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var outcome = await _testCatalogService.CreateQuestionsAsync(model, userName);

            if (!outcome.Success)
            {
                TempData["ErrorMessage"] = outcome.Message;
                return View("Test/CreateQuestions", outcome.Model ?? model);
            }

            TempData["SuccessMessage"] = outcome.Message;
            return RedirectToAction(nameof(IndexTest), "Catalog");
        }

        /// <summary>
        /// Displays the form to edit a test's questions.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditQuestions(int id)
        {
            var vm = await _testCatalogService.GetEditQuestionsViewModelAsync(id);
            if (vm == null)
            {
                TempData["ErrorMessage"] = TestCatalogMessages.TestNotFound;
                return RedirectToAction(nameof(IndexTest));
            }

            return View("Test/EditQuestions", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditQuestions(int id, TestCreateViewModel model)
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var outcome = await _testCatalogService.EditQuestionsAsync(id, model, userName);

            if (outcome.RedirectToIndex)
            {
                TempData["ErrorMessage"] = outcome.Message;
                return RedirectToAction(nameof(IndexTest));
            }

            if (!outcome.Success)
            {
                // CHANGED: redirecting used to lose what the user had already typed; now we
                // re-render the same view with the model that came from the POST (see the
                // original comments that motivated this change in the service).
                TempData["ErrorMessage"] = outcome.Message;
                return View("Test/EditQuestions", outcome.Model ?? model);
            }

            TempData["SuccessMessage"] = outcome.Message;
            return RedirectToAction(nameof(IndexTest));
        }
    }
}
