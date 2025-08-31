namespace RH_CM.Service.DTOs
{
    public class ExternalEvidenceDTOs
    {
        public int ID {  get; set; }
        public string? FullName { get; set; }
        public string? CourseName { get; set; }
        public string? LevelName { get; set; }
        public decimal? Score { get; set; }
        public DateTime? CreateDate { get; set; }
        public string? Status { get; set; }

    }
}
