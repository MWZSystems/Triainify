using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.ViewModels;
using static RH_CM.ViewModels.ViewModels;
using RH_CM.Messages.Trainify;

namespace RH_CM.Service.Trainify
{
    /// <summary>
    /// Diagnostic/Exam engine: gating, answer grading, the minimum passing score rule, and result persistence.
    /// </summary>
    public class DiagnosticExamService
    {
        public const int PassingScore = 80;

        private readonly db_abcd61_rhchdbContext _context;
        public DiagnosticExamService(db_abcd61_rhchdbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// An answer is correct when the selected option set exactly matches the correct option set (order doesn't matter).
        /// </summary>
        public static bool IsAnswerCorrect(IEnumerable<int> selectedIds, IEnumerable<int> correctIds)
        {
            var selected = selectedIds as ICollection<int> ?? selectedIds.ToList();
            var correct = correctIds as ICollection<int> ?? correctIds.ToList();

            return selected.Count == correct.Count
                && !selected.Except(correct).Any()
                && !correct.Except(selected).Any();
        }

        /// <summary>
        /// Rounded 0-100 score; a test with no questions counts as 1 to avoid dividing by zero.
        /// </summary>
        public static int CalculateScore(int correctCount, int totalQuestions)
        {
            return (int)Math.Round((double)correctCount * 100.0 / Math.Max(1, totalQuestions), 0);
        }

        public static bool IsPassingScore(int score) => score >= PassingScore;

        public static int CourseStatusForScore(int score) => IsPassingScore(score) ? 1 : 5;

        public static bool HasExactDistinctIds(IEnumerable<int> submittedIds, IEnumerable<int> expectedIds)
        {
            var submitted = submittedIds.ToList();
            var expected = expectedIds.Distinct().ToList();
            return submitted.Count == submitted.Distinct().Count()
                && submitted.Count == expected.Count
                && !submitted.Except(expected).Any();
        }

        public async Task<bool> ExistsDiagnosticTestAsync(int courseId, int levelId)
        {
            return await _context.CtTests
                .AsNoTracking()
                .AnyAsync(t => t.Available == 1 && t.FkCourse == courseId && t.FkLevelcourse == levelId);
        }

        public async Task<bool> ExistsMaterialAsync(int courseId, int levelId)
        {
            return await (
                from clm in _context.CtCourseLevelMaterials.AsNoTracking()
                join m in _context.CtCoursematerials.AsNoTracking()
                    on clm.FkCourseMaterial equals m.PkCoursematerial
                where clm.Available == 1
                   && clm.FkCourse == courseId
                   && clm.FkLevelCourse == levelId
                   && (m.Available ?? 0) == 1
                   && ((m.File != null && m.File.Length > 0) || !string.IsNullOrWhiteSpace(m.UrlPath))
                select clm.PkCourseLevelMaterial
            ).AnyAsync();
        }

        public async Task<int?> GetDiagnosticTestIdAsync(int courseId, int levelId)
        {
            return await _context.CtTests
                .AsNoTracking()
                .Where(t => t.Available == 1 && t.FkCourse == courseId && t.FkLevelcourse == levelId)
                .OrderByDescending(t => t.PkTest)
                .Select(t => (int?)t.PkTest)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Resolves the active material (PDF/Video) linked to a course+level, or null if none is current.
        /// </summary>
        public async Task<CtCoursematerial?> GetActiveMaterialAsync(int courseId, int levelId)
        {
            var link = await _context.CtCourseLevelMaterials
                .AsNoTracking()
                .Where(x => x.Available == 1 && x.FkCourse == courseId && x.FkLevelCourse == levelId)
                .OrderByDescending(x => x.PkCourseLevelMaterial)
                .FirstOrDefaultAsync();

            if (link == null)
            {
                return null;
            }

            return await _context.CtCoursematerials
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.PkCoursematerial == link.FkCourseMaterial && (m.Available ?? 0) == 1);
        }

        private async Task<(int HeadcountId, int ControlNumber)?> GetHeadcountInfoByUserNameAsync(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return null;

            var userRow = await _context.AspNetUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (userRow == null || string.IsNullOrWhiteSpace(userRow.EmployeeNumber))
                return null;

            if (!int.TryParse(userRow.EmployeeNumber.Trim(), out var controlNumber))
                return null;

            var hc = await _context.SyHeadcounts
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.ControlNumber == controlNumber && h.Available == 1);

            if (hc == null)
                return null;

            return (hc.PkHeadcount, controlNumber);
        }

        public async Task<bool> HasDiagnosticAttemptAsync(string userName, int courseId, int levelId)
        {
            var current = await GetHeadcountInfoByUserNameAsync(userName);
            if (current == null)
                return false;

            var testId = await GetDiagnosticTestIdAsync(courseId, levelId);
            if (!testId.HasValue)
                return false;

            return await _context.SyUserDiagnostics
                .AsNoTracking()
                .AnyAsync(d =>
                    d.FkHeadcount == current.Value.HeadcountId &&
                    d.FkTest == testId.Value &&
                    d.Available == 1);
        }

        private async Task<int> GetNextDiagnosticCodeExamAsync()
        {
            var currentTransaction = _context.Database.CurrentTransaction
                ?? throw new InvalidOperationException("A database transaction is required to allocate an exam code.");

            var connection = _context.Database.GetDbConnection();
            await using var cmd = connection.CreateCommand();
            cmd.Transaction = currentTransaction.GetDbTransaction();

            // Legacy data predates the SQL sequence and already uses values ahead of it.
            // Serialize allocation inside the same transaction that persists the attempt,
            // then choose MAX+1 across every evidence table. The transaction-owned app lock
            // prevents concurrent requests (including other app instances) from colliding.
            cmd.CommandText = @"
DECLARE @lockResult int;
EXEC @lockResult = sys.sp_getapplock
    @Resource = N'Trainify.CODE_EXAM.Allocation',
    @LockMode = N'Exclusive',
    @LockOwner = N'Transaction',
    @LockTimeout = 15000;

IF @lockResult < 0
    THROW 51000, 'Unable to allocate a unique exam code.', 1;

SELECT ISNULL(MAX(CODE_EXAM), 0) + 1
FROM
(
    SELECT CODE_EXAM FROM dbo.SY_USER_DIAGNOSTIC
    UNION ALL
    SELECT CODE_EXAM FROM dbo.SY_USER_ANSWERS
    UNION ALL
    SELECT CODE_EXAM FROM dbo.SY_COURSEMOVEMENTS
) AS EvidenceCodes;";
            var result = await cmd.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }

        private async Task<int> GetLastDiagnosticCodeExamAsync(int fkHeadcount, int fkTest)
        {
            return await _context.SyUserDiagnostics
                .AsNoTracking()
                .Where(d => d.FkHeadcount == fkHeadcount && d.FkTest == fkTest && d.Available == 1)
                .OrderByDescending(d => d.Createdate)
                .Select(d => d.CodeExam)
                .FirstOrDefaultAsync();
        }

        private async Task<SubmitTestViewModel> BuildSubmitTestViewModelAsync(
            CtTest test, int courseId, int levelId, int? courseAssignmentId)
        {
            var questions = await _context.CtQuestions
                .AsNoTracking()
                .Where(q => q.FkTest == test.PkTest && q.Available == 1)
                .OrderBy(q => q.PkQuestions)
                .ToListAsync();

            var questionIds = questions.Select(q => q.PkQuestions).ToList();

            var optionList = await _context.CtOptions
                .AsNoTracking()
                .Where(o => questionIds.Contains(o.FkQuestions) && o.Available == 1)
                .OrderBy(o => o.PkOptions)
                .ToListAsync();

            return new SubmitTestViewModel
            {
                FkTest = test.PkTest,
                TestName = test.TestName,
                NextCourseId = courseId,
                NextLevelId = levelId,
                CourseAssignmentId = courseAssignmentId,
                Questions = questions.Select(q =>
                {
                    var opts = optionList
                        .Where(o => o.FkQuestions == q.PkQuestions)
                        .OrderBy(o => o.PkOptions)
                        .ToList();

                    return new SubmitQuestionViewModel
                    {
                        FkQuestion = q.PkQuestions,
                        QuestionText = q.Question,
                        IsMultiple = opts.Count(o => o.Answer == 1) > 1,
                        Options = opts.Select(o => new SubmitOptionViewModel
                        {
                            FkOption = o.PkOptions,
                            OptionText = o.Options,
                            IsSelected = false
                        }).ToList()
                    };
                }).ToList()
            };
        }

        private sealed class SubmissionValidationResult
        {
            public SubmitTestViewModel? CanonicalModel { get; init; }
            public string? ErrorMessage { get; init; }
        }

        /// <summary>
        /// Rebuilds an exam from current database records and copies only valid selections.
        /// Hidden form values (test, questions, options, course, level and assignment) are never
        /// trusted as authoritative because a client can modify them before posting.
        /// </summary>
        private async Task<SubmissionValidationResult> ValidateSubmissionAsync(
            SubmitTestViewModel posted,
            SyHeadcount employee,
            bool requireCourseAssignment)
        {
            var test = await _context.CtTests
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.PkTest == posted.FkTest && x.Available == 1);

            if (test == null)
                return new SubmissionValidationResult { ErrorMessage = TrainifyMessages.ExamNotFoundOrNotAvailable };

            var canonical = await BuildSubmitTestViewModelAsync(
                test,
                test.FkCourse,
                test.FkLevelcourse,
                posted.CourseAssignmentId);

            var postedQuestions = posted.Questions ?? new List<SubmitQuestionViewModel>();
            if (!HasExactDistinctIds(postedQuestions.Select(x => x.FkQuestion), canonical.Questions.Select(x => x.FkQuestion)))
            {
                return new SubmissionValidationResult
                {
                    ErrorMessage = "The exam changed or the submitted questions are invalid. Reload the exam and try again."
                };
            }

            foreach (var canonicalQuestion in canonical.Questions)
            {
                var postedQuestion = postedQuestions.Single(x => x.FkQuestion == canonicalQuestion.FkQuestion);
                var selectedIds = (postedQuestion.Options ?? new List<SubmitOptionViewModel>())
                    .Where(x => x.IsSelected)
                    .Select(x => x.FkOption)
                    .ToList();
                var validIds = canonicalQuestion.Options.Select(x => x.FkOption).ToList();

                if (selectedIds.Count != selectedIds.Distinct().Count() || selectedIds.Except(validIds).Any())
                {
                    return new SubmissionValidationResult
                    {
                        ErrorMessage = "The submitted answer contains an option that does not belong to this question. Reload the exam and try again."
                    };
                }

                foreach (var option in canonicalQuestion.Options)
                    option.IsSelected = selectedIds.Contains(option.FkOption);
            }

            if (requireCourseAssignment)
            {
                if (!posted.CourseAssignmentId.HasValue || posted.CourseAssignmentId.Value <= 0)
                    return new SubmissionValidationResult { ErrorMessage = TrainifyMessages.MissingCourseAssignmentPleaseContactYourProvider };

                var assignmentIsValid = await _context.CtCourseassignments.AsNoTracking().AnyAsync(x =>
                    x.PkCourseAssignment == posted.CourseAssignmentId.Value &&
                    x.FkCourse == test.FkCourse &&
                    x.FkRequiredCourseLevels == test.FkLevelcourse &&
                    x.FkPosition == employee.FkPosition &&
                    x.Available == 1);

                if (!assignmentIsValid)
                {
                    return new SubmissionValidationResult
                    {
                        ErrorMessage = "The course assignment is no longer active or does not belong to your position. Reload Learning and try again."
                    };
                }

                var hasDiagnostic = await _context.SyUserDiagnostics.AsNoTracking().AnyAsync(x =>
                    x.FkHeadcount == employee.PkHeadcount &&
                    x.FkTest == test.PkTest &&
                    x.Available == 1);

                if (!hasDiagnostic)
                    return new SubmissionValidationResult { ErrorMessage = TrainifyMessages.YouMustCompleteTheDiagnosticTestBeforeTaking };
            }

            return new SubmissionValidationResult { CanonicalModel = canonical };
        }

        public class TestViewModelResult
        {
            public SubmitTestViewModel? Model { get; init; }
            public string? ErrorMessage { get; init; }
        }

        public async Task<TestViewModelResult> GetDiagnosticViewModelAsync(
            int testId, int? courseId, int? levelId, int? courseAssignmentId)
        {
            var test = await _context.CtTests
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.PkTest == testId && t.Available == 1);

            if (test == null)
            {
                return new TestViewModelResult { ErrorMessage = TrainifyMessages.TestNotFoundOrNotAvailable };
            }

            var cId = courseId ?? test.FkCourse;
            var lId = levelId ?? test.FkLevelcourse;

            var model = await BuildSubmitTestViewModelAsync(test, cId, lId, courseAssignmentId);

            if (!model.Questions.Any())
            {
                return new TestViewModelResult { ErrorMessage = TrainifyMessages.TestHasNoQuestionsPleaseContactHr };
            }

            return new TestViewModelResult { Model = model };
        }

        public async Task<TestViewModelResult> GetExamViewModelAsync(
            int? testId, int courseId, int levelId, int? courseAssignmentId)
        {
            CtTest? test;

            if (testId.HasValue)
            {
                test = await _context.CtTests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.PkTest == testId.Value && t.Available == 1);
            }
            else
            {
                test = await _context.CtTests
                    .AsNoTracking()
                    .Where(t => t.Available == 1 && t.FkCourse == courseId && t.FkLevelcourse == levelId)
                    .OrderByDescending(t => t.PkTest)
                    .FirstOrDefaultAsync();
            }

            if (test == null)
            {
                return new TestViewModelResult { ErrorMessage = TrainifyMessages.ExamNotFoundOrNotAvailable };
            }

            var model = await BuildSubmitTestViewModelAsync(test, courseId, levelId, courseAssignmentId);

            if (!model.Questions.Any())
            {
                return new TestViewModelResult { ErrorMessage = TrainifyMessages.ExamHasNoQuestionsPleaseContactHr };
            }

            return new TestViewModelResult { Model = model };
        }

        public class DiagnosticSubmissionOutcome
        {
            public bool RedirectToLearning { get; init; }
            public bool Success { get; init; }
            public string? ErrorMessage { get; init; }
            public DiagnosticResultViewModel? Result { get; init; }
        }

        public async Task<DiagnosticSubmissionOutcome> SubmitDiagnosticAsync(SubmitTestViewModel model, string userName)
        {
            if (model?.Questions == null || !model.Questions.Any())
            {
                return new DiagnosticSubmissionOutcome
                {
                    RedirectToLearning = true,
                    ErrorMessage = TrainifyMessages.ThereAreNoAnswersToSubmit
                };
            }

            var hasAnySelection = model.Questions
                .SelectMany(q => q.Options ?? new List<SubmitOptionViewModel>())
                .Any(o => o.IsSelected);

            if (!hasAnySelection)
            {
                return new DiagnosticSubmissionOutcome { ErrorMessage = TrainifyMessages.SelectAtLeastOneOptionBeforeSubmitting };
            }

            var now = DateTime.Now;

            var userRow = await _context.AspNetUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (userRow == null || string.IsNullOrWhiteSpace(userRow.EmployeeNumber))
            {
                return new DiagnosticSubmissionOutcome { ErrorMessage = TrainifyMessages.UnableToResolveTheCurrentUsersEmployee };
            }

            if (!int.TryParse(userRow.EmployeeNumber.Trim(), out var controlNumber))
            {
                return new DiagnosticSubmissionOutcome { ErrorMessage = TrainifyMessages.EmployeeNumberIsNotAValidNumber };
            }

            var hc = await _context.SyHeadcounts
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.ControlNumber == controlNumber && h.Available == 1);

            if (hc == null)
            {
                return new DiagnosticSubmissionOutcome { ErrorMessage = TrainifyMessages.EmployeeHeadcountAssociatedWithTheCurrentUser };
            }

            var validation = await ValidateSubmissionAsync(model, hc, requireCourseAssignment: false);
            if (validation.CanonicalModel == null)
                return new DiagnosticSubmissionOutcome { ErrorMessage = validation.ErrorMessage };

            model = validation.CanonicalModel;

            if (model.Questions.Any(q => !(q.Options ?? new List<SubmitOptionViewModel>()).Any(o => o.IsSelected)))
                return new DiagnosticSubmissionOutcome { ErrorMessage = TrainifyMessages.AnswerEveryQuestionBeforeSubmitting };

            var questionIds = model.Questions.Select(q => q.FkQuestion).Distinct().ToList();

            var questionTextMap = await _context.CtQuestions
                .Where(qq => questionIds.Contains(qq.PkQuestions))
                .ToDictionaryAsync(qq => qq.PkQuestions, qq => qq.Question);

            var correctMap = await _context.CtOptions
                .Where(o => questionIds.Contains(o.FkQuestions) && o.Available == 1 && o.Answer == 1)
                .GroupBy(o => o.FkQuestions)
                .Select(g => new
                {
                    FkQuestion = g.Key,
                    Ids = g.Select(x => x.PkOptions).ToList(),
                    Csv = string.Join(",", g.OrderBy(x => x.PkOptions).Select(x => x.PkOptions))
                })
                .ToDictionaryAsync(x => x.FkQuestion, x => (Ids: x.Ids, Csv: x.Csv));

            var resultVm = new DiagnosticResultViewModel
            {
                FkTest = model.FkTest,
                TestName = model.TestName,
                NextCourseId = model.NextCourseId,
                NextLevelId = model.NextLevelId,
                Questions = new List<DiagnosticQuestionResultViewModel>(),
                CourseAssignmentId = model.CourseAssignmentId ?? 0
            };

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var codeExam = await GetNextDiagnosticCodeExamAsync();

                foreach (var q in model.Questions)
                {
                    var selectedIds = (q.Options ?? new List<SubmitOptionViewModel>())
                        .Where(o => o.IsSelected)
                        .Select(o => o.FkOption)
                        .OrderBy(id => id)
                        .ToList();

                    var csvSelected = string.Join(",", selectedIds);
                    var correctIds = correctMap.TryGetValue(q.FkQuestion, out var t1) ? t1.Ids : new List<int>();
                    var csvCorrect = correctMap.TryGetValue(q.FkQuestion, out var t2) ? t2.Csv : string.Empty;

                    _context.SyUserDiagnostics.Add(new SyUserDiagnostic
                    {
                        CodeExam = codeExam,
                        FkTest = model.FkTest,
                        FkQuestions = q.FkQuestion,
                        FkOptionSelected = csvSelected,
                        FkOptionCorrected = csvCorrect,
                        FkHeadcount = hc.PkHeadcount,
                        Createuser = userName,
                        Createdate = now,
                        Available = 1
                    });

                    var optionResults = (q.Options ?? new List<SubmitOptionViewModel>())
                        .Select(o => new DiagnosticOptionResultViewModel
                        {
                            FkOption = o.FkOption,
                            OptionText = o.OptionText,
                            IsSelected = selectedIds.Contains(o.FkOption),
                            IsCorrect = correctIds.Contains(o.FkOption)
                        })
                        .OrderBy(o => o.FkOption)
                        .ToList();

                    bool questionCorrect = IsAnswerCorrect(selectedIds, correctIds);

                    var safeQuestionText =
                        !string.IsNullOrWhiteSpace(q.QuestionText)
                            ? q.QuestionText
                            : (questionTextMap.TryGetValue(q.FkQuestion, out var qt) ? qt : string.Empty);

                    resultVm.Questions.Add(new DiagnosticQuestionResultViewModel
                    {
                        FkQuestion = q.FkQuestion,
                        QuestionText = safeQuestionText,
                        IsMultiple = q.IsMultiple,
                        SelectedOptionIds = selectedIds,
                        CorrectOptionIds = correctIds,
                        IsCorrect = questionCorrect,
                        Options = optionResults
                    });
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new DiagnosticSubmissionOutcome { ErrorMessage = string.Format(TrainifyMessages.ErrorSavingDiagnosticFormat, ex.Message) };
            }

            resultVm.TotalQuestions = resultVm.Questions.Count;
            resultVm.CorrectCount = resultVm.Questions.Count(x => x.IsCorrect);
            resultVm.Score = CalculateScore(resultVm.CorrectCount, resultVm.TotalQuestions);
            resultVm.HasMaterial = await ExistsMaterialAsync(resultVm.NextCourseId, resultVm.NextLevelId);

            return new DiagnosticSubmissionOutcome { Success = true, Result = resultVm };
        }

        public class ExamSubmissionOutcome
        {
            public bool RedirectToLearning { get; init; }
            public string? RedirectMessage { get; init; }
            public bool ReshowExamForm { get; init; }
            public string? ReshowMessage { get; init; }
            public DiagnosticResultViewModel? Result { get; init; }
            public string? ResultErrorMessage { get; init; }
        }

        public async Task<ExamSubmissionOutcome> SubmitExamAsync(SubmitTestViewModel model, string userName)
        {
            if (model?.Questions == null || !model.Questions.Any())
            {
                return new ExamSubmissionOutcome
                {
                    RedirectToLearning = true,
                    RedirectMessage = TrainifyMessages.ThereAreNoAnswersToSubmit
                };
            }

            var hasAnySelection = model.Questions
                .SelectMany(q => q.Options ?? new List<SubmitOptionViewModel>())
                .Any(o => o.IsSelected);

            if (!hasAnySelection)
            {
                return new ExamSubmissionOutcome
                {
                    ReshowExamForm = true,
                    ReshowMessage = TrainifyMessages.SelectAtLeastOneOptionBeforeSubmitting
                };
            }

            var now = DateTime.Now;

            var userRow = await _context.AspNetUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (userRow == null || string.IsNullOrWhiteSpace(userRow.EmployeeNumber))
            {
                return new ExamSubmissionOutcome
                {
                    ReshowExamForm = true,
                    ReshowMessage = TrainifyMessages.UnableToResolveTheCurrentUsersEmployee
                };
            }

            if (!int.TryParse(userRow.EmployeeNumber.Trim(), out var controlNumber))
            {
                return new ExamSubmissionOutcome
                {
                    ReshowExamForm = true,
                    ReshowMessage = TrainifyMessages.EmployeeNumberIsNotAValidNumber
                };
            }

            var hc = await _context.SyHeadcounts
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.ControlNumber == controlNumber && h.Available == 1);

            if (hc == null)
            {
                return new ExamSubmissionOutcome
                {
                    ReshowExamForm = true,
                    ReshowMessage = TrainifyMessages.EmployeeHeadcountAssociatedWithTheCurrentUser
                };
            }

            var validation = await ValidateSubmissionAsync(model, hc, requireCourseAssignment: true);
            if (validation.CanonicalModel == null)
            {
                return new ExamSubmissionOutcome
                {
                    ReshowExamForm = true,
                    ReshowMessage = validation.ErrorMessage
                };
            }

            model = validation.CanonicalModel;

            if (model.Questions.Any(q => !(q.Options ?? new List<SubmitOptionViewModel>()).Any(o => o.IsSelected)))
            {
                return new ExamSubmissionOutcome
                {
                    ReshowExamForm = true,
                    ReshowMessage = TrainifyMessages.AnswerEveryQuestionBeforeSubmitting
                };
            }

            var questionIds = model.Questions.Select(q => q.FkQuestion).Distinct().ToList();

            var questionTextMap = await _context.CtQuestions
                .Where(qq => questionIds.Contains(qq.PkQuestions))
                .ToDictionaryAsync(qq => qq.PkQuestions, qq => qq.Question);

            var correctMap = await _context.CtOptions
                .Where(o => questionIds.Contains(o.FkQuestions) && o.Available == 1 && o.Answer == 1)
                .GroupBy(o => o.FkQuestions)
                .Select(g => new
                {
                    FkQuestion = g.Key,
                    Ids = g.Select(x => x.PkOptions).ToList(),
                    Csv = string.Join(",", g.OrderBy(x => x.PkOptions).Select(x => x.PkOptions))
                })
                .ToDictionaryAsync(x => x.FkQuestion, x => (Ids: x.Ids, Csv: x.Csv));

            var resultVm = new DiagnosticResultViewModel
            {
                FkTest = model.FkTest,
                TestName = model.TestName,
                NextCourseId = model.NextCourseId,
                NextLevelId = model.NextLevelId,
                Questions = new List<DiagnosticQuestionResultViewModel>()
            };

            foreach (var q in model.Questions)
            {
                var selectedIds = (q.Options ?? new List<SubmitOptionViewModel>())
                    .Where(o => o.IsSelected)
                    .Select(o => o.FkOption)
                    .OrderBy(id => id)
                    .ToList();

                var correctIds = correctMap.TryGetValue(q.FkQuestion, out var t1) ? t1.Ids : new List<int>();

                bool questionCorrect = IsAnswerCorrect(selectedIds, correctIds);

                var optionResults = (q.Options ?? new List<SubmitOptionViewModel>())
                    .Select(o => new DiagnosticOptionResultViewModel
                    {
                        FkOption = o.FkOption,
                        OptionText = o.OptionText,
                        IsSelected = selectedIds.Contains(o.FkOption),
                        IsCorrect = correctIds.Contains(o.FkOption)
                    })
                    .OrderBy(o => o.FkOption)
                    .ToList();

                var safeQuestionText =
                    !string.IsNullOrWhiteSpace(q.QuestionText)
                        ? q.QuestionText
                        : (questionTextMap.TryGetValue(q.FkQuestion, out var qt) ? qt : string.Empty);

                resultVm.Questions.Add(new DiagnosticQuestionResultViewModel
                {
                    FkQuestion = q.FkQuestion,
                    QuestionText = safeQuestionText,
                    IsMultiple = q.IsMultiple,
                    SelectedOptionIds = selectedIds,
                    CorrectOptionIds = correctIds,
                    IsCorrect = questionCorrect,
                    Options = optionResults
                });
            }

            resultVm.TotalQuestions = resultVm.Questions.Count;
            resultVm.CorrectCount = resultVm.Questions.Count(x => x.IsCorrect);
            resultVm.Score = CalculateScore(resultVm.CorrectCount, resultVm.TotalQuestions);

            var passed = IsPassingScore(resultVm.Score);
            var assignmentId = model.CourseAssignmentId!.Value; // validated by ValidateSubmissionAsync

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Each final-exam attempt receives its own code. Reusing the diagnostic code
                // would merge retries in the reporting views and could report a wrong score.
                var sourceDiagnosticCode = await GetLastDiagnosticCodeExamAsync(hc.PkHeadcount, model.FkTest);
                var sourceDiagnostics = await _context.SyUserDiagnostics
                    .AsNoTracking()
                    .Where(x => x.CodeExam == sourceDiagnosticCode &&
                                x.FkHeadcount == hc.PkHeadcount &&
                                x.FkTest == model.FkTest &&
                                x.Available == 1)
                    .ToListAsync();

                if (sourceDiagnosticCode <= 0 || sourceDiagnostics.Count == 0)
                    throw new InvalidOperationException(TrainifyMessages.YouMustCompleteTheDiagnosticTestBeforeTaking);

                var codeExam = await GetNextDiagnosticCodeExamAsync();

                // Copy the diagnostic snapshot so the existing evidence/report screens can
                // review this attempt by its unique CodeExam without mixing it with retries.
                _context.SyUserDiagnostics.AddRange(sourceDiagnostics.Select(x => new SyUserDiagnostic
                {
                    CodeExam = codeExam,
                    FkTest = x.FkTest,
                    FkQuestions = x.FkQuestions,
                    FkOptionSelected = x.FkOptionSelected,
                    FkOptionCorrected = x.FkOptionCorrected,
                    FkHeadcount = x.FkHeadcount,
                    Createuser = userName,
                    Createdate = now,
                    Available = 1
                }));

                foreach (var q in resultVm.Questions)
                {
                    var csvSelected = string.Join(",", q.SelectedOptionIds ?? new List<int>());
                    var csvCorrect = string.Join(",", q.CorrectOptionIds ?? new List<int>());

                    _context.SyUserAnswers.Add(new SyUserAnswer
                    {
                        CodeExam = codeExam,
                        FkTest = model.FkTest,
                        FkQuestions = q.FkQuestion,
                        FkOptionSelected = csvSelected,
                        FkOptionCorrected = csvCorrect,
                        FkHeadcount = hc.PkHeadcount,
                        Createuser = userName,
                        Createdate = now,
                        Available = 1
                    });
                }

                _context.SyCoursemovements.Add(new SyCoursemovement
                {
                    CodeExam = codeExam,
                    FkCourseAssignment = assignmentId,
                    FkCourseStatus = CourseStatusForScore(resultVm.Score), // COMPLETED / PENDING
                    FkDeliveryMode = 1,
                    FkHeadcount = hc.PkHeadcount,
                    Score = resultVm.Score,
                    CreateUser = userName,
                    CreateDate = now,
                    LastUpdateUser = userName,
                    LastUpdateDate = now,
                    Avaialble = 1
                });

                // A failed attempt is evidence, but it must never mark the course completed.
                if (passed)
                {
                    var existingCompleted = await _context.SyCoursecompleteds
                        .FirstOrDefaultAsync(c =>
                            c.FkCourseAssignment == assignmentId &&
                            c.FkHeadcount == hc.PkHeadcount &&
                            c.Avaialble == 1);

                    if (existingCompleted != null)
                    {
                        existingCompleted.FkCourseStatus = 1;
                        existingCompleted.FkDeliveryMode = 1;
                        existingCompleted.Score = resultVm.Score;
                        existingCompleted.LastUpdateUser = userName;
                        existingCompleted.LastUpdateDate = now;
                    }
                    else
                    {
                        _context.SyCoursecompleteds.Add(new SyCoursecompleted
                        {
                            FkCourseAssignment = assignmentId,
                            FkCourseStatus = 1,
                            FkDeliveryMode = 1,
                            FkHeadcount = hc.PkHeadcount,
                            Score = resultVm.Score,
                            CreateUser = userName,
                            CreateDate = now,
                            LastUpdateUser = userName,
                            LastUpdateDate = now,
                            Avaialble = 1
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return new ExamSubmissionOutcome
                {
                    ReshowExamForm = true,
                    ReshowMessage = string.Format(TrainifyMessages.ErrorSavingExamFormat, ex.Message)
                };
            }

            resultVm.HasMaterial = await ExistsMaterialAsync(resultVm.NextCourseId, resultVm.NextLevelId);

            return new ExamSubmissionOutcome
            {
                Result = resultVm,
                ResultErrorMessage = passed
                    ? null
                    : string.Format(TrainifyMessages.MinimumScoreAttemptRecordedFormat, PassingScore)
            };
        }
    }
}
