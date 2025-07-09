using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtCoursematerial
    {
        public int PkCoursematerial { get; set; }
        public string NameMaterial { get; set; } = null!;
        public byte[] File { get; set; } = null!;
        public int? FkCourse { get; set; }
        public int? Level { get; set; }
        public string? Createuser { get; set; }
        public DateTime? Createdate { get; set; }
        public string? Lastupdateuser { get; set; }
        public DateTime? Lastupdatedate { get; set; }
        public int? Available { get; set; }
    }
}
