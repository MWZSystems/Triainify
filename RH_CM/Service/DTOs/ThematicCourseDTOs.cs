namespace RH_CM.Service.DTOs
{
    public class ThematicCourseDTOs
    {
        public List<ThematicCourseList>? ThematicCourses {  get; set; }
        public List<Course>? Courses { get; set; }
        public List<Thematic>? Thematics { get; set; }
    }

    public class ThematicCourseList 
    {
        public int Id {  get; set; }
        public int FkCourse {  get; set; }
        public string? CourseName { get; set; }
        public int FkThematicArea { get; set; }
        public string? ThematicName { get; set; }
        public int ThematicCode { get; set; }
        public int Available {  get; set; }

    }

    public class Course
    {
        public string? Courses { get; set; }
    }
    public class Thematic
    {
        public string? Thematics { get; set; }
    }
}
