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
                    usuario.Rol = "Ninguno";
                }
                else
                {
                    usuario.Rol = roles.FirstOrDefault(u => u.Id == rol.RoleId).Name;
                }
            }

            return View(usuarios);
        }

        // ==============================
        // CREAR USUARIO
        // ==============================

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult Crear()
        {
            var modelo = new AppUsuario();

            // Combo de Roles
            modelo.ListaRoles = _contexto.Roles
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id
                });

            return View(modelo);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(AppUsuario usuario /*, string? password */)
        {
            // Validaciones básicas
            if (usuario == null)
            {
                TempData["Error"] = "Solicitud inválida: datos de usuario vacíos.";
                return RedirectToAction(nameof(Crear));
            }

            if (string.IsNullOrWhiteSpace(usuario.UserName))
            {
                TempData["Error"] = "El nombre de usuario es requerido.";
                return RedirectToAction(nameof(Crear));
            }

            if (string.IsNullOrWhiteSpace(usuario.Email))
            {
                TempData["Error"] = "El correo electrónico es requerido.";
                return RedirectToAction(nameof(Crear));
            }

            if (string.IsNullOrWhiteSpace(usuario.Names) || string.IsNullOrWhiteSpace(usuario.LastName))
            {
                TempData["Error"] = "El nombre y apellido son requeridos.";
                return RedirectToAction(nameof(Crear));
            }

            if (string.IsNullOrWhiteSpace(usuario.IdRol))
            {
                TempData["Error"] = "Debe seleccionar un rol para continuar.";
                return RedirectToAction(nameof(Crear));
            }

            var rol = await _contexto.Roles.FirstOrDefaultAsync(r => r.Id == usuario.IdRol);
            if (rol == null)
            {
                TempData["Error"] = "El rol seleccionado no existe.";
                return RedirectToAction(nameof(Crear));
            }

            // Duplicados
            var existeUserName = await _userManager.FindByNameAsync(usuario.UserName);
            if (existeUserName != null)
            {
                TempData["Error"] = "El nombre de usuario ya existe. Elige otro.";
                return RedirectToAction(nameof(Crear));
            }

            var existeEmail = await _userManager.FindByEmailAsync(usuario.Email);
            if (existeEmail != null)
            {
                TempData["Error"] = "El correo electrónico ya está en uso.";
                return RedirectToAction(nameof(Crear));
            }

            // Construir la entidad nueva (copiar solo campos necesarios)
            var nuevoUsuario = new AppUsuario
            {
                UserName = usuario.UserName,
                Email = usuario.Email,
                Names = usuario.Names,
                LastName = usuario.LastName,
                // Asigna aquí otros campos que uses en tu modelo:
                // EmployeeNumber = usuario.EmployeeNumber,
                // Ntuser = usuario.Ntuser,
                EmailConfirmed = true, // opcional: si quieres confirmación automática
                LockoutEnabled = true
            };

            // Crear usuario en Identity (sin password). Si deseas usar password, descomenta la variante con password.
            IdentityResult createResult = await _userManager.CreateAsync(nuevoUsuario);
            // IdentityResult createResult = await _userManager.CreateAsync(nuevoUsuario, password);

            if (!createResult.Succeeded)
            {
                var errores = string.Join(" | ", createResult.Errors.Select(e => e.Description));
                TempData["Error"] = $"No se pudo crear el usuario: {errores}";
                return RedirectToAction(nameof(Crear));
            }

            // Asignar Rol
            var addRoleResult = await _userManager.AddToRoleAsync(nuevoUsuario, rol.Name);
            if (!addRoleResult.Succeeded)
            {
                // Rollback: si falla asignar el rol, elimina el usuario para no dejarlo inconsistente
                await _userManager.DeleteAsync(nuevoUsuario);
                var errores = string.Join(" | ", addRoleResult.Errors.Select(e => e.Description));
                TempData["Error"] = $"No se pudo asignar el rol seleccionado: {errores}";
                return RedirectToAction(nameof(Crear));
            }

            // Guardar cambios en el contexto, por si usas tablas extendidas
            await _contexto.SaveChangesAsync();

            TempData["Correcto"] = "Usuario creado y rol asignado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        // Editar usuario (Asignación de rol)
        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult Editar(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Solicitud inválida: el identificador del usuario es requerido.";
                return RedirectToAction(nameof(Index));
            }

            var usuarioBD = _contexto.AppUsuario.FirstOrDefault(u => u.Id == id);
            if (usuarioBD == null)
            {
                TempData["Error"] = "El usuario no fue encontrado.";
                return RedirectToAction(nameof(Index));
            }

            // Rol actual del usuario
            var rolUsuario = _contexto.UserRoles.FirstOrDefault(u => u.UserId == usuarioBD.Id);
            if (rolUsuario != null)
            {
                var rolActual = _contexto.Roles.FirstOrDefault(u => u.Id == rolUsuario.RoleId);
                if (rolActual != null)
                {
                    usuarioBD.IdRol = rolActual.Id;
                }
            }

            // Lista de Roles
            usuarioBD.ListaRoles = _contexto.Roles.Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Text = u.Name,
                Value = u.Id
            });

            return View(usuarioBD);
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(AppUsuario usuario)
        {
            // Validaciones básicas
            if (usuario == null || string.IsNullOrWhiteSpace(usuario.Id))
            {
                TempData["Error"] = "Solicitud inválida: datos de usuario incompletos.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(usuario.IdRol))
            {
                TempData["Error"] = "Debe seleccionar un rol para continuar.";
                return RedirectToAction(nameof(Editar), new { id = usuario.Id });
            }

            var usuarioBD = _contexto.AppUsuario.FirstOrDefault(u => u.Id == usuario.Id);
            if (usuarioBD == null)
            {
                TempData["Error"] = "El usuario no fue encontrado.";
                return RedirectToAction(nameof(Index));
            }

            var nuevoRol = _contexto.Roles.FirstOrDefault(r => r.Id == usuario.IdRol);
            if (nuevoRol == null)
            {
                TempData["Error"] = "El rol seleccionado no existe.";
                return RedirectToAction(nameof(Editar), new { id = usuario.Id });
            }

            try
            {
                // Quitar rol actual si existe
                var rolUsuario = _contexto.UserRoles.FirstOrDefault(u => u.UserId == usuarioBD.Id);
                if (rolUsuario != null)
                {
                    var rolActualNombre = _contexto.Roles
                        .Where(u => u.Id == rolUsuario.RoleId)
                        .Select(e => e.Name)
                        .FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(rolActualNombre))
                    {
                        var removeResult = await _userManager.RemoveFromRoleAsync(usuarioBD, rolActualNombre);
                        if (!removeResult.Succeeded)
                        {
                            var errores = string.Join(" | ", removeResult.Errors.Select(e => e.Description));
                            TempData["Error"] = $"No se pudo remover el rol actual: {errores}";
                            return RedirectToAction(nameof(Editar), new { id = usuario.Id });
                        }
                    }
                }

                // Agregar nuevo rol
                var addResult = await _userManager.AddToRoleAsync(usuarioBD, nuevoRol.Name);
                if (!addResult.Succeeded)
                {
                    var errores = string.Join(" | ", addResult.Errors.Select(e => e.Description));
                    TempData["Error"] = $"No se pudo asignar el nuevo rol: {errores}";
                    return RedirectToAction(nameof(Editar), new { id = usuario.Id });
                }

                await _contexto.SaveChangesAsync();
                TempData["Correcto"] = "Cambios realizados correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ocurrió un error al guardar los cambios: {ex.Message}";
                return RedirectToAction(nameof(Editar), new { id = usuario.Id });
            }
        }

        //Método bloquear/desbloquear usuario
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult BloquearDesbloquear(string idUsuario)
        {

            var usuariBD = _contexto.AppUsuario.FirstOrDefault(u => u.Id == idUsuario);
            if (usuariBD == null)
            {
                return NotFound();
            }

            if (usuariBD.LockoutEnd != null && usuariBD.LockoutEnd > DateTime.Now)
            {
                //El usuario se encuentra bloqueado y lo podemos desbloquear
                usuariBD.LockoutEnd = DateTime.Now;
                TempData["Correcto"] = "Usuario desbloqueado correctamente";
            }
            else
            {
                //El usuario no está bloqueado y lo podemos bloquear
                usuariBD.LockoutEnd = DateTime.Now.AddYears(100);
                TempData["Error"] = "Usuario bloqueado correctamente";
            }

            _contexto.SaveChanges();
            return RedirectToAction(nameof(Index));
        }


        //Método para borrar usuario
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult Borrar(string idUsuario)
        {

            var usuariBD = _contexto.AppUsuario.FirstOrDefault(u => u.Id == idUsuario);
            if (usuariBD == null)
            {
                return NotFound();
            }

            _contexto.AppUsuario.Remove(usuariBD);
            _contexto.SaveChanges();
            TempData["Correcto"] = "Usuario borrado correctamente";
            return RedirectToAction(nameof(Index));
        }


        //Editar perfil
        [HttpGet]
        public IActionResult EditarPerfil(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var usuarioBd = _contexto.AppUsuario.Find(id);
            if (usuarioBd == null)
            {
                return NotFound();
            }

            return View(usuarioBd);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarPerfil(AppUsuario appUsuario)
        {
            if (ModelState.IsValid)
            {
                var usuario = await _contexto.AppUsuario.FindAsync(appUsuario.Id);
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

        //Manejo de claims
        [HttpGet]
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
        public async Task<IActionResult> AdministrarClaimsUsuario(ClaimsUsuarioViewModel cuViewModel)
        {
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
            TempData["Correcto"] = "Cambios realizados correctamente";
            return RedirectToAction(nameof(Index));
        }

        // Toggle Available (0 / 1)
        [HttpPost]
        [Authorize(Roles = "Administrador")]
        [ValidateAntiForgeryToken]
        public IActionResult ToggleAvailable(string idUsuario)
        {
            if (string.IsNullOrWhiteSpace(idUsuario))
            {
                TempData["Error"] = "Invalid request: user id is required.";
                return RedirectToAction(nameof(Index));
            }

            var usuarioBD = _contexto.AppUsuario.FirstOrDefault(u => u.Id == idUsuario);
            if (usuarioBD == null)
            {
                return NotFound();
            }

            // Si es 1 -> pasa a 0, si es 0 (o cualquier otro) -> pasa a 1
            usuarioBD.Available = (usuarioBD.Available == 1) ? 0 : 1;

            _contexto.SaveChanges();

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
