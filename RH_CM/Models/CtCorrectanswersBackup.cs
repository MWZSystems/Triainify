using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtCorrectanswersBackup
    {
        public int PkAnswers { get; set; }
        public int FkQuestions { get; set; }
        public string FkOptions { get; set; } = null!;
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupatedate { get; set; }
        public int Available { get; set; }
    }
}
