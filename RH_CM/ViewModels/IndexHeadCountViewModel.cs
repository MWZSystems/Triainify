namespace RH_CM.ViewModels
{
    public class IndexHeadCountViewModel
    {
        public int PkHeadcount { get; set; }
        public int ControlNumber { get; set; }
        public string Names { get; set; } = null!;
        public string? LastName { get; set; }
        public string? SecondName { get; set; }
        public string? LevelEmployee { get; set; }
        public string ShiftWork { get; set; } = null!;
        public DateTime StarDate { get; set; }
        public string Curp { get; set; } = null!;
        public string Rfc { get; set; } = null!;
        public string SocialSecurity { get; set; } = null!;
        public int Age { get; set; }
        public DateTime Birthdate { get; set; }
        public string Sex { get; set; } = null!;
        public string? MaritalStatus { get; set; }
        public string? Street { get; set; }
        public string? Neighborhood { get; set; }
        public string? City { get; set; }
        public int? ZipCode { get; set; }
        public string? Phone1 { get; set; }
        public string? Phone2 { get; set; }
        public string? Email { get; set; }
        public string? EducationLevel { get; set; }
        public string? Specialization { get; set; }
        public int FkDepartment { get; set; }
        public string NameDeparment { get; set; } = null!;
        public int FkPosition { get; set; }
        public string NamePosition { get; set; }
        public int ZipCodesat { get; set; }
        public int Supervisor { get; set; }
    }
}
