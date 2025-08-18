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
        private readonly db_abcd61_rhchdbContext _context;

        public CatalogController(db_abcd61_rhchdbContext context)
        {
            _context = context;
        }


    }
}
