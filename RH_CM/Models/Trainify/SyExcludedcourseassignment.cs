using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class SyExcludedcourseassignment
    {
        public int PkExcludedCourses { get; set; }
        public int FkCourseAssignment { get; set; }
        public string Comment { get; set; } = null!;
        public string CreateUser { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string LastUpdateUser { get; set; } = null!;
        public DateTime LastUpdateDate { get; set; }
        public int Available { get; set; }
    }
}
