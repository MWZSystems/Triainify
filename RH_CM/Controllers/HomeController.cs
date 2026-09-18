using Microsoft.AspNetCore.Authorization;
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
        private readonly AccessGroupsService _accessGroupService;

        public HomeController(AccessGroupsService accessGroupsService)
        {
            _accessGroupService = accessGroupsService;
        }

        public async Task<IActionResult> Index()
        {
            // Clear the session from previous cookies
            HttpContext.Session.Remove("Accesos");

            // This stores the user's access rights in Session so the NavBar can read them from there instead of refreshing them on every request.
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Accesos")))
            {
                GroupsAccessDTOs accesos = await _accessGroupService.GetMenusToShow();

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
