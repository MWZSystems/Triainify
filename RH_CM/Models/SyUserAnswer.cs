using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class SyUserAnswer
    {
        public int PkUserAnswers { get; set; }
        public int FkTest { get; set; }
        public int FkQuestions { get; set; }
        public int FkOptions { get; set; }
        public bool IsSelected { get; set; }
        public bool IsCorrected { get; set; }
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public int Available { get; set; }
    }
}
