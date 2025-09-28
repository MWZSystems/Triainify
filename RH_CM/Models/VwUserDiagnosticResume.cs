using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class VwUserDiagnosticResume
    {
        public int CodeExam { get; set; }
        public double? Score { get; set; }
        public DateTime? ExamDate { get; set; }
    }
}
