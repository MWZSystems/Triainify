using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace RH_CM.Service.AccessGroups
{
    public class AcessGroupsService
    {
        private readonly string _menu;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public bool ShowMenu { get; set; }

        public  AcessGroupsService(UserManager<IdentityUser> userManager,
                                    IHttpContextAccessor httpContextAccessor,
                                    string menu = "")
        {
            _menu = menu;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;

                        //Llena la listas.

            IsValid(); //Inicializa el valor para saber si muestra el menu o no.


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



        private async Task<bool> IsValid()
        {
            var user = await GetCurrentUserAsync();
            var roles = await _userManager.GetRolesAsync(user);

            this.ShowMenu = false;

            return true;
           
        }

    }
}
