using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtTest
    {
        public int PkTest { get; set; }
        public int FkCourse { get; set; }
        public int CourseLevel { get; set; }
        public string TestName { get; set; } = null!;
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupatedate { get; set; }
        public int Available { get; set; }
    }
}
