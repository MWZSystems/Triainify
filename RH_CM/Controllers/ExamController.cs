using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace RH_CM.Controllers
{
    public class ExamController : Controller
    {
        // GET: ExamController
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Exam2()
        {
            return View();
        }
    }
}
