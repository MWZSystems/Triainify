using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using RH_CM.Service.SQLSMS;
using System.Security.Claims;

namespace RH_CM.Service.AccessGroups
{
    public class AcessGroupsService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UnitOfWork _unitOfWork;

        //Menus to Show.
        public bool ShowCatalogsMenu { get; set; }
        public bool ShowAssignmentsMenu { get; set; }
        public bool ShowReportsMenu { get; set; }
        public bool ShowTrainifyMenu { get; set; }

        //Views to Show:

        public bool Onboarding {  get; set; }
        public bool Offboarding { get; set; }
        public bool Supervisor { get; set; }
        public bool Department { get; set; }
        public bool Position { get; set; }
        public bool Course { get; set; }
        public bool CourseMaterial { get; set; }


        public bool CourseAssignments { get; set; }
        public bool CourseCompleted { get; set; }
        public bool CourseLevelMaterial { get; set; }
        public bool ExternalEvidence { get; set; }


        public bool MatrixByPosition { get; set; }
        public bool MatrixBySupervisor { get; set; }
        public bool HRReport { get; set; }
        public bool MaterialExamMissing { get; set; }
        public bool EvidenceByUser { get; set; }


        public bool MatrixByEmployee { get; set; }
        public bool Learning { get; set; }


        public bool Users { get; set; }
        public bool RegisterNewUser { get; set; }
        public bool Roles { get; set; }
        public bool Permissions { get; set; }
        public bool ResetPassword { get; set; }

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

        public AcessGroupsService(UserManager<IdentityUser> userManager,
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



        private async Task GetMenusToShow()
        {
            var user = await GetCurrentUserAsync();
            var roles = await _userManager.GetRolesAsync(user);

           List<> 

           // return true;
           
        }

    }
}
