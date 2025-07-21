using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;
using RH_CM.ViewModels;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {

        // GET: CtTest/IndexTest
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> IndexTest()
        {
            var tests = await _context.CtTests
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

            return View(tests);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTest(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Obtener todas las preguntas del test
                var questions = await _context.CtQuestions
                    .Where(q => q.FkTest == id)
                    .ToListAsync();

                foreach (var question in questions)
                {
                    // 2. Eliminar respuestas correctas
                    var correctAnswers = await _context.CtCorrectanswers
                        .Where(ca => ca.FkQuestions == question.PkQuestions)
                        .ToListAsync();
                    _context.CtCorrectanswers.RemoveRange(correctAnswers);

                    // 3. Eliminar opciones
                    var options = await _context.CtOptions
                        .Where(o => o.FkQuestions == question.PkQuestions)
                        .ToListAsync();
                    _context.CtOptions.RemoveRange(options);
                }

                // 4. Eliminar preguntas
                _context.CtQuestions.RemoveRange(questions);

                // 5. Eliminar el test
                var test = await _context.CtTests.FindAsync(id);
                if (test != null)
                {
                    _context.CtTests.Remove(test);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Test and all related data were deleted successfully.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error deleting test: {ex.Message}";
            }

            return RedirectToAction(nameof(IndexTest));
        }

        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditTest(int? id)
        {
            if (id == null)
            {
                TempData["ErrorMessage"] = "No test ID was provided.";
                return RedirectToAction(nameof(IndexTest));
            }

            var ctTest = await _context.CtTests.FindAsync(id);
            if (ctTest == null)
            {
                TempData["ErrorMessage"] = "The test could not be found.";
                return RedirectToAction(nameof(IndexTest));
            }

            ViewBag.Courses = await _context.CtCourses
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            ViewBag.AvailableLevels = await _context.CtLevelcourses
                .Where(l => l.Available == 1)
                .OrderBy(l => l.PkLevelcourse)
                .ToListAsync();

            return View(ctTest);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditTest(int id, CtTest model)
        {
            if (id != model.PkTest)
            {
                TempData["ErrorMessage"] = "Test not found.";
                return RedirectToAction(nameof(IndexTest));
            }

            // Validación manual
            if (string.IsNullOrWhiteSpace(model.TestName))
            {
                TempData["ErrorMessage"] = "The Test Name field is required.";
            }
            else if (model.FkCourse == null || model.FkCourse == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid course.";
            }
            else if (model.FkLevelcourse == null || model.FkLevelcourse == 0)
            {
                TempData["ErrorMessage"] = "Please select a valid course level.";
            }

            if (TempData["ErrorMessage"] != null)
            {
                // Recargar listas para la vista
                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                ViewBag.AvailableLevels = await _context.CtLevelcourses
                    .Where(l => l.Available == 1)
                    .OrderBy(l => l.PkLevelcourse)
                    .ToListAsync();

                return View(model);
            }

            try
            {
                var existingTest = await _context.CtTests.FindAsync(id);
                if (existingTest == null)
                {
                    TempData["ErrorMessage"] = "Test not found in the database.";
                    return RedirectToAction(nameof(IndexTest));
                }

                existingTest.TestName = model.TestName;
                existingTest.FkCourse = model.FkCourse;
                existingTest.FkLevelcourse = model.FkLevelcourse;

                _context.Update(existingTest);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(IndexTest));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "A concurrency error occurred while updating the test.";
                return RedirectToAction(nameof(EditTest), new { id });
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An unexpected error occurred while updating the test.";
                return RedirectToAction(nameof(EditTest), new { id });
            }
        }




        // GET: Catalog/CreateQuestions
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> CreateQuestions()
        {
            var vm = new TestCreateViewModel
            {
                AvailableCourses = await _context.CtCourses
            .Where(c => c.Available == 1)
            .OrderBy(c => c.PkCourse)
            .ToListAsync(),

                AvailableLevels = await _context.CtLevelcourses
            .Where(l => l.Available == 1)
            .OrderBy(l => l.PkLevelcourse)
            .ToListAsync(),

                Questions = new List<QuestionCreateViewModel>
            {
                new QuestionCreateViewModel
                {
                    Options = new List<OptionCreateViewModel>
                    {
                        new OptionCreateViewModel(),
                        new OptionCreateViewModel()
                    }
                }
            }
                    };

            return View(vm);
        }


        // POST: Catalog/CreateQuestions
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateQuestions(TestCreateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.TestName))
            {
                TempData["ErrorMessage"] = "The Test Name field is required.";
            }
            else if (model.FkRequiredCourseLevels < 0)
            {
                TempData["ErrorMessage"] = "The Course Level cannot be negative.";
            }
            else if (model.Questions == null || !model.Questions.Any())
            {
                TempData["ErrorMessage"] = "At least one question must be added.";
            }
            else if (model.Questions.Any(q => q.Options == null || !q.Options.Any()))
            {
                TempData["ErrorMessage"] = "Each question must have at least one option.";
            }

            if (TempData.ContainsKey("ErrorMessage"))
            {
                model.AvailableCourses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                model.AvailableLevels = await _context.CtLevelcourses
                    .Where(l => l.Available == 1)
                    .OrderBy(l => l.DescripctionLevel)
                    .ToListAsync();

                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Insert Test
                var test = new CtTest
                {
                    FkCourse = model.FkCourse,
                    FkLevelcourse = model.FkRequiredCourseLevels,
                    TestName = model.TestName,
                    Createuser = User.Identity?.Name ?? "Unknown",
                    Createdate = DateTime.Now,
                    Lastupdateuser = User.Identity?.Name ?? "Unknown",
                    Lastupatedate = DateTime.Now,
                    Available = 1
                };

                _context.CtTests.Add(test);
                await _context.SaveChangesAsync();

                // Insert Questions and Options
                foreach (var q in model.Questions)
                {
                    var question = new CtQuestion
                    {
                        FkTest = test.PkTest,
                        Question = q.QuestionText,
                        Createuser = test.Createuser,
                        Createdate = DateTime.Now,
                        Lastupdateuser = test.Createuser,
                        Lastupatedate = DateTime.Now,
                        Available = 1
                    };

                    _context.CtQuestions.Add(question);
                    await _context.SaveChangesAsync();

                    foreach (var optionVm in q.Options)
                    {
                        var option = new CtOption
                        {
                            FkQuestions = question.PkQuestions,
                            Options = optionVm.OptionText,
                            Answer = optionVm.IsCorrect ? 1 : 0,
                            Createuser = test.Createuser,
                            Createdate = DateTime.Now,
                            Lastupdateuser = test.Createuser,
                            Lastupatedate = DateTime.Now,
                            Available = 1
                        };

                        _context.CtOptions.Add(option);
                        await _context.SaveChangesAsync();

                        if (optionVm.IsCorrect)
                        {
                            var correctAnswer = new CtCorrectanswer
                            {
                                FkQuestions = question.PkQuestions,
                                FkOptions = option.PkOptions.ToString(),
                                Createuser = test.Createuser,
                                Createdate = DateTime.Now,
                                Lastupdateuser = test.Createuser,
                                Lastupatedate = DateTime.Now,
                                Available = 1
                            };

                            _context.CtCorrectanswers.Add(correctAnswer);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Test, questions, and options created successfully.";
                return RedirectToAction(nameof(IndexTest), "Catalog");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error saving data: {ex.Message}";

                model.AvailableCourses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();

                model.AvailableLevels = await _context.CtLevelcourses
                    .Where(l => l.Available == 1)
                    .OrderBy(l => l.DescripctionLevel)
                    .ToListAsync();

                return View(model);
            }
        }


        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditQuestions(int id)
        {
            var test = await _context.CtTests.FirstOrDefaultAsync(t => t.PkTest == id);
            if (test == null)
            {
                TempData["ErrorMessage"] = "Test not found.";
                return RedirectToAction(nameof(IndexTest));
            }

            var questions = await _context.CtQuestions
                .Where(q => q.FkTest == id)
                .Select(q => new QuestionCreateViewModel
                {
                    QuestionText = q.Question,
                    Options = _context.CtOptions
                        .Where(o => o.FkQuestions == q.PkQuestions)
                        .OrderBy(o => o.PkOptions)
                        .Select(o => new OptionCreateViewModel
                        {
                            OptionText = o.Options,
                            IsCorrect = o.Answer == 1
                        }).ToList()
                }).ToListAsync();

            var vm = new TestCreateViewModel
            {
                TestName = test.TestName,
                FkCourse = test.FkCourse,
                FkRequiredCourseLevels = test.FkLevelcourse,
                Questions = questions,
                AvailableCourses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync(),
                AvailableLevels = await _context.CtLevelcourses
                    .Where(l => l.Available == 1)
                    .OrderBy(l => l.PkLevelcourse)
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditQuestions(int id, TestCreateViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var test = await _context.CtTests.FindAsync(id);
                if (test == null)
                {
                    TempData["ErrorMessage"] = "Test not found.";
                    return RedirectToAction(nameof(IndexTest));
                }

                test.Lastupdateuser = User.Identity?.Name ?? "Unknown";
                test.Lastupatedate = DateTime.Now;

                var questions = await _context.CtQuestions
                    .Where(q => q.FkTest == id)
                    .ToListAsync();

                for (int i = 0; i < model.Questions.Count; i++)
                {
                    var vmQuestion = model.Questions[i];
                    var dbQuestion = questions.ElementAtOrDefault(i);
                    if (dbQuestion == null) continue;

                    dbQuestion.Question = vmQuestion.QuestionText;
                    dbQuestion.Lastupdateuser = test.Lastupdateuser;
                    dbQuestion.Lastupatedate = DateTime.Now;

                    var dbOptions = await _context.CtOptions
                        .Where(o => o.FkQuestions == dbQuestion.PkQuestions)
                        .OrderBy(o => o.PkOptions)
                        .ToListAsync();

                    for (int j = 0; j < vmQuestion.Options.Count; j++)
                    {
                        var vmOption = vmQuestion.Options[j];
                        var dbOption = dbOptions.ElementAtOrDefault(j);
                        if (dbOption == null) continue;

                        dbOption.Options = vmOption.OptionText;
                        dbOption.Answer = vmOption.IsCorrect ? 1 : 0;
                        dbOption.Lastupdateuser = test.Lastupdateuser;
                        dbOption.Lastupatedate = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = "Test questions updated successfully.";
                return RedirectToAction(nameof(IndexTest));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Error updating questions: {ex.Message}";
                return View(model);
            }
        }

    }
}
