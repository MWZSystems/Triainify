using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Models;

namespace RH_CM.Controllers
{
    public partial class CatalogController
    {

        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> IndexSupervisor()
        {
            var supervisors = _context.CtSupervisors
                //.Where(s => s.Available == 1)
                .Join(_context.SyHeadcounts.Where(h => h.Available == 1),
                    s => s.FkHeadcount,
                    h => h.PkHeadcount,
                    (s, h) => new { s, h })
                .Join(_context.CtDepartments.Where(d => d.Available == 1),
                    temp => temp.s.FkDepartment,
                    d => d.PkDepartment,
                    (temp, d) => new { temp.s, temp.h, d })
                .Join(_context.CtPositions.Where(p => p.Available == 1),
                    temp => temp.s.FkPosition,
                    p => p.PkPosition,
                    (temp, p) => new
                    {
                        temp.s.PkSupervisorId,
                        temp.h.ControlNumber,
                        FullName = $"{temp.h.Names} {(temp.h.LastName ?? "")} {(temp.h.SecondName ?? "")}".Trim(),
                        temp.s.Available,
                        DepartmentName = temp.d.NameDeparment,
                        PositionName = p.NamePosition
                    })
                .ToList();

            return View(supervisors);
        }

        // GET: CtSupervisor/Create
        [Authorize(Roles = "Administrador, RHGerente")]
        public IActionResult CreateSupervisor()
        {
            ViewBag.Headcount = _context.SyHeadcounts.Where(d => d.Available == 1).ToList();
            ViewBag.Departments = _context.CtDepartments.Where(d => d.Available == 1).ToList();
            ViewBag.Positions = _context.CtPositions.Where(p => p.Available == 1).ToList();

            return View();
        }


        // POST: CtSupervisor/Create
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupervisor(CtSupervisor ctSupervisor)
        {
            // Validar combinación única FkHeadcount + FkDepartment
            bool combinationExists = await _context.CtSupervisors
                .AnyAsync(s => s.FkHeadcount == ctSupervisor.FkHeadcount && s.FkDepartment == ctSupervisor.FkDepartment && ctSupervisor.Available == 1);

            if (combinationExists)
            {
                TempData["ErrorMessage"] = "A supervisor with the same Headcount and Department already exists.";
                // Redirigir para mostrar el mensaje en la vista
                return RedirectToAction(nameof(CreateSupervisor));
            }

            // Asignar datos de auditoría
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
                TempData["ErrorMessage"] = $"Error creating supervisor: {ex.Message}";
                return RedirectToAction(nameof(CreateSupervisor));
            }
        }

        // GET: CtSupervisor/Edit/5
        [Authorize(Roles = "Administrador, RHGerente")]
        public async Task<IActionResult> EditSupervisor(int? id)
        {
            if (id == null)
                return NotFound();

            var ctSupervisor = await _context.CtSupervisors.FindAsync(id);
            if (ctSupervisor == null)
                return NotFound();

            // Cargar los dropdowns
            ViewBag.Headcount = _context.SyHeadcounts.Where(h => h.Available == 1).ToList();
            ViewBag.Departments = _context.CtDepartments.Where(d => d.Available == 1).ToList();
            ViewBag.Positions = _context.CtPositions.Where(p => p.Available == 1).ToList();

            return View(ctSupervisor);
        }

        // POST: CtSupervisor/Edit/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditSupervisor(int id, CtSupervisor ctSupervisor)
        {
            if (id != ctSupervisor.PkSupervisorId)
            {
                TempData["ErrorMessage"] = "Supervisor not found.";
                return RedirectToAction(nameof(IndexSupervisor));
            }

            // Validar combinación única FkHeadcount + FkDepartment (excluyendo el mismo registro)
            bool combinationExists = await _context.CtSupervisors
                .AnyAsync(s => s.FkHeadcount == ctSupervisor.FkHeadcount
                            && s.FkDepartment == ctSupervisor.FkDepartment
                            && s.PkSupervisorId != id
                            && s.Available == 1);

            if (combinationExists)
            {
                TempData["ErrorMessage"] = "Another supervisor with the same Headcount and Department already exists.";
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
                TempData["ErrorMessage"] = $"Error updating supervisor: {ex.Message}";
                return RedirectToAction(nameof(EditSupervisor), new { id });
            }
        }


        // POST: /Catalog/ToggleSupervisor/5
        [HttpPost]
        [Authorize(Roles = "Administrador, RHGerente")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSupervisor(int id)
        {
            var supervisor = await _context.CtSupervisors.FindAsync(id);
            if (supervisor == null)
            {
                TempData["ErrorMessage"] = "Supervisor not found.";
                return RedirectToAction(nameof(IndexSupervisor));
            }

            // Si se va a habilitar, validar que no exista combinación duplicada
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
                    TempData["ErrorMessage"] = "Cannot enable this supervisor because another active supervisor with the same Headcount and Department already exists.";
                    return RedirectToAction(nameof(IndexSupervisor));
                }
            }

            // Alternar el estado
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
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSupervisor(int id)
        {
            var supervisor = await _context.CtSupervisors.FindAsync(id);
            if (supervisor != null)
            {
                _context.CtSupervisors.Remove(supervisor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(IndexSupervisor));
        }

        private bool CtSupervisorExists(int id)
        {
            return _context.CtSupervisors.Any(e => e.PkSupervisorId == id);
        }
    }
}
