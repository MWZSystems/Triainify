namespace RH_CM.Service.DTOs
{
    public class GetExternalEvidenceDTOs
    {
        public int ID {  get; set; }
        public string? FullName { get; set; }
        public string? CourseName { get; set; }
        public string? LevelName { get; set; }
        public decimal? Score { get; set; }
        public string? Status { get; set; }

    }
}
