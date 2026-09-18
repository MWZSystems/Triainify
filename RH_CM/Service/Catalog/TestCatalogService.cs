using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.ViewModels;
using RH_CM.Messages.Catalog;

namespace RH_CM.Service.Catalog
{
    /// <summary>
    /// Manages the test question bank: duplicate validation, the "can't edit/delete an already-answered test" rule, and question create/edit transactions.
    /// </summary>
    public class TestCatalogService
    {
        private readonly db_abcd61_rhchdbContext _context;

        public TestCatalogService(db_abcd61_rhchdbContext context)
        {
            _context = context;
        }

        public async Task<bool> HasBeenAnsweredAsync(int testId)
        {
            return await _context.SyUserAnswers.AnyAsync(ua => ua.FkTest == testId)
                || await _context.SyUserDiagnostics.AnyAsync(ud => ud.FkTest == testId);
        }

        public async Task<(List<CtCourse> Courses, List<CtLevelcourse> Levels)> GetTestFormOptionsAsync()
        {
            var courses = await _context.CtCourses
                .AsNoTracking()
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            var levels = await _context.CtLevelcourses
                .AsNoTracking()
                .Where(l => l.Available == 1)
                .OrderBy(l => l.PkLevelcourse)
                .ToListAsync();

            return (courses, levels);
        }

        public async Task PopulateTestFormOptionsAsync(TestCreateViewModel model)
        {
            model.AvailableCourses = await _context.CtCourses
                .AsNoTracking()
                .Where(c => c.Available == 1)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            model.AvailableLevels = await _context.CtLevelcourses
                .AsNoTracking()
                .Where(l => l.Available == 1)
                .OrderBy(l => l.PkLevelcourse)
                .ToListAsync();

            model.AvailableOptionTypes = await _context.CtOptiontypes
                .AsNoTracking()
                .Where(o => o.Available == 1)
                .OrderBy(o => o.PkOptiontype)
                .ToListAsync();
        }

        public class OperationResult
        {
            public bool Success { get; init; }
            public string? Message { get; init; }
        }

        public async Task<OperationResult> DeleteTestAsync(int testId)
        {
            if (await HasBeenAnsweredAsync(testId))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = TestCatalogMessages.TestHasAlreadyBeenAnsweredByA
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var questions = await _context.CtQuestions
                    .Where(q => q.FkTest == testId)
                    .ToListAsync();

                var questionIds = questions.Select(q => q.PkQuestions).ToList();

                var correctAnswers = await _context.CtCorrectanswers
                    .Where(ca => questionIds.Contains(ca.FkQuestions))
                    .ToListAsync();
                _context.CtCorrectanswers.RemoveRange(correctAnswers);

                var options = await _context.CtOptions
                    .Where(o => questionIds.Contains(o.FkQuestions))
                    .ToListAsync();
                _context.CtOptions.RemoveRange(options);

                _context.CtQuestions.RemoveRange(questions);

                var test = await _context.CtTests.FindAsync(testId);
                if (test != null)
                {
                    _context.CtTests.Remove(test);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return new OperationResult { Success = true, Message = TestCatalogMessages.TestAndAllRelatedDataWereDeleted };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new OperationResult { Success = false, Message = string.Format(TestCatalogMessages.ErrorDeletingTestFormat, ex.Message) };
            }
        }

        public async Task<OperationResult> ToggleTestAsync(int testId)
        {
            var test = await _context.CtTests.FindAsync(testId);
            if (test == null)
            {
                return new OperationResult { Success = false, Message = TestCatalogMessages.TestNotFound };
            }

            // Validacion si se enciende, checar que no haya ya una combinacion (Course/Level) activa
            if (test.Available == 0 && await _context.CtTests
                             .AnyAsync(t => t.FkCourse == test.FkCourse
                                             && t.FkLevelcourse == test.FkLevelcourse
                                             && t.Available == 1))
            {
                return new OperationResult
                {
                    Success = false,
                    Message = TestCatalogMessages.ThereIsAlreadyATestActiveFor
                };
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

            return new OperationResult { Success = true, Message = TestCatalogMessages.TestStatusUpdatedSuccessfully };
        }

        public class EditTestLoadResult
        {
            public CtTest? Test { get; init; }
            public string? ErrorMessage { get; init; }
            public List<CtCourse>? Courses { get; init; }
            public List<CtLevelcourse>? Levels { get; init; }
        }

        public async Task<EditTestLoadResult> GetEditTestViewModelAsync(int? id)
        {
            if (id == null)
            {
                return new EditTestLoadResult { ErrorMessage = TestCatalogMessages.NoTestIdWasProvided };
            }

            var ctTest = await _context.CtTests.FindAsync(id);
            if (ctTest == null)
            {
                return new EditTestLoadResult { ErrorMessage = TestCatalogMessages.TestCouldNotBeFound };
            }

            var (courses, levels) = await GetTestFormOptionsAsync();
            return new EditTestLoadResult { Test = ctTest, Courses = courses, Levels = levels };
        }

        public class EditTestOutcome
        {
            public bool RedirectToIndex { get; init; }
            public bool RedirectToEditForm { get; init; }
            public bool ReshowForm { get; init; }
            public bool IsSuccess { get; init; }
            public string? Message { get; init; }
            public List<CtCourse>? Courses { get; init; }
            public List<CtLevelcourse>? Levels { get; init; }
        }

        public async Task<EditTestOutcome> EditTestAsync(int id, CtTest model)
        {
            if (id != model.PkTest)
            {
                return new EditTestOutcome { RedirectToIndex = true, Message = TestCatalogMessages.TestNotFound };
            }

            // Manual validation (same as the original: the message is recorded but doesn't stop
            // the flow here — it's only used further below, after the duplicate validations).
            string? manualValidationError = null;
            if (string.IsNullOrWhiteSpace(model.TestName))
            {
                manualValidationError = "The Test Name field is required.";
            }
            else if (model.FkCourse == 0)
            {
                manualValidationError = "Please select a valid course.";
            }
            else if (model.FkLevelcourse == 0)
            {
                manualValidationError = "Please select a valid course level.";
            }

            var existingTest = await _context.CtTests.FindAsync(id);
            if (existingTest == null)
            {
                return new EditTestOutcome { RedirectToIndex = true, Message = TestCatalogMessages.TestNotFoundInTheDatabase };
            }

            // If only the name changes, the operation continues; otherwise, run all the extra validations.
            if (!(existingTest.TestName != model.TestName && existingTest.FkCourse == model.FkCourse && existingTest.FkLevelcourse == model.FkLevelcourse))
            {
                if (await HasBeenAnsweredAsync(id))
                {
                    return new EditTestOutcome
                    {
                        RedirectToIndex = true,
                        Message = TestCatalogMessages.TestHasAlreadyBeenAnsweredByA
                    };
                }

                if (!await _context.CtCourseassignments
                             .AnyAsync(ca => ca.FkCourse == model.FkCourse
                                             && ca.FkRequiredCourseLevels == model.FkLevelcourse))
                {
                    return new EditTestOutcome
                    {
                        RedirectToIndex = true,
                        Message = TestCatalogMessages.CourseLevelCombinationDoesntExistInCourseassignments
                    };
                }

                if (await _context.CtTests
                             .AnyAsync(t => t.FkCourse == model.FkCourse
                                             && t.FkLevelcourse == model.FkLevelcourse
                                             && t.Available == 1))
                {
                    return new EditTestOutcome
                    {
                        RedirectToIndex = true,
                        Message = TestCatalogMessages.CourseLevelCombinationAlreadyHasATest
                    };
                }
            }

            if (manualValidationError != null)
            {
                var (courses, levels) = await GetTestFormOptionsAsync();
                return new EditTestOutcome { ReshowForm = true, Message = manualValidationError, Courses = courses, Levels = levels };
            }

            try
            {
                existingTest.TestName = model.TestName;
                existingTest.FkCourse = model.FkCourse;
                existingTest.FkLevelcourse = model.FkLevelcourse;

                _context.Update(existingTest);
                await _context.SaveChangesAsync();

                return new EditTestOutcome { RedirectToIndex = true, IsSuccess = true, Message = TestCatalogMessages.TestSuccessfullyUpdated };
            }
            catch (DbUpdateConcurrencyException)
            {
                return new EditTestOutcome { RedirectToEditForm = true, Message = TestCatalogMessages.ConcurrencyErrorOccurredWhileUpdatingTheTest };
            }
            catch (Exception)
            {
                return new EditTestOutcome { RedirectToEditForm = true, Message = TestCatalogMessages.UnexpectedErrorOccurredWhileUpdatingTheTest };
            }
        }

        public class CreateQuestionsOutcome
        {
            public bool Success { get; init; }
            public string? Message { get; init; }
            public TestCreateViewModel? Model { get; init; }
        }

        public async Task<CreateQuestionsOutcome> CreateQuestionsAsync(TestCreateViewModel model, string userName)
        {
            string? validationError = null;

            if (string.IsNullOrWhiteSpace(model.TestName))
            {
                validationError = "The Test Name field is required.";
            }
            else if (model.FkRequiredCourseLevels < 0)
            {
                validationError = "The Course Level cannot be negative.";
            }
            else if (model.Questions == null || !model.Questions.Any())
            {
                validationError = "At least one question must be added.";
            }
            else if (model.Questions.Any(q => q.Options == null || !q.Options.Any()))
            {
                validationError = "Each question must have at least one option.";
            }
            else if (model.Questions.Any(q => q.Options.All(o => o.IsCorrect == false)))
            {
                validationError = "Each question must have at least one correct option.";
            }
            else if (!await _context.CtCourseassignments
                             .AnyAsync(ca => ca.FkCourse == model.FkCourse
                                             && ca.FkRequiredCourseLevels == model.FkRequiredCourseLevels))
            {
                validationError = "This Course-Level combination doesn't exist in Courseassignments.";
            }
            else if (await _context.CtTests
                         .AnyAsync(t => t.FkCourse == model.FkCourse
                                        && t.FkLevelcourse == model.FkRequiredCourseLevels
                                        && t.Available == 1))
            {
                validationError = "A test for this Course and Level combination already exists. Please disbable the other one first.";
            }

            if (validationError != null)
            {
                await PopulateTestFormOptionsAsync(model);
                return new CreateQuestionsOutcome { Success = false, Message = validationError, Model = model };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var test = new CtTest
                {
                    FkCourse = model.FkCourse,
                    FkLevelcourse = model.FkRequiredCourseLevels,
                    TestName = model.TestName,
                    Createuser = userName,
                    Createdate = DateTime.Now,
                    Lastupdateuser = userName,
                    Lastupatedate = DateTime.Now,
                    Available = 1
                };

                _context.CtTests.Add(test);
                await _context.SaveChangesAsync();

                foreach (var q in model.Questions ?? Enumerable.Empty<QuestionCreateViewModel>())
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
                    await _context.SaveChangesAsync(); // the generated PkQuestions is needed before creating its options

                    var options = new List<CtOption>();
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

                        options.Add(option);
                        _context.CtOptions.Add(option);
                    }

                    // A single round-trip for all of this question's options.
                    await _context.SaveChangesAsync();

                    var correctAnswers = q.Options
                        .Zip(options, (optionVm, option) => (optionVm, option))
                        .Where(x => x.optionVm.IsCorrect)
                        .Select(x => new CtCorrectanswer
                        {
                            FkQuestions = question.PkQuestions,
                            FkOptions = x.option.PkOptions.ToString(),
                            Createuser = test.Createuser,
                            Createdate = DateTime.Now,
                            Lastupdateuser = test.Createuser,
                            Lastupatedate = DateTime.Now,
                            Available = 1
                        })
                        .ToList();

                    if (correctAnswers.Count > 0)
                    {
                        _context.CtCorrectanswers.AddRange(correctAnswers);
                        await _context.SaveChangesAsync();
                    }
                }

                await transaction.CommitAsync();

                return new CreateQuestionsOutcome { Success = true, Message = TestCatalogMessages.TestQuestionsAndOptionsCreatedSuccessfully };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await PopulateTestFormOptionsAsync(model);
                return new CreateQuestionsOutcome { Success = false, Message = string.Format(TestCatalogMessages.ErrorSavingDataFormat, ex.Message), Model = model };
            }
        }

        public async Task<TestCreateViewModel?> GetEditQuestionsViewModelAsync(int id)
        {
            var test = await _context.CtTests.FirstOrDefaultAsync(t => t.PkTest == id);
            if (test == null)
            {
                return null;
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

            await PopulateTestFormOptionsAsync(vm);

            return vm;
        }

        public class EditQuestionsOutcome
        {
            public bool Success { get; init; }
            public bool RedirectToIndex { get; init; }
            public string? Message { get; init; }
            public TestCreateViewModel? Model { get; init; }
        }

        public async Task<EditQuestionsOutcome> EditQuestionsAsync(int id, TestCreateViewModel model, string userName)
        {
            if (await HasBeenAnsweredAsync(id))
            {
                await PopulateTestFormOptionsAsync(model);
                return new EditQuestionsOutcome
                {
                    Message = TestCatalogMessages.TestHasAlreadyBeenAnsweredByUsers,
                    Model = model
                };
            }

            if (model.Questions.Any(q => q.Options.All(o => o.IsCorrect == false)))
            {
                await PopulateTestFormOptionsAsync(model);
                return new EditQuestionsOutcome
                {
                    Message = TestCatalogMessages.EachQuestionMustHaveAtLeastOne,
                    Model = model
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var test = await _context.CtTests.FindAsync(id);
                if (test == null)
                {
                    return new EditQuestionsOutcome { RedirectToIndex = true, Message = TestCatalogMessages.TestNotFound };
                }

                test.Lastupdateuser = userName;
                test.Lastupatedate = DateTime.Now;

                var questions = await _context.CtQuestions
                    .Where(q => q.FkTest == id)
                    .ToListAsync();

                var questionIds = questions.Select(q => q.PkQuestions).ToList();
                var allOptions = await _context.CtOptions
                    .Where(o => questionIds.Contains(o.FkQuestions))
                    .OrderBy(o => o.PkOptions)
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

                    var dbOptions = allOptions
                        .Where(o => o.FkQuestions == dbQuestion.PkQuestions)
                        .ToList();

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

                return new EditQuestionsOutcome { Success = true, Message = TestCatalogMessages.TestQuestionsUpdatedSuccessfully };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await PopulateTestFormOptionsAsync(model);
                return new EditQuestionsOutcome { Message = string.Format(TestCatalogMessages.ErrorUpdatingQuestionsFormat, ex.Message), Model = model };
            }
        }
    }
}
