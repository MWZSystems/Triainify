namespace RH_CM.ViewModels
{
    public class CourseAssignmentListItemViewModel
    {
        public int PkCourseAssignment { get; set; }
        public string PositionName { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        // 👇 NUEVO
        public int FkRequiredCourseLevels { get; set; }
        public string RequiredCourseLevelDescription { get; set; } = string.Empty;

        public bool Requiered { get; set; }
        public int Available { get; set; }
        public string DeliveryModeDescription { get; set; } = string.Empty;
    }
}
