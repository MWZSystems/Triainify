// File: Controllers/CatalogController.Supervisor.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;
using RH_CM.Messages.Catalog;
using RH_CM.Service.Export;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> IndexSupervisor()
        {
            // ✅ Use AsNoTracking() for read-only list + build the joins as IQueryable (do NOT materialize early)
            var supervisors = await _context.CtSupervisors
                .AsNoTracking()
                .Join(
                    _context.SyHeadcounts.AsNoTracking().Where(h => h.Available == 1),
                    s => s.FkHeadcount,
                    h => h.PkHeadcount,
                    (s, h) => new { s, h }
                )
                .Join(
                    _context.CtDepartments.AsNoTracking().Where(d => d.Available == 1),
                    temp => temp.s.FkDepartment,
                    d => d.PkDepartment,
                    (temp, d) => new { temp.s, temp.h, d }
                )
                .Join(
                    _context.CtPositions.AsNoTracking().Where(p => p.Available == 1),
                    temp => temp.s.FkPosition,
                    p => p.PkPosition,
                    (temp, p) => new
                    {
                        temp.s.PkSupervisorId,
                        temp.h.ControlNumber,
                        FullName = (temp.h.Names + " " + (temp.h.LastName ?? "") + " " + (temp.h.SecondName ?? "")).Trim(),
                        temp.s.Available,
                        DepartmentName = temp.d.NameDeparment,
                        PositionName = p.NamePosition
                    }
                )
                .ToListAsync();

            return View("Supervisor/IndexSupervisor", supervisors);
        }

        /// <summary>
        /// Raw export of every column in CtSupervisors, with no joins or translations,
        /// so staff can cross-check the data behind the Supervisors catalog.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> ExportSupervisorFullData()
        {
            var data = await _context.CtSupervisors.AsNoTracking().ToListAsync();
            var bytes = RawExcelExportHelper.ExportFullData(data, "Supervisors");
            return File(bytes, RawExcelExportHelper.ExcelContentType, RawExcelExportHelper.BuildFileName("Supervisors"));
        }

        /// <summary>
        /// Displays the form to create a new supervisor.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> CreateSupervisor()
        {
            // ✅ AsNoTracking for dropdown data (read-only)
            ViewBag.Headcount = await _context.SyHeadcounts
                .AsNoTracking()
                .Where(h => h.Available == 1)
                .OrderBy(h => h.ControlNumber)
                .ToListAsync();

            ViewBag.Departments = await _context.CtDepartments
                .AsNoTracking()
                .Where(d => d.Available == 1)
                .OrderBy(d => d.NameDeparment)
                .ToListAsync();

            ViewBag.Positions = await _context.CtPositions
                .AsNoTracking()
                .Where(p => p.Available == 1)
                .OrderBy(p => p.NamePosition)
                .ToListAsync();

            return View("Supervisor/CreateSupervisor");
        }

        /// <summary>
        /// Creates a new supervisor.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupervisor(CtSupervisor ctSupervisor)
        {
            // Validate unique combination FkHeadcount + FkDepartment for ACTIVE supervisors
            bool combinationExists = await _context.CtSupervisors
                .AnyAsync(s =>
                    s.FkHeadcount == ctSupervisor.FkHeadcount &&
                    s.FkDepartment == ctSupervisor.FkDepartment &&
                    s.Available == 1);

            if (combinationExists)
            {
                TempData["ErrorMessage"] =
                    SupervisorMessages.SupervisorWithTheSameHeadcountAndDepartment;

                return RedirectToAction(nameof(CreateSupervisor));
            }

            // Audit fields
            ctSupervisor.Createuser = User.Identity?.Name ?? "Unknown";
            ctSupervisor.Createdate = DateTime.Now;
            ctSupervisor.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            ctSupervisor.Lastupdatedate = DateTime.Now;
            ctSupervisor.Available = 1;

            try
            {
                _context.Add(ctSupervisor);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = SupervisorMessages.SupervisorCreatedSuccessfully;
                return RedirectToAction(nameof(IndexSupervisor));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(SupervisorMessages.ErrorOccurredWhileCreatingTheSupervisorFormat, ex.Message);
                return RedirectToAction(nameof(CreateSupervisor));
            }
        }

        /// <summary>
        /// Displays the form to edit an existing supervisor.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditSupervisor(int? id)
        {
            if (id == null) return NotFound();

            // ✅ AsNoTracking on GET (we will load tracked entity on POST to update)
            var ctSupervisor = await _context.CtSupervisors
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.PkSupervisorId == id);

            if (ctSupervisor == null) return NotFound();

            // ✅ AsNoTracking for dropdown data
            ViewBag.Headcount = await _context.SyHeadcounts
                .AsNoTracking()
                .Where(h => h.Available == 1)
                .OrderBy(h => h.ControlNumber)
                .ToListAsync();

            ViewBag.Departments = await _context.CtDepartments
                .AsNoTracking()
                .Where(d => d.Available == 1)
                .OrderBy(d => d.NameDeparment)
                .ToListAsync();

            ViewBag.Positions = await _context.CtPositions
                .AsNoTracking()
                .Where(p => p.Available == 1)
                .OrderBy(p => p.NamePosition)
                .ToListAsync();

            return View("Supervisor/EditSupervisor", ctSupervisor);
        }

        /// <summary>
        /// Updates an existing supervisor.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupervisor(int id, CtSupervisor ctSupervisor)
        {
            if (id != ctSupervisor.PkSupervisorId)
            {
                TempData["ErrorMessage"] = SupervisorMessages.SupervisorNotFound;
                return RedirectToAction(nameof(IndexSupervisor));
            }

            // Validate unique combination FkHeadcount + FkDepartment (excluding this record), only for ACTIVE ones
            bool combinationExists = await _context.CtSupervisors
                .AnyAsync(s =>
                    s.FkHeadcount == ctSupervisor.FkHeadcount &&
                    s.FkDepartment == ctSupervisor.FkDepartment &&
                    s.PkSupervisorId != id &&
                    s.Available == 1);

            if (combinationExists)
            {
                TempData["ErrorMessage"] =
                    SupervisorMessages.AnotherActiveSupervisorWithTheSameHeadcount;
                return RedirectToAction(nameof(EditSupervisor), new { id });
            }

            try
            {
                var existing = await _context.CtSupervisors.FindAsync(id);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = SupervisorMessages.SupervisorNoLongerExists;
                    return RedirectToAction(nameof(IndexSupervisor));
                }

                existing.FkHeadcount = ctSupervisor.FkHeadcount;
                existing.FkDepartment = ctSupervisor.FkDepartment;
                existing.FkPosition = ctSupervisor.FkPosition;
                existing.Lastupdateuser = User.Identity?.Name ?? "Unknown";
                existing.Lastupdatedate = DateTime.Now;

                _context.Update(existing);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = SupervisorMessages.SupervisorUpdatedSuccessfully;
                return RedirectToAction(nameof(IndexSupervisor));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(SupervisorMessages.ErrorOccurredWhileUpdatingTheSupervisorFormat, ex.Message);
                return RedirectToAction(nameof(EditSupervisor), new { id });
            }
        }

        /// <summary>
        /// Toggles a supervisor's availability.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSupervisor(int id)
        {
            var supervisor = await _context.CtSupervisors.FindAsync(id);
            if (supervisor == null)
            {
                TempData["ErrorMessage"] = SupervisorMessages.SupervisorNotFound;
                return RedirectToAction(nameof(IndexSupervisor));
            }

            // If enabling, validate no duplicate active combination exists
            if (supervisor.Available == 0)
            {
                bool combinationExists = await _context.CtSupervisors
                    .AnyAsync(s =>
                        s.FkHeadcount == supervisor.FkHeadcount &&
                        s.FkDepartment == supervisor.FkDepartment &&
                        s.PkSupervisorId != id &&
                        s.Available == 1);

                if (combinationExists)
                {
                    TempData["ErrorMessage"] =
                        SupervisorMessages.SupervisorCannotBeEnabledBecauseAnotherActive;
                    return RedirectToAction(nameof(IndexSupervisor));
                }
            }
            else
            {
                var dependency = await _catalogIntegrityService.SupervisorDependencyAsync(id, activeOnly: true);
                if (dependency != null)
                {
                    TempData["ErrorMessage"] = dependency;
                    return RedirectToAction(nameof(IndexSupervisor));
                }
            }

            supervisor.Available = supervisor.Available == 1 ? 0 : 1;
            supervisor.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            supervisor.Lastupdatedate = DateTime.Now;

            _context.Update(supervisor);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = SupervisorMessages.SupervisorStatusUpdatedSuccessfully;
            return RedirectToAction(nameof(IndexSupervisor));
        }

        /// <summary>
        /// Deletes a supervisor.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupervisor(int id)
        {
            // 1) Is any employee (Headcount) linked to this supervisor?
            var dependency = await _catalogIntegrityService.SupervisorDependencyAsync(id);
            if (dependency != null)
            {
                TempData["ErrorMessage"] = dependency;
                return RedirectToAction(nameof(IndexSupervisor));
            }

            // 2) Find supervisor
            var supervisor = await _context.CtSupervisors.FindAsync(id);
            if (supervisor == null)
            {
                TempData["ErrorMessage"] = SupervisorMessages.SupervisorDoesNotExistOrHasAlready;
                return RedirectToAction(nameof(IndexSupervisor));
            }

            try
            {
                _context.CtSupervisors.Remove(supervisor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = SupervisorMessages.SupervisorDeletedSuccessfully;
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] = "The supervisor could not be deleted because another employee started using it. Reassign the employee and try again.";
            }

            return RedirectToAction(nameof(IndexSupervisor));
        }

        private bool CtSupervisorExists(int id)
        {
            return _context.CtSupervisors.Any(e => e.PkSupervisorId == id);
        }
    }
}
