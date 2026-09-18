// File: Controllers/CatalogController.Position.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;
using RH_CM.Messages.Catalog;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        /// <summary>
        /// Displays the list of positions.
        /// </summary>
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> IndexPosition()
        {
            // ✅ Use AsNoTracking() for read-only lists to reduce memory usage (no EF tracking)
            var positions = await _context.CtPositions
                .AsNoTracking()
                .OrderBy(p => p.NamePosition)
                .ToListAsync();

            return View("Position/IndexPosition", positions);
        }

        /// <summary>
        /// Displays the form to create a new position.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public IActionResult CreatePosition()
        {
            return View("Position/CreatePosition");
        }

        /// <summary>
        /// Creates a new position.
        /// </summary>
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

                return View("Position/CreatePosition", ctPosition);
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

                TempData["SuccessMessage"] = PositionMessages.PositionCreatedSuccessfully;
                return RedirectToAction(nameof(IndexPosition));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(PositionMessages.ErrorOccurredWhileCreatingThePositionFormat, ex.Message);
                return View("Position/CreatePosition", ctPosition);
            }
        }

        /// <summary>
        /// Displays the form to edit an existing position.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditPosition(int? id)
        {
            if (id == null) return NotFound();

            // ✅ No tracking: edit form can work with a detached entity, we re-load tracked entity on POST
            var ctPosition = await _context.CtPositions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PkPosition == id);

            return ctPosition == null ? NotFound() : View("Position/EditPosition", ctPosition);
        }

        /// <summary>
        /// Updates an existing position.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPosition(int id, CtPosition ctPosition)
        {
            if (id != ctPosition.PkPosition)
            {
                TempData["ErrorMessage"] = PositionMessages.SpecifiedPositionWasNotFound;
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

                return View("Position/EditPosition", ctPosition);
            }

            try
            {
                // Reload tracked entity to update safely
                var existingPosition = await _context.CtPositions.FindAsync(id);
                if (existingPosition == null)
                {
                    TempData["ErrorMessage"] = PositionMessages.PositionNoLongerExistsInTheDatabase;
                    return NotFound();
                }

                // Update only allowed fields
                existingPosition.NamePosition = ctPosition.NamePosition;
                existingPosition.NamePositionEnglish = ctPosition.NamePositionEnglish;
                existingPosition.Lastupdateuser = User.Identity?.Name ?? "Unknown";
                existingPosition.Lastupdatedate = DateTime.Now;

                _context.Update(existingPosition);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = PositionMessages.PositionUpdatedSuccessfully;
                return RedirectToAction(nameof(IndexPosition));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] =
                    PositionMessages.ConcurrencyErrorOccurredWhileUpdatingThePosition;
                return View("Position/EditPosition", ctPosition);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(PositionMessages.ErrorOccurredWhileUpdatingThePositionFormat, ex.Message);
                return View("Position/EditPosition", ctPosition);
            }
        }

        /// <summary>
        /// Toggles a position's availability.
        /// </summary>
        [HttpPost]
        [Route("TogglePosition")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePosition(int id)
        {
            var ctPosition = await _context.CtPositions.FindAsync(id);
            if (ctPosition == null)
            {
                TempData["ErrorMessage"] = PositionMessages.PositionDoesNotExistOrHasAlready;
                return RedirectToAction(nameof(IndexPosition));
            }

            if (ctPosition.Available == 1)
            {
                var dependency = await _catalogIntegrityService.PositionDependencyAsync(id, activeOnly: true);
                if (dependency != null)
                {
                    TempData["ErrorMessage"] = dependency;
                    return RedirectToAction(nameof(IndexPosition));
                }
            }

            ctPosition.Available = ctPosition.Available == 1 ? 0 : 1;
            ctPosition.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            ctPosition.Lastupdatedate = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = ctPosition.Available == 1
                ? "Position enabled successfully."
                : "Position disabled successfully.";

            return RedirectToAction(nameof(IndexPosition));
        }

        /// <summary>
        /// Deletes a position.
        /// </summary>
        [HttpPost]
        [Route("DeletePosition")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedPosition(int id)
        {
            // 1) Is there any headcount linked to this Position?
            var dependency = await _catalogIntegrityService.PositionDependencyAsync(id);
            if (dependency != null)
            {
                TempData["ErrorMessage"] = dependency;
                return RedirectToAction(nameof(IndexPosition));
            }

            // 2) Find the position
            var ctPosition = await _context.CtPositions.FindAsync(id);
            if (ctPosition == null)
            {
                TempData["ErrorMessage"] = PositionMessages.PositionDoesNotExistOrHasAlready;
                return RedirectToAction(nameof(IndexPosition));
            }

            try
            {
                _context.CtPositions.Remove(ctPosition);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = PositionMessages.PositionDeletedSuccessfully;
            }
            catch (DbUpdateException)
            {
                // If a relationship was created after our validation, the DB will block deletion here
                TempData["ErrorMessage"] = "The position could not be deleted because another record started using it. Unlink it and try again.";
            }

            return RedirectToAction(nameof(IndexPosition));
        }

        private bool CtPositionExists(int id)
        {
            return _context.CtPositions.Any(e => e.PkPosition == id);
        }
    }
}
