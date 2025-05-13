using Microsoft.AspNetCore.Mvc;
using RH_CM.Data;

namespace RH_CM.Controllers
{
    public class MatrizController : Controller
    {
        private readonly RH_CHDBContext _context;

        public MatrizController(RH_CHDBContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

    }
}
