namespace RH_CM.ViewModels
{
    public class CourseAssignmentListItemViewModel
    {
        public int PkCourseAssignment { get; set; }
        public string PositionName { get; set; }
        public string CourseName { get; set; }
        // 👇 NUEVO
        public int FkRequiredCourseLevels { get; set; }
        public string RequiredCourseLevelDescription { get; set; }

        public bool Requiered { get; set; }
        public int Available { get; set; }
        public string DeliveryModeDescription { get; set; }
    }
}
