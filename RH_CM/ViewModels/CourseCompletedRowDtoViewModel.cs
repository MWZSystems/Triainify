namespace RH_CM.ViewModels
{
    public class CourseCompletedRowDtoViewModel
    {
        public int PkCourseCompleted { get; set; }
        public string HeadcountControlNumber { get; set; } = "";
        public string HeadcountFullName { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string LevelName { get; set; } = "";
        public string StatusName { get; set; } = "";
        public string DeliveryModeName { get; set; } = "";
        public int Score { get; set; }
        public DateTime CreateDate { get; set; }
    }
}
