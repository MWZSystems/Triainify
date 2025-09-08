namespace RH_CM.ViewModels
{
    public class CoursesByPositionViewModel
    {
        public int FK_Position { get; set; }
        public string PositionName { get; set; } = "";

        public int PK_CourseAssignment { get; set; }

        public int FK_Course { get; set; }
        public string CourseName { get; set; } = "";

        public int? LevelId { get; set; }            // puede venir NULL si no aplica
        public string? LevelName { get; set; }

        public bool Requiered { get; set; }          // typo intencionado (match DB)

        public int? DeliveryModeId { get; set; }
        public string? DeliveryMode { get; set; }

        public int? CourseValidityDays { get; set; }
    }
}
