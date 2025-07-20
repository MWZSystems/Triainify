// File: Controllers/CatalogController.Department.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> IndexDepartment()
        {
            var departments = await _context.CtDepartments.ToListAsync();
            return View(departments);
        }

        public IActionResult CreateDepartment() => View();

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
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

        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditDepartment(int? id)
        {
            if (id == null) return NotFound();

            var department = await _context.CtDepartments.FindAsync(id);
            return department == null ? NotFound() : View(department);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
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
                TempData["ErrorMessage"] = "Concurrency error occurred.";
                return View(ctDepartment);
            }
        }

        [HttpPost]
        [Route("ToggleDepartment")]
        [Authorize(Roles = "Administrador, RHGerente")]
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

        [HttpPost]
        [Route("DeleteDepartment")]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var department = await _context.CtDepartments.FindAsync(id);
            if (department != null)
            {
                _context.CtDepartments.Remove(department);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexDepartment));
        }

        private bool CtDepartmentExists(int id)
        {
            return _context.CtDepartments.Any(e => e.PkDepartment == id);
        }
    }
}
