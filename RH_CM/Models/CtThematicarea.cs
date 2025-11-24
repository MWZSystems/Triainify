using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtThematicarea
    {
        public int PkThematicarea { get; set; }
        public int ThematicCode { get; set; }
        public string ThematicName { get; set; } = null!;
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupdatedate { get; set; }
        public int Available { get; set; }
    }
}
