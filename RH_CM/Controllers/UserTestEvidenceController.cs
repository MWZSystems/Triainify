using BootstrapBlazor.Components;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.DC3Service;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.UserTestEvidence;
using RH_CM.Service.ExternalEvidence;
using RH_CM.Service.UserTestEvidence;

namespace RH_CM.Controllers
{
    public class UserTestEvidenceController : Controller
    {
        private readonly UserTestEvidenceService _userTestEvidenceService;
        private readonly db_abcd61_rhchdbContext _context;
        private readonly DC3Service _dC3Service;
        public UserTestEvidenceController(UserTestEvidenceService userTestEvidenceService,
                                            db_abcd61_rhchdbContext context,
                                            DC3Service dC3Service)
        {
            _userTestEvidenceService = userTestEvidenceService;
            _context = context;
            _dC3Service = dC3Service;
        }

        // GET: UserTestEvidenceController
        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> Index()
        {
            List<UserDTOs> users = await _userTestEvidenceService.GetIndexAsync();
            return View(users);
        }

        // GET: UserTestEvidenceController/Details/5
        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> DetailUser(string id)
        {
            DetailDTOs detailDTOs = await _userTestEvidenceService.GetDetailUserAsync(id);
            return View(detailDTOs);
        }
        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> DiagnosticExamReview(int id)
        {
            FullExamDTOs fullExam = await _userTestEvidenceService.GetDiagnosticExamAsync(id);

            return View(fullExam);
        }
        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> ExamReview(int id)
        {
            FullExamDTOs fullExam = await _userTestEvidenceService.GetExamAsync(id);

            return View(fullExam);
        }
        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> EvidenceReview(int id)
        {

            var externalEvidence = await _context.SyExternalevidences
                .Where(ev => ev.FkMovementCourse == id)
                .FirstOrDefaultAsync();


            if (externalEvidence == null)
                return NotFound();

            return File(externalEvidence.EvidenceFile, "application/pdf");

        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> DeleteExams(int id)
        {

            ServiceAnswer serviceAnswer = await _userTestEvidenceService.DeleteExamAsync(id);
            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction(nameof(Index));

        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> ExportEvidencesByUser(int id)
        {

            ExcelExportDTOs answerFile = await _userTestEvidenceService.ExportExcelAsync(id);

            string fullName = answerFile.FullName;
            string controlNumbr = answerFile.ControlNumber.ToString();


            string fechaActual = DateTime.Now.ToString("yyyyMMdd");
            return File(
                    answerFile.File,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"{controlNumbr}-{fullName}-ExamEvidences-{fechaActual}.xlsx"
            );

        }


        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> ExportMassiveEvidencesByUser()
        {

            ExcelExportDTOs answerFile = await _userTestEvidenceService.ExportExcelAsync();



            string fechaActual = DateTime.Now.ToString("yyyyMMdd");
            return File(
                    answerFile.File,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Massive-User-ExamEvidences-{fechaActual}.xlsx"
            );

        }


        //DC3PdfDownload
        public async Task<ActionResult> DC3PdfDownload(int ControlNumber, string Course, string CompletedDate, string bywho)
        {

            ExcelExportDTOs answerFile = await _dC3Service.GetDC3File(ControlNumber, Course, CompletedDate, bywho);

            string fullName = answerFile.FullName;
            string controlNumbr = ControlNumber.ToString();


            return File(
                    answerFile.File,
                    "application/pdf",
                    $"{controlNumbr}-{fullName}-Completed.pdf"
            );

        }

    }
}
