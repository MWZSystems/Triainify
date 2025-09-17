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

            //Si hay alguien ya respondio el test, no permite el borrado
            if (await _context.SyUserAnswers
                             .AnyAsync(ua => ua.FkTest == id))
            {
                TempData["ErrorMessage"] = "This test has already been answered by a user. Please delete that record first.";
                return RedirectToAction(nameof(IndexTest));
            }


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

        //GET
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


            //Si se modifico el Course/Level
                //Verificar Si el ID del examen existe en [SY_USER_ANSWERS], Si Si existe, que no permita hacer modificacion.
                //Que la combinacion si exista en CA
                //comprobar que no exista ya esa combinacion con un examen

            //si la modificacion es solo el nombre del examen, no importa que continue normal



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

                TempData["SuccessMessage"] = "Test Successfully Updated";
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


        /// <summary>
        /// Gets Every Course, every level, and option Types into the object within the object: TestCreateViewModel
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private async Task InitializeCreateTestViewModelAsync(TestCreateViewModel model)
        {
            model.AvailableCourses = await _context.CtCourses
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            model.AvailableLevels = await _context.CtLevelcourses
                .Where(l => l.Available == 1)
                .OrderBy(l => l.PkLevelcourse)
                .ToListAsync();

            model.AvailableOptionTypes = await _context.CtOptiontypes
                .Where(o => o.Available == 1)
                .OrderBy(o => o.PkOptiontype)
                .ToListAsync();
        }


        // GET: Catalog/CreateQuestions
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> CreateQuestions()
        {
            var vm = new TestCreateViewModel();
            await InitializeCreateTestViewModelAsync(vm);

            vm.Questions = new List<QuestionCreateViewModel>
            {
                new QuestionCreateViewModel
                {
                    Options = new List<OptionCreateViewModel>
                    {
                        new OptionCreateViewModel(),
                        new OptionCreateViewModel()
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
            //Nueva validación: cada pregunta debe tener al menos una opción correcta
            else if (model.Questions.Any(q => q.Options.All(o => o.IsCorrect == false)))
            {
                TempData["ErrorMessage"] = "Each question must have at least one correct option.";
            }
           //Validación: la combinación debe existir en CtCourseassignments
            else if (!await _context.CtCourseassignments
                             .AnyAsync(ca => ca.FkCourse == model.FkCourse
                                             && ca.FkRequiredCourseLevels == model.FkRequiredCourseLevels))
            {
                TempData["ErrorMessage"] = "This Course-Level combination doesn't exist in Courseassignments.";
            }
            //  Validación: no debe existir un test ya creado para este Course + Level
            else if (await _context.CtTests
                         .AnyAsync(t => t.FkCourse == model.FkCourse
                                        && t.FkLevelcourse == model.FkRequiredCourseLevels
                                        && t.Available == 1))
            {
                TempData["ErrorMessage"] = "A test for this Course and Level combination already exists. Please disbable the other one first.";
            }


            if (TempData.ContainsKey("ErrorMessage"))
            {
                await InitializeCreateTestViewModelAsync(model);
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
                        FkTypeOption = q.FkTypeOption,
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

                await InitializeCreateTestViewModelAsync(model);
                return View(model);
            }
        }

        //GET
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
                FkTypeOption = q.FkTypeOption, // ✅ NUEVO
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
                Questions = questions
            };

            await InitializeCreateTestViewModelAsync(vm);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditQuestions(int id, TestCreateViewModel model)
        {

            //si el examen ya fue respondido, no se puede editar, se necesita hacer toggle y añadir uno nuevo.
            if (await _context.SyUserAnswers
                             .AnyAsync(ua => ua.FkTest == id))
            {
                TempData["ErrorMessage"] = "This test has already been answered by users. Please disable this one, and create a new one.";
                return RedirectToAction(nameof(EditQuestions), new { id = id });

            }

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
                    dbQuestion.FkTypeOption = vmQuestion.FkTypeOption;
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


        [Authorize(Roles = "Empleado, RHGerente, Administrador")]
        public async Task<IActionResult> AnswerTest(int id)
        {
            var test = await _context.CtTests
                .Where(t => t.PkTest == id && t.Available == 1)
                .FirstOrDefaultAsync();

            if (test == null)
            {
                return NotFound();
            }

            var questions = await _context.CtQuestions
                .Where(q => q.FkTest == test.PkTest && q.Available == 1)
                .ToListAsync();

            var model = new SubmitTestViewModel
            {
                FkTest = test.PkTest,
                TestName = test.TestName,
                Questions = new List<SubmitQuestionViewModel>()
            };

            foreach (var question in questions)
            {
                var options = await _context.CtOptions
                    .Where(o => o.FkQuestions == question.PkQuestions && o.Available == 1)
                    .ToListAsync();

                var questionVm = new SubmitQuestionViewModel
                {
                    FkQuestion = question.PkQuestions,
                    QuestionText = question.Question,
                    IsMultiple = options.Count(o => o.Answer == 1) > 1,
                    Options = options.Select(o => new SubmitOptionViewModel
                    {
                        FkOption = o.PkOptions,
                        OptionText = o.Options,
                        IsSelected = false // inicializa sin selección
                    }).ToList()
                };

                model.Questions.Add(questionVm);
            }

            return View("SubmitTest", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Empleado, RHGerente, Administrador")]
        public async Task<IActionResult> SubmitTest(SubmitTestViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("SubmitTest", model);
            }

            var currentUser = User.Identity?.Name ?? "Anon";
            var now = DateTime.Now;

            foreach (var question in model.Questions)
            {
                foreach (var option in question.Options)
                {
                    if (!option.IsSelected)
                        continue;

                    var correct = await _context.CtOptions
                        .Where(o => o.PkOptions == option.FkOption)
                        .Select(o => o.Answer == 1)
                        .FirstOrDefaultAsync();

                    var response = new SyUserAnswer
                    {
                        FkTest = model.FkTest,
                        FkQuestions = question.FkQuestion,
                        FkOptions = option.FkOption,
                        IsSelected = true,
                        IsCorrected = correct,
                        Createuser = currentUser,
                        Createdate = now,
                        Available = 1
                    };

                    _context.SyUserAnswers.Add(response);
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("TestResult");
        }



    }
}
