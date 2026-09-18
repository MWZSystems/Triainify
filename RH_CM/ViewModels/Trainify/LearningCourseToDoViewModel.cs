namespace RH_CM.ViewModels
{
    public class LearningCourseToDoViewModel
    {
        public int PK_CourseAssignment { get; set; }
        public int FK_Course { get; set; }
        public string CourseName { get; set; } = "—";
        public string CourseLevel { get; set; } = "—";
        public int FK_RequiredCourseLevels { get; set; }   // new
        public string DeliveryMode { get; set; } = "—";
        public int? CourseValidityDays { get; set; }
        public string NAME_POSITION_ENGLISH { get; set; } = "—";
        public string CONTROL_NUMBER { get; set; } = "—";
        public string FullName { get; set; } = "—";
        public DateTime? LastUpdateDate { get; set; }
        public string CourseStatus { get; set; } = "—";
    }
}
