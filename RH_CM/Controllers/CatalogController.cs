using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Data;
using RH_CM.Service.Catalog;
using RH_CM.Service.ExternalEvidence;

namespace RH_CM.Controllers
{
    [Route("[controller]/[action]")]
    public partial class CatalogController : Controller
    {
        private readonly db_abcd61_rhchdbContext _context;
        private readonly ExternalEvidenceService _externalEvidenceService;
        private readonly TestCatalogService _testCatalogService;
        private readonly CatalogIntegrityService _catalogIntegrityService;

        public CatalogController(
            db_abcd61_rhchdbContext context,
            ExternalEvidenceService externalEvidenceService,
            TestCatalogService testCatalogService,
            CatalogIntegrityService catalogIntegrityService)
        {
            _context = context;
            _externalEvidenceService = externalEvidenceService;
            _testCatalogService = testCatalogService;
            _catalogIntegrityService = catalogIntegrityService;
        }
    }
}
