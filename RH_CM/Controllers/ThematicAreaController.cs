using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.OcupationKey;
using RH_CM.Service.DTOs.ThematicArea;
using RH_CM.Service.OcupationKey;
using RH_CM.Service.ThematicArea;

namespace RH_CM.Controllers
{
   
    public class ThematicAreaController : Controller
    {
        private readonly ThematicAreaService _thematicAreaService;

        public ThematicAreaController(ThematicAreaService thematicAreaService)
        {
            _thematicAreaService = thematicAreaService;
        }

        [HttpGet]
        public async Task<ActionResult> Index()
        {
            List<ThematicAreaDTOs> result = await _thematicAreaService.IndexGet_async();
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddThematicArea(string ThematicAreaName, int ThematicAreaCode)
        {
            string userName = User?.Identity?.Name;

            ServiceAnswer serviceAnswer = await _thematicAreaService.AddThematicAreaAsync(ThematicAreaName, ThematicAreaCode, userName);

            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        //EditOcupationCode

        [HttpGet]
        public async Task<IActionResult> EditThematicArea(int id)
        {
            ThematicAreaDTOs thematicAreaDTOs = await _thematicAreaService.GetEditThematicAreaAsync(id);

            return View(thematicAreaDTOs);
        }


        [HttpPost]
        public async Task<IActionResult> EditThematicArea(ThematicAreaDTOs answer)
        {
            string? userName = User?.Identity?.Name;

            ServiceAnswer serviceAnswer = await _thematicAreaService.PostEditThematicAreaAsync(answer, userName);

            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleThematicArea(int id)
        {
            ServiceAnswer serviceAnswer = await _thematicAreaService.PostToggleThematicAreaAsync(id);

            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteThematicArea(int id)
        {
            ServiceAnswer serviceAnswer = await _thematicAreaService.PostDeleteThematicAreaAsync(id);

            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

    }
}
