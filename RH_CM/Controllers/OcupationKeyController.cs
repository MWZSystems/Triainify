using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.OcupationKey;
using RH_CM.Service.OcupationKey;
using System.Threading.Tasks;
using RH_CM.Messages.OcupationKey;
using Microsoft.AspNetCore.Authorization;

namespace RH_CM.Controllers
{
    [Authorize(Policy = "ViewAccess")]
    [AutoValidateAntiforgeryToken]
    public class OcupationKeyController : Controller
    {
        private readonly OcupationKeyService _ocupationKeyService;
        public OcupationKeyController(OcupationKeyService ocupationKeyService)
        {
            _ocupationKeyService = ocupationKeyService;
        }

        /// <summary>
        /// Displays the list of occupation codes.
        /// </summary>
        public async Task<ActionResult> Index()
        {
            OcupationDTOs result = await _ocupationKeyService.IndexGet_async();
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> AddOcupationCode(string Position, int OcupationCode)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string userName = User?.Identity?.Name ?? "system";

            ServiceAnswer serviceAnswer = await _ocupationKeyService.AddOcupationCodeAsync(Position, OcupationCode, userName);

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        //EditOcupationCode

        [HttpGet]
        public async Task<IActionResult> EditOcupationCode(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            OcupationsList? ocupationsList = await _ocupationKeyService.GetEditOcupationCodeAsync(id);

            if (ocupationsList == null)
            {
                TempData["ErrorMessage"] = OcupationKeyMessages.OccupationCodeNotFound;
                return RedirectToAction(nameof(Index));
            }

            return View(ocupationsList);
        }


        [HttpPost]
        public async Task<IActionResult> EditOcupationCode(OcupationsList answer)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string userName = User?.Identity?.Name ?? "system";

            ServiceAnswer serviceAnswer = await _ocupationKeyService.PostEditOcupationCodeAsync(answer, userName);

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleOcupationcode(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ServiceAnswer serviceAnswer = await _ocupationKeyService.PostToggleOcupationCodeAsync(id);

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteOcupationcode(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ServiceAnswer serviceAnswer = await _ocupationKeyService.PostDeleteOcupationCodeAsync(id);

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }
    }
}
