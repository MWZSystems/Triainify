using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RH_CM.Models;
using RH_CM.Service.AccessGroups;
using RH_CM.Service.DTOs;
using System.Diagnostics;
using System.Threading.Tasks;

namespace RH_CM.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AccessGroupsService _accessGroupService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HomeController(ILogger<HomeController> logger, 
                                AccessGroupsService accessGroupsService,
                                UserManager<IdentityUser> userManager,
                                IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _accessGroupService = accessGroupsService;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<IActionResult> Index()
        {
            // Borra la sesion de las cookies anteriores
            HttpContext.Session.Remove("Accesos");

            // Esta parte guarda los accesos que tiene este usuario para el NavBar los este leyendo de ahi. y no tener que estar refrescandolos.
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Accesos")))
            {
                GroupsAccessDTOs accesos = await _accessGroupService.GetMenusToShow(); //_accesosService.CargarAccesos(usuarioId);

                HttpContext.Session.SetString("Accesos", JsonConvert.SerializeObject(accesos));
            }
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
