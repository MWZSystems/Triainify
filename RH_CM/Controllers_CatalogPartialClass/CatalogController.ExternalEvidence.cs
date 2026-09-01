using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RH_CM.Service.DTOs;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> IndexExternalEvidence()
        {
            List<ExternalEvidenceDTOs> result = await _externalEvidenceService.GetIndexAsync();
            return View(result);
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> CreateExternalEvidence()
        {
            CreateExternalEvidenceDTOs result = await _externalEvidenceService.GetCreateExternalEvidenceAsync();

            if (TempData["ExternalEvidenceFeedbackJson"] is string feedbackJson && !string.IsNullOrWhiteSpace(feedbackJson))
            {
                ViewBag.FeedbackItems = JsonConvert.DeserializeObject<List<ExternalEvidenceFeedbackItemDTOs>>(feedbackJson);
            }

            return View(result);
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExternalEvidence(CreateExternalEvidenceInputDTOs model)
        {
            if (model.UploadedFile == null)
            {
                ModelState.AddModelError(nameof(model.UploadedFile), "You must attach an evidence file.");
            }
            else
            {
                string extension = Path.GetExtension(model.UploadedFile.FileName).ToLowerInvariant();
                const long maxSizeBytes = 10 * 1024 * 1024;

                if (extension != ".pdf")
                {
                    ModelState.AddModelError(nameof(model.UploadedFile), "The file must be in PDF format.");
                }
                else if (model.UploadedFile.Length == 0)
                {
                    ModelState.AddModelError(nameof(model.UploadedFile), "The attached file is empty.");
                }
                else if (model.UploadedFile.Length > maxSizeBytes)
                {
                    ModelState.AddModelError(nameof(model.UploadedFile), "The file size must not exceed 10 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                IEnumerable<string> errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));

                TempData[ServiceAnswer.MessageType_Error] = errors.Any()
                    ? string.Join(" ", errors)
                    : "Please review the highlighted fields and try again.";

                return RedirectToAction(nameof(CreateExternalEvidence));
            }

            model.UserName = User.Identity?.Name ?? "Unknown";

            ServiceAnswerAndFeedbackDTOs result = await _externalEvidenceService.PostCreateExternalEvidenceAsync(model);

            if (result.ServiceAnswer is not { } serviceAnswer)
            {
                TempData[ServiceAnswer.MessageType_Error] = "An unexpected error occurred while creating the record. Please try again or contact support.";
                return RedirectToAction(nameof(CreateExternalEvidence));
            }

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] =
                serviceAnswer.Message ?? "The operation finished, but no message was returned.";

            if (result.FeedbackItems is { Count: > 0 } feedbackItems)
            {
                TempData["ExternalEvidenceFeedbackJson"] = JsonConvert.SerializeObject(feedbackItems);
            }

            return RedirectToAction(nameof(CreateExternalEvidence));
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> ViewEvidenceMaterial(int id)
        {
            if (!ModelState.IsValid || id <= 0)
            {
                TempData[ServiceAnswer.MessageType_Error] = "The evidence identifier is not valid.";
                return RedirectToAction(nameof(IndexExternalEvidence));
            }

            ServiceAnswerAndFeedbackDTOs result = await _externalEvidenceService.GetEvidenceMaterialAsync(id, 0);

            if (result.FeedbackFile is not { } fileContents)
            {
                TempData[ServiceAnswer.MessageType_Error] = result.ServiceAnswer?.Message ?? "The requested file could not be found.";
                return RedirectToAction(nameof(IndexExternalEvidence));
            }

            return File(fileContents, "application/pdf");
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> DownloadEvidenceMaterial(int id)
        {
            if (!ModelState.IsValid || id <= 0)
            {
                TempData[ServiceAnswer.MessageType_Error] = "The evidence identifier is not valid.";
                return RedirectToAction(nameof(IndexExternalEvidence));
            }

            ServiceAnswerAndFeedbackDTOs result = await _externalEvidenceService.GetEvidenceMaterialAsync(id, 1);

            if (result.FeedbackFile is not { } fileContents)
            {
                TempData[ServiceAnswer.MessageType_Error] = result.ServiceAnswer?.Message ?? "The requested file could not be found.";
                return RedirectToAction(nameof(IndexExternalEvidence));
            }

            string fileName = result.ServiceAnswer?.Message ?? $"Evidence_{id}";
            return File(fileContents, "application/pdf", $"{fileName}.pdf");
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEvidenceMaterial(int id)
        {
            if (!ModelState.IsValid || id <= 0)
            {
                TempData[ServiceAnswer.MessageType_Error] = "The evidence identifier is not valid.";
                return RedirectToAction(nameof(IndexExternalEvidence));
            }

            ServiceAnswer serviceAnswer = await _externalEvidenceService.ToggleRecord(id);
            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;
            return RedirectToAction(nameof(IndexExternalEvidence));
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditEvidenceMaterial(int? id)
        {
            if (!ModelState.IsValid || id is null || id <= 0)
            {
                TempData[ServiceAnswer.MessageType_Error] = "The evidence identifier is not valid.";
                return RedirectToAction(nameof(IndexExternalEvidence));
            }

            EditExternalEvidenceDTOs getResult = await _externalEvidenceService.GetUpdateRecordAsync(id);
            return View(getResult);
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvidenceMaterial(EditExternalEvidenceDTOs DTOs, IFormFile? uploadedFile)
        {
            if (DTOs.PK_ExternalEvidence is null || DTOs.PK_ExternalEvidence <= 0)
            {
                ModelState.AddModelError(nameof(DTOs.PK_ExternalEvidence), "The evidence identifier is not valid.");
            }

            if (uploadedFile == null)
            {
                ModelState.AddModelError(nameof(uploadedFile), "You must attach an evidence file.");
            }
            else
            {
                string extension = Path.GetExtension(uploadedFile.FileName).ToLowerInvariant();
                const long maxSizeBytes = 10 * 1024 * 1024;

                if (extension != ".pdf")
                {
                    ModelState.AddModelError(nameof(uploadedFile), "The file must be in PDF format.");
                }
                else if (uploadedFile.Length == 0)
                {
                    ModelState.AddModelError(nameof(uploadedFile), "The attached file is empty.");
                }
                else if (uploadedFile.Length > maxSizeBytes)
                {
                    ModelState.AddModelError(nameof(uploadedFile), "The file size must not exceed 10 MB.");
                }
            }

            if (!ModelState.IsValid)
            {
                IEnumerable<string> errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .Where(m => !string.IsNullOrWhiteSpace(m));

                TempData[ServiceAnswer.MessageType_Error] = errors.Any()
                    ? string.Join(" ", errors)
                    : "Please review the highlighted fields and try again.";

                return RedirectToAction(nameof(EditEvidenceMaterial), new { id = DTOs.PK_ExternalEvidence });
            }

            DTOs.UserName = User.Identity?.Name ?? "Unknown";

            ServiceAnswer serviceAnswer = await _externalEvidenceService.PostUpdateRecordAsync(DTOs, uploadedFile!);
            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;
            return RedirectToAction(nameof(IndexExternalEvidence));
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvidenceMaterial(int id)
        {
            if (!ModelState.IsValid || id <= 0)
            {
                TempData[ServiceAnswer.MessageType_Error] = "The evidence identifier is not valid.";
                return RedirectToAction(nameof(IndexExternalEvidence));
            }

            ServiceAnswer serviceAnswer = await _externalEvidenceService.PostDeleteRecordAsync(id);
            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;
            return RedirectToAction(nameof(IndexExternalEvidence));
        }
    }
}