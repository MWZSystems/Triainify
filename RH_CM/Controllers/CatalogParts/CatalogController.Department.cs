// File: Controllers/CatalogController.Department.cs
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
        //[Authorize(Roles = "Administrador, RHGerente, RHAdmin")]
        [Authorize(Policy = "ViewAccess")] // OnBoardingView
        public async Task<IActionResult> IndexDepartment()
        {
            // ✅ Use AsNoTracking() for read-only lists to reduce memory usage (no EF tracking)
            var departments = await _context.CtDepartments
                .AsNoTracking()
                .OrderBy(d => d.NameDeparment)
                .ToListAsync();

            return View("Department/IndexDepartment", departments);
        }

        /// <summary>
        /// Raw export of every column in CtDepartments, with no joins or translations,
        /// so staff can cross-check the data behind the Departments catalog.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> ExportDepartmentFullData()
        {
            var data = await _context.CtDepartments.AsNoTracking().ToListAsync();
            var bytes = RawExcelExportHelper.ExportFullData(data, "Departments");
            return File(bytes, RawExcelExportHelper.ExcelContentType, RawExcelExportHelper.BuildFileName("Departments"));
        }

        public IActionResult CreateDepartment() => View("Department/CreateDepartment");

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment(CtDepartment ctDepartment)
        {
            if (await _context.CtDepartments.AnyAsync(d => d.NameDeparment == ctDepartment.NameDeparment))
            {
                TempData["ErrorMessage"] = DepartmentMessages.DepartmentNameAlreadyExistsPleaseChooseA;
                return View("Department/CreateDepartment", ctDepartment);
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

                TempData["SuccessMessage"] = DepartmentMessages.DepartmentCreatedSuccessfully;
                return RedirectToAction(nameof(IndexDepartment));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(DepartmentMessages.ErrorOccurredWhileCreatingTheDepartmentFormat, ex.Message);
                return View("Department/CreateDepartment", ctDepartment);
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

            return department == null ? NotFound() : View("Department/EditDepartment", department);
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
                TempData["ErrorMessage"] = DepartmentMessages.DepartmentNameAlreadyExists;
                return View("Department/EditDepartment", ctDepartment);
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

                TempData["SuccessMessage"] = DepartmentMessages.DepartmentUpdatedSuccessfully;
                return RedirectToAction(nameof(IndexDepartment));
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = DepartmentMessages.ConcurrencyErrorOccurredWhileUpdatingTheDepartment;
                return View("Department/EditDepartment", ctDepartment);
            }
        }

        [HttpPost]
        [Route("ToggleDepartment")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleDepartment(int id)
        {
            var department = await _context.CtDepartments.FindAsync(id);
            if (department == null)
            {
                TempData["ErrorMessage"] = DepartmentMessages.DepartmentDoesNotExistOrHasAlready;
                return RedirectToAction(nameof(IndexDepartment));
            }

            if (department.Available == 1)
            {
                var dependency = await _catalogIntegrityService.DepartmentDependencyAsync(id, activeOnly: true);
                if (dependency != null)
                {
                    TempData["ErrorMessage"] = dependency;
                    return RedirectToAction(nameof(IndexDepartment));
                }
            }

            department.Available = department.Available == 1 ? 0 : 1;
            department.Lastupdateuser = User.Identity?.Name ?? "Unknown";
            department.Lastupdatedate = DateTime.Now;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = department.Available == 1
                ? "Department enabled successfully."
                : "Department disabled successfully.";

            return RedirectToAction(nameof(IndexDepartment));
        }

        /// <summary>
        /// Deletes a department.
        /// </summary>
        [HttpPost]
        [Route("DeleteDepartment")]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            // 1) Is there any headcount linked to this Department?
            var dependency = await _catalogIntegrityService.DepartmentDependencyAsync(id);
            if (dependency != null)
            {
                TempData["ErrorMessage"] = dependency;
                return RedirectToAction(nameof(IndexDepartment));
            }

            // 2) Find the department
            var department = await _context.CtDepartments.FindAsync(id);
            if (department == null)
            {
                TempData["ErrorMessage"] = DepartmentMessages.DepartmentDoesNotExistOrHasAlready;
                return RedirectToAction(nameof(IndexDepartment));
            }

            try
            {
                _context.CtDepartments.Remove(department);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = DepartmentMessages.DepartmentDeletedSuccessfully;
            }
            catch (DbUpdateException)
            {
                // If a relationship was created after our validation, the DB will block deletion here
                TempData["ErrorMessage"] = "The department could not be deleted because another record started using it. Unlink it and try again.";
            }

            return RedirectToAction(nameof(IndexDepartment));
        }

        private bool CtDepartmentExists(int id)
        {
            return _context.CtDepartments.Any(e => e.PkDepartment == id);
        }
    }
}
