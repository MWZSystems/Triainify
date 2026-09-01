using System.ComponentModel.DataAnnotations;
using DocumentFormat.OpenXml.Bibliography;

namespace RH_CM.Service.DTOs
{
    public class EditExternalEvidenceDTOs
    {
        [Required(ErrorMessage = "El identificador del registro es requerido.")]
        public int? PK_ExternalEvidence { get; set; }

        // Not [Required]: the Edit Evidence form only shows this as read-only text (no input
        // named "ControlNumber"), so it is never posted back — same reasoning as Score below.
        public int? ControlNumber { get; set; }

        public string? FullName { get; set; }
        public string? NamePosition { get; set; }
        public string? CourseName { get; set; }
        public string? Level { get; set; }
        public byte[]? EvidenceFile { get; set; }

        // Not [Required]: this field isn't collected on the Edit Evidence form (no input for it)
        // and isn't sent to the update stored procedure — marking it Required made every
        // submission of this form fail ModelState validation with no way for the user to fix it.
        [Range(0, 100, ErrorMessage = "La calificación debe estar entre 0 y 100.")]
        public decimal? Score { get; set; }

        public string? UserName { get; set; }
    }
}