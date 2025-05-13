using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtPosition
    {
        public int PkPosition { get; set; }
        public string NamePosition { get; set; } = null!;
        public string NamePositionEnglish { get; set; } = null!;
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupdatedate { get; set; }
        public int Available { get; set; }
    }
}
