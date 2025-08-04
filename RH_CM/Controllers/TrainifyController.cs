using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Data;

namespace RH_CM.Controllers
{
    public class TrainifyController : Controller
    {
        private readonly RH_CHDBContext _context;

        public TrainifyController(RH_CHDBContext context)
        {
            _context = context;
        }
        // GET: TrainifyController
        public ActionResult TrainifyHome()
        {

            return View();
        }

        // GET: TrainifyController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: TrainifyController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: TrainifyController/Create
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

        // GET: TrainifyController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: TrainifyController/Edit/5
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

        // GET: TrainifyController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: TrainifyController/Delete/5
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
