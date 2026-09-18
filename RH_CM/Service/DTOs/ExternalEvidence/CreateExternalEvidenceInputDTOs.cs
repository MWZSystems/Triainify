using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RH_CM.Service.DTOs
{
    public class CreateExternalEvidenceInputDTOs
    {
        [Required(ErrorMessage = "You must select at least one user.")]
        [MinLength(1, ErrorMessage = "You must select at least one user.")]
        public List<string>? SelectedUsers { get; set; }

        [Required(ErrorMessage = "You must select a course.")]
        public string? SelectedCourse { get; set; }

        [Required(ErrorMessage = "You must select a level.")]
        public string? SelectedLevel { get; set; }

        // Changed from 'decimal' to 'decimal?': a non-nullable value type bound as controller
        // input silently defaults to 0 when the client omits it (under-posting) instead of
        // failing validation. Nullable + [Required] makes an omitted Score show up as a real
        // ModelState error.
        [Required(ErrorMessage = "Score is required.")]
        [Range(0, 100, ErrorMessage = "Score must be between 0 and 100.")]
        public decimal? Score { get; set; }

        public string? UserName { get; set; }

        // Validated manually in the controller (extension, size, presence) rather than with
        // DataAnnotations, since IFormFile doesn't validate well with attributes alone.
        public IFormFile? UploadedFile { get; set; }
    }
}