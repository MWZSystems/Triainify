namespace RH_CM.ViewModels
{
    public class MatrizByEmployeeViewModel
    {
        // De SY_HEADCOUNT / CT_POSITION / CT_COURSEASSIGNMENTS / CT_COURSE
        public int FK_Position { get; set; }
        public string NAME_POSITION_ENGLISH { get; set; } = string.Empty;

        public int FK_Course { get; set; }
        public string CourseName { get; set; } = string.Empty;

        public int FK_RequiredCourseLevels { get; set; }

        public bool Requiered { get; set; }                  // BIT -> bool
        public int FK_DeliveryMode { get; set; }             // viene en el SELECT

        public int? CourseValidityDays { get; set; }         // puede ser NULL en la BD

        // Usuario / empleado
        public string UserName { get; set; } = string.Empty;
        public string CONTROL_NUMBER { get; set; } = string.Empty;
        public string NAMES { get; set; } = string.Empty;
        public string LAST_NAME { get; set; } = string.Empty;
        public string SECOND_NAME { get; set; } = string.Empty;

        // Desde CTE/StatusCalculado y mapeo final
        public DateTime? LastUpdateDate { get; set; }
        public string CourseStatus { get; set; } = string.Empty;

        // LEFT JOIN CT_LEVELCOURSE
        public string? DESCRIPCTION_LEVEL { get; set; }      // puede venir NULL
    }
}
