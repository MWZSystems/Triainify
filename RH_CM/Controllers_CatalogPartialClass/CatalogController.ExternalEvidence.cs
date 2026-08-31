using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Service.DTOs;
using System;
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
            return View(result);
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> CreateExternalEvidence(CreateExternalEvidenceInputDTOs model)
        {
            // --- Manual file validation (extension / size / presence) -----------------
            // IFormFile can't be expressed well with plain DataAnnotations, so we validate it
            // here and feed the result into ModelState, next to the DataAnnotations errors
            // already produced for SelectedUsers / SelectedCourse / SelectedLevel / Score.
            if (model.UploadedFile == null)
            {
                ModelState.AddModelError(nameof(model.UploadedFile), "Debes adjuntar un archivo de evidencia.");
            }
            else
            {
                string extension = Path.GetExtension(model.UploadedFile.FileName).ToLowerInvariant();
                const long maxSizeBytes = 10 * 1024 * 1024; // ajusta al límite real que necesites

                if (extension != ".pdf")
                {
                    ModelState.AddModelError(nameof(model.UploadedFile), "El archivo debe tener formato PDF.");
                }
                else if (model.UploadedFile.Length == 0)
                {
                    ModelState.AddModelError(nameof(model.UploadedFile), "El archivo adjunto está vacío.");
                }
                else if (model.UploadedFile.Length > maxSizeBytes)
                {
                    ModelState.AddModelError(nameof(model.UploadedFile), "El archivo no debe superar los 10 MB.");
                }
            }

            // Fix: "ModelState.IsValid should be checked in controller actions."
            // Also gives real detail: concatenates every ModelState error (DataAnnotations +
            // the manual file checks above) instead of a generic "something went wrong".
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

            // Fix: "Dereference of a possibly null reference."
            // ServiceAnswerAndFeedbackDTOs.ServiceAnswer is nullable — handle the service
            // not returning one at all, with a message that says so instead of crashing.
            if (result.ServiceAnswer is not { } serviceAnswer)
            {
                TempData[ServiceAnswer.MessageType_Error] = "An unexpected error occurred while creating the record. Please try again or contact support.";
                return RedirectToAction(nameof(CreateExternalEvidence));
            }

            // Fix: "Possible null reference argument for parameter 'key'"
            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] =
                serviceAnswer.Message ?? "The operation finished, but no message was returned.";

            // Fix: "Possible null reference argument for parameter 'fileContents'"
            if (result.FeedbackFile is { } feedbackFile)
            {
                string fechaActual = DateTime.Now.ToString("yyyyMMdd");
                return File(
                     feedbackFile,
                     "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                     $"External_Evidence_Error_Feedback_{fechaActual}.xlsx"
                 );
            }

            // Si no hay archivo, solo recarga la vista con mensaje
            return RedirectToAction(nameof(CreateExternalEvidence));
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> ViewEvidenceMaterial(int id)
        {
            ServiceAnswerAndFeedbackDTOs result = await _externalEvidenceService.GetEvidenceMaterialAsync(id, 0);

            if (result.FeedbackFile is not { } fileContents)
            {
                TempData[ServiceAnswer.MessageType_Error] = result.ServiceAnswer?.Message ?? "The requested file could not be found.";
                return RedirectToAction(nameof(IndexExternalEvidence));
            }

            return File(fileContents, "application/pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadEvidenceMaterial(int id)
        {
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
            ServiceAnswer serviceAnswer = await _externalEvidenceService.ToggleRecord(id);
            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;
            return RedirectToAction(nameof(IndexExternalEvidence));
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditEvidenceMaterial(int? id)
        {
            EditExternalEvidenceDTOs getResult = await _externalEvidenceService.GetUpdateRecordAsync(id);
            return View(getResult);
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvidenceMaterial(EditExternalEvidenceDTOs DTOs, IFormFile? uploadedFile)
        {
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

            // Fix: "Possible null reference argument for parameter 'file' in PostUpdateRecordAsync(...)"
            // Option A (used here): a file is REQUIRED on every edit.
            // Option B: make editing without replacing the file valid — change the service's
            // signature to `PostUpdateRecordAsync(EditExternalEvidenceDTOs DTOs, IFormFile? file)`
            // and skip the file-replace logic there when null; then this null check goes away.
            if (uploadedFile == null)
            {
                TempData[ServiceAnswer.MessageType_Error] = "Please attach a file.";
                return RedirectToAction(nameof(EditEvidenceMaterial), new { id = DTOs.PK_ExternalEvidence });
            }

            ServiceAnswer serviceAnswer = await _externalEvidenceService.PostUpdateRecordAsync(DTOs, uploadedFile);
            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;
            return RedirectToAction(nameof(IndexExternalEvidence));
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvidenceMaterial(int id)
        {
            ServiceAnswer serviceAnswer = await _externalEvidenceService.PostDeleteRecordAsync(id);
            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;
            return RedirectToAction(nameof(IndexExternalEvidence));
        }
    }
}