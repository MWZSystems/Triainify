using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class SyHeadCount
    {
        public int PkHeadcount { get; set; }
        public int ControlNumber { get; set; }
        public byte[]? Photo { get; set; }
        public string Names { get; set; } = null!;
        public string? LastName { get; set; }
        public string? SecondName { get; set; }
        public string LevelEmployee { get; set; } = null!;
        public string ShiftWork { get; set; } = null!;
        public DateTime StarDate { get; set; }
        public string Curp { get; set; } = null!;
        public string Rfc { get; set; } = null!;
        public string SocialSecurity { get; set; } = null!;
        public DateTime Birthdate { get; set; }
        public string Sex { get; set; } = null!;
        public string? MaritalStatus { get; set; }
        public string Street { get; set; } = null!;
        public string Neighborhood { get; set; } = null!;
        public string City { get; set; } = null!;
        public int ZipCode { get; set; }
        public string Phone1 { get; set; } = null!;
        public string? Phone2 { get; set; }
        public string? Email { get; set; }
        public string? EducationLevel { get; set; }
        public string? Specialization { get; set; }
        public int FkDepartment { get; set; }
        public int FkPosition { get; set; }
        public int ZipCodesat { get; set; }
        public string? SupervisorCode { get; set; }
        public string? Supervisor { get; set; }
        public string Createuser { get; set; } = null!;
        public string Lastuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public DateTime Lastupdate { get; set; }
        public int Available { get; set; }
    }
}
