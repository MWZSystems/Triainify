using RH_CM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Encodings.Web;
using RH_CM.Data;

namespace RH_CM.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class CuentasController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        //private readonly IEmailSender _emailSender;
        public readonly UrlEncoder _urlEncoder;
        private readonly ApplicationDbContext _contexto;

        public CuentasController(UserManager<IdentityUser> userManager, 
                                SignInManager<IdentityUser> signInManager, 
                                UrlEncoder urlEncoder, 
                                RoleManager<IdentityRole> roleManager, 
                                ApplicationDbContext contexto) //, IEmailSender emailSender
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            //_emailSender = emailSender;
            _urlEncoder = urlEncoder;
            _contexto = contexto;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Registro()
        {
            //Para la creación de los roles
            if (!await _roleManager.RoleExistsAsync("Administrador"))
            {
                //Creación de rol usuario Administrador
                await _roleManager.CreateAsync(new IdentityRole("Administrador"));
            }

            //Para la creación de los roles
            if (!await _roleManager.RoleExistsAsync("Registrado"))
            {
                //Creación de rol usuario Registrado
                await _roleManager.CreateAsync(new IdentityRole("Registrado"));
            }

            RegistroViewModel registroVM = new RegistroViewModel();
            return View(registroVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Registro(RegistroViewModel rgViewModel)
        {
            //rgViewModel.ListaRoles = await ObtenerListaRolesAsync();
            if (ModelState.IsValid)
            {
                var usuario = new AppUsuario
                {
                    UserName = rgViewModel.Ntuser,
                    EmployeeNumber = rgViewModel.EmployeeNumber,
                    Email = rgViewModel.Email,
                    Names = rgViewModel.Names,
                    LastName = rgViewModel.LastName,
                    Available = 1,
                    CreateDate = DateTime.Today,
                    Ntuser = rgViewModel.Ntuser
                };

                var resultado = await _userManager.CreateAsync(usuario, rgViewModel.Password);

                if (resultado.Succeeded)
                {
                    //await _emailSender.SendEmailAsync(usuario.Email, "Registro exitoso", "Tu registro ha sido realizado correctamente");
                    // Asignar el rol al usuario
                    await _userManager.AddToRoleAsync(usuario, "Registrado");

                    // Auto login
                    await _signInManager.SignInAsync(usuario, isPersistent: false);

                    return RedirectToAction("Index", "Home");
                }

                ValidarErrores(resultado);
            }

            return View(rgViewModel);
        }

        //Registro especial solo para los administrador
        [HttpGet]
        [Authorize(Roles = "ToolCribAdmin, Administrador")]
        public async Task<IActionResult> RegistroAdministrador()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "ToolCribAdmin, Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegistroAdministrador(RegistroAdminViewModel rgViewModel)
        {
            if (ModelState.IsValid)
            {
                var usuario = new AppUsuario
                {
                    UserName = rgViewModel.Ntuser,
                    EmployeeNumber = rgViewModel.EmployeeNumber,
                    Email = rgViewModel.Email,
                    Names = rgViewModel.Names,
                    LastName = rgViewModel.LastName,
                    Available = 1,
                    CreateDate = DateTime.Today,
                    Ntuser = rgViewModel.Ntuser
                };

                var resultado = await _userManager.CreateAsync(usuario, rgViewModel.Password);

                if (resultado.Succeeded)
                {
                    //await _emailSender.SendEmailAsync(usuario.Email, "Registro exitoso", "Tu registro ha sido realizado correctamente");
                    // Asignar el rol al usuario
                    await _userManager.AddToRoleAsync(usuario, "Registrado");

                    // Auto login
                    await _signInManager.SignInAsync(usuario, isPersistent: false);

                    TempData["SuccessMessage"] = "Usuario registrado correctamente";
                    return RedirectToAction("Index", "Home");
                }

                ValidarErrores(resultado);
            }

            return View();
        }


        [AllowAnonymous]
        //Manejador de errores
        private void ValidarErrores(IdentityResult resultado)
        {
            foreach (var error in resultado.Errors)
            {
                ModelState.AddModelError(String.Empty, error.Description);
            }
        }

        //Método mostrar fomulario de acceso
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Acceso()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Acceso(AccesoViewModel accViewModel)
        {
            if (ModelState.IsValid)
            {
                var resultado = await _signInManager.PasswordSignInAsync(accViewModel.UserName, accViewModel.Password, accViewModel.RememberMe, lockoutOnFailure: true);

                if (resultado.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                if (resultado.IsLockedOut)
                {
                    return View("Bloqueado");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Acceso inválido");
                    return View(accViewModel);
                }
            }

            return View(accViewModel);
        }

        //Salir o cerrar sesión de la aplicacion (logout)
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalirAplicacion()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Acceso", "Cuentas");
        }

        //Funcionalidad para recuperar contraseña
        [HttpGet]
        [Authorize(Roles = "ToolCribAdmin, Administrador")]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "ToolCribAdmin, Administrador")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(RecuperaPasswordViewModel rpViewModel)
        {
            if (ModelState.IsValid)
            {
                var usuario = await _userManager.FindByNameAsync(rpViewModel.UserName);
                if (usuario == null)
                {
                    // Si el usuario no existe, redirige a una vista de confirmación
                    TempData["Error"] = "Usuario no existe";
                    return RedirectToAction("ResetPassword");
                }

                // Genera un token para resetear la contraseña (opcional)
                var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);

                // Resetea la contraseña del usuario con la nueva contraseña
                var resultado = await _userManager.ResetPasswordAsync(usuario, token, rpViewModel.Password);
                if (resultado.Succeeded)
                {
                    // Redirige a una vista de confirmación
                    TempData["Correcto"] = "Password cambiada";
                    return RedirectToAction("ResetPassword");
                }

                // Si hay errores, muestra los mensajes de error
                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Si hay algún error en el modelo, vuelve a mostrar el formulario
            return View(rpViewModel);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Denegado(string returnurl = null)
        {
            ViewData["ReturnUrl"] = returnurl;
            returnurl = returnurl ?? Url.Content("~/");
            return View();
        }
    }
}
