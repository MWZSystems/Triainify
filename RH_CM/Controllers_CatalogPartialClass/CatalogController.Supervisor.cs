// File: Controllers/CatalogController.Supervisor.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;

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

            return View(supervisors);
        }

        // GET: CtSupervisor/Create
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

            return View();
        }

        // POST: CtSupervisor/Create
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
                    "A supervisor with the same Headcount and Department already exists and is active.";

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

                TempData["SuccessMessage"] = "Supervisor created successfully.";
                return RedirectToAction(nameof(IndexSupervisor));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the supervisor: {ex.Message}";
                return RedirectToAction(nameof(CreateSupervisor));
            }
        }

        // GET: CtSupervisor/Edit/5
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

            return View(ctSupervisor);
        }

        // POST: CtSupervisor/Edit/5
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupervisor(int id, CtSupervisor ctSupervisor)
        {
            if (id != ctSupervisor.PkSupervisorId)
            {
                TempData["ErrorMessage"] = "Supervisor not found.";
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
                    "Another active supervisor with the same Headcount and Department already exists.";
                return RedirectToAction(nameof(EditSupervisor), new { id });
            }

            try
            {
                var existing = await _context.CtSupervisors.FindAsync(id);
                if (existing == null)
                {
                    TempData["ErrorMessage"] = "Supervisor no longer exists.";
                    return RedirectToAction(nameof(IndexSupervisor));
                }

                existing.FkHeadcount = ctSupervisor.FkHeadcount;
                existing.FkDepartment = ctSupervisor.FkDepartment;
                existing.FkPosition = ctSupervisor.FkPosition;
                existing.Lastupdateuser = User.Identity?.Name ?? "Unknown";
                existing.Lastupdatedate = DateTime.Now;

                _context.Update(existing);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Supervisor updated successfully.";
                return RedirectToAction(nameof(IndexSupervisor));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while updating the supervisor: {ex.Message}";
                return RedirectToAction(nameof(EditSupervisor), new { id });
            }
        }

        // POST: /Catalog/ToggleSupervisor/5
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSupervisor(int id)
        {
            var supervisor = await _context.CtSupervisors.FindAsync(id);
            if (supervisor == null)
            {
                TempData["ErrorMessage"] = "Supervisor not found.";
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
                        "This supervisor cannot be enabled because another active supervisor with the same Headcount and Department already exists.";
                    return RedirectToAction(nameof(IndexSupervisor));
                }
            }

            supervisor.Available = supervisor.Available == 1 ? 0 : 1;
            supervisor.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            supervisor.Lastupdatedate = DateTime.Now;

            _context.Update(supervisor);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Supervisor status updated successfully.";
            return RedirectToAction(nameof(IndexSupervisor));
        }

        // POST: /Catalog/DeleteSupervisor/5
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupervisor(int id)
        {
            // 1) Is any employee (Headcount) linked to this supervisor?
            bool hasHeadcount = await _context.SyHeadcounts
                .AsNoTracking()
                .AnyAsync(h => h.FkSupervisorId == id);

            if (hasHeadcount)
            {
                TempData["ErrorMessage"] =
                    "This supervisor cannot be deleted because it is linked to employees (Headcount). " +
                    "Please remove the supervisor from employees first.";
                return RedirectToAction(nameof(IndexSupervisor));
            }

            // 2) Find supervisor
            var supervisor = await _context.CtSupervisors.FindAsync(id);
            if (supervisor == null)
            {
                TempData["ErrorMessage"] = "The supervisor does not exist or has already been deleted.";
                return RedirectToAction(nameof(IndexSupervisor));
            }

            try
            {
                _context.CtSupervisors.Remove(supervisor);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Supervisor deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["ErrorMessage"] =
                    "This supervisor cannot be deleted because it is linked to employees (Headcount). " +
                    "Please remove the supervisor from employees first.";
            }

            return RedirectToAction(nameof(IndexSupervisor));
        }

        private bool CtSupervisorExists(int id)
        {
            return _context.CtSupervisors.Any(e => e.PkSupervisorId == id);
        }
    }
}
