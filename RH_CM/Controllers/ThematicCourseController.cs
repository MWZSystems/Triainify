using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.ThematicArea;
using RH_CM.Service.ThematicArea;
using RH_CM.Service.ThematicCourse;
using RH_CM.Service.Export;
using RH_CM.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace RH_CM.Controllers
{
    [Authorize(Policy = "ViewAccess")]
    [AutoValidateAntiforgeryToken]
    public class ThematicCourseController : Controller
    {
        private readonly ThematicCourseService _thematicCourseService;
        private readonly db_abcd61_rhchdbContext _context;
        public ThematicCourseController(ThematicCourseService thematicCourseService, db_abcd61_rhchdbContext context)
        {
            _thematicCourseService = thematicCourseService;
            _context = context;
        }


        [HttpGet]
        public async Task<ActionResult> Index()
        {
            ThematicCourseDTOs result = await _thematicCourseService.IndexGet_async();
            return View(result);
        }

        /// <summary>
        /// Raw export of every column in CtThematiccourses, with no joins or translations,
        /// so staff can cross-check the data behind the Thematic Course links.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ExportThematicCourseFullData()
        {
            var data = await _context.CtThematiccourses.AsNoTracking().ToListAsync();
            var bytes = RawExcelExportHelper.ExportFullData(data, "ThematicCourses");
            return File(bytes, RawExcelExportHelper.ExcelContentType, RawExcelExportHelper.BuildFileName("ThematicCourses"));
        }

        [HttpPost]
        public async Task<IActionResult> AddThematicCourse(string ThematicArea, string Course)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string userName = User?.Identity?.Name ?? "system";

            ServiceAnswer serviceAnswer = await _thematicCourseService.AddThematicCourseAsync(ThematicArea, Course, userName);

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        //EditOcupationCode

        [HttpGet]
        public async Task<IActionResult> EditThematicCourse(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ThematicCourseDTOs thematicCourseDTOs = await _thematicCourseService.GetEditThematicCourseAsync(id);

            return View(thematicCourseDTOs);
        }


        [HttpPost]
        public async Task<IActionResult> EditThematicCourse(int id, string ThematicArea, string Course)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string userName = User?.Identity?.Name ?? "system";

            ServiceAnswer serviceAnswer = await _thematicCourseService.PostEditThematicCourseAsync(id, ThematicArea, Course, userName);

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleThematicCourse(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ServiceAnswer serviceAnswer = await _thematicCourseService.PostToggleThematicCourseAsync(id);

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> DeleteThematicCourse(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ServiceAnswer serviceAnswer = await _thematicCourseService.PostDeleteThematicCourseAsync(id);

            TempData[serviceAnswer.MessageType ?? ServiceAnswer.MessageType_Error] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }


    }
}
