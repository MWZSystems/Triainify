using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Service.DTOs;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        [HttpGet]
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> IndexExternalEvidence()
        {
            List<ExternalEvidenceDTOs> result = await _externalEvidenceService.GetIndexAsync();
            return View(result);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> CreateExternalEvidence()
        {
            CreateExternalEvidenceDTOs result = await _externalEvidenceService.GetCreateExternalEvidenceAsync();
            return View(result);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> CreateExternalEvidence(CreateExternalEvidenceInputDTOs model)
        {
            model.UserName = User.Identity?.Name ?? "Unknown";

            ServiceAnswerAndFeedbackDTOs serviceAnswerAndFeedbackDTO = await _externalEvidenceService.PostCreateExternalEvidenceAsync(model);

            TempData[serviceAnswerAndFeedbackDTO.ServiceAnswer.MessageType] = serviceAnswerAndFeedbackDTO.ServiceAnswer.Message;

            if (serviceAnswerAndFeedbackDTO.FeedbackFile != null)
            {
                string fechaActual = DateTime.Now.ToString("yyyyMMdd");
                return File(
                     serviceAnswerAndFeedbackDTO.FeedbackFile,
                     "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                     $"External_Evidence_Error_Feedback_{fechaActual}.xlsx"
                 );
            }

            // Si no hay archivo, solo recarga la vista con mensaje
            return RedirectToAction(nameof(CreateExternalEvidence));
        }

        [HttpGet]
        public async Task<IActionResult> ViewEvidenceMaterial(int id)
        {
            ServiceAnswerAndFeedbackDTOs serviceAnswerAndFeedbackDTOs = await _externalEvidenceService.GetEvidenceMaterialAsync(id,0);
            return File(serviceAnswerAndFeedbackDTOs.FeedbackFile, "application/pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadEvidenceMaterial(int id)
        {
            ServiceAnswerAndFeedbackDTOs serviceAnswerAndFeedbackDTOs = await _externalEvidenceService.GetEvidenceMaterialAsync(id,1);
            return File(serviceAnswerAndFeedbackDTOs.FeedbackFile, "application/pdf", $"{serviceAnswerAndFeedbackDTOs.ServiceAnswer.Message}.pdf");
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEvidenceMaterial(int id)
        {
            ServiceAnswer serviceAnswer = await _externalEvidenceService.ToggleRecord(id);
            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;
            return RedirectToAction(nameof(IndexExternalEvidence));
        }

        [HttpGet]
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditEvidenceMaterial(int? id)
        {
            EditExternalEvidenceDTOs getResult = await _externalEvidenceService.GetUpdateRecordAsync(id);
            return View(getResult);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvidenceMaterial(EditExternalEvidenceDTOs DTOs, IFormFile? uploadedFile)
        {
            DTOs.UserName = User.Identity?.Name ?? "Unknown";

            ServiceAnswer serviceAnswer = await _externalEvidenceService.PostUpdateRecordAsync(DTOs, uploadedFile);
            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;
            return RedirectToAction(nameof(IndexExternalEvidence));
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvidenceMaterial(int id)
        {
            ServiceAnswer serviceAnswer = await _externalEvidenceService.PostDeleteRecordAsync(id);
            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;
            return RedirectToAction(nameof(IndexExternalEvidence));
        }
    }
}
