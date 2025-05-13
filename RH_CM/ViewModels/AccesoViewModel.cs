using System.ComponentModel.DataAnnotations;

namespace RH_CM.ViewModels
{
    public class AccesoViewModel
    {
        [Required(ErrorMessage = "El email es obligatorio")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }

        [Display(Name = "Recordar datos?")]
        public bool RememberMe { get; set; }
    }
}
