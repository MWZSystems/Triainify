namespace RH_CM.ViewModels
{
    public class SyHeadCountViewModel
    {
        public int PK_HEADCOUNT { get; set; }
        public int CONTROL_NUMBER { get; set; }
        public byte[]? PHOTO { get; set; }  // Assuming PHOTO is stored as a binary image
        public string NAMES { get; set; } = string.Empty;
        public string? LAST_NAME { get; set; }
        public string? SECOND_NAME { get; set; }
        public string? LEVEL_EMPLOYEE { get; set; }
        public string SHIFT_WORK { get; set; } = string.Empty;
        public DateTime STAR_DATE { get; set; }
        public string CURP { get; set; } = null!;
        public string RFC { get; set; } = null!;
        public string SOCIAL_SECURITY { get; set; } = null!;
        public DateTime BIRTHDATE { get; set; }
        public int AGE { get; set; }  // Calculated field
        public string SEX { get; set; } = null!;
        public string MARITAL_STATUS { get; set; } = string.Empty;
        public string? STREET { get; set; }
        public string? NEIGHBORHOOD { get; set; }
        public string? CITY { get; set; }
        public int ZIP_CODE { get; set; }
        public string? PHONE1 { get; set; }
        public string? PHONE2 { get; set; }
        public string? EMAIL { get; set; }
        public string? EDUCATION_LEVEL { get; set; }
        public string SPECIALIZATION { get; set; } = string.Empty;
        //public int FK_DEPARTMENT { get; set; }
        public string NAME_DEPARMENT { get; set; } = string.Empty;  // NAME_DEPARTMENT could be corrected in the DB
        //public int FK_POSITION { get; set; }
        public string NAME_POSITION { get; set; } = string.Empty;
        public int ZIP_CODESAT { get; set; }
        //public int SUPERVISOR { get; set; }
        public string SUPERVISOR_NAME { get; set; } = string.Empty;
        public int CONTROL_NUMBER_SUPERVISOR { get; set; }
        public string CREATEUSER { get; set; } = null!;
        public string LASTUSER { get; set; } = null!;
        public DateTime CREATEDATE { get; set; }
        public DateTime LASTUPDATE { get; set; }
        public int AVAILABLE { get; set; }
    }
}
