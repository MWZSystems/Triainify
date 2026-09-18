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
using RH_CM.Messages.UserTestEvidence;

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

        /// <summary>
        /// Displays the list of users with test evidence.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> Index()
        {
            List<UserDTOs> users = await _userTestEvidenceService.GetIndexAsync();
            return View(users);
        }

        /// <summary>
        /// Displays a user's test evidence details.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> DetailUser(string id)
        {
            DetailDTOs detailDTOs = await _userTestEvidenceService.GetDetailUserAsync(id);
            return View(detailDTOs);
        }
        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> DiagnosticExamReview(int id, int controlNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            FullExamDTOs? fullExam = await _userTestEvidenceService.GetDiagnosticExamAsync(id, controlNumber);

            if (fullExam == null)
            {
                TempData["ErrorMessage"] = UserTestEvidenceMessages.DiagnosticExamNotFound;
                return RedirectToAction(nameof(Index));
            }

            return View(fullExam);
        }
        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> ExamReview(int id, int controlNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            FullExamDTOs? fullExam = await _userTestEvidenceService.GetExamAsync(id, controlNumber);

            if (fullExam == null)
            {
                TempData["ErrorMessage"] = UserTestEvidenceMessages.ExamNotFound;
                return RedirectToAction(nameof(Index));
            }

            return View(fullExam);
        }
        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> EvidenceReview(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var externalEvidence = await _context.SyExternalevidences
                .Where(ev => ev.FkMovementCourse == id)
                .FirstOrDefaultAsync();


            if (externalEvidence?.EvidenceFile == null)
                return NotFound();

            return File(externalEvidence.EvidenceFile, "application/pdf");

        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteExams(int id, int controlNumber)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ServiceAnswer serviceAnswer = await _userTestEvidenceService.DeleteExamAsync(id, controlNumber);
            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction(nameof(Index));

        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> ExportEvidencesByUser(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ExcelExportDTOs answerFile = await _userTestEvidenceService.ExportExcelAsync(id);

            if (answerFile?.File == null || answerFile.File.Length == 0)
            {
                TempData["ErrorMessage"] = UserTestEvidenceMessages.NoDataAvailableToExport;
                return RedirectToAction(nameof(Index));
            }

            string fullName = answerFile.FullName ?? string.Empty;
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

            if (answerFile?.File == null || answerFile.File.Length == 0)
            {
                TempData["ErrorMessage"] = UserTestEvidenceMessages.NoDataAvailableToExport;
                return RedirectToAction(nameof(Index));
            }

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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ExcelExportDTOs answerFile = await _dC3Service.GetDC3File(ControlNumber, Course, CompletedDate, bywho);

            if (answerFile?.File == null || answerFile.File.Length == 0)
            {
                TempData["ErrorMessage"] = UserTestEvidenceMessages.Dc3CertificateCouldNotBeGeneratedFor;
                return RedirectToAction(nameof(Index));
            }

            string fullName = answerFile.FullName ?? string.Empty;
            string controlNumbr = ControlNumber.ToString();


            return File(
                    answerFile.File,
                    "application/pdf",
                    $"{controlNumbr}-{fullName}-Completed.pdf"
            );

        }

    }
}
