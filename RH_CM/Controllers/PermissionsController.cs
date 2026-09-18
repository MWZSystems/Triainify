using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.Permissions;
using RH_CM.Service.Permissions;
using System.Threading.Tasks;

namespace RH_CM.Controllers
{
    public class PermissionsController : Controller
    {
        private readonly PermissionsService _permissionsService;

        public PermissionsController(PermissionsService permissionsService)
        {
            _permissionsService = permissionsService;
        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> Index()
        {
            List<PermissionsIndexDTOs> groups = await _permissionsService.GetIndexAsync();
            return View(groups);
        }

        [Authorize(Policy = "ViewAccess")]
        public async Task<ActionResult> GroupDetail(string SelectedGroup)
        {
            PermissionsDetailDTOs detailDTOs = await _permissionsService.GetGroupDetailAsync(SelectedGroup);

            return View(detailDTOs);
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
