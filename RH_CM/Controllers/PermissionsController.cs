using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.Permissions;
using RH_CM.Service.Permissions;
using RH_CM.Service.Export;
using System.Threading.Tasks;

namespace RH_CM.Controllers
{
    public class PermissionsController : Controller
    {
        private readonly PermissionsService _permissionsService;
        private readonly db_abcd61_rhchdbContext _context;

        public PermissionsController(PermissionsService permissionsService, db_abcd61_rhchdbContext context)
        {
            _permissionsService = permissionsService;
            _context = context;
        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> Index()
        {
            List<PermissionGroupSummaryDTOs> groups = await _permissionsService.GetGroupsSummaryAsync();
            return View(groups);
        }

        /// <summary>
        /// Registers a brand-new permission group (a new controller/screen) so an admin can
        /// then assign roles to it from GroupDetail — no manual DB INSERT needed anymore.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "ViewAccess")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateGroup(string groupName, string? controllerName)
        {
            ServiceAnswerPermissionsAdd answer = await _permissionsService.CreateGroupAsync(groupName, controllerName);

            TempData[answer.MessageType ?? ServiceAnswer.MessageType_Error] = answer.Message;

            if (answer.MessageType == ServiceAnswer.MessageType_Success && !string.IsNullOrWhiteSpace(answer.GroupKey))
            {
                return RedirectToAction(nameof(GroupDetail), new { SelectedGroup = answer.GroupKey });
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> GroupDetail(string SelectedGroup)
        {
            PermissionsDetailDTOs detailDTOs = await _permissionsService.GetGroupDetailAsync(SelectedGroup);

            return View(detailDTOs);
        }

        /// <summary>
        /// Raw export of every column in CtPermissions (the full catalog, not just the
        /// selected group), with no joins or translations, so staff can cross-check the
        /// data behind the Permissions catalog.
        /// </summary>
        [Authorize(Policy = "ViewAccess")]
        public async Task<IActionResult> ExportPermissionsFullData()
        {
            var data = await _context.CtPermissions.AsNoTracking().ToListAsync();
            var bytes = RawExcelExportHelper.ExportFullData(data, "Permissions");
            return File(bytes, RawExcelExportHelper.ExcelContentType, RawExcelExportHelper.BuildFileName("Permissions"));
        }


        //AddRole
        [HttpPost]
        //[Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> AddRoleToGroup(int GroupId, string RoleId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ServiceAnswerPermissionsAdd answer = await _permissionsService.PostAddRoleToGroup(GroupId, RoleId);

            TempData[answer.MessageType ?? ServiceAnswer.MessageType_Error] = answer.Message;

            return RedirectToAction(nameof(GroupDetail), new { SelectedGroup = answer.GroupKey });
        }


        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> DeleteRoleFromGroup(int GroupId, string RoleId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ServiceAnswerPermissionsAdd answer = await _permissionsService.PostDeleteRoleFromGroup(GroupId, RoleId);

            TempData[answer.MessageType ?? ServiceAnswer.MessageType_Error] = answer.Message;

            return RedirectToAction(nameof(GroupDetail), new { SelectedGroup = answer.GroupKey });
        }


    }
}
