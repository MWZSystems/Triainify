namespace RH_CM.ViewModels
{
    public class CourseCompletedIndexItemViewModel
    {
        public int PkCourseCompleted { get; set; }
        public string ControlNumber { get; set; } = "";
        public string HeadcountName { get; set; } = "";
        public string CourseName { get; set; } = "";
        public string LevelDescription { get; set; } = "";
        public DateTime? LastUpdateDate { get; set; }
        public int Avaialble { get; set; }  // yes, misspelled — that's how the column is written in the DB
    }
}
