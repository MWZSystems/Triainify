using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RH_CM.Service.DTOs
{
    public class CreateExternalEvidenceInputDTOs
    {
        [Required(ErrorMessage = "Debes seleccionar al menos un usuario.")]
        [MinLength(1, ErrorMessage = "Debes seleccionar al menos un usuario.")]
        public List<string>? SelectedUsers { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un curso.")]
        public string? SelectedCourse { get; set; }

        [Required(ErrorMessage = "Debes seleccionar un nivel.")]
        public string? SelectedLevel { get; set; }

        // Changed from 'decimal' to 'decimal?': a non-nullable value type bound as controller
        // input silently defaults to 0 when the client omits it (under-posting) instead of
        // failing validation. Nullable + [Required] makes an omitted Score show up as a real
        // ModelState error.
        [Required(ErrorMessage = "La calificación es requerida.")]
        [Range(0, 100, ErrorMessage = "La calificación debe estar entre 0 y 100.")]
        public decimal? Score { get; set; }

        public string? UserName { get; set; }

        // Validated manually in the controller (extension, size, presence) rather than with
        // DataAnnotations, since IFormFile doesn't validate well with attributes alone.
        public IFormFile? UploadedFile { get; set; }
    }
}