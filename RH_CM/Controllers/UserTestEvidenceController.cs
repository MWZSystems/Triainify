using Microsoft.AspNetCore.Mvc;
using RH_CM.Service.DTOs.UserTestEvidence;
using RH_CM.Service.UserTestEvidence;

namespace RH_CM.Controllers
{
    public class UserTestEvidenceController : Controller
    {
        private readonly UserTestEvidenceService _userTestEvidenceService;

        public UserTestEvidenceController(UserTestEvidenceService userTestEvidenceService)
        {
            _userTestEvidenceService = userTestEvidenceService;
        }

        // GET: UserTestEvidenceController
        public async Task<ActionResult> Index()
        {
            List<UserDTOs> users = await _userTestEvidenceService.GetIndexAsync();
            return View(users);
        }

        // GET: UserTestEvidenceController/Details/5
        public async Task<ActionResult> DetailUser(string id)
        {
            DetailDTOs detailDTOs = await _userTestEvidenceService.GetDetailUserAsync(id);
            return View(detailDTOs);
        }
    }
}
