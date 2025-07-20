using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.ViewModels;

namespace RH_CM.Controllers
{
    public partial class CatalogController : Controller
    {
        private readonly RH_CHDBContext _context;

        public CatalogController(RH_CHDBContext context)
        {
            _context = context;
        }


    }
}
