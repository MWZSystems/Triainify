using BootstrapBlazor.Components;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.UserTestEvidence;
using RH_CM.Service.SQLSMS;
using RH_CM.Service.Trainify;
using RH_CM.ViewModels;
using System.Data;
using System.Security.Claims;
using static RH_CM.ViewModels.ViewModels;
using RH_CM.Messages.UserTestEvidence;

namespace RH_CM.Service.UserTestEvidence
{
    public class UserTestEvidenceService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly db_abcd61_rhchdbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserTestEvidenceService(UnitOfWork unitOfWork,
                                        db_abcd61_rhchdbContext context,
                                        UserManager<IdentityUser> userManager,
                                        IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        private ClaimsPrincipal? UserPrincipal => _httpContextAccessor.HttpContext?.User;

        public async Task<IdentityUser?> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(UserPrincipal);
        }

        public async Task<IList<string>> GetUserRolesAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Array.Empty<string>();
            }
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<List<UserDTOs>> GetIndexAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return new List<UserDTOs>();
            }

            var roles = await _userManager.GetRolesAsync(user);

            List<UserDTOs> users = await _unitOfWork.ExecuteStoredProcedureToListAsync<UserDTOs>("[sp_UserTestEvidenceService_Index_Get]");

            if (roles.Contains("Administrador"))
            {
                //If its an Admin brings everything
                return users;
            }

            // Non-Admin user: they can only see their own history.
            string query = "SELECT TOP 1 EmployeeNumber FROM [AspNetUsers] WHERE Ntuser = @pNtuser";
            string controlNumber = await _unitOfWork.QuerySingleScalarAsync(query, new Dictionary<string, object>
            {
                { "@pNtuser", user.UserName ?? string.Empty }
            });

            if (!int.TryParse(controlNumber, out int parsedControlNumber))
            {
                // No matching AspNetUsers record for this account — nothing to show instead of crashing.
                return new List<UserDTOs>();
            }

            return users.Where(u => u.ControlNumber == parsedControlNumber).ToList();
        }

        public async Task<DetailDTOs> GetDetailUserAsync(string ControlNumber)
        {

            DetailDTOs detailDTOs = new();

            var parameters = new Dictionary<string, object>
            {
                { "@pControlNumber", ControlNumber }
            };

            List<DetailUserExamDTOs> userList = await _unitOfWork.ExecuteStoredProcedureToListAsync<DetailUserExamDTOs>("[sp_UserTestEvidenceService_Detail_Get]", parameters);

            detailDTOs.Details = userList;

            detailDTOs.ControlNumber = ControlNumber;

            if (!int.TryParse(ControlNumber, out int parsedControlNumber))
            {
                return detailDTOs;
            }

            detailDTOs.FullName = await _context.SyHeadcounts
                                    .Where(h => h.ControlNumber == parsedControlNumber)
                                    .Select(h =>
                                        h.Names
                                        + " " + (h.LastName ?? "")
                                        + " " + (h.SecondName ?? "")
                                    )
                                    .FirstOrDefaultAsync();

            detailDTOs.Position = await _context.SyHeadcounts
                            .Where(h => h.ControlNumber == parsedControlNumber)
                            .Join(
                                _context.CtPositions,
                                h => h.FkPosition,
                                p => p.PkPosition,
                                (h, p) => p.NamePosition
                            )
                            .FirstOrDefaultAsync();

            return detailDTOs;
        }

        public async Task<FullExamDTOs?> GetDiagnosticExamAsync(int examID, int controlNumber)
        {
            int fk_headcount = await _context.SyHeadcounts
                .Where(h => h.ControlNumber == controlNumber)
                .Select(h => h.PkHeadcount)
                .FirstOrDefaultAsync();

            if (fk_headcount == 0)
                return null;
            
            int ControlNumber = await _context.SyHeadcounts
                .Where(hc => hc.PkHeadcount == fk_headcount)
                .Select(hc => hc.ControlNumber).FirstOrDefaultAsync();


            int PkTest = await _context.SyUserAnswers
                                    .Where(ua => ua.CodeExam == examID && ua.FkHeadcount == fk_headcount)
                                    .Select(ua => ua.FkTest)
                                    .FirstOrDefaultAsync();

            CtTest? test = await _context.CtTests
                                     .SingleOrDefaultAsync(t => t.PkTest == PkTest);


            if (test == null)
            {
                // No diagnostic exam found for this examID (bad/stale link) — nothing to show.
                return null;
            }

            List<SyUserDiagnostic> userDiagnostic = await _context.SyUserDiagnostics
                                                         .Where(ud => ud.CodeExam == examID && ud.FkHeadcount == fk_headcount)
                                                         .ToListAsync();

            List<CtQuestion> questionList = await _context.CtQuestions
                                                       .Where(q => _context.SyUserDiagnostics
                                                        .Where(d => d.CodeExam == examID && d.FkHeadcount == fk_headcount)
                                                        .Select(d => d.FkQuestions)
                                                        .Contains(q.PkQuestions))
                                                    .ToListAsync();

            var query = from q in _context.CtQuestions
                        join o in _context.CtOptions
                            on q.PkQuestions equals o.FkQuestions into optionsGroup
                        from o in optionsGroup.DefaultIfEmpty() // LEFT JOIN
                        where _context.SyUserDiagnostics
                                      .Where(d => d.CodeExam == examID && d.FkHeadcount == fk_headcount)
                                      .Select(d => d.FkQuestions)
                                      .Contains(q.PkQuestions)
                        select new
                        {
                            Question = q,
                            Option = o
                        };

            int totalQuestions = await _context.SyUserDiagnostics
                                .Where(d => d.CodeExam == examID && d.FkHeadcount == fk_headcount)
                                .CountAsync();

            int correctAnswers = await _context.SyUserDiagnostics
                                    .Where(d => d.CodeExam == examID && d.FkHeadcount == fk_headcount && d.FkOptionSelected == d.FkOptionCorrected)
                                    .CountAsync();

            // The join of all the exam's questions/options is materialized once,
            // instead of repeating the query for every question inside the foreach below.
            var rows = await query.ToListAsync();
            Dictionary<int, List<CtOption>> optionsByQuestion =
                GroupOptionsByQuestion(rows.Select(x => (x.Question, x.Option)));

            var model = new FullExamDTOs
            {
                TestName = "Diagnostic " + test.TestName,
                ControlNumber = ControlNumber,
                Score = DiagnosticExamService.CalculateScore(correctAnswers, totalQuestions),
                CorrectCount = correctAnswers,
                TotalQuestions = totalQuestions,
                HasMaterial = true,
                NextCourseId = 101,
                NextLevelId = 2,
                Questions = new List<DiagnosticQuestion>()
            };
            
            foreach (var q in userDiagnostic)
            {
                bool correctOrNot = false;

                if (q.FkOptionSelected == q.FkOptionCorrected)
                {
                    correctOrNot = true;
                }

                List<int> selectedIds = q.FkOptionSelected.Split(',',StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                List<int> correctIds = q.FkOptionCorrected.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();


                List<DiagnosticOption> optionResults = new List<DiagnosticOption>();

                var options = optionsByQuestion.TryGetValue(q.FkQuestions, out var opts)
                    ? opts
                    : new List<CtOption>();

                foreach (var opt in options)
                {
                    bool elegido = selectedIds.Contains(opt.PkOptions);

                    optionResults.Add(new DiagnosticOption
                    {
                        OptionId = opt.PkOptions,
                        OptionText = opt.Options,
                        IsCorrect = Convert.ToBoolean(opt.Answer),
                        IsSelected = elegido
                    });
                }

                model.Questions.Add(new DiagnosticQuestion
                    {
                        QuestionText = questionList.FirstOrDefault(d => d.PkQuestions == q.FkQuestions)?.Question ?? string.Empty,
                        IsCorrect = correctOrNot,
                        SelectedOptionIds = selectedIds,
                        CorrectOptionIds = correctIds,

                        Options = optionResults
                    });
            }

            return model;
        }

        public async Task<FullExamDTOs?> GetExamAsync(int examID, int controlNumber)
        {
            int fk_headcount = await _context.SyHeadcounts
                .Where(h => h.ControlNumber == controlNumber)
                .Select(h => h.PkHeadcount)
                .FirstOrDefaultAsync();

            if (fk_headcount == 0)
                return null;

            int ControlNumber = await _context.SyHeadcounts
                .Where(hc => hc.PkHeadcount == fk_headcount)
                .Select(hc => hc.ControlNumber).FirstOrDefaultAsync();


            int PkTest = await _context.SyUserAnswers
                                    .Where(ua => ua.CodeExam == examID && ua.FkHeadcount == fk_headcount)
                                    .Select(ua => ua.FkTest)
                                    .FirstOrDefaultAsync();

            CtTest? test = await _context.CtTests
                                     .SingleOrDefaultAsync(t => t.PkTest == PkTest);


            if (test == null)
            {
                // No exam found for this examID (bad/stale link) — nothing to show.
                return null;
            }

            List<SyUserAnswer> userAnswer = await _context.SyUserAnswers
                                                         .Where(ud => ud.CodeExam == examID && ud.FkHeadcount == fk_headcount)
                                                         .ToListAsync();

            List<CtQuestion> questionList = await _context.CtQuestions
                                                       .Where(q => _context.SyUserAnswers
                                                        .Where(d => d.CodeExam == examID && d.FkHeadcount == fk_headcount)
                                                        .Select(d => d.FkQuestions)
                                                        .Contains(q.PkQuestions))
                                                    .ToListAsync();

            var query = from q in _context.CtQuestions
                        join o in _context.CtOptions
                            on q.PkQuestions equals o.FkQuestions into optionsGroup
                        from o in optionsGroup.DefaultIfEmpty() // LEFT JOIN
                        where _context.SyUserAnswers
                                      .Where(d => d.CodeExam == examID && d.FkHeadcount == fk_headcount)
                                      .Select(d => d.FkQuestions)
                                      .Contains(q.PkQuestions)
                        select new
                        {
                            Question = q,
                            Option = o
                        };

            int totalQuestions = await _context.SyUserAnswers
                                .Where(d => d.CodeExam == examID && d.FkHeadcount == fk_headcount)
                                .CountAsync();

            int correctAnswers = await _context.SyUserAnswers
                                    .Where(d => d.CodeExam == examID && d.FkHeadcount == fk_headcount && d.FkOptionSelected == d.FkOptionCorrected)
                                    .CountAsync();

            // The join of all the exam's questions/options is materialized once,
            // instead of repeating the query for every question inside the foreach below.
            var rows = await query.ToListAsync();
            Dictionary<int, List<CtOption>> optionsByQuestion =
                GroupOptionsByQuestion(rows.Select(x => (x.Question, x.Option)));

            var model = new FullExamDTOs
            {
                TestName = test.TestName,
                ControlNumber = ControlNumber,
                Score = DiagnosticExamService.CalculateScore(correctAnswers, totalQuestions),
                CorrectCount = correctAnswers,
                TotalQuestions = totalQuestions,
                HasMaterial = true,
                NextCourseId = 101,
                NextLevelId = 2,
                Questions = new List<DiagnosticQuestion>()
            };

            foreach (var q in userAnswer)
            {
                bool correctOrNot = false;

                if (q.FkOptionSelected == q.FkOptionCorrected)
                {
                    correctOrNot = true;
                }

                List<int> selectedIds = q.FkOptionSelected.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                List<int> correctIds = q.FkOptionCorrected.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();


                List<DiagnosticOption> optionResults = new List<DiagnosticOption>();

                var options = optionsByQuestion.TryGetValue(q.FkQuestions, out var opts)
                    ? opts
                    : new List<CtOption>();

                foreach (var opt in options)
                {
                    bool elegido = selectedIds.Contains(opt.PkOptions);

                    optionResults.Add(new DiagnosticOption
                    {
                        OptionId = opt.PkOptions,
                        OptionText = opt.Options,
                        IsCorrect = Convert.ToBoolean(opt.Answer),
                        IsSelected = elegido
                    });
                }

                model.Questions.Add(new DiagnosticQuestion
                {
                    QuestionText = questionList.FirstOrDefault(d => d.PkQuestions == q.FkQuestions)?.Question ?? string.Empty,
                    IsCorrect = correctOrNot,
                    SelectedOptionIds = selectedIds,
                    CorrectOptionIds = correctIds,

                    Options = optionResults
                });
            }

            return model;
        }

        /// <summary>
        /// Groups a Question-Option LEFT JOIN's rows by PkQuestions, discarding rows with no option.
        /// </summary>
        public static Dictionary<int, List<CtOption>> GroupOptionsByQuestion(
            IEnumerable<(CtQuestion Question, CtOption Option)> rows)
        {
            return rows
                .Where(x => x.Option != null)
                .GroupBy(x => x.Question.PkQuestions)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Option).ToList());
        }

        public async Task<ServiceAnswer> DeleteExamAsync(int ExamID, int controlNumber)
        {
            var headcountId = await _context.SyHeadcounts
                .Where(x => x.ControlNumber == controlNumber)
                .Select(x => x.PkHeadcount)
                .FirstOrDefaultAsync();

            if (headcountId == 0)
                return new ServiceAnswer(false, ServiceAnswer.MessageType_Error, UserTestEvidenceMessages.ExamNotFound);

            var movements = await _context.SyCoursemovements
                .Where(x => x.CodeExam == ExamID && x.FkHeadcount == headcountId)
                .ToListAsync();

            if (movements.Count == 0)
            {
                return new ServiceAnswer(false, ServiceAnswer.MessageType_Error, UserTestEvidenceMessages.ExamNotFound);
            }

            var assignmentId = movements[0].FkCourseAssignment;

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var answers = await _context.SyUserAnswers.Where(x => x.CodeExam == ExamID && x.FkHeadcount == headcountId).ToListAsync();
                var diagnostics = await _context.SyUserDiagnostics.Where(x => x.CodeExam == ExamID && x.FkHeadcount == headcountId).ToListAsync();

                _context.SyUserAnswers.RemoveRange(answers);
                _context.SyUserDiagnostics.RemoveRange(diagnostics);
                _context.SyCoursemovements.RemoveRange(movements);

                // Only a genuinely completed movement may back the completion summary.
                // A PENDING/failed retry must never make a course appear completed.
                var previousCompletion = await _context.SyCoursemovements
                    .AsNoTracking()
                    .Where(x => x.CodeExam != ExamID &&
                                x.FkCourseAssignment == assignmentId &&
                                x.FkHeadcount == headcountId &&
                                x.FkCourseStatus == 1 &&
                                x.Avaialble == 1)
                    .OrderByDescending(x => x.CreateDate)
                    .ThenByDescending(x => x.PkMovementCourse)
                    .FirstOrDefaultAsync();

                var completionRows = await _context.SyCoursecompleteds
                    .Where(x => x.FkCourseAssignment == assignmentId && x.FkHeadcount == headcountId)
                    .ToListAsync();

                if (previousCompletion == null)
                {
                    _context.SyCoursecompleteds.RemoveRange(completionRows);
                }
                else
                {
                    var completion = completionRows.FirstOrDefault();
                    if (completion == null)
                    {
                        completion = new SyCoursecompleted
                        {
                            FkCourseAssignment = assignmentId,
                            FkHeadcount = headcountId,
                            CreateUser = previousCompletion.CreateUser,
                            CreateDate = previousCompletion.CreateDate
                        };
                        _context.SyCoursecompleteds.Add(completion);
                    }

                    completion.FkCourseStatus = 1;
                    completion.FkDeliveryMode = previousCompletion.FkDeliveryMode;
                    completion.Score = previousCompletion.Score;
                    completion.LastUpdateUser = previousCompletion.LastUpdateUser;
                    completion.LastUpdateDate = previousCompletion.LastUpdateDate;
                    completion.Avaialble = 1;

                    if (completionRows.Count > 1)
                        _context.SyCoursecompleteds.RemoveRange(completionRows.Skip(1));
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return new ServiceAnswer(true, ServiceAnswer.MessageType_Success, UserTestEvidenceMessages.ExamsSuccessfullyDeleted);
            }
            catch
            {
                await transaction.RollbackAsync();
                return new ServiceAnswer(false, ServiceAnswer.MessageType_Error, UserTestEvidenceMessages.ExamCouldNotBeDeleted);
            }
        }

        public async Task<ExcelExportDTOs> ExportExcelAsync(int ControlNumber)
        {
            ExcelExportDTOs excelExport = new();

            DetailDTOs detailDTOs = await GetDetailUserAsync(ControlNumber.ToString());

            excelExport.File = ObjetctListToStream(detailDTOs);

            excelExport.FullName = detailDTOs.FullName;

            excelExport.ControlNumber = ControlNumber;



            return excelExport;
        }

        public async Task<ExcelExportDTOs> ExportExcelAsync()
        {
            // Added at Emmanuel's request, to avoid touching the DB further — query left as-is. fmarquez
            string query = @"	   	   SELECT 
			CAST(HC.CONTROL_NUMBER AS VARCHAR(MAX))							[ControlNumber]
			,HC.NAMES + ' ' + HC.LAST_NAME + ' ' + HC.SECOND_NAME  AS		[FullName]
			,P.NAME_POSITION_ENGLISH										[Position]
			,C.CourseName													[Course]
			,LC.DESCRIPCTION_LEVEL											[Level]
			,DM.DESCRIPTION_DELIVERYMODE									[Delivery]
			, COALESCE(CAST(ROUND(UD.Score, 2) AS VARCHAR(100)), 'NA') AS	[DiagnosticScore]
			, COALESCE(CAST(ROUND(UA.Score, 2) AS VARCHAR(100)), 'NA') AS	[FinalExamScore]
			,CAST(CM.CreateDate	AS VARCHAR(100)) AS							[CompletedDate]
			,COALESCE(CAST(UA.[CODE_EXAM]	AS VARCHAR(100)),'NA')			[ExamsID]
			,HC.CONTROL_NUMBER												[ControlNumber]
			,CM.PK_MovementCourse											[PkCourseMovement]
	   FROM SY_COURSEMOVEMENTS CM
			LEFT JOIN SY_HEADCOUNT HC ON HC.PK_HEADCOUNT = CM.FK_Headcount
			LEFT JOIN CT_COURSEASSIGNMENTS CA ON Ca.PK_CourseAssignment = CM.FK_CourseAssignment
			LEFT JOIN CT_COURSE C ON C.PK_Course = CA.FK_Course
			LEFT JOIN CT_LEVELCOURSE LC ON LC.PK_LEVELCOURSE = CA.FK_RequiredCourseLevels
			LEFT JOIN CT_DELIVERYMODE DM ON DM.PK_DELIVERYMODE = CA.FK_DeliveryMode
			LEFT JOIN [vw_UserAnswersResume] UA ON UA.[CODE_EXAM] = CM.CODE_EXAM
			LEFT JOIN [vw_UserDiagnosticResume] UD ON UD.[CODE_EXAM] = CM.CODE_EXAM
			LEFT JOIN CT_POSITION P ON P.PK_POSITION = HC.FK_POSITION
		WHERE (CM.Avaialble = 1
	   AND CA.Available = 1
	   AND C.Available = 1
	   AND DM.Available = 1
	   AND LC.Available = 1
	   ANd HC.AVAILABLE = 1
	   )
	   --ANd HC.CONTROL_NUMBER = @pControlNumber
	   ORDER BY CM.CreateDate DESC";

            List<DetailMassiveExamDTOs> userList = await _unitOfWork.QueryListAsync<DetailMassiveExamDTOs>(query);

            ExcelExportDTOs excelExport = new();

            excelExport.File = ObjetctListToStream(userList);

            return excelExport;
        }


        /// <summary>
        /// Builds an Excel file from the exam detail rows.
        /// </summary>
        private byte[] ObjetctListToStream(List<DetailMassiveExamDTOs> items)
        {
            //Crear Excel
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("UserExamsEvidences");

            // Encabezados
            ws.Cell(1, 1).Value = "Control Number";
            ws.Cell(1, 2).Value = "Full Name";
            ws.Cell(1, 3).Value = "Position Name";
            ws.Cell(1, 4).Value = "Course";
            ws.Cell(1, 5).Value = "Level";
            ws.Cell(1, 6).Value = "Delivery";
            ws.Cell(1, 7).Value = "Diagnostic Score";
            ws.Cell(1, 8).Value = "Final Score";
            ws.Cell(1, 9).Value = "Completed Date";
            ws.Cell(1, 10).Value = "ExamID";

            var header = ws.Range("A1:J1");
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGreen;

            // 2) Datos
            int row = 2;
            foreach (var linea in items)
            {
                ws.Cell(row, 1).Value = linea.ControlNumber ?? string.Empty;
                ws.Cell(row, 2).Value = linea.FullName ?? string.Empty;
                ws.Cell(row, 3).Value = linea.Position ?? string.Empty;
                ws.Cell(row, 4).Value = linea.Course ?? string.Empty;
                ws.Cell(row, 5).Value = linea.Level ?? string.Empty;
                ws.Cell(row, 6).Value = linea.Delivery ?? string.Empty;
                ws.Cell(row, 7).Value = linea.DiagnosticScore ?? string.Empty;
                ws.Cell(row, 8).Value = linea.FinalExamScore ?? string.Empty;
                ws.Cell(row, 9).Value = linea.CompletedDate ?? string.Empty;
                ws.Cell(row, 10).Value = linea.ExamsID ?? string.Empty;
                row++;
            }

            // 3) Estilos y formato
            int lastRow = row - 1;
            var dataRange = ws.Range(1, 1, lastRow, 10);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dataRange.SetAutoFilter();

            // Ajuste de columnas y congelar encabezado
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            // 4) a excel.
            //string fechaActual = DateTime.Now.ToString("yyyyMMdd");
            using var stream = new MemoryStream();
            wb.SaveAs(stream);


            return stream.ToArray();
        }


        private byte[] ObjetctListToStream(DetailDTOs items)
        {
            //Crear Excel
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("UserExamsEvidences");

            // Encabezados
            ws.Cell(1, 1).Value = "Control Number";
            ws.Cell(1, 2).Value = "Full Name";
            ws.Cell(1, 3).Value = "Position Name";
            ws.Cell(1, 4).Value = "Course";
            ws.Cell(1, 5).Value = "Level";
            ws.Cell(1, 6).Value = "Delivery";
            ws.Cell(1, 7).Value = "Diagnostic Score";
            ws.Cell(1, 8).Value = "Final Score";
            ws.Cell(1, 9).Value = "Completed Date";
            ws.Cell(1, 10).Value = "ExamID";

            var header = ws.Range("A1:J1");
            header.Style.Font.Bold = true;
            header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            header.Style.Fill.BackgroundColor = XLColor.LightGreen;

            // 2) Datos
            int row = 2;
            foreach (var linea in items.Details ?? Enumerable.Empty<DetailUserExamDTOs>())
            {
                ws.Cell(row, 1).Value = items.ControlNumber ?? string.Empty;
                ws.Cell(row, 2).Value = items.FullName ?? string.Empty;
                ws.Cell(row, 3).Value = items.Position ?? string.Empty;
                ws.Cell(row, 4).Value = linea.Course ?? string.Empty;
                ws.Cell(row, 5).Value = linea.Level ?? string.Empty;
                ws.Cell(row, 6).Value = linea.Delivery ?? string.Empty;
                ws.Cell(row, 7).Value = linea.DiagnosticScore ?? string.Empty;
                ws.Cell(row, 8).Value = linea.FinalExamScore ?? string.Empty;
                ws.Cell(row, 9).Value = linea.CompletedDate ?? string.Empty;
                ws.Cell(row, 10).Value = linea.ExamsID ?? string.Empty;
                row++;
            }

            // 3) Estilos y formato
            int lastRow = row - 1;
            var dataRange = ws.Range(1, 1, lastRow, 10);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            dataRange.SetAutoFilter();

            // Ajuste de columnas y congelar encabezado
            ws.Columns().AdjustToContents();
            ws.SheetView.FreezeRows(1);

            // 4) a excel.
            //string fechaActual = DateTime.Now.ToString("yyyyMMdd");
            using var stream = new MemoryStream();
            wb.SaveAs(stream);


            return stream.ToArray();
        }


    }
}
