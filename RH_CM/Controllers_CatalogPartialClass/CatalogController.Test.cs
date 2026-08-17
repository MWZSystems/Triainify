using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;
using RH_CM.Models;
using RH_CM.Service.DTOs;
using RH_CM.ViewModels;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {

        // GET: CtTest/IndexTest
        [Authorize(Policy = "ViewAccess")]
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
        [Authorize(Policy = "ViewAccess")]
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
        [Authorize(Policy = "ViewAccess")]
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

        /// //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        //hay que agregar un Toggle, al desactivar que no haya impedimento
        //al activar que valide primero que no haya otro test con la combinacion en available

        // POST: CtTest/Toggle
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTest(int id)
        {
            CtTest test = await _context.CtTests.FindAsync(id);

            //Validacion si se enciende, checar que no haya ya una combinacion (Course/Level) activa


            if (test.Available == 0 && await _context.CtTests
                             .AnyAsync(t => t.FkCourse == test.FkCourse
                                             && t.FkLevelcourse == test.FkLevelcourse
                                             && t.Available == 1))
            {
                TempData["ErrorMessage"] = "There is already a Test Active for this Course/Level, please disable it first.";
                return RedirectToAction(nameof(IndexTest));
            }

            test.Available = test.Available == 1 ? 0 : 1;

            var questions = await _context.CtQuestions
                .Where(q => q.FkTest == test.PkTest)
                .ToListAsync();

            var questionIds = questions.Select(q => q.PkQuestions).ToList();

            var options = await _context.CtOptions
                .Where(o => questionIds.Contains(o.FkQuestions))
                .ToListAsync();

            foreach (var question in questions)
            {
                question.Available = test.Available;
            }

            foreach (var option in options)
            {
                option.Available = test.Available;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Test status updated successfully.";
            return RedirectToAction(nameof(IndexTest));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ViewAccess")]
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


            var existingTest = await _context.CtTests.FindAsync(id);
            if (existingTest == null)
            {
                TempData["ErrorMessage"] = "Test not found in the database.";
                return RedirectToAction(nameof(IndexTest));
            }

            //Si solo el nombre cambia la operacion continua, si no, hace todas las validaciones extras.
            if (!(existingTest.TestName != model.TestName && existingTest.FkCourse == model.FkCourse && existingTest.FkLevelcourse == model.FkLevelcourse))
            {
                //Si hay alguien ya respondio el test, no permite cambiar el Course/Level
                if (await _context.SyUserAnswers
                                 .AnyAsync(ua => ua.FkTest == id))
                {
                    TempData["ErrorMessage"] = "This test has already been answered by a user. Please delete that record first.";
                    return RedirectToAction(nameof(IndexTest));
                }
                //Verifica que exista la combinacion Course/Level
                else if (!await _context.CtCourseassignments
                             .AnyAsync(ca => ca.FkCourse == model.FkCourse
                                             && ca.FkRequiredCourseLevels == model.FkLevelcourse))
                {
                    TempData["ErrorMessage"] = "This Course-Level combination doesn't exist in Courseassignments.";
                    return RedirectToAction(nameof(IndexTest));
                }
                else if (await _context.CtTests
                             .AnyAsync(t => t.FkCourse == model.FkCourse
                                             && t.FkLevelcourse == model.FkLevelcourse
                                             && t.Available == 1))
                {
                    TempData["ErrorMessage"] = "This Course-Level combination already has a Test Available, please disable it first.";
                    return RedirectToAction(nameof(IndexTest));
                }
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
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> CreateQuestions()
        {
            var vm = new TestCreateViewModel();
            await InitializeCreateTestViewModelAsync(vm);

            // CHANGED: antes se pre-cargaba una pregunta "dummy" con 2 opciones vacías, pero la vista
            // nunca la usaba (el questions-container se armaba 100% por JS). Ahora que la vista SÍ
            // renderiza Model.Questions (para poder recuperar datos tras un error de validación),
            // dejamos la lista vacía en el alta inicial para no mostrar una tarjeta en blanco.
            vm.Questions = new List<QuestionCreateViewModel>();

            return View(vm);
        }

        // POST: Catalog/CreateQuestions
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
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
                // NOTE: model.Questions ya viene completo desde el POST (el model binding de ASP.NET
                // reconstruye la lista a partir de "Questions[i].QuestionText", "Questions[i].Options[j].OptionText",
                // etc.). Solo faltaba que la vista lo usara para re-pintar el HTML — ver CreateQuestions.cshtml.
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
        [Authorize(Policy = "ViewAccess")]
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
                FkTypeOption = q.FkTypeOption,
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
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditQuestions(int id, TestCreateViewModel model)
        {

            //si el examen ya fue respondido, no se puede editar, se necesita hacer toggle y añadir uno nuevo.
            if (await _context.SyUserAnswers
                             .AnyAsync(ua => ua.FkTest == id))
            {
                TempData["ErrorMessage"] = "This test has already been answered by users. Please disable this one, and create a new one.";

                // CHANGED: antes se hacía RedirectToAction(EditQuestions, { id }), lo que disparaba un
                // nuevo GET que volvía a leer el test desde la base de datos y descartaba cualquier
                // cambio que el usuario ya hubiera escrito en el formulario. Ahora devolvemos la misma
                // vista con el "model" que llegó del POST, así no se pierde nada de lo ya editado.
                await InitializeCreateTestViewModelAsync(model);
                return View(model);
            }
            //Nueva validación: cada pregunta debe tener al menos una opción correcta
            else if (model.Questions.Any(q => q.Options.All(o => o.IsCorrect == false)))
            {
                TempData["ErrorMessage"] = "Each question must have at least one correct option.";

                // CHANGED: mismo motivo que arriba — se preserva lo que el usuario ya tenía escrito.
                await InitializeCreateTestViewModelAsync(model);
                return View(model);
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

                // CHANGED: antes devolvía View(model) sin AvailableCourses/AvailableLevels/AvailableOptionTypes,
                // lo cual no rompía nada gracias al "?." en la vista, pero para mantener consistencia con los
                // otros dos casos de error dejamos también esta ruta re-inicializando las listas.
                await InitializeCreateTestViewModelAsync(model);
                return View(model);
            }
        }

    }
}
