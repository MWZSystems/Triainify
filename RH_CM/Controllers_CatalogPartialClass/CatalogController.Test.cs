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
                .OrderByDescending(t => t.Createdate)
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

        // GET: CtTest/EditTest/5
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditTest(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ctTest = await _context.CtTests.FindAsync(id);
            if (ctTest == null)
            {
                return NotFound();
            }

            ViewBag.Courses = await _context.CtCourses
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            return View(ctTest);
        }

        // GET: Catalog/CreateQuestions
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> CreateQuestions()
        {
            ViewBag.Courses = await _context.CtCourses
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            var vm = new TestCreateViewModel
            {
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
                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();
                return View(model);
            }

            if (model.CourseLevel < 0)
            {
                TempData["ErrorMessage"] = "The Course Level cannot be negative.";
                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
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
                    CourseLevel = model.CourseLevel,
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
                ViewBag.Courses = await _context.CtCourses
                    .Where(c => c.Available == 1)
                    .OrderBy(c => c.CourseName)
                    .ToListAsync();
                return View(model);
            }
        }

        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditQuestions(int id)
        {
            // Obtiene el test
            var test = await _context.CtTests
                .FirstOrDefaultAsync(t => t.PkTest == id);

            if (test == null)
            {
                return NotFound();
            }

            // Obtiene preguntas y opciones
            var questions = await _context.CtQuestions
                .Where(q => q.FkTest == id)
                .Select(q => new QuestionCreateViewModel
                {
                    QuestionText = q.Question,
                    Options = _context.CtOptions
                        .Where(o => o.FkQuestions == q.PkQuestions)
                        .Select(o => new OptionCreateViewModel
                        {
                            OptionText = o.Options,
                            IsCorrect = o.Answer == 1
                        })
                        .ToList()
                })
                .ToListAsync();

            ViewBag.Courses = await _context.CtCourses
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            var vm = new TestCreateViewModel
            {
                FkCourse = test.FkCourse,
                CourseLevel = test.CourseLevel,
                TestName = test.TestName,
                Questions = questions
            };

            return View(vm);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestions(int id, TestCreateViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Actualizar datos del test
                var test = await _context.CtTests.FindAsync(id);
                if (test == null)
                {
                    return NotFound();
                }

                test.TestName = model.TestName;
                test.FkCourse = model.FkCourse;
                test.CourseLevel = model.CourseLevel;
                test.Lastupdateuser = User.Identity?.Name ?? "Unknown";
                test.Lastupatedate = DateTime.Now;

                await _context.SaveChangesAsync();

                // Eliminar preguntas, opciones y respuestas previas
                var oldQuestions = await _context.CtQuestions
                    .Where(q => q.FkTest == id)
                    .ToListAsync();

                foreach (var q in oldQuestions)
                {
                    var correctAnswers = await _context.CtCorrectanswers
                        .Where(c => c.FkQuestions == q.PkQuestions)
                        .ToListAsync();
                    _context.CtCorrectanswers.RemoveRange(correctAnswers);

                    var options = await _context.CtOptions
                        .Where(o => o.FkQuestions == q.PkQuestions)
                        .ToListAsync();
                    _context.CtOptions.RemoveRange(options);
                }

                _context.CtQuestions.RemoveRange(oldQuestions);
                await _context.SaveChangesAsync();

                // Insertar nuevas preguntas y opciones
                foreach (var q in model.Questions)
                {
                    var question = new CtQuestion
                    {
                        FkTest = test.PkTest,
                        Question = q.QuestionText,
                        Createuser = test.Createuser,
                        Createdate = DateTime.Now,
                        Lastupdateuser = test.Lastupdateuser,
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
                            Lastupdateuser = test.Lastupdateuser,
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
                                Lastupdateuser = test.Lastupdateuser,
                                Lastupatedate = DateTime.Now,
                                Available = 1
                            };

                            _context.CtCorrectanswers.Add(correctAnswer);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

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
