using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using System.Linq;
using System.Threading.Tasks;
using RH_CM.Messages.Identity;
using RH_CM.Service.Export;

namespace RH_CM.Controllers
{
    public class RolesController : Controller
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _contexto;

        public RolesController(RoleManager<IdentityRole> roleManager, ApplicationDbContext contexto)
        {
            _roleManager = roleManager;
            _contexto = contexto;
        }

        [HttpGet]
        //[Authorize(Policy = "ViewAccess")]
        public IActionResult Index()
        {
            var roles = _contexto.Roles.ToList();
            return View(roles);
        }

        /// <summary>
        /// Raw export of every column in AspNetRoles, with no joins or translations,
        /// so staff can cross-check the data behind the Roles catalog.
        /// </summary>
        [HttpGet]
        public IActionResult ExportRolesFullData()
        {
            var data = _contexto.Roles.AsNoTracking().ToList();
            var bytes = RawExcelExportHelper.ExportFullData(data, "Roles");
            return File(bytes, RawExcelExportHelper.ExcelContentType, RawExcelExportHelper.BuildFileName("Roles"));
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public IActionResult Crear()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> Crear(IdentityRole rol)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (await _roleManager.RoleExistsAsync(rol.Name!))
            {
                TempData["ErrorMessage"] = RolesMessages.RoleAlreadyExists;
                return RedirectToAction(nameof(Index));
            }

            // Create the role
            await _roleManager.CreateAsync(new IdentityRole() { Name = rol.Name });

            TempData["SuccessMessage"] = RolesMessages.RoleCreatedSuccessfully;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public IActionResult Editar(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return View();
            }
            else
            {
                // Load the role to edit
                var rolBD = _contexto.Roles.FirstOrDefault(r => r.Id == id);
                return View(rolBD);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> Editar(IdentityRole rol)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (rol == null || string.IsNullOrWhiteSpace(rol.Id))
            {
                TempData["ErrorMessage"] = RolesMessages.InvalidRequest;
                return RedirectToAction(nameof(Index));
            }

            // Get existing role from DB
            var rolBD = await _roleManager.FindByIdAsync(rol.Id);
            if (rolBD == null)
            {
                TempData["ErrorMessage"] = RolesMessages.RoleDoesNotExist;
                return RedirectToAction(nameof(Index));
            }

            // If the name is being changed, check if the new name is already in use
            if (!string.Equals(rolBD.Name, rol.Name, StringComparison.OrdinalIgnoreCase)
                && await _roleManager.RoleExistsAsync(rol.Name!))
            {
                TempData["ErrorMessage"] = RolesMessages.RoleAlreadyExists;
                return RedirectToAction(nameof(Index));
            }

            rolBD.Name = rol.Name;
            rolBD.NormalizedName = rol.Name!.ToUpperInvariant();

            var result = await _roleManager.UpdateAsync(rolBD);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = string.Format(RolesMessages.ErrorUpdatingRoleFormat, errors);
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = RolesMessages.RoleEditedSuccessfully;
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> Borrar(string id)
        {
            var rolBD = _contexto.Roles.FirstOrDefault(r => r.Id == id);
            if (rolBD == null)
            {
                TempData["ErrorMessage"] = RolesMessages.RoleDoesNotExist;
                return RedirectToAction(nameof(Index));
            }

            var usuariosParaEsteRol = _contexto.UserRoles.Count(u => u.RoleId == id);
            if (usuariosParaEsteRol > 0)
            {
                TempData["ErrorMessage"] = RolesMessages.RoleHasUsersAssignedItCannotBe;
                return RedirectToAction(nameof(Index));
            }

            await _roleManager.DeleteAsync(rolBD);

            TempData["SuccessMessage"] = RolesMessages.RoleDeletedSuccessfully;
            return RedirectToAction(nameof(Index));
        }
    }
}