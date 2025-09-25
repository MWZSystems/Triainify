using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class SyUserDiagnostic
    {
        public int PkUserDiagnostic { get; set; }
        public int CodeExam { get; set; }
        public int FkTest { get; set; }
        public int FkQuestions { get; set; }
        public string FkOptionSelected { get; set; } = null!;
        public string FkOptionCorrected { get; set; } = null!;
        public int FkHeadcount { get; set; }
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public int Available { get; set; }
    }
}
