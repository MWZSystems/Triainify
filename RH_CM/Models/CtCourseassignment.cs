using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtCourseassignment
    {
        public int PkCourseAssignment { get; set; }
        public int FkPosition { get; set; }
        public int FkCourse { get; set; }
        public int FkRequiredCourseLevels { get; set; }
        public bool Requiered { get; set; }
        public string CreateUser { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string LastUpdateUser { get; set; } = null!;
        public DateTime LastUpdateDate { get; set; }
        public int Available { get; set; }
    }
}
