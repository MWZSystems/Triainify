// File: Controllers/CatalogController.Department.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [Authorize(Policy = "ViewAccess")] // OnBoardingView
        public async Task<IActionResult> IndexDepartment()
        {
            // ✅ Use AsNoTracking() for read-only lists to reduce memory usage (no EF tracking)
            var departments = await _context.CtDepartments
                .AsNoTracking()
                .OrderBy(d => d.NameDeparment)
                .ToListAsync();

            return View(departments);
        }

        public IActionResult CreateDepartment() => View();

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment(CtDepartment ctDepartment)
        {
            if (await _context.CtDepartments.AnyAsync(d => d.NameDeparment == ctDepartment.NameDeparment))
            {
                TempData["ErrorMessage"] = "The department name already exists. Please choose a different name.";
                return View(ctDepartment);
            }

            ctDepartment.Createuser = User.Identity?.Name ?? "Unknown";
            ctDepartment.Createdate = DateTime.Now;
            ctDepartment.Lastupdateuser = ctDepartment.Createuser;
            ctDepartment.Lastupdatedate = DateTime.Now;
            ctDepartment.Available = 1;

            try
            {
                _context.Add(ctDepartment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Department created successfully.";
                return RedirectToAction(nameof(IndexDepartment));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred while creating the department: {ex.Message}";
                return View(ctDepartment);
            }
        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> EditDepartment(int? id)
        {
            if (id == null) return NotFound();

            // ✅ No tracking: edit form can work with a detached entity, we re-load tracked entity on POST
            var department = await _context.CtDepartments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.PkDepartment == id);

            return department == null ? NotFound() : View(department);
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDepartment(int id, CtDepartment ctDepartment)
        {
            if (id != ctDepartment.PkDepartment) return NotFound();

            if (await _context.CtDepartments.AnyAsync(d =>
                    d.NameDeparment == ctDepartment.NameDeparment &&
                    d.PkDepartment != ctDepartment.PkDepartment))
            {
                TempData["ErrorMessage"] = "The department name already exists.";
                return View(ctDepartment);
            }

            var existing = await _context.CtDepartments.FindAsync(id);
            if (existing == null) return NotFound();

            existing.NameDeparment = ctDepartment.NameDeparment;
            existing.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            existing.Lastupdatedate = DateTime.Now;

            try
            {
                _context.Update(existing);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Department updated successfully.";
                return RedirectToAction(nameof(IndexDepartment));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "A concurrency error occurred while updating the department. Please try again.";
                return View(ctDepartment);
            }
        }

        [HttpPost]
        [Route("ToggleDepartment")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDepartment(int id)
        {
            var department = await _context.CtDepartments.FindAsync(id);
            if (department != null)
            {
                department.Available = department.Available == 1 ? 0 : 1;
                _context.Update(department);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(IndexDepartment));
        }

        // POST: /CtDepartment/DeleteDepartment/5
        [HttpPost]
        [Route("DeleteDepartment")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            // 1) Is there any headcount linked to this Department?
            bool hasHeadcount = await _context.SyHeadcounts
                .AsNoTracking()
                .AnyAsync(h => h.FkDepartment == id);

            if (hasHeadcount)
            {
                TempData["ErrorMessage"] =
                    "This department cannot be deleted because it is linked to employees (Headcount). " +
                    "Please remove the department from employee assignments first.";
                return RedirectToAction(nameof(IndexDepartment));
            }

            // 2) Find the department
            var department = await _context.CtDepartments.FindAsync(id);
            if (department == null)
            {
                TempData["ErrorMessage"] = "The department does not exist or has already been deleted.";
                return RedirectToAction(nameof(IndexDepartment));
            }

            try
            {
                _context.CtDepartments.Remove(department);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Department deleted successfully.";
            }
            catch (DbUpdateException)
            {
                // If a relationship was created after our validation, the DB will block deletion here
                TempData["ErrorMessage"] =
                    "This department cannot be deleted because it is linked to employees (Headcount). " +
                    "Please remove the department from employee assignments first.";
            }

            return RedirectToAction(nameof(IndexDepartment));
        }

        private bool CtDepartmentExists(int id)
        {
            return _context.CtDepartments.Any(e => e.PkDepartment == id);
        }
    }
}
