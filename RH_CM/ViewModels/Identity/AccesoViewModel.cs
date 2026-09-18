using System.ComponentModel.DataAnnotations;

namespace RH_CM.ViewModels
{
    public class AccesoViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}
