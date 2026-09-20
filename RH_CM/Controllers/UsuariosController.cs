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
using RH_CM.Messages.Identity;
using RH_CM.Service.Export;


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
            var usuarios = await _contexto.AppUsuario.AsNoTracking().ToListAsync();
            var rolesUsuario = await _contexto.UserRoles.AsNoTracking().ToListAsync();
            var roles = await _contexto.Roles.AsNoTracking().ToListAsync();
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

        /// <summary>
        /// Raw export of every column in AppUsuario, with no joins or translations, so staff
        /// can cross-check the data behind the Users catalog. Credential/security columns
        /// (password hash, security stamp, concurrency stamp) are always excluded.
        /// </summary>
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> ExportUsuariosFullData()
        {
            var data = await _contexto.AppUsuario.AsNoTracking().ToListAsync();
            var bytes = RawExcelExportHelper.ExportFullData(data, "Users",
                excludeProperties: new[] { "PasswordHash", "SecurityStamp", "ConcurrencyStamp" });
            return File(bytes, RawExcelExportHelper.ExcelContentType, RawExcelExportHelper.BuildFileName("Users"));
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
                TempData["ErrorMessage"] = UsuariosMessages.InvalidRequestUserIdIsRequired;
                return RedirectToAction(nameof(Index));
            }

            var usuario = await GetUserWithRoleAsync(id);
            if (usuario == null)
            {
                TempData["ErrorMessage"] = UsuariosMessages.UserNotFound;
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
                TempData["ErrorMessage"] = UsuariosMessages.InvalidRequestPleaseReviewTheSubmittedData;
                return RedirectToAction(nameof(Crear));
            }

            if (usuario == null)
            {
                TempData["ErrorMessage"] = UsuariosMessages.InvalidRequestUserDataIsEmpty;
                return RedirectToAction(nameof(Crear));
            }

            if (string.IsNullOrWhiteSpace(usuario.UserName))
            {
                TempData["ErrorMessage"] = UsuariosMessages.UsernameIsRequired;
                return RedirectToAction(nameof(Crear));
            }

            if (string.IsNullOrWhiteSpace(usuario.Email))
            {
                TempData["ErrorMessage"] = UsuariosMessages.EmailIsRequired;
                return RedirectToAction(nameof(Crear));
            }

            if (string.IsNullOrWhiteSpace(usuario.Names) || string.IsNullOrWhiteSpace(usuario.LastName))
            {
                TempData["ErrorMessage"] = UsuariosMessages.FirstNameAndLastNameAreRequired;
                return RedirectToAction(nameof(Crear));
            }

            if (string.IsNullOrWhiteSpace(usuario.IdRol))
            {
                TempData["ErrorMessage"] = UsuariosMessages.YouMustSelectARoleToContinue;
                return RedirectToAction(nameof(Crear));
            }

            var rol = await _contexto.Roles.FirstOrDefaultAsync(r => r.Id == usuario.IdRol);
            if (rol == null)
            {
                TempData["ErrorMessage"] = UsuariosMessages.SelectedRoleDoesNotExist;
                return RedirectToAction(nameof(Crear));
            }

            var existeUserName = await _userManager.FindByNameAsync(usuario.UserName);
            if (existeUserName != null)
            {
                TempData["ErrorMessage"] = UsuariosMessages.UsernameAlreadyExistsPleaseChooseAnotherOne;
                return RedirectToAction(nameof(Crear));
            }

            var existeEmail = await _userManager.FindByEmailAsync(usuario.Email);
            if (existeEmail != null)
            {
                TempData["ErrorMessage"] = UsuariosMessages.EmailIsAlreadyInUse;
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
                TempData["ErrorMessage"] = string.Format(UsuariosMessages.CouldNotCreateTheUserFormat, errores);
                return RedirectToAction(nameof(Crear));
            }

            var addRoleResult = await _userManager.AddToRoleAsync(nuevoUsuario, rol.Name);
            if (!addRoleResult.Succeeded)
            {
                await _userManager.DeleteAsync(nuevoUsuario);
                var errores = string.Join(" | ", addRoleResult.Errors.Select(e => e.Description));
                TempData["ErrorMessage"] = string.Format(UsuariosMessages.CouldNotAssignTheSelectedRoleFormat, errores);
                return RedirectToAction(nameof(Crear));
            }

            await _contexto.SaveChangesAsync();

            TempData["SuccessMessage"] = UsuariosMessages.UserCreatedAndRoleAssignedSuccessfully;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Editar(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = UsuariosMessages.InvalidRequestUserIdIsRequired;
                return RedirectToAction(nameof(Index));
            }

            var usuarioBD = await _contexto.AppUsuario.FirstOrDefaultAsync(u => u.Id == id);
            if (usuarioBD == null)
            {
                TempData["ErrorMessage"] = UsuariosMessages.UserNotFound;
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
        public async Task<IActionResult> Editar(EditUserRoleViewModel usuario)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = UsuariosMessages.InvalidRequestPleaseReviewTheSubmittedData;
                return RedirectToAction(nameof(Editar), new { id = usuario?.Id });
            }

            if (usuario == null || string.IsNullOrWhiteSpace(usuario.Id))
            {
                TempData["ErrorMessage"] = UsuariosMessages.InvalidRequestIncompleteUserData;
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(usuario.IdRol))
            {
                TempData["ErrorMessage"] = UsuariosMessages.YouMustSelectARoleToContinue;
                return RedirectToAction(nameof(Editar), new { id = usuario.Id });
            }

            var usuarioBD = await _contexto.AppUsuario.FirstOrDefaultAsync(u => u.Id == usuario.Id);
            if (usuarioBD == null)
            {
                TempData["ErrorMessage"] = UsuariosMessages.UserNotFound;
                return RedirectToAction(nameof(Index));
            }

            var nuevoRol = await _contexto.Roles.FirstOrDefaultAsync(r => r.Id == usuario.IdRol);
            if (nuevoRol == null)
            {
                TempData["ErrorMessage"] = UsuariosMessages.SelectedRoleDoesNotExist;
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
                            TempData["ErrorMessage"] = string.Format(UsuariosMessages.CouldNotRemoveTheCurrentRoleFormat, errores);
                            return RedirectToAction(nameof(Editar), new { id = usuario.Id });
                        }
                    }
                }

                var addResult = await _userManager.AddToRoleAsync(usuarioBD, nuevoRol.Name);
                if (!addResult.Succeeded)
                {
                    var errores = string.Join(" | ", addResult.Errors.Select(e => e.Description));
                    TempData["ErrorMessage"] = string.Format(UsuariosMessages.CouldNotAssignTheNewRoleFormat, errores);
                    return RedirectToAction(nameof(Editar), new { id = usuario.Id });
                }

                await _contexto.SaveChangesAsync();
                TempData["SuccessMessage"] = UsuariosMessages.ChangesSavedSuccessfully;
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = string.Format(UsuariosMessages.ErrorOccurredWhileSavingChangesFormat, ex.Message);
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
                TempData["ErrorMessage"] = UsuariosMessages.InvalidRequestUserIdIsRequired;
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
                TempData["SuccessMessage"] = UsuariosMessages.UserUnlockedSuccessfully;
            }
            else
            {
                usuariBD.LockoutEnd = DateTime.Now.AddYears(100);
                TempData["SuccessMessage"] = UsuariosMessages.UserLockedSuccessfully;
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
                TempData["ErrorMessage"] = UsuariosMessages.InvalidRequestUserIdIsRequired;
                return RedirectToAction(nameof(Index));
            }

            var usuariBD = await _contexto.AppUsuario.FirstOrDefaultAsync(u => u.Id == idUsuario);
            if (usuariBD == null)
            {
                return NotFound();
            }

            _contexto.AppUsuario.Remove(usuariBD);
            await _contexto.SaveChangesAsync();
            TempData["SuccessMessage"] = UsuariosMessages.UserDeletedSuccessfully;
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
            TempData["SuccessMessage"] = UsuariosMessages.ChangesSavedSuccessfully;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailable(string idUsuario)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
            {
                TempData["ErrorMessage"] = UsuariosMessages.InvalidRequestUserIdIsRequired;
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
                TempData["SuccessMessage"] = UsuariosMessages.UserMarkedAsAvailable;
            }
            else
            {
                TempData["SuccessMessage"] = UsuariosMessages.UserMarkedAsUnavailable;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
