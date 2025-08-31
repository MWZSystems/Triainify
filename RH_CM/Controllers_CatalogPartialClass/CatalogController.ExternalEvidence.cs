using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;
using RH_CM.Service.DTOs;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        [HttpGet]
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> IndexExternalEvidence()
        {
            List<ExternalEvidenceDTOs> result = await _externalEvidenceService.GetIndex();
            
            return View(result);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> CreateExternalEvidence()
        {
            CreateExternalEvidenceDTOs result = await _externalEvidenceService.GetCreateExternalEvidence();

            return View(result);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> CreateExternalEvidence(CreateExternalEvidenceInputDTOs model)
        {
            model.UserName = User.Identity?.Name ?? "Unknown";

            ServiceAnswerAndFeedbackDTOs serviceAnswerAndFeedbackDTO = await _externalEvidenceService.PostCreateExternalEvidence(model);

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
            ServiceAnswerAndFeedbackDTOs serviceAnswerAndFeedbackDTOs = new ServiceAnswerAndFeedbackDTOs();

            serviceAnswerAndFeedbackDTOs = await _externalEvidenceService.GetEvidenceMaterial(id,0);

            return File(serviceAnswerAndFeedbackDTOs.FeedbackFile, "application/pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadEvidenceMaterial(int id)
        {
            ServiceAnswerAndFeedbackDTOs serviceAnswerAndFeedbackDTOs = new ServiceAnswerAndFeedbackDTOs();

            serviceAnswerAndFeedbackDTOs = await _externalEvidenceService.GetEvidenceMaterial(id,1);

            return File(serviceAnswerAndFeedbackDTOs.FeedbackFile, "application/pdf", $"{serviceAnswerAndFeedbackDTOs.ServiceAnswer.Message}.pdf");
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEvidenceMaterial(int id)
        {
            ServiceAnswer serviceAnswer = new ServiceAnswer();

            serviceAnswer = await _externalEvidenceService.ToggleRecord(id);

            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;
            return RedirectToAction(nameof(IndexExternalEvidence));
        }



        [HttpGet]
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditEvidenceMaterial(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null) return NotFound();

            return View(material); // ya no cargamos ViewBags
        }



        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvidenceMaterial(/*int id,*/ CtCoursematerial model, IFormFile? uploadedFile)
        {
            //if (id != model.PkCoursematerial) return NotFound();

            int id = model.PkCoursematerial;


            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Choose a File.";
                return RedirectToAction(nameof(EditCourseMaterial), new { id });
            }


            var existing = await _context.CtCoursematerials.FindAsync(id);
            if (existing == null) return NotFound();

            var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? "";

            // Actualizar campos editables
            existing.NameMaterial = fileName;
            existing.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            existing.LastUpdateDate = DateTime.Now;


            // Si subió nuevo archivo (PDF)
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                //var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? "";
                if (!fileName.EndsWith(".pdf"))
                {
                    TempData["ErrorMessage"] = "Only PDF files are allowed (.pdf).";
                    return RedirectToAction(nameof(EditCourseMaterial), new { id });
                }

                using var ms = new MemoryStream();
                await uploadedFile.CopyToAsync(ms);
                existing.File = ms.ToArray();
            }

            _context.Update(existing);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }


        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvidenceMaterial(int id)
        {

            var validation = await _context.CtCourseLevelMaterials
            .FirstOrDefaultAsync(x => x.FkCourseMaterial == id);

            if (validation != null)
            {
                TempData["ErrorMessage"] = "This material is linked to a Course-Level";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }


            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material != null)
            {

                _context.CtCoursematerials.Remove(material);
                await _context.SaveChangesAsync();
            }
            TempData["SuccessMessage"] = "Material has been deleted Successfully";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }



    }
}
