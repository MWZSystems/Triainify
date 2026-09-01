namespace RH_CM.Service.DTOs
{
    public class ExternalEvidenceFeedbackItemDTOs
    {
        public int ControlNumber { get; set; }
        public string FullName { get; set; } = "";
        public string PositionName { get; set; } = "";
        public int CourseID { get; set; }
        public string CourseName { get; set; } = "";
        public string Level { get; set; } = "";
        public string EvidenceFileName { get; set; } = "";
        public bool Success { get; set; }
        public string FeedBackComment { get; set; } = "";
    }
}
