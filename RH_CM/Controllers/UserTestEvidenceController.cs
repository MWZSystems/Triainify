using BootstrapBlazor.Components;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
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
        public UserTestEvidenceController(UserTestEvidenceService userTestEvidenceService,
                                            db_abcd61_rhchdbContext context)
        {
            _userTestEvidenceService = userTestEvidenceService;
            _context = context;
        }

        // GET: UserTestEvidenceController
        public async Task<ActionResult> Index()
        {
            List<UserDTOs> users = await _userTestEvidenceService.GetIndexAsync();
            return View(users);
        }

        // GET: UserTestEvidenceController/Details/5
        public async Task<ActionResult> DetailUser(string id)
        {
            DetailDTOs detailDTOs = await _userTestEvidenceService.GetDetailUserAsync(id);
            return View(detailDTOs);
        }

        public async Task<ActionResult> DiagnosticExamReview(int id)
        {
            FullExamDTOs fullExam = await _userTestEvidenceService.GetDiagnosticExamAsync(id);

            return View(fullExam);
        }

        public async Task<ActionResult> ExamReview(int id)
        {
            FullExamDTOs fullExam = await _userTestEvidenceService.GetExamAsync(id);

            return View(fullExam);
        }

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
        public async Task<ActionResult> DeleteExams(int id)
        {

            ServiceAnswer serviceAnswer = await _userTestEvidenceService.DeleteExamAsync(id);
            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction(nameof(Index));

        }


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


    }
}
