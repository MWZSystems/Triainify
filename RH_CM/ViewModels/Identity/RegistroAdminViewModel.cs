using System.ComponentModel.DataAnnotations;

namespace RH_CM.ViewModels
{
    public class RegistroAdminViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(50, ErrorMessage = "The {0} must be at least {2} characters long", MinimumLength = 5)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        //CenturyMold
        public string? Ntuser { get; set; }
        [Display(Name = "EMPLOYEE NUMBER")]
        public string? EmployeeNumber { get; set; }

        [Display(Name = "NAMES")]
        public string Names { get; set; } = null!;
        [Display(Name = "LASTNAME")]
        public string LastName { get; set; } = null!;

        ////Nuevas propiedades para usar roles y asignación de un rol a un usuario
        //[NotMapped]
        //[Display(Name = "Rol para el usuario")]
        //public string IdRol { get; set; }
        //[NotMapped]
        //public IEnumerable<SelectListItem> ListaRoles { get; set; 
    }
}
