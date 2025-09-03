namespace RH_CM.Service.DTOs
{
    public class CreateExternalEvidenceInputDTOs
    {
         
        public List<string>? SelectedUsers { get; set; }
        public string? SelectedCourse { get; set; }
        public string? SelectedLevel { get; set; }
        public decimal Score { get; set; }
        public string? UserName { get; set; }
        public IFormFile? UploadedFile { get; set; }
        

    }
}
