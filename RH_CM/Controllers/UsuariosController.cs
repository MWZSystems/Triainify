using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.ViewModels;
using RH_CM.Models;
using System.Security.Claims;
using RH_CM.Claims;
using static RH_CM.ViewModels.ClaimsUsuarioViewModel;


namespace RH_CM.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ApplicationDbContext _contexto;
        public UsuariosController(UserManager<IdentityUser> userManager, ApplicationDbContext contexto)
        {
            _userManager = userManager;
            _contexto = contexto;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Index()
        {
            var usuarios = await _contexto.AppUsuario.ToListAsync();
            var rolesUsuario = await _contexto.UserRoles.ToListAsync();
            var roles = await _contexto.Roles.ToListAsync();
            foreach (var usuario in usuarios)
            {
                var rol = rolesUsuario.FirstOrDefault(u => u.UserId == usuario.Id);
                if (rol == null)
                {
                    usuario.Rol = "None";
                }
                else
                {
                    usuario.Rol = roles.FirstOrDefault(u => u.Id == rol.RoleId)?.Name ?? "None";
                }
            }

            return View(usuarios);
        }

        private async Task<AppUsuario?> GetUserWithRoleAsync(string id)
        {
            var usuario = await _contexto.AppUsuario.FirstOrDefaultAsync(u => u.Id == id);
            if (usuario == null)
            {
                return null;
            }

            var rolUsuario = await _contexto.UserRoles.FirstOrDefaultAsync(u => u.UserId == usuario.Id);
            if (rolUsuario == null)
            {
                usuario.Rol = "None";
                return usuario;
            }

            var rol = await _contexto.Roles.FirstOrDefaultAsync(r => r.Id == rolUsuario.RoleId);
            usuario.Rol = rol?.Name ?? "None";

            return usuario;
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> UserDetail(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Invalid request: user id is required.";
                return RedirectToAction(nameof(Index));
            }

            var usuario = await GetUserWithRoleAsync(id);
            if (usuario == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(usuario);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Crear()
        {
            var modelo = new AppUsuario();

            modelo.ListaRoles = await _contexto.Roles
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id
                })
                .ToListAsync();

            return View(modelo);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(AppUsuario usuario)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid request: please review the submitted data.";
                return RedirectToAction(nameof(Crear));
            }

            if (usuario == null)
            {
                TempData["Error"] = "Invalid request: user data is empty.";
                return RedirectToAction(nameof(Crear));
            }

            if (string.IsNullOrWhiteSpace(usuario.UserName))
            {
                TempData["Error"] = "Username is required.";
                return RedirectToAction(nameof(Crear));
            }

            if (string.IsNullOrWhiteSpace(usuario.Email))
            {
                TempData["Error"] = "Email is required.";
                return RedirectToAction(nameof(Crear));
            }

            if (string.IsNullOrWhiteSpace(usuario.Names) || string.IsNullOrWhiteSpace(usuario.LastName))
            {
                TempData["Error"] = "First name and last name are required.";
                return RedirectToAction(nameof(Crear));
            }

            if (string.IsNullOrWhiteSpace(usuario.IdRol))
            {
                TempData["Error"] = "You must select a role to continue.";
                return RedirectToAction(nameof(Crear));
            }

            var rol = await _contexto.Roles.FirstOrDefaultAsync(r => r.Id == usuario.IdRol);
            if (rol == null)
            {
                TempData["Error"] = "The selected role does not exist.";
                return RedirectToAction(nameof(Crear));
            }

            var existeUserName = await _userManager.FindByNameAsync(usuario.UserName);
            if (existeUserName != null)
            {
                TempData["Error"] = "This username already exists. Please choose another one.";
                return RedirectToAction(nameof(Crear));
            }

            var existeEmail = await _userManager.FindByEmailAsync(usuario.Email);
            if (existeEmail != null)
            {
                TempData["Error"] = "This email is already in use.";
                return RedirectToAction(nameof(Crear));
            }

            var nuevoUsuario = new AppUsuario
            {
                UserName = usuario.UserName,
                Email = usuario.Email,
                Names = usuario.Names,
                LastName = usuario.LastName,
                EmailConfirmed = true,
                LockoutEnabled = true
            };

            IdentityResult createResult = await _userManager.CreateAsync(nuevoUsuario);

            if (!createResult.Succeeded)
            {
                var errores = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                TempData["Error"] = $"Could not create the user: {errores}";
                return RedirectToAction(nameof(Crear));
            }

            var addRoleResult = await _userManager.AddToRoleAsync(nuevoUsuario, rol.Name);
            if (!addRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(nuevoUsuario);
                var errores = string.Join(" | ", addRoleResult.Errors.Select(e => e.Description));
                TempData["Error"] = $"Could not assign the selected role: {errores}";
                return RedirectToAction(nameof(Crear));
            }

            await _contexto.SaveChangesAsync();

            TempData["Correcto"] = "User created and role assigned successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Editar(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Invalid request: user id is required.";
                return RedirectToAction(nameof(Index));
            }

            var usuarioBD = await _contexto.AppUsuario.FirstOrDefaultAsync(u => u.Id == id);
            if (usuarioBD == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var rolUsuario = await _contexto.UserRoles.FirstOrDefaultAsync(u => u.UserId == usuarioBD.Id);
            if (rolUsuario != null)
            {
                var rolActual = await _contexto.Roles.FirstOrDefaultAsync(u => u.Id == rolUsuario.RoleId);
                if (rolActual != null)
                {
                    usuarioBD.IdRol = rolActual.Id;
                }
            }

            usuarioBD.ListaRoles = await _contexto.Roles
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id
                })
                .ToListAsync();

            return View(usuarioBD);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(AppUsuario usuario)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Invalid request: please review the submitted data.";
                return RedirectToAction(nameof(Editar), new { id = usuario?.Id });
            }

            if (usuario == null || string.IsNullOrWhiteSpace(usuario.Id))
            {
                TempData["Error"] = "Invalid request: incomplete user data.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(usuario.IdRol))
            {
                TempData["Error"] = "You must select a role to continue.";
                return RedirectToAction(nameof(Editar), new { id = usuario.Id });
            }

            var usuarioBD = await _contexto.AppUsuario.FirstOrDefaultAsync(u => u.Id == usuario.Id);
            if (usuarioBD == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var nuevoRol = await _contexto.Roles.FirstOrDefaultAsync(r => r.Id == usuario.IdRol);
            if (nuevoRol == null)
            {
                TempData["Error"] = "The selected role does not exist.";
                return RedirectToAction(nameof(Editar), new { id = usuario.Id });
            }

            try
            {
                var rolUsuario = await _contexto.UserRoles.FirstOrDefaultAsync(u => u.UserId == usuarioBD.Id);
                if (rolUsuario != null)
                {
                    var rolActualNombre = await _contexto.Roles
                        .Where(u => u.Id == rolUsuario.RoleId)
                        .Select(e => e.Name)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrWhiteSpace(rolActualNombre))
                    {
                        var removeResult = await _userManager.RemoveFromRoleAsync(usuarioBD, rolActualNombre);
                        if (!removeResult.Succeeded)
                        {
                            var errores = string.Join(" | ", removeResult.Errors.Select(e => e.Description));
                            TempData["Error"] = $"Could not remove the current role: {errores}";
                            return RedirectToAction(nameof(Editar), new { id = usuario.Id });
                        }
                    }
                }

                var addResult = await _userManager.AddToRoleAsync(usuarioBD, nuevoRol.Name);
                if (!addResult.Succeeded)
                {
                    var errores = string.Join(" | ", addResult.Errors.Select(e => e.Description));
                    TempData["Error"] = $"Could not assign the new role: {errores}";
                    return RedirectToAction(nameof(Editar), new { id = usuario.Id });
                }

                await _contexto.SaveChangesAsync();
                TempData["Correcto"] = "Changes saved successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while saving changes: {ex.Message}";
                return RedirectToAction(nameof(Editar), new { id = usuario.Id });
            }
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BloquearDesbloquear(string idUsuario)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
            {
                TempData["Error"] = "Invalid request: user id is required.";
                return RedirectToAction(nameof(Index));
            }

            var usuariBD = await _contexto.AppUsuario.FirstOrDefaultAsync(u => u.Id == idUsuario);
            if (usuariBD == null)
            {
                return NotFound();
            }

            if (usuariBD.LockoutEnd != null && usuariBD.LockoutEnd > DateTime.Now)
            {
                usuariBD.LockoutEnd = DateTime.Now;
                TempData["Correcto"] = "User unlocked successfully.";
            }
            else
            {
                usuariBD.LockoutEnd = DateTime.Now.AddYears(100);
                TempData["Error"] = "User locked successfully.";
            }

            await _contexto.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Borrar(string idUsuario)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
            {
                TempData["Error"] = "Invalid request: user id is required.";
                return RedirectToAction(nameof(Index));
            }

            var usuariBD = await _contexto.AppUsuario.FirstOrDefaultAsync(u => u.Id == idUsuario);
            if (usuariBD == null)
            {
                return NotFound();
            }

            _contexto.AppUsuario.Remove(usuariBD);
            await _contexto.SaveChangesAsync();
            TempData["Correcto"] = "User deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditarPerfil(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var usuarioBd = await _contexto.AppUsuario.FindAsync(id);
            if (usuarioBd == null)
            {
                return NotFound();
            }

            return View(usuarioBd);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPerfil(AppUsuario appUsuario)
        {
            if (ModelState.IsValid)
            {
                var usuario = await _contexto.AppUsuario.FindAsync(appUsuario.Id);
                if (usuario == null)
                {
                    return NotFound();
                }

                usuario.UserName = appUsuario.Ntuser;
                usuario.EmployeeNumber = appUsuario.EmployeeNumber;
                usuario.Email = appUsuario.Email;
                usuario.Names = appUsuario.Names;
                usuario.LastName = appUsuario.LastName;
                usuario.Ntuser = appUsuario.Ntuser;

                await _userManager.UpdateAsync(usuario);

                return RedirectToAction(nameof(Index), "Home");
            }
            return View(appUsuario);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> AdministrarClaimsUsuario(string idUsuario)
        {
            IdentityUser usuario = await _userManager.FindByIdAsync(idUsuario);
            if (usuario == null)
            {
                return NotFound();
            }

            var claimUsuarioActual = await _userManager.GetClaimsAsync(usuario);

            var modelo = new ClaimsUsuarioViewModel()
            {
                IdUsuario = idUsuario
            };

            foreach (Claim claim in ManejoClaims.listaClaims)
            {
                ClaimUsuario claimUsuario = new ClaimUsuario
                {
                    TipoClaim = claim.Type
                };
                if (claimUsuarioActual.Any(c => c.Type == claim.Type))
                {
                    claimUsuario.Seleccionado = true;
                }
                modelo.Claims.Add(claimUsuario);
            }

            return View(modelo);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdministrarClaimsUsuario(ClaimsUsuarioViewModel cuViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(cuViewModel);
            }

            IdentityUser usuario = await _userManager.FindByIdAsync(cuViewModel.IdUsuario);
            if (usuario == null)
            {
                return NotFound();
            }

            var claims = await _userManager.GetClaimsAsync(usuario);
            var resultado = await _userManager.RemoveClaimsAsync(usuario, claims);

            if (!resultado.Succeeded)
            {
                return View(cuViewModel);
            }

            resultado = await _userManager.AddClaimsAsync(usuario, cuViewModel.Claims.Where(c => c.Seleccionado)
                .Select(c => new Claim(c.TipoClaim, c.Seleccionado.ToString())));

            if (!resultado.Succeeded)
            {
                return View(cuViewModel);
            }
            TempData["Correcto"] = "Changes saved successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailable(string idUsuario)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
            {
                TempData["Error"] = "Invalid request: user id is required.";
                return RedirectToAction(nameof(Index));
            }

            var usuarioBD = await _contexto.AppUsuario.FirstOrDefaultAsync(u => u.Id == idUsuario);
            if (usuarioBD == null)
            {
                return NotFound();
            }

            usuarioBD.Available = (usuarioBD.Available == 1) ? 0 : 1;

            await _contexto.SaveChangesAsync();

            if (usuarioBD.Available == 1)
            {
                TempData["Correcto"] = "User marked as available.";
            }
            else
            {
                TempData["Correcto"] = "User marked as unavailable.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
