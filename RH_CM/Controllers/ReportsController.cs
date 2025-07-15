using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RH_CM.Controllers
{
    public class ReportsController : Controller
    {
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

        // GET: ReportsController
        public ActionResult TestQuestions()
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
