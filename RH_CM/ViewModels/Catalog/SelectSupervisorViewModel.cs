using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace RH_CM.ViewModels
{
    public class SelectSupervisorViewModel
    {
        [Display(Name = "Supervisor")]
        [Required(ErrorMessage = "Please select a supervisor.")]
        public int? SelectedSupervisorId { get; set; }

        public List<SelectListItem> Supervisors { get; set; } = new();
    }
}
