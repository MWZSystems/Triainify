using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtOptionsBackup
    {
        public int PkOptions { get; set; }
        public int FkQuestions { get; set; }
        public string Options { get; set; } = null!;
        public int Answer { get; set; }
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupatedate { get; set; }
        public int Available { get; set; }
    }
}
