using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        // GET: CtPosition
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> IndexPosition()
        {
            var positions = await _context.CtPositions.ToListAsync();
            return View(positions);
        }

        // GET: CtPosition/Create
        [Authorize(Roles = "Administrador, RHGerente")]
        public IActionResult CreatePosition()
        {
            return View();
        }

        // POST: CtPosition/Create
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePosition(CtPosition ctPosition)
        {
            // Validar si el nombre de la posición ya existe (español)
            bool positionExists = await _context.CtPositions
                .AnyAsync(p => p.NamePosition == ctPosition.NamePosition);

            // Validar si el nombre de la posición en inglés ya existe
            bool englishPositionExists = await _context.CtPositions
                .AnyAsync(p => p.NamePositionEnglish == ctPosition.NamePositionEnglish);

            if (positionExists || englishPositionExists)
            {
                // Añadir un mensaje de error al TempData
                TempData["ErrorMessage"] = positionExists
                    ? "The position name already exists. Please choose a different name."
                    : "The English position name already exists. Please choose a different name.";

                // Volver a cargar la vista con el modelo actual
                return View(ctPosition);
            }

            // Asignar valores automáticos
            ctPosition.Createuser = User.Identity.Name ?? "Unknown"; // Si el usuario es nulo
            ctPosition.Createdate = DateTime.Now;
            ctPosition.Lastupdateuser = User.Identity.Name ?? "Unknown"; // Si el usuario es nulo
            ctPosition.Lastupdatedate = DateTime.Now;
            ctPosition.Available = 1; // Valor predeterminado: habilitado

            try
            {
                _context.Add(ctPosition);
                await _context.SaveChangesAsync();

                // Añadir un mensaje de éxito al TempData
                TempData["SuccessMessage"] = "Position created successfully.";

                return RedirectToAction(nameof(IndexPosition));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the position: {ex.Message}";
                return View(ctPosition);
            }
        }

        // GET: CtPosition/Edit/5
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditPosition(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var ctPosition = await _context.CtPositions.FindAsync(id);
            if (ctPosition == null)
            {
                return NotFound();
            }

            return View(ctPosition);
        }

        // POST: CtPosition/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPosition(int id, CtPosition ctPosition)
        {
            if (id != ctPosition.PkPosition)
            {
                TempData["ErrorMessage"] = "The specified position was not found.";
                return RedirectToAction(nameof(IndexPosition));
            }

            // Verificar si el nombre de la posición ya existe
            bool positionExists = await _context.CtPositions
                .AnyAsync(p => p.NamePosition == ctPosition.NamePosition && p.PkPosition != ctPosition.PkPosition);

            bool englishPositionExists = await _context.CtPositions
                .AnyAsync(p => p.NamePositionEnglish == ctPosition.NamePositionEnglish && p.PkPosition != ctPosition.PkPosition);

            if (positionExists || englishPositionExists)
            {
                TempData["ErrorMessage"] = positionExists
                    ? "The position name already exists. Please choose a different name."
                    : "The English position name already exists. Please choose a different name.";

                return View(ctPosition);
            }

            try
            {
                // Recuperar los datos originales para evitar sobrescrituras accidentales
                var existingPosition = await _context.CtPositions.FindAsync(id);
                if (existingPosition == null)
                {
                    TempData["ErrorMessage"] = "The position no longer exists in the database.";
                    return NotFound();
                }

                // Actualizar solo los campos que pueden ser modificados
                existingPosition.NamePosition = ctPosition.NamePosition;
                existingPosition.NamePositionEnglish = ctPosition.NamePositionEnglish;
                existingPosition.Lastupdateuser = User.Identity.Name ?? "Unknown"; // Usuario actual
                existingPosition.Lastupdatedate = DateTime.Now; // Fecha de actualización

                _context.Update(existingPosition);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Position updated successfully.";
                return RedirectToAction(nameof(IndexPosition));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "There was a concurrency error while updating the position. Please try again.";
                return View(ctPosition);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the position: {ex.Message}";
                return View(ctPosition);
            }
        }

        // POST: /Catalog/TogglePosition/5
        [HttpPost]
        [Route("TogglePosition")]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePosition(int id)
        {
            var ctPosition = await _context.CtPositions.FindAsync(id);
            if (ctPosition != null)
            {
                // Alternar el estado de disponibilidad
                ctPosition.Available = ctPosition.Available == 1 ? 0 : 1;

                _context.Update(ctPosition);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexPosition));
        }

        // POST: /Catalog/DeletePosition/5
        [HttpPost]
        [Route("DeletePosition")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedPosition(int id)
        {
            var ctPosition = await _context.CtPositions.FindAsync(id);
            if (ctPosition != null)
            {
                _context.CtPositions.Remove(ctPosition); // Eliminar la posición de la base de datos
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexPosition));
        }
        private bool CtPositionExists(int id)
        {
            return _context.CtPositions.Any(e => e.PkPosition == id);
        }
    }
}
