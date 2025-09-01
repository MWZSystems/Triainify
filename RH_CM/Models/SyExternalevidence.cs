using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class SyExternalevidence
    {
        public int PkExternalEvidence { get; set; }
        public int? FkMovementCourse { get; set; }
        public string? EvidenceFileName { get; set; }
        public byte[]? EvidenceFile { get; set; }
        public decimal? Score { get; set; }
        public string? CreateUser { get; set; }
        public DateTime? CreateDate { get; set; }
        public string? LastUpdateUser { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public int? Available { get; set; }
    }
}
