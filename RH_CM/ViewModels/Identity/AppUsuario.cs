using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace RH_CM.ViewModels
{
    public class AppUsuario : IdentityUser
    {
        //CenturyMold
        public string? Ntuser { get; set; }
        public string? EmployeeNumber { get; set; }
        public string Names { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public int Available { get; set; }

        // Properties used for roles and assigning a role to a user
        [NotMapped]
        [Display(Name = "Role for the user")]
        public string IdRol { get; set; } = string.Empty;
        [NotMapped]
        public string Rol { get; set; } = string.Empty;
        [NotMapped]
        public IEnumerable<SelectListItem> ListaRoles { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
