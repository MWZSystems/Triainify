namespace RH_CM.ViewModels
{
    public class MatrizByEmployeeViewModel
    {
        public int FK_Position { get; set; }
        public string NAME_POSITION_ENGLISH { get; set; }
        public int FK_Course { get; set; }
        public string CourseName { get; set; }
        public int FK_RequiredCourseLevels { get; set; }
        public bool Requiered { get; set; }
        public int FK_DeliveryMode { get; set; }
        public string DESCRIPTION_DELIVERYMODE { get; set; }  // <-- NUEVO
        public int? CourseValidityDays { get; set; }
        public string UserName { get; set; }
        public string CONTROL_NUMBER { get; set; }
        public string NAMES { get; set; }
        public string LAST_NAME { get; set; }
        public string SECOND_NAME { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public string CourseStatus { get; set; }
    }
}
