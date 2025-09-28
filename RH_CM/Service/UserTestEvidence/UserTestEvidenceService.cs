using BootstrapBlazor.Components;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.UserTestEvidence;
using RH_CM.Service.SQLSMS;
using RH_CM.ViewModels;
using System.Data;
using static RH_CM.ViewModels.ViewModels;

namespace RH_CM.Service.UserTestEvidence
{
    public class UserTestEvidenceService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly db_abcd61_rhchdbContext _context;

        public UserTestEvidenceService(UnitOfWork unitOfWork,
                                        db_abcd61_rhchdbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<List<UserDTOs>> GetIndexAsync()
        {

            List<UserDTOs> user = await _unitOfWork.ExecuteStoredProcedureToListAsync<UserDTOs>("[sp_UserTestEvidenceService_Index_Get]");

            return user;
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

            detailDTOs.FullName = await _context.SyHeadcounts
                                    .Where(h => h.ControlNumber == int.Parse(ControlNumber))
                                    .Select(h =>
                                        h.Names
                                        + " " + (h.LastName ?? "")
                                        + " " + (h.SecondName ?? "")
                                    )
                                    .FirstOrDefaultAsync();

            detailDTOs.Position = await _context.SyHeadcounts
                            .Where(h => h.ControlNumber == int.Parse(ControlNumber))
                            .Join(
                                _context.CtPositions,
                                h => h.FkPosition,
                                p => p.PkPosition,
                                (h, p) => p.NamePosition
                            )
                            .FirstOrDefaultAsync();

            return detailDTOs;
        }

        public async Task<FullExamDTOs> GetDiagnosticExamAsync(int examID)
        {
            int fk_headcount = await _context.SyUserAnswers
                .Where(ua => ua.CodeExam == examID)
                .Select(ua => ua.FkHeadcount).FirstOrDefaultAsync();
            
            int ControlNumber = await _context.SyHeadcounts
                .Where(hc => hc.PkHeadcount == fk_headcount)
                .Select(hc => hc.ControlNumber).FirstOrDefaultAsync();


            int PkTest = await _context.SyUserAnswers
                                    .Where(ua => ua.CodeExam == examID)
                                    .Select(ua => ua.FkTest)
                                    .FirstOrDefaultAsync();

            CtTest test = await _context.CtTests
                                     .SingleOrDefaultAsync(t => t.PkTest == PkTest);


            VwUserDiagnosticResume diagnosticResume = await _context.VwUserDiagnosticResumes
                                                         .Where(dr => dr.CodeExam == examID)
                                                         .FirstOrDefaultAsync();


            List<SyUserDiagnostic> userDiagnostic = await _context.SyUserDiagnostics
                                                         .Where(ud => ud.CodeExam == examID)
                                                         .ToListAsync();

            List<CtQuestion> questionList = await _context.CtQuestions
                                                       .Where(q => _context.SyUserDiagnostics
                                                        .Where(d => d.CodeExam == examID)
                                                        .Select(d => d.FkQuestions)
                                                        .Contains(q.PkQuestions))
                                                    .ToListAsync();

            var query = from q in _context.CtQuestions
                        join o in _context.CtOptions
                            on q.PkQuestions equals o.FkQuestions into optionsGroup
                        from o in optionsGroup.DefaultIfEmpty() // LEFT JOIN
                        where _context.SyUserDiagnostics
                                      .Where(d => d.CodeExam == examID)
                                      .Select(d => d.FkQuestions)
                                      .Contains(q.PkQuestions)
                        select new
                        {
                            Question = q,
                            Option = o
                        };

            int totalQuestions = await _context.SyUserDiagnostics
                                .Where(d => d.CodeExam == examID)
                                .CountAsync();

            int correctAnswers = await _context.SyUserDiagnostics
                                    .Where(d => d.CodeExam == examID && d.FkOptionSelected == d.FkOptionCorrected)
                                    .CountAsync();

            var model = new FullExamDTOs
            {
                TestName = "Diagnostic " + test.TestName,
                ControlNumber = ControlNumber,
                Score = Math.Round((double)diagnosticResume.Score, 2),
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

                var options = await query
                        .Where(x => x.Question.PkQuestions == q.FkQuestions)
                        .Select(x => x.Option)
                        .ToListAsync();

                foreach (var opt in options.Where(o => o != null))
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
                        QuestionText = questionList.FirstOrDefault(d => d.PkQuestions == q.FkQuestions)?.Question,
                        IsCorrect = correctOrNot,
                        SelectedOptionIds = selectedIds,
                        CorrectOptionIds = correctIds,

                        Options = optionResults
                    });
            }

            return model;
        }

        public async Task<FullExamDTOs> GetExamAsync(int examID)
        {
            int fk_headcount = await _context.SyUserAnswers
                .Where(ua => ua.CodeExam == examID)
                .Select(ua => ua.FkHeadcount).FirstOrDefaultAsync();

            int ControlNumber = await _context.SyHeadcounts
                .Where(hc => hc.PkHeadcount == fk_headcount)
                .Select(hc => hc.ControlNumber).FirstOrDefaultAsync();


            int PkTest = await _context.SyUserAnswers
                                    .Where(ua => ua.CodeExam == examID)
                                    .Select(ua => ua.FkTest)
                                    .FirstOrDefaultAsync();

            CtTest test = await _context.CtTests
                                     .SingleOrDefaultAsync(t => t.PkTest == PkTest);


            VwUserAnswersResume answerResume = await _context.VwUserAnswersResumes
                                                         .Where(dr => dr.CodeExam == examID)
                                                         .FirstOrDefaultAsync();


            List<SyUserAnswer> userAnswer = await _context.SyUserAnswers
                                                         .Where(ud => ud.CodeExam == examID)
                                                         .ToListAsync();

            List<CtQuestion> questionList = await _context.CtQuestions
                                                       .Where(q => _context.SyUserAnswers
                                                        .Where(d => d.CodeExam == examID)
                                                        .Select(d => d.FkQuestions)
                                                        .Contains(q.PkQuestions))
                                                    .ToListAsync();

            var query = from q in _context.CtQuestions
                        join o in _context.CtOptions
                            on q.PkQuestions equals o.FkQuestions into optionsGroup
                        from o in optionsGroup.DefaultIfEmpty() // LEFT JOIN
                        where _context.SyUserAnswers
                                      .Where(d => d.CodeExam == examID)
                                      .Select(d => d.FkQuestions)
                                      .Contains(q.PkQuestions)
                        select new
                        {
                            Question = q,
                            Option = o
                        };

            int totalQuestions = await _context.SyUserAnswers
                                .Where(d => d.CodeExam == examID)
                                .CountAsync();

            int correctAnswers = await _context.SyUserAnswers
                                    .Where(d => d.CodeExam == examID && d.FkOptionSelected == d.FkOptionCorrected)
                                    .CountAsync();

            var model = new FullExamDTOs
            {
                TestName = test.TestName,
                ControlNumber = ControlNumber,
                Score = Math.Round((double)answerResume.Score, 2),
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

                var options = await query
                        .Where(x => x.Question.PkQuestions == q.FkQuestions)
                        .Select(x => x.Option)
                        .ToListAsync();

                foreach (var opt in options.Where(o => o != null))
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
                    QuestionText = questionList.FirstOrDefault(d => d.PkQuestions == q.FkQuestions)?.Question,
                    IsCorrect = correctOrNot,
                    SelectedOptionIds = selectedIds,
                    CorrectOptionIds = correctIds,

                    Options = optionResults
                });
            }

            return model;
        }

        public async Task<ServiceAnswer> DeleteExamAsync(int ExamID)
        {
            ServiceAnswer serviceAnswer = new();


            var parameters = new Dictionary<string, object>
            {
                { "@pExamCode", ExamID }
            };

            string result = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_UserTestEvidenceService_IndexDelete_Post]", parameters);

            if (result == "Completed")
            {
                serviceAnswer.Message = ServiceAnswer.MessageType_Success;
                serviceAnswer.Message = "Exams Successfully Deleted";
            }
            else
            {
                serviceAnswer.Message = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = "Exams Successfully Deleted";
            }


                return serviceAnswer;
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


        /// <summary>
        /// Recibe Datatable y devuelve el archivo Excel.
        /// </summary>
        /// <param name="dataTable"></param>
        /// <returns></returns>
        private byte[] ObjetctListToStream(DetailDTOs items)
        {
            byte[] content = null;

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
            foreach (var linea in items.Details)
            {
                ws.Cell(row, 1).Value = items.ControlNumber.ToString();
                ws.Cell(row, 2).Value = items.FullName.ToString();
                ws.Cell(row, 3).Value = items.Position.ToString();
                ws.Cell(row, 4).Value = linea.Course.ToString();
                ws.Cell(row, 5).Value = linea.Level.ToString();
                ws.Cell(row, 6).Value = linea.Delivery.ToString();
                ws.Cell(row, 7).Value = linea.DiagnosticScore.ToString();
                ws.Cell(row, 8).Value = linea.FinalExamScore.ToString();
                ws.Cell(row, 9).Value = linea.CompletedDate.ToString();
                ws.Cell(row, 10).Value = linea.ExamsID.ToString();
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


            content = stream.ToArray();

            return content;
        }


    }
}
