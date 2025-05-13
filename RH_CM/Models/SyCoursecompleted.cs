using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class SyCoursecompleted
    {
        public int PkCourseCompleted { get; set; }
        public int FkCourseAssignment { get; set; }
        public int FkHeadcount { get; set; }
        public DateTime StartdateCourse { get; set; }
        public DateTime EnddateCourse { get; set; }
        public string CreateUser { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string LastUpdateUser { get; set; } = null!;
        public DateTime LastUpdateDate { get; set; }
        public int Avaialble { get; set; }
    }
}
