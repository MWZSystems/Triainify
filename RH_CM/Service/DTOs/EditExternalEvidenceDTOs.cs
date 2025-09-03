using DocumentFormat.OpenXml.Bibliography;

namespace RH_CM.Service.DTOs
{
    public class EditExternalEvidenceDTOs
    {
        public int PK_ExternalEvidence { get; set; }
        public int ControlNumber { get; set; }
        public string? FullName { get; set; }
        public string? NamePosition { get; set; }
        public string? CourseName { get; set; }
        public string? Level { get; set; }
        public byte[]? EvidenceFile { get; set; }
        public decimal Score { get; set; }
    }
}
