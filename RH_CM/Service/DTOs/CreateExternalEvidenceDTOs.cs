namespace RH_CM.Service.DTOs
{
    public class CreateExternalEvidenceDTOs
    {
        public List<string>? User {  get; set; }

        public List<string>? Course { get; set; }

        public List<string>? Level { get; set; }

        public byte[]? FileContent { get; set; }

    }
}
