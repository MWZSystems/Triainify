using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.OcupationKey;
using RH_CM.Service.OcupationKey;
using RH_CM.Service.Export;
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
        private readonly db_abcd61_rhchdbContext _context;
        public OcupationKeyController(OcupationKeyService ocupationKeyService, db_abcd61_rhchdbContext context)
        {
            _ocupationKeyService = ocupationKeyService;
            _context = context;
        }

        /// <summary>
        /// Displays the list of occupation codes.
        /// </summary>
        public async Task<ActionResult> Index()
        {
            OcupationDTOs result = await _ocupationKeyService.IndexGet_async();
            return View(result);
        }

        /// <summary>
        /// Raw export of every column in CtOcupationcodes, with no joins or translations,
        /// so staff can cross-check the data behind the Occupation Codes catalog.
        /// </summary>
        public async Task<IActionResult> ExportOcupationCodeFullData()
        {
            var data = await _context.CtOcupationcodes.AsNoTracking().ToListAsync();
            var bytes = RawExcelExportHelper.ExportFullData(data, "OcupationCodes");
            return File(bytes, RawExcelExportHelper.ExcelContentType, RawExcelExportHelper.BuildFileName("OcupationCodes"));
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
