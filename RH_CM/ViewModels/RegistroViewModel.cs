using System.ComponentModel.DataAnnotations;

namespace RH_CM.ViewModels
{
    public class RegistroViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(50, ErrorMessage = "El {0} debe estar entre al menos {2} caracteres de longitud", MinimumLength = 5)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Required(ErrorMessage = "La confirmación de contraseña es obligatoria")]
        [Compare("Password", ErrorMessage = "La contraseña y confirmación de contraseña no coinciden")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Contraseña")]
        public string ConfirmPassword { get; set; }

        //CenturyMold
        public string? Ntuser { get; set; }
        [Display(Name = "EMPLOYEE NUMBER")]
        public string? EmployeeNumber { get; set; }

        [Display(Name = "NAMES")]
        public string Names { get; set; } = null!;
        [Display(Name = "LASTNAME")]
        public string LastName { get; set; } = null!;

        ////Para selección de roles
        //[Display(Name = "Seleccionar rol")]
        //public IEnumerable<SelectListItem> ListaRoles { get; set; }
        //[Display(Name = "Rol seleccionado")]
        //public string RolSeleccionado { get; set; }
    }
}
