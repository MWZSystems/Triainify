using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.OcupationKey;
using RH_CM.Service.OcupationKey;
using System.Threading.Tasks;

namespace RH_CM.Controllers
{
    public class OcupationKeyController : Controller
    {
        private readonly OcupationKeyService _ocupationKeyService;
        public OcupationKeyController(OcupationKeyService ocupationKeyService) 
        { 
            _ocupationKeyService = ocupationKeyService;
        }

        // GET: OcupationKeyController
        public async Task<ActionResult> Index()
        {
            OcupationDTOs result = await _ocupationKeyService.IndexGet_async();
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddOcupationCode(string Position, int OcupationCode)
        {
            string userName = User?.Identity?.Name;

            ServiceAnswer serviceAnswer = await _ocupationKeyService.AddOcupationCodeAsync(Position, OcupationCode, userName);

            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        //EditOcupationCode

        [HttpGet]
        public async Task<IActionResult> EditOcupationCode(int id)
        {
            OcupationsList ocupationsList = await _ocupationKeyService.GetEditOcupationCodeAsync(id);

            return View(ocupationsList);
        }


        [HttpPost]
        public async Task<IActionResult> EditOcupationCode(OcupationsList answer)
        {
            string? userName = User?.Identity?.Name;

            ServiceAnswer serviceAnswer = await _ocupationKeyService.PostEditOcupationCodeAsync(answer, userName);

            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleOcupationcode(int id)
        {
            ServiceAnswer serviceAnswer = await _ocupationKeyService.PostToggleOcupationCodeAsync(id);

            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOcupationcode(int id)
        {
            ServiceAnswer serviceAnswer = await _ocupationKeyService.PostDeleteOcupationCodeAsync(id);

            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }
    }
}
