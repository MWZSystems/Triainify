using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtQuestion
    {
        public int PkQuestions { get; set; }
        public int FkTest { get; set; }
        public string Question { get; set; } = null!;
        public int FkTypeOption { get; set; }
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupatedate { get; set; }
        public int Available { get; set; }
    }
}
