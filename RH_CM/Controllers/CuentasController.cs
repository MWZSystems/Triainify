using RH_CM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Encodings.Web;
using RH_CM.Data;
using Microsoft.Data.SqlClient; // or System.Data.SqlClient, depending on your version
using Microsoft.Extensions.Logging;

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

                        TempData["SuccessMessage"] = "User registered successfully.";
                        return RedirectToAction("Index", "Home");
                    }

                    ValidarErrores(resultado);
                }
                catch (SqlException ex) when (ex.Number == 208) // 208 = Invalid object name (missing table)
                {
                    _logger.LogError(ex, "SQL error: missing table during user registration.");

                    TempData["ErrorMessage"] = "There is a problem with the database structure (missing table). Please contact IT support.";
                    ModelState.AddModelError(string.Empty,
                        "There is a problem with the database structure (missing table). Please contact IT support.");
                }
                catch (SqlException ex)
                {
                    _logger.LogError(ex, "SQL connection error during user registration.");

                    TempData["ErrorMessage"] = "Unable to connect to the database. Please try again later or contact IT support.";
                    ModelState.AddModelError(string.Empty,
                        "Unable to connect to the database. Please try again later or contact IT support.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "General error during user registration.");

                    TempData["ErrorMessage"] = "An unexpected error occurred while registering the user. If the problem persists, please contact IT support.";
                    ModelState.AddModelError(string.Empty,
                        "An unexpected error occurred while registering the user. If the problem persists, please contact IT support.");
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

                        TempData["SuccessMessage"] = "Employee user created successfully.";
                        return RedirectToAction("Index", "Home");
                    }

                    ValidarErrores(resultado);
                }
                catch (SqlException ex) when (ex.Number == 208)
                {
                    _logger.LogError(ex, "SQL error: missing table during admin registration.");

                    TempData["ErrorMessage"] = "There is a problem with the database structure (missing table). Please contact IT support.";
                    ModelState.AddModelError(string.Empty,
                        "There is a problem with the database structure (missing table). Please contact IT support.");
                }
                catch (SqlException ex)
                {
                    _logger.LogError(ex, "SQL connection error during admin registration.");

                    TempData["ErrorMessage"] = "Unable to connect to the database. Please try again later or contact IT support.";
                    ModelState.AddModelError(string.Empty,
                        "Unable to connect to the database. Please try again later or contact IT support.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "General error during admin registration.");

                    TempData["ErrorMessage"] = "An unexpected error occurred while registering the user. If the problem persists, please contact IT support.";
                    ModelState.AddModelError(string.Empty,
                        "An unexpected error occurred while registering the user. If the problem persists, please contact IT support.");
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
                    TempData["ErrorMessage"] =
                        "The application could not establish a connection to the database. " +
                        "Please try again later or contact IT support.";
                }
            }
            catch (SqlException ex) when (ex.Number == 53)
            {
                _logger.LogError(ex, "SQL server not reachable when loading login page.");

                TempData["ErrorMessage"] =
                    "The database server could not be found or is not reachable (SQL error 53). " +
                    "Please verify the SQL Server instance or contact IT support.";
            }
            catch (SqlException ex) when (ex.Number == 18456 || ex.Number == 18452)
            {
                _logger.LogError(ex, "Login failed for configured database account when loading login page.");

                TempData["ErrorMessage"] =
                    "The account configured for the database connection (IIS application pool identity " +
                    "or the user in the 'ConexionSQL' connection string) does not have permission to access the database " +
                    $"or the credentials are invalid (SQL error {ex.Number}). Please verify the database login configuration or contact IT support.";
            }
            catch (SqlException ex) when (ex.Number == 4060)
            {
                _logger.LogError(ex, "Database not found or not accessible when loading login page.");

                TempData["ErrorMessage"] =
                    "The configured database could not be found or opened (SQL error 4060). " +
                    "Please verify the database name and that the configured account has access to it, or contact IT support.";
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL error when loading login page.");

                TempData["ErrorMessage"] =
                    $"A database error occurred while loading the login page (SQL error {ex.Number}). " +
                    "Please try again later or contact IT support.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error when loading login page.");

                TempData["ErrorMessage"] =
                    "An unexpected error occurred while loading the login page. " +
                    "If the problem persists, please contact IT support.";
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
                    lockoutOnFailure: true);

                if (resultado.Succeeded)
                {
                    TempData["SuccessMessage"] = "Login successful. Welcome back!";
                    return RedirectToAction("Index", "Home");
                }

                if (resultado.IsLockedOut)
                {
                    TempData["ErrorMessage"] = "Your account is locked. Please contact IT support.";
                    return View("Bloqueado");
                }

                TempData["ErrorMessage"] = "Invalid username or password.";
                ModelState.AddModelError(string.Empty, "Invalid username or password.");
                return View(accViewModel);
            }
            catch (SqlException ex) when (ex.Number == 208) // Invalid object name (missing table)
            {
                _logger.LogError(ex, "SQL error: missing table during login.");

                TempData["ErrorMessage"] =
                    "There is a problem with the database structure (missing table, SQL error 208). " +
                    "Please contact IT support.";
                ModelState.AddModelError(string.Empty,
                    "There is a problem with the database structure (missing table, SQL error 208). Please contact IT support.");
                return View(accViewModel);
            }
            catch (SqlException ex) when (ex.Number == 53)
            {
                _logger.LogError(ex, "SQL server not reachable during login.");

                TempData["ErrorMessage"] =
                    "The database server could not be found or is not reachable (SQL error 53). " +
                    "Please verify the SQL Server instance or contact IT support.";
                ModelState.AddModelError(string.Empty,
                    "The database server could not be found or is not reachable (SQL error 53). Please try again later or contact IT support.");
                return View(accViewModel);
            }
            catch (SqlException ex) when (ex.Number == 18456 || ex.Number == 18452)
            {
                _logger.LogError(ex, "Login failed for configured database account during login.");

                TempData["ErrorMessage"] =
                    "The account configured for the database connection (IIS application pool identity " +
                    "or the user in the 'ConexionSQL' connection string) does not have permission to access the database " +
                    $"or the credentials are invalid (SQL error {ex.Number}). Please verify the database login configuration or contact IT support.";
                ModelState.AddModelError(string.Empty,
                    "The configured database account does not have permission to access the database or the credentials are invalid. Please contact IT support.");
                return View(accViewModel);
            }
            catch (SqlException ex) when (ex.Number == 4060)
            {
                _logger.LogError(ex, "Database not found or not accessible during login.");

                TempData["ErrorMessage"] =
                    "The configured database could not be found or opened (SQL error 4060). " +
                    "Please verify the database exists and that the configured account has access to it, or contact IT support.";
                ModelState.AddModelError(string.Empty,
                    "The configured database could not be found or opened (SQL error 4060). Please contact IT support.");
                return View(accViewModel);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL connection error during login.");

                TempData["ErrorMessage"] =
                    $"Unable to connect to the database (SQL error {ex.Number}). " +
                    "Please try again later or contact IT support.";
                ModelState.AddModelError(string.Empty,
                    "Unable to connect to the database. Please try again later or contact IT support.");
                return View(accViewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error during login.");

                TempData["ErrorMessage"] =
                    "An unexpected error occurred while trying to sign in. " +
                    "If the problem persists, please contact IT support.";
                ModelState.AddModelError(string.Empty,
                    "An unexpected error occurred while trying to sign in. If the problem persists, please contact IT support.");
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
            TempData["SuccessMessage"] = "You have been signed out.";
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
                    TempData["ErrorMessage"] = "User does not exist.";
                    ModelState.AddModelError(string.Empty, "User does not exist.");
                    return View(rpViewModel);
                }

                // Generate reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(usuario);

                // Reset password
                var resultado = await _userManager.ResetPasswordAsync(usuario, token, rpViewModel.Password);
                if (resultado.Succeeded)
                {
                    TempData["SuccessMessage"] = "Password changed successfully.";
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

                TempData["ErrorMessage"] = "There is a problem with the database structure (missing table). Please contact IT support.";
                ModelState.AddModelError(string.Empty,
                    "There is a problem with the database structure (missing table). Please contact IT support.");
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "SQL connection error during password reset.");

                TempData["ErrorMessage"] = "Unable to connect to the database. Please try again later or contact IT support.";
                ModelState.AddModelError(string.Empty,
                    "Unable to connect to the database. Please try again later or contact IT support.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error during password reset.");

                TempData["ErrorMessage"] = "An unexpected error occurred while trying to reset the password. If the problem persists, please contact IT support.";
                ModelState.AddModelError(string.Empty,
                    "An unexpected error occurred while trying to reset the password. If the problem persists, please contact IT support.");
            }

            return View(rpViewModel);
        }

        // =================
        //  ACCESS DENIED
        // =================
        [HttpGet]
        [Authorize(Policy = "ViewAccess")]
        [AllowAnonymous]
        public IActionResult Denegado(string returnurl = null)
        {
            // Mensaje genérico si no se recibió uno desde otra página
            if (TempData["ErrorMessage"] == null)
                TempData["ErrorMessage"] = "You do not have permission to access this section.";

            ViewData["ReturnUrl"] = returnurl ?? Url.Content("~/");

            return View();
        }

    }
}
