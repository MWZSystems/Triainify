using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.ThematicArea;
using RH_CM.Service.ThematicArea;
using RH_CM.Service.ThematicCourse;
using Microsoft.AspNetCore.Authorization;

namespace RH_CM.Controllers
{
    [Authorize(Policy = "ViewAccess")]
    [AutoValidateAntiforgeryToken]
    public class ThematicCourseController : Controller
    {
        private readonly ThematicCourseService _thematicCourseService;
        public ThematicCourseController(ThematicCourseService thematicCourseService) 
        {
            _thematicCourseService = thematicCourseService;
        }


        [HttpGet]
        public async Task<ActionResult> Index()
        {
            ThematicCourseDTOs result = await _thematicCourseService.IndexGet_async();
            return View(result);
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
