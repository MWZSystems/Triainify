using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtCoursematerial
    {
        public int PkCoursematerial { get; set; }
        public string NameMaterial { get; set; } = null!;
        public byte[] File { get; set; } = null!;
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupdatedate { get; set; }
        public int Available { get; set; }
    }
}
