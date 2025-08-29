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
            List<GetExternalEvidenceDTOs> answer = await _externalEvidenceService.GetIndex();
            return View(answer);
        }


        [HttpGet]
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> CreateExternalEvidence(List<IFormFile> uploadedFiles)
        {
            if (uploadedFiles == null || !uploadedFiles.Any())
            {
                TempData["ErrorMessage"] = "You must upload at least one PDF file.";
                return RedirectToAction(nameof(CreateCourseMaterial), new { type = "PDF" });
            }

            foreach (var uploadedFile in uploadedFiles)
            {
                if (uploadedFile.Length == 0) continue;

                var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? "";

                if (!fileName.EndsWith(".pdf"))
                {
                    TempData["ErrorMessage"] = "Only PDF files are allowed (.pdf).";
                    return RedirectToAction(nameof(CreateCourseMaterial), new { type = "PDF" });
                }

                // Verificar si ya existe un material con ese nombre (opcional)
                var existsName = await _context.CtCoursematerials
                    .AsNoTracking()
                    .AnyAsync(m => m.NameMaterial == fileName && m.Available == 1);

                if (existsName)
                {
                    TempData["ErrorMessage"] = $"A material with the name {fileName} already exists.";
                    return RedirectToAction(nameof(CreateCourseMaterial), new { type = "PDF" });
                }

                // Guardar archivo
                using var ms = new MemoryStream();
                await uploadedFile.CopyToAsync(ms);

                var material = new CtCoursematerial
                {
                    NameMaterial = fileName,   // nombre del archivo con extensión
                    MaterialType = "PDF",
                    File = ms.ToArray(),
                    CreateUser = User.Identity?.Name ?? "Unknown",
                    CreateDate = DateTime.Now,
                    LastUpdateUser = User.Identity?.Name ?? "Unknown",
                    LastUpdateDate = DateTime.Now,
                    Available = 1
                };

                _context.Add(material);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Materials uploaded successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        [HttpGet]
        public async Task<IActionResult> ViewEvidenceMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
                return NotFound();

            return File(material.File, "application/pdf");
        }


        public async Task<IActionResult> DownloadEvidenceMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
                return NotFound();

            return File(material.File, "application/pdf", $"{material.NameMaterial}.pdf");
        }

        // POST: CtCoursematerial/Toggle/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleEvidenceMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
            {
                TempData["ErrorMessage"] = "Material not found.";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            material.Available = material.Available == 1 ? 0 : 1;
            material.LastUpdateUser = User.Identity?.Name ?? "Unknown";
            material.LastUpdateDate = DateTime.Now;

            _context.Update(material);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material status updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
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
