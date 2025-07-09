using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtResult
    {
        public int PkResults { get; set; }
        public int FkHeadcount { get; set; }
        public int FkTest { get; set; }
        public decimal Results { get; set; }
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupatedate { get; set; }
        public int Available { get; set; }
    }
}
