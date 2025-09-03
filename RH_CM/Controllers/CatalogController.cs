using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Models;
using RH_CM.Service.ExternalEvidence;
using RH_CM.ViewModels;

namespace RH_CM.Controllers
{
    public partial class CatalogController : Controller
    {
        private readonly db_abcd61_rhchdbContext _context;
        private readonly ExternalEvidenceService _externalEvidenceService;

        public CatalogController(db_abcd61_rhchdbContext context,
                                  ExternalEvidenceService externalEvidenceService )
        {
            _context = context;
            _externalEvidenceService = externalEvidenceService; //inyeccion de servicio de external evidence
        }


    }
}
