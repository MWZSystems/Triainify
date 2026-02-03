using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Data;
using System.Linq;
using System.Threading.Tasks;

namespace RH_CM.Controllers
{
    public class RolesController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _contexto;

        public RolesController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext contexto)
        {
            _userManager = userManager;
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
            if (await _roleManager.RoleExistsAsync(rol.Name!))
            {
                // Mensaje original: "El rol ya existe"
                TempData["Error"] = "The role already exists";
                return RedirectToAction(nameof(Index));
            }

            //Se crea el rol
            await _roleManager.CreateAsync(new IdentityRole() { Name = rol.Name });

            // Mensaje original: "Rol creado correctamente"
            TempData["Correcto"] = "Role created successfully";
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
                //Actualizar el rol
                var rolBD = _contexto.Roles.FirstOrDefault(r => r.Id == id);
                return View(rolBD);
            }

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> Editar(IdentityRole rol)
        {
            if (rol == null || string.IsNullOrWhiteSpace(rol.Id))
            {
                TempData["Error"] = "Invalid request.";
                return RedirectToAction(nameof(Index));
            }

            // Get existing role from DB
            var rolBD = await _roleManager.FindByIdAsync(rol.Id);
            if (rolBD == null)
            {
                TempData["Error"] = "The role does not exist";
                return RedirectToAction(nameof(Index));
            }

            // If the name is being changed, check if the new name is already in use
            if (!string.Equals(rolBD.Name, rol.Name, StringComparison.OrdinalIgnoreCase)
                && await _roleManager.RoleExistsAsync(rol.Name!))
            {
                TempData["Error"] = "The role already exists";
                return RedirectToAction(nameof(Index));
            }

            rolBD.Name = rol.Name;
            rolBD.NormalizedName = rol.Name!.ToUpperInvariant();

            var result = await _roleManager.UpdateAsync(rolBD);

            if (!result.Succeeded)
            {
                var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
                TempData["Error"] = $"Error updating role: {errors}";
                return RedirectToAction(nameof(Index));
            }

            TempData["Correcto"] = "Role edited successfully";
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
                // Mensaje original: "No existe el rol"
                TempData["Error"] = "The role does not exist";
                return RedirectToAction(nameof(Index));
            }

            var usuariosParaEsteRol = _contexto.UserRoles.Where(u => u.RoleId == id).Count();
            if (usuariosParaEsteRol > 0)
            {
                // Mensaje original: "El rol tiene usuarios, no se puede borrar"
                TempData["Error"] = "The role has users assigned, it cannot be deleted";
                return RedirectToAction(nameof(Index));
            }

            await _roleManager.DeleteAsync(rolBD);

            // Mensaje original: "Rol borrado correctamente"
            TempData["Correcto"] = "Role deleted successfully";
            return RedirectToAction(nameof(Index));
        }
    }
}