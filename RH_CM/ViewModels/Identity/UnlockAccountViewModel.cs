using System.ComponentModel.DataAnnotations;

namespace RH_CM.ViewModels
{
    public class UnlockAccountViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; } = string.Empty;
    }
}
