using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient; // or System.Data.SqlClient, depending on your version
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.ViewModels;
using System.Text.Encodings.Web;
using RH_CM.Messages.Identity;

namespace RH_CM.Controllers
{
    public class CuentasController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        public readonly UrlEncoder _urlEncoder;
        private readonly ApplicationDbContext _contexto;
        private readonly ILogger<CuentasController> _logger;

        public CuentasController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            UrlEncoder urlEncoder,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext contexto,
            ILogger<CuentasController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _urlEncoder = urlEncoder;
            _contexto = contexto;
            _logger = logger;
        }

        // =========================
        //  USER REGISTRATION (USER)
        // =========================

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Registro()
        {
            // Create default roles if they do not exist
            if (!await _roleManager.RoleExistsAsync("Administrador"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Administrador"));
            }

            if (!await _roleManager.RoleExistsAsync("Empleado"))
            {
                await _roleManager.CreateAsync(new IdentityRole("Empleado"));
            }

            RegistroViewModel registroVM = new RegistroViewModel();
            return View(registroVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Registro(RegistroViewModel rgViewModel)
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

                try
                {
                    var resultado = await _userManager.CreateAsync(usuario, rgViewModel.Password);

                    if (resultado.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(usuario, "Empleado");

                        // Auto login
                        await _signInManager.SignInAsync(usuario, isPersistent: false);

                        TempData["SuccessMessage"] = CuentasMessages.UserRegisteredSuccessfully;
                        return RedirectToAction("Index", "Home");
                    }

                    ValidarErrores(resultado);
                }
                catch (SqlException ex) when (ex.Number == 208) // 208 = Invalid object name (missing table)
                {
                    _logger.LogError(ex, "SQL error: missing table during user registration.");

                    TempData["ErrorMessage"] = CuentasMessages.ThereIsAProblemWithTheDatabase;
                    ModelState.AddModelError(string.Empty, CuentasMessages.ThereIsAProblemWithTheDatabase);
                }
                catch (SqlException ex)
                {
                    _logger.LogError(ex, "SQL connection error during user registration.");

                    TempData["ErrorMessage"] = CuentasMessages.UnableToConnectToTheDatabasePlease;
                    ModelState.AddModelError(string.Empty, CuentasMessages.UnableToConnectToTheDatabasePlease);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "General error during user registration.");

                    TempData["ErrorMessage"] = CuentasMessages.UnexpectedErrorOccurredWhileRegisteringTheUser;
                    ModelState.AddModelError(string.Empty, CuentasMessages.UnexpectedErrorOccurredWhileRegisteringTheUser);
                }
            }

            return View(rgViewModel);
        }

        // =========================
        //  USER REGISTRATION (ADMIN)
        // =========================

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> RegistroAdministrador()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
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

                try
                {
                    var resultado = await _userManager.CreateAsync(usuario, rgViewModel.Password);

                    if (resultado.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(usuario, "Empleado");

                        // Auto login
                        await _signInManager.SignInAsync(usuario, isPersistent: false);

                        TempData["SuccessMessage"] = CuentasMessages.EmployeeUserCreatedSuccessfully;
                        return RedirectToAction("Index", "Home");
                    }

                    ValidarErrores(resultado);
                }
                catch (SqlException ex) when (ex.Number == 208)
                {
                    _logger.LogError(ex, "SQL error: missing table during admin registration.");

                    TempData["ErrorMessage"] = CuentasMessages.ThereIsAProblemWithTheDatabase;
                    ModelState.AddModelError(string.Empty, CuentasMessages.ThereIsAProblemWithTheDatabase);
                }
                catch (SqlException ex)
                {
                    _logger.LogError(ex, "SQL connection error during admin registration.");

                    TempData["ErrorMessage"] = CuentasMessages.UnableToConnectToTheDatabasePlease;
                    ModelState.AddModelError(string.Empty, CuentasMessages.UnableToConnectToTheDatabasePlease);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "General error during admin registration.");

                    TempData["ErrorMessage"] = CuentasMessages.UnexpectedErrorOccurredWhileRegisteringTheUser;
                    ModelState.AddModelError(string.Empty, CuentasMessages.UnexpectedErrorOccurredWhileRegisteringTheUser);
                }
            }

            return View(rgViewModel);
        }

        // =========================
        //  COMMON IDENTITY ERRORS
        // =========================

        [AllowAnonymous]
        private void ValidarErrores(IdentityResult resultado)
        {
            foreach (var error in resultado.Errors)
            {
                // Identity error descriptions are already in English by default
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // =========================
        //  LOGIN
        // =========================

        // Show login form
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Acceso()
        {
            // Optional: quick connection test before showing login
            try
            {
                var canConnect = await _contexto.Database.CanConnectAsync();
                if (!canConnect)
                {
                    TempData["ErrorMessage"] = CuentasMessages.ApplicationCouldNotConnectToTheDatabase;
                }
            }
            catch (SqlException ex) when (ex.Number == 53)
            {
                _logger.LogError(ex, "SQL server not reachable when loading login page.");

                TempData["ErrorMessage"] = CuentasMessages.DatabaseServerNotReachableVerifyInstance;
            }
            catch (SqlException ex) when (ex.Number == 18456 || ex.Number == 18452)
            {
                _logger.LogError(ex, "Login failed for configured database account when loading login page.");

                TempData["ErrorMessage"] = string.Format(CuentasMessages.DatabaseAccountNoPermissionDetailedFormat, ex.Number);
            }
            catch (SqlException ex) when (ex.Number == 4060)
            {
                _logger.LogError(ex, "Database not found or not accessible when loading login page.");

                TempData["ErrorMessage"] = CuentasMessages.ConfiguredDatabaseNotFoundVerifyNameSqlError4060;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error when loading login page.");

                TempData["ErrorMessage"] = string.Format(CuentasMessages.DatabaseErrorLoadingTheLoginPageFormat, ex.Number);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error when loading login page.");

                TempData["ErrorMessage"] = CuentasMessages.UnexpectedErrorOccurredWhileLoadingTheLoginPage;
            }

            return View();
        }

        // Handle login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> Acceso(AccesoViewModel accViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(accViewModel);
            }

            try
            {
                var resultado = await _signInManager.PasswordSignInAsync(
                    accViewModel.UserName,
                    accViewModel.Password,
                    accViewModel.RememberMe,
                    lockoutOnFailure: false);

                if (resultado.Succeeded)
                {
                    TempData["SuccessMessage"] = CuentasMessages.LoginSuccessfulWelcomeBack;
                    return RedirectToAction("Index", "Home");
                }

                if (resultado.IsLockedOut)
                {
                    TempData["ErrorMessage"] = CuentasMessages.AccountIsLockedPleaseContactItSupport;
                    return View("Bloqueado");
                }

                TempData["ErrorMessage"] = CuentasMessages.InvalidUsernameOrPassword;
                ModelState.AddModelError(string.Empty, CuentasMessages.InvalidUsernameOrPassword);
                return View(accViewModel);
            }
            catch (SqlException ex) when (ex.Number == 208) // Invalid object name (missing table)
            {
                _logger.LogError(ex, "SQL error: missing table during login.");

                TempData["ErrorMessage"] = CuentasMessages.ThereIsAProblemWithTheDatabaseStructureSqlError208;
                ModelState.AddModelError(string.Empty, CuentasMessages.ThereIsAProblemWithTheDatabaseStructureSqlError208);
                return View(accViewModel);
            }
            catch (SqlException ex) when (ex.Number == 53)
            {
                _logger.LogError(ex, "SQL server not reachable during login.");

                TempData["ErrorMessage"] = CuentasMessages.DatabaseServerNotReachableVerifyInstance;
                ModelState.AddModelError(string.Empty, CuentasMessages.DatabaseServerCouldNotBeFoundOrIsNotReachable);
                return View(accViewModel);
            }
            catch (SqlException ex) when (ex.Number == 18456 || ex.Number == 18452)
            {
                _logger.LogError(ex, "Login failed for configured database account during login.");

                TempData["ErrorMessage"] = string.Format(CuentasMessages.DatabaseAccountNoPermissionDetailedFormat, ex.Number);
                ModelState.AddModelError(string.Empty, CuentasMessages.ConfiguredDatabaseAccountDoesNotHavePermission);
                return View(accViewModel);
            }
            catch (SqlException ex) when (ex.Number == 4060)
            {
                _logger.LogError(ex, "Database not found or not accessible during login.");

                TempData["ErrorMessage"] = CuentasMessages.ConfiguredDatabaseNotFoundVerifyExistsSqlError4060;
                ModelState.AddModelError(string.Empty, CuentasMessages.ConfiguredDatabaseCouldNotBeFoundOrOpened);
                return View(accViewModel);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL connection error during login.");

                TempData["ErrorMessage"] = string.Format(CuentasMessages.UnableToConnectToTheDatabaseSqlErrorFormat, ex.Number);
                ModelState.AddModelError(string.Empty, CuentasMessages.UnableToConnectToTheDatabasePlease);
                return View(accViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error during login.");

                TempData["ErrorMessage"] = CuentasMessages.UnexpectedErrorOccurredWhileTryingToSignIn;
                ModelState.AddModelError(string.Empty, CuentasMessages.UnexpectedErrorOccurredWhileTryingToSignIn);
                return View(accViewModel);
            }
        }


        // =========================
        //  LOGOUT
        // =========================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SalirAplicacion()
        {
            await _signInManager.SignOutAsync();
            TempData["SuccessMessage"] = CuentasMessages.YouHaveBeenSignedOut;
            return RedirectToAction("Acceso", "Cuentas");
        }

        // =========================
        //  RESET PASSWORD
        // =========================

        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        public IActionResult ResetPassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(RecuperaPasswordViewModel rpViewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(rpViewModel);
            }

            try
            {
                var usuario = await _userManager.FindByNameAsync(rpViewModel.UserName);
                if (usuario == null)
                {
                    TempData["ErrorMessage"] = CuentasMessages.UserDoesNotExist;
                    ModelState.AddModelError(string.Empty, CuentasMessages.UserDoesNotExist);
                    return View(rpViewModel);
                }

                // Generate reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);

                // Reset password
                var resultado = await _userManager.ResetPasswordAsync(usuario, token, rpViewModel.Password);
                if (resultado.Succeeded)
                {
                    TempData["SuccessMessage"] = CuentasMessages.PasswordChangedSuccessfully;
                    return RedirectToAction("ResetPassword");
                }

                foreach (var error in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            catch (SqlException ex) when (ex.Number == 208)
            {
                _logger.LogError(ex, "SQL error: missing table during password reset.");

                TempData["ErrorMessage"] = CuentasMessages.ThereIsAProblemWithTheDatabase;
                ModelState.AddModelError(string.Empty, CuentasMessages.ThereIsAProblemWithTheDatabase);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL connection error during password reset.");

                TempData["ErrorMessage"] = CuentasMessages.UnableToConnectToTheDatabasePlease;
                ModelState.AddModelError(string.Empty, CuentasMessages.UnableToConnectToTheDatabasePlease);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error during password reset.");

                TempData["ErrorMessage"] = CuentasMessages.UnexpectedErrorOccurredWhileTryingToReset;
                ModelState.AddModelError(string.Empty, CuentasMessages.UnexpectedErrorOccurredWhileTryingToReset);
            }

            return View(rpViewModel);
        }

        // =================
        //  ACCESS DENIED
        // =================
        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        [AllowAnonymous]
        public IActionResult Denegado(string? returnurl = null)
        {
            // Generic message when none was passed in from another page
            if (TempData["ErrorMessage"] == null)
                TempData["ErrorMessage"] = CuentasMessages.YouDoNotHavePermissionToAccess;

            ViewData["ReturnUrl"] = returnurl ?? Url.Content("~/");

            return View();
        }
    }
}
