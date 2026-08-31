using System.ComponentModel.DataAnnotations;
using DocumentFormat.OpenXml.Bibliography;

namespace RH_CM.Service.DTOs
{
    public class EditExternalEvidenceDTOs
    {
        [Required(ErrorMessage = "El identificador del registro es requerido.")]
        public int? PK_ExternalEvidence { get; set; }

        [Required(ErrorMessage = "El número de control es requerido.")]
        public int? ControlNumber { get; set; }

        public string? FullName { get; set; }
        public string? NamePosition { get; set; }
        public string? CourseName { get; set; }
        public string? Level { get; set; }
        public byte[]? EvidenceFile { get; set; }

        [Required(ErrorMessage = "La calificación es requerida.")]
        [Range(0, 100, ErrorMessage = "La calificación debe estar entre 0 y 100.")]
        public decimal? Score { get; set; }

        public string? UserName { get; set; }
    }
}