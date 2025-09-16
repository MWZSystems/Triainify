namespace RH_CM.ViewModels
{
    public class CourseMaterialViewerViewModel
    {
        public int CourseId { get; set; }
        public int LevelId { get; set; }
        public string StreamUrl { get; set; } = string.Empty; // URL que sirve el PDF
        public string? CourseName { get; set; }               // opcional
    }
}
