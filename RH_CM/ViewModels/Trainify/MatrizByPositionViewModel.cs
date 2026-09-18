namespace RH_CM.ViewModels
{
    public class MatrizByPositionViewModel
    {
        public int FK_Position { get; set; }
        public string? NAME_POSITION_ENGLISH { get; set; }
        public int FK_Course { get; set; }
        public string? CourseName { get; set; }
        public int FK_RequiredCourseLevels { get; set; }
        public string? DESCRIPCTION_LEVEL { get; set; }
        public bool Requiered { get; set; }
        public int FK_DeliveryMode { get; set; }
        public int? CourseValidityDays { get; set; }
    }
}
