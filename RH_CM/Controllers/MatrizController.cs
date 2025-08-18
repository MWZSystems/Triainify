using Microsoft.AspNetCore.Mvc;
using RH_CM.Data;

namespace RH_CM.Controllers
{
    public class MatrizController : Controller
    {
        private readonly db_abcd61_rhchdbContext _context;

        public MatrizController(db_abcd61_rhchdbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

    }
}
