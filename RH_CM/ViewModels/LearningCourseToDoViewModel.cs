namespace RH_CM.ViewModels
{
    public class LearningCourseToDoViewModel
    {
        public int PK_CourseAssignment { get; set; }
        //public int FK_Position { get; set; }
        public string NAME_POSITION_ENGLISH { get; set; } = "—";

        // Opcional si decides incluir FK_Course en el SELECT del SP
        public int FK_Course { get; set; }

        public string CourseName { get; set; } = "—";
        public string CourseLevel { get; set; } = "—";      // LC.DESCRIPCTION_LEVEL
        public string DeliveryMode { get; set; } = "—";     // CASE 0 => "Not Assigned" | DM.DESCRIPTION_DELIVERYMODE
        public int? CourseValidityDays { get; set; }

        //public string UserName { get; set; } = "—";
        public string CONTROL_NUMBER { get; set; } = "—";
        public string FullName { get; set; } = "—";

        public DateTime? LastUpdateDate { get; set; }       // np.LastUpdateDate
        public string CourseStatus { get; set; } = "—";     // CS.DESCRIPTION_COURSESTATUS (si lo devuelves)
    }
}
