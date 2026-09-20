using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using RH_CM.Service.DTOs;
using RH_CM.Service.Requirements;
using RH_CM.Service.SQLSMS;
using System.Linq;
using System.Reflection;
using System.Security.Claims;

namespace RH_CM.Service.AccessGroups
{
    public class AccessGroupsService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UnitOfWork _unitOfWork;
        private readonly IAccessService _accessService;

        /*
        Lista de menus

            Catalogos
                Onboarding
                Offboarding
                Supervisor
                Department
                Position
                Course
                Course Material

            Assignments
                Course Assignments
                Course Completed
                CourseLevelMaterial
                External Evidence

            Reports
                Matrix By Position
                Matrix By Supervisor
                HR Report
                Material/Exam Missing
                Evidence By User

            Trainify
                Matrix By Employee
                Learning
            
        Vistas donde el menu se mostrara para todos:
            Users
            Register New User
            Roles
            Permissions
            Reset Password

        */

        public AccessGroupsService(UserManager<IdentityUser> userManager,
                                    IHttpContextAccessor httpContextAccessor,
                                    UnitOfWork unitOfWork,
                                    IAccessService accessService)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
            _accessService = accessService;
        }

        private ClaimsPrincipal? UserPrincipal => _httpContextAccessor.HttpContext?.User;

        /// <summary>
        /// For NEW menu items only: checks the same CT_PERMISSION-backed access check used by
        /// the "ViewAccess" authorization policy (ViewAccessHandler/AccessService), so a new
        /// screen's menu link is guaranteed to agree with whether the user can actually open it
        /// — without needing a new column/table for the old GroupsAccessDTOs/session-cached
        /// menu system. Existing menu items keep using GetMenusToShow(); this is additive.
        /// </summary>
        public async Task<bool> HasControllerActionAccessAsync(string controller, string action)
        {
            string userName = UserPrincipal?.Identity?.Name ?? string.Empty;
            if (string.IsNullOrEmpty(userName))
            {
                return false;
            }

            return await _accessService.HasAccessAsync(userName, controller, action);
        }


        public async Task<IdentityUser?> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(UserPrincipal);
        }

        public async Task<IList<string>> GetUserRolesAsync()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                return Array.Empty<string>();
            }
            return await _userManager.GetRolesAsync(user);
        }



        public async Task<GroupsAccessDTOs> GetMenusToShow()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
            {
                // Not authenticated (or the user record was deleted) — no menus to show.
                return new GroupsAccessDTOs();
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles == null || roles.Count == 0)
            {
                // User has no role assigned — no menus to show instead of crashing on roles[0].
                return new GroupsAccessDTOs();
            }

            var parameters = new Dictionary<string, object>
            {
                { "@pRoleName", roles[0] }
            };

            List<GroupsAccessDTOs> result = await _unitOfWork.ExecuteStoredProcedureToListAsync<GroupsAccessDTOs>("[sp_AcessGroupService_GetAccess]", parameters);

            // The role might not have a matching row in the access-groups table.
            GroupsAccessDTOs groupsAccess = result.FirstOrDefault() ?? new GroupsAccessDTOs();

            if (groupsAccess.OnBoarding == true ||
                groupsAccess.Offboarding == true ||
                groupsAccess.Supervisor == true ||
                groupsAccess.Department == true ||
                groupsAccess.Position == true ||
                groupsAccess.Course == true ||
                groupsAccess.CourseMaterial == true ||
                groupsAccess.Test == true ||
                groupsAccess.ThematicArea == true)
            {
                groupsAccess.ShowCatalogsMenu = true;
            }

            if (groupsAccess.CourseAssignments == true ||
                groupsAccess.CourseCompleted == true ||
                groupsAccess.CourseLevelMaterial == true ||
                groupsAccess.ExternalEvidence == true ||
                groupsAccess.OcupationCode == true ||
                groupsAccess.ThematicCourse == true
                )
            {
                groupsAccess.ShowAssignmentsMenu = true;
            }

            if (groupsAccess.MatrixByPosition == true ||
               groupsAccess.MatrixBySupervisor == true ||
               groupsAccess.HRReport == true ||
               groupsAccess.MaterialExamMissing == true ||
               groupsAccess.EvidenceByUser == true)
            {
                groupsAccess.ShowReportsMenu = true;
            }

            return groupsAccess;
        }
    }
}
