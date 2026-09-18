using Microsoft.AspNetCore.Mvc.Rendering;

namespace RH_CM.ViewModels
{
    public class MatrizByPositionPageViewModel
    {
        public int? SelectedPositionId { get; set; }
        public string? PositionNameEnglish { get; set; }
        public IEnumerable<MatrizByPositionViewModel> Results { get; set; } = Enumerable.Empty<MatrizByPositionViewModel>();
    }
}
