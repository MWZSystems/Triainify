using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.ThematicArea;
using RH_CM.Service.ThematicArea;
using RH_CM.Service.ThematicCourse;

namespace RH_CM.Controllers
{
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
            string userName = User?.Identity?.Name;

            ServiceAnswer serviceAnswer = await _thematicCourseService.AddThematicCourseAsync(ThematicArea, Course, userName);

            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }

        //EditOcupationCode

        [HttpGet]
        public async Task<IActionResult> EditThematicCourse(int id)
        {
            ThematicCourseDTOs thematicCourseDTOs = await _thematicCourseService.GetEditThematicCourseAsync(id);

            return View(thematicCourseDTOs);
        }


        [HttpPost]
        public async Task<IActionResult> EditThematicCourse(int id, string ThematicArea, string Course)
        {
            string? userName = User?.Identity?.Name;

            ServiceAnswer serviceAnswer = await _thematicCourseService.PostEditThematicCourseAsync(id, ThematicArea, Course, userName);

            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> DeleteThematicCourse(int id)
        {
            ServiceAnswer serviceAnswer = await _thematicCourseService.PostDeleteThematicCourseAsync(id);

            TempData[serviceAnswer.MessageType] = serviceAnswer.Message;

            return RedirectToAction("Index");
        }


    }
}
