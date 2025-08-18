using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtCoursestatus
    {
        public int PkCoursestatus { get; set; }
        public string DescriptionCoursestatus { get; set; } = null!;
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupatedate { get; set; }
        public int Available { get; set; }
    }
}
