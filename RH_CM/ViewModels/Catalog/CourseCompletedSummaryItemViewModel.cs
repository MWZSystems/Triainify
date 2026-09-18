namespace RH_CM.ViewModels
{
    public class CourseCompletedSummaryItemViewModel
    {
        public string CourseName { get; set; } = "";
        public string LevelName { get; set; } = "";
        public string DeliveryModeName { get; set; } = "";
        public string Course_Status { get; set; } = "";
        public int TotalCompletions { get; set; }
    }
}
