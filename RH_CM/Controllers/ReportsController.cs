using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.ViewModels;

namespace RH_CM.Controllers
{
    public class ReportsController : Controller
    {
        private readonly RH_CHDBContext _context;

        public ReportsController(RH_CHDBContext context)
        {
            _context = context;
        }
        // GET: ReportsController
        public ActionResult SupervisorEmployeesCheck()
        {
            return View();
        }

        // GET: ReportsController
        public ActionResult MatrizByEmployeesCheck()
        {
            return View();
        }

        // GET: ReportsController
        public ActionResult MatrizByEmployeesGeneral()
        {
            return View();
        }

        // GET: ReportsController
        public ActionResult MatrizByDeparmentGeneral_RH()
        {
            return View();
        }

        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public async Task<IActionResult> HeadCountbySupervisor()
        {
            return View();
        }

        // GET: CtCoursematerial
        [Authorize(Roles = "Administrador, RHGerente, RHAdmin, RH")]
        public IActionResult CourseToDobyEmpleyee()
        {
            var materials = _context.CtCoursematerials
                .Where(m => m.Available == 1)
                .Join(
                    _context.CtCourses.Where(c => c.Available == 1),
                    m => m.FkCourse,
                    c => c.PkCourse,
                    (m, c) => new { m, c }
                )
                .Select(temp => new
                {
                    temp.m.PkCoursematerial,
                    MaterialName = temp.m.NameMaterial,  // NOMBRE CONSISTENTE
                    temp.m.Available,
                    CourseName = temp.c.CourseName,
                    temp.c.ManagementSystem,
                })
                .ToList();

            return View(materials);
        }

        // GET: ReportsController
        public ActionResult TestQuestions()
        {
            return View();
        }

        public ActionResult TestQuestions2()
        {
            return View();
        }


        // GET: ReportsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ReportsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ReportsController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ReportsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ReportsController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ReportsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ReportsController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
