using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtThematiccourse
    {
        public int PkThematiccourse { get; set; }
        public int FkCourse { get; set; }
        public int FkThematicarea { get; set; }
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupdatedate { get; set; }
        public int Available { get; set; }
    }
}
