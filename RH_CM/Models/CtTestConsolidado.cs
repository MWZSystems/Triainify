using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtTestConsolidado
    {
        public int? FkCourse { get; set; }
        public int? Level { get; set; }
        public string? TestName { get; set; }
        public int? QuestionNumber { get; set; }
        public string? Question { get; set; }
        public string? Options { get; set; }
        public int? Answer { get; set; }
        public string? CreateUser { get; set; }
        public DateTime? CreateDate { get; set; }
        public string? LastUpdateUser { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public int? Available { get; set; }
    }
}
