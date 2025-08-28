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
        private readonly db_abcd61_rhchdbContext _context;

        public ReportsController(db_abcd61_rhchdbContext context)
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
                .AsNoTracking()
                .Where(m => m.Available == 1)
                .OrderBy(m => m.NameMaterial)
                .Select(m => new
                {
                    m.PkCoursematerial,
                    MaterialName = m.NameMaterial,
                    m.Available,
                    m.CreateUser,
                    m.CreateDate
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
