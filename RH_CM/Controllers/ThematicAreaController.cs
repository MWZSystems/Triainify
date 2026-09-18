using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.OcupationKey;
using RH_CM.Service.DTOs.ThematicArea;
using RH_CM.Service.OcupationKey;
using RH_CM.Service.ThematicArea;
using RH_CM.Messages.ThematicArea;
using RH_CM.Service.Catalog;
using Microsoft.AspNetCore.Authorization;

namespace RH_CM.Controllers
{

    [Authorize(Policy = "ViewAccess")]
    [AutoValidateAntiforgeryToken]
    public class ThematicAreaController : Controller
    {
        private readonly ThematicAreaService _thematicAreaService;
        private readonly CatalogIntegrityService _catalogIntegrityService;

        public ThematicAreaController(ThematicAreaService thematicAreaService, CatalogIntegrityService catalogIntegrityService)
        {
            _thematicAreaService = thematicAreaService;
            _catalogIntegrityService = catalogIntegrityService;
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
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string userName = User?.Identity?.Name ?? "system";

            ServiceAnswer serviceAnswer = await _thematicAreaService.AddThematicAreaAsync(ThematicAreaName, ThematicAreaCode, userName);

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        //EditOcupationCode

        [HttpGet]
        public async Task<IActionResult> EditThematicArea(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ThematicAreaDTOs? thematicAreaDTOs = await _thematicAreaService.GetEditThematicAreaAsync(id);

            if (thematicAreaDTOs == null)
            {
                TempData["ErrorMessage"] = ThematicAreaMessages.ThematicAreaNotFound;
                return RedirectToAction(nameof(Index));
            }

            return View(thematicAreaDTOs);
        }


        [HttpPost]
        public async Task<IActionResult> EditThematicArea(ThematicAreaDTOs answer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string userName = User?.Identity?.Name ?? "system";

            ServiceAnswer serviceAnswer = await _thematicAreaService.PostEditThematicAreaAsync(answer, userName);

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleThematicArea(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ServiceAnswer serviceAnswer = await _thematicAreaService.PostToggleThematicAreaAsync(id);

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteThematicArea(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var dependency = await _catalogIntegrityService.ThematicAreaDependencyAsync(id);
            if (dependency != null)
            {
                TempData["ErrorMessage"] = dependency;
                return RedirectToAction("Index");
            }

            ServiceAnswer serviceAnswer = await _thematicAreaService.PostDeleteThematicAreaAsync(id);

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

    }
}
