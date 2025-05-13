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

        //Nuevas propiedades para usar roles y asignación de un rol a un usuario
        [NotMapped]
        [Display(Name = "Rol para el usuario")]
        public string IdRol { get; set; }
        [NotMapped]
        public string Rol { get; set; }
        [NotMapped]
        public IEnumerable<SelectListItem> ListaRoles { get; set; }
    }
}
