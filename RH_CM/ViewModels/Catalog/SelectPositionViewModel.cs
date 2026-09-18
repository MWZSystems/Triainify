using Microsoft.AspNetCore.Mvc.Rendering;

namespace RH_CM.ViewModels
{
    public class SelectPositionViewModel
    {
        public int? SelectedPositionId { get; set; }
        public IEnumerable<SelectListItem> Positions { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}
