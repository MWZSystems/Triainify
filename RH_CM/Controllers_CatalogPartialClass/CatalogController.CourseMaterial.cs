using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        // GET: CtCoursematerial
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public IActionResult IndexCourseMaterial()
        {
            var materials = _context.CtCoursematerials
                .AsNoTracking()
                .Where(m => m.Available == 1)
                .OrderBy(m => m.NameMaterial)
                .Select(m => new
                {
                    m.PkCoursematerial,
                    MaterialName = m.NameMaterial,
                    m.Available,
                    m.Createuser,
                    m.Createdate,
                    m.Lastupdateuser,
                    m.Lastupdatedate
                })
                .ToList();

            return View(materials);
        }

        // GET: CtCoursematerial/Create
        [HttpGet]
        [Authorize(Roles = "Administrador, RHGerente")]
        public IActionResult CreateCourseMaterial()
        {
            // Ya no cargamos listas ni ViewBags
            return View(new CtCoursematerial());
        }

        // POST: CtCoursematerial/Create
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourseMaterial(CtCoursematerial model, IFormFile uploadedFile)
        {
            // Validar nombre
            if (string.IsNullOrWhiteSpace(model.NameMaterial))
            {
                TempData["ErrorMessage"] = "Material Name is required.";
                return View(model);
            }

            // (Opcional) Evitar duplicado por nombre (ajusta si quieres case-insensitive)
            var existsName = await _context.CtCoursematerials
                .AsNoTracking()
                .AnyAsync(m => m.NameMaterial == model.NameMaterial && m.Available == 1);
            if (existsName)
            {
                TempData["ErrorMessage"] = "A material with the same name already exists.";
                return View(model);
            }

            // Validar archivo
            if (uploadedFile == null || uploadedFile.Length == 0)
            {
                TempData["ErrorMessage"] = "You must upload a PDF file.";
                return View(model);
            }
            var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? "";
            if (!fileName.EndsWith(".pdf"))
            {
                TempData["ErrorMessage"] = "Only PDF files are allowed (.pdf).";
                return View(model);
            }
            // (Opcional) validar content-type reportado por el navegador
            // if (uploadedFile.ContentType != "application/pdf") { ... }

            // Guardar archivo en varbinary(max)
            using (var ms = new MemoryStream())
            {
                await uploadedFile.CopyToAsync(ms);
                model.File = ms.ToArray();
            }

            // Metadatos
            model.Createuser = User.Identity?.Name ?? "Unknown";
            model.Createdate = DateTime.Now;
            model.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            model.Lastupdatedate = DateTime.Now;
            model.Available = 1;

            _context.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material created successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }
        // GET: CtCoursematerial/Edit/5
        [Authorize(Roles = "Administrador, RHGerente")]
        [HttpGet]
        public async Task<IActionResult> EditCourseMaterial(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null) return NotFound();

            return View(material); // ya no cargamos ViewBags
        }

        // POST: CtCoursematerial/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourseMaterial(int id, CtCoursematerial model, IFormFile? uploadedFile)
        {
            if (id != model.PkCoursematerial) return NotFound();

            var existing = await _context.CtCoursematerials.FindAsync(id);
            if (existing == null) return NotFound();

            // Validaciones mínimas
            if (string.IsNullOrWhiteSpace(model.NameMaterial))
            {
                TempData["ErrorMessage"] = "Material Name is required.";
                return View(model);
            }

            // Actualizar campos editables
            existing.NameMaterial = model.NameMaterial;
            existing.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            existing.Lastupdatedate = DateTime.Now;

            // Si subió nuevo archivo (PDF)
            if (uploadedFile != null && uploadedFile.Length > 0)
            {
                var fileName = uploadedFile.FileName?.ToLowerInvariant() ?? "";
                if (!fileName.EndsWith(".pdf"))
                {
                    TempData["ErrorMessage"] = "Only PDF files are allowed (.pdf).";
                    return View(model);
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



        // POST: CtCoursematerial/Toggle/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleCourseMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
            {
                TempData["ErrorMessage"] = "Material not found.";
                return RedirectToAction(nameof(IndexCourseMaterial));
            }

            material.Available = material.Available == 1 ? 0 : 1;
            material.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            material.Lastupdatedate = DateTime.Now;

            _context.Update(material);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Material status updated successfully.";
            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        // POST: CtCoursematerial/Delete/5
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material != null)
            {
                _context.CtCoursematerials.Remove(material);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(IndexCourseMaterial));
        }

        // GET: CtCoursematerial/Download/5
        public async Task<IActionResult> DownloadCourseMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
                return NotFound();

            return File(material.File, "application/pdf", $"{material.NameMaterial}.pdf");
        }

        // GET: CtCoursematerial/ViewPdf/5
        public async Task<IActionResult> ViewPdfCourseMaterial(int id)
        {
            var material = await _context.CtCoursematerials.FindAsync(id);
            if (material == null)
                return NotFound();

            return File(material.File, "application/pdf");
        }
    }
}
