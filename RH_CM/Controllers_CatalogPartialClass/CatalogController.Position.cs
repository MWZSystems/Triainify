// File: Controllers/CatalogController.Position.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        // GET: CtPosition
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> IndexPosition()
        {
            // ✅ Use AsNoTracking() for read-only lists to reduce memory usage (no EF tracking)
            var positions = await _context.CtPositions
                .AsNoTracking()
                .OrderBy(p => p.NamePosition)
                .ToListAsync();

            return View(positions);
        }

        // GET: CtPosition/Create
        [Authorize(Policy = "ViewAccess")]
        public IActionResult CreatePosition()
        {
            return View();
        }

        // POST: CtPosition/Create
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePosition(CtPosition ctPosition)
        {
            // Validate if the position name already exists
            bool positionExists = await _context.CtPositions
                .AnyAsync(p => p.NamePosition == ctPosition.NamePosition);

            // Validate if the English position name already exists
            bool englishPositionExists = await _context.CtPositions
                .AnyAsync(p => p.NamePositionEnglish == ctPosition.NamePositionEnglish);

            if (positionExists || englishPositionExists)
            {
                TempData["ErrorMessage"] = positionExists
                    ? "The position name already exists. Please choose a different name."
                    : "The English position name already exists. Please choose a different name.";

                return View(ctPosition);
            }

            // Assign automatic values
            ctPosition.Createuser = User.Identity?.Name ?? "Unknown";
            ctPosition.Createdate = DateTime.Now;
            ctPosition.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            ctPosition.Lastupdatedate = DateTime.Now;
            ctPosition.Available = 1; // default: enabled

            try
            {
                _context.Add(ctPosition);
                await _context.SaveChangesAsync();

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
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditPosition(int? id)
        {
            if (id == null) return NotFound();

            // ✅ No tracking: edit form can work with a detached entity, we re-load tracked entity on POST
            var ctPosition = await _context.CtPositions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PkPosition == id);

            return ctPosition == null ? NotFound() : View(ctPosition);
        }

        // POST: CtPosition/Edit/5
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPosition(int id, CtPosition ctPosition)
        {
            if (id != ctPosition.PkPosition)
            {
                TempData["ErrorMessage"] = "The specified position was not found.";
                return RedirectToAction(nameof(IndexPosition));
            }

            // Check if position name already exists (excluding current record)
            bool positionExists = await _context.CtPositions
                .AnyAsync(p => p.NamePosition == ctPosition.NamePosition &&
                               p.PkPosition != ctPosition.PkPosition);

            bool englishPositionExists = await _context.CtPositions
                .AnyAsync(p => p.NamePositionEnglish == ctPosition.NamePositionEnglish &&
                               p.PkPosition != ctPosition.PkPosition);

            if (positionExists || englishPositionExists)
            {
                TempData["ErrorMessage"] = positionExists
                    ? "The position name already exists. Please choose a different name."
                    : "The English position name already exists. Please choose a different name.";

                return View(ctPosition);
            }

            try
            {
                // Reload tracked entity to update safely
                var existingPosition = await _context.CtPositions.FindAsync(id);
                if (existingPosition == null)
                {
                    TempData["ErrorMessage"] = "The position no longer exists in the database.";
                    return NotFound();
                }

                // Update only allowed fields
                existingPosition.NamePosition = ctPosition.NamePosition;
                existingPosition.NamePositionEnglish = ctPosition.NamePositionEnglish;
                existingPosition.Lastupdateuser = User.Identity?.Name ?? "Unknown";
                existingPosition.Lastupdatedate = DateTime.Now;

                _context.Update(existingPosition);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Position updated successfully.";
                return RedirectToAction(nameof(IndexPosition));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] =
                    "A concurrency error occurred while updating the position. Please try again.";
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
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePosition(int id)
        {
            var ctPosition = await _context.CtPositions.FindAsync(id);
            if (ctPosition != null)
            {
                ctPosition.Available = ctPosition.Available == 1 ? 0 : 1;
                _context.Update(ctPosition);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(IndexPosition));
        }

        // POST: /Catalog/DeletePosition/5
        [HttpPost]
        [Route("DeletePosition")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedPosition(int id)
        {
            // 1) Is there any headcount linked to this Position?
            bool hasHeadcount = await _context.SyHeadcounts
                .AsNoTracking()
                .AnyAsync(h => h.FkPosition == id);

            if (hasHeadcount)
            {
                TempData["ErrorMessage"] =
                    "This position cannot be deleted because it is linked to employees (Headcount). " +
                    "Please remove the position from employee assignments first.";
                return RedirectToAction(nameof(IndexPosition));
            }

            // 2) Find the position
            var ctPosition = await _context.CtPositions.FindAsync(id);
            if (ctPosition == null)
            {
                TempData["ErrorMessage"] = "The position does not exist or has already been deleted.";
                return RedirectToAction(nameof(IndexPosition));
            }

            try
            {
                _context.CtPositions.Remove(ctPosition);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Position deleted successfully.";
            }
            catch (DbUpdateException)
            {
                // If a relationship was created after our validation, the DB will block deletion here
                TempData["ErrorMessage"] =
                    "This position cannot be deleted because it is linked to employees (Headcount). " +
                    "Please remove the position from employee assignments first.";
            }

            return RedirectToAction(nameof(IndexPosition));
        }

        private bool CtPositionExists(int id)
        {
            return _context.CtPositions.Any(e => e.PkPosition == id);
        }
    }
}
