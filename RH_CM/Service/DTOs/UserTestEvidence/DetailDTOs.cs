namespace RH_CM.Service.DTOs.UserTestEvidence
{
    public class DetailDTOs
    {
        public string? ControlNumber { get; set; }
        public string? FullName { get; set; }
        public string? Position { get; set; }
        public List<DetailUserExamDTOs> Details { get; set; } = new();
    }
}
