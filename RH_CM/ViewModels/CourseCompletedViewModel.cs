namespace RH_CM.ViewModels
{
    public class CourseCompletedViewModel
    {
        public int PkCourseCompleted { get; set; }
        public int FkCourseAssignment { get; set; }
        public int FkCourseStatus { get; set; }
        public int FkDeliveryMode { get; set; }
        public int FkHeadcount { get; set; }
        public int Score { get; set; }
        public string CreateUser { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string LastUpdateUser { get; set; } = null!;
        public DateTime LastUpdateDate { get; set; }
        public int Avaialble { get; set; }
    }
}
