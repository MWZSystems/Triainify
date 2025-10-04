using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using RH_CM.Service.DTOs;
using RH_CM.Service.SQLSMS;
using System.Reflection;
using System.Security.Claims;

namespace RH_CM.Service.AccessGroups
{
    public class AccessGroupsService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UnitOfWork _unitOfWork;

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
                                    UnitOfWork unitOfWork)
        {
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
        }

        private ClaimsPrincipal UserPrincipal => _httpContextAccessor.HttpContext?.User;


        public async Task<IdentityUser> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(UserPrincipal);
        }

        public async Task<IList<string>> GetUserRolesAsync()
        {
            var user = await GetCurrentUserAsync();
            return await _userManager.GetRolesAsync(user);
        }



        public async Task<GroupsAccessDTOs> GetMenusToShow()
        {
            var user = await GetCurrentUserAsync();
            var roles = await _userManager.GetRolesAsync(user);

            var parameters = new Dictionary<string, object>
            {
                { "@pRoleName", roles[0] }
            };

            List<GroupsAccessDTOs> result = await _unitOfWork.ExecuteStoredProcedureToListAsync<GroupsAccessDTOs>("[sp_AcessGroupService_GetAccess]", parameters);

            GroupsAccessDTOs groupsAccess = result[0];

            if (groupsAccess.OnBoarding == true ||
                groupsAccess.Offboarding == true ||
                groupsAccess.Supervisor == true ||
                groupsAccess.Department == true ||
                groupsAccess.Position == true ||
                groupsAccess.Course == true ||
                groupsAccess.CourseMaterial == true ||
                groupsAccess.Test == true)
            {
                groupsAccess.ShowCatalogsMenu = true;
            }

            if (groupsAccess.CourseAssignments == true ||
                groupsAccess.CourseCompleted == true ||
                groupsAccess.CourseLevelMaterial == true ||
                groupsAccess.ExternalEvidence == true)
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
