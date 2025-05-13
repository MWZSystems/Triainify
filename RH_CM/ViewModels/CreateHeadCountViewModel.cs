using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using RH_CM.Models;
using System.ComponentModel.DataAnnotations;

namespace RH_CM.ViewModels
{
    public class CreateHeadCountViewModel
    {
        [Required(ErrorMessage = "Control Number is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Control Number must be greater than 0.")]
        public int ControlNumber { get; set; }

        public byte[]? Photo { get; set; }

        [Required(ErrorMessage = "Names field is required.")]
        public string Names { get; set; } = null!;

        public string? LastName { get; set; }
        public string? SecondName { get; set; }

        [Required(ErrorMessage = "Level Employee field is required.")]
        public string? LevelEmployee { get; set; }

        [Required(ErrorMessage = "Shift Work field is required.")]
        public string ShiftWork { get; set; } = null!;

        [Required(ErrorMessage = "Start Date is required.")]
        [Range(typeof(DateTime), "1900-01-02", "9999-12-31", ErrorMessage = "Start Date must be a valid date.")]
        public DateTime StarDate { get; set; } = new DateTime(1900, 1, 1);

        [Required(ErrorMessage = "CURP is required.")]
        public string Curp { get; set; } = null!;

        [Required(ErrorMessage = "RFC is required.")]
        public string Rfc { get; set; } = null!;

        [Required(ErrorMessage = "Social Security field is required.")]
        public string SocialSecurity { get; set; } = null!;

        [Required(ErrorMessage = "Birthdate is required.")]
        [Range(typeof(DateTime), "1900-01-02", "9999-12-31", ErrorMessage = "Birthdate must be a valid date.")]
        public DateTime Birthdate { get; set; } = new DateTime(1900, 1, 1);

        [Required(ErrorMessage = "Sex field is required.")]
        public string Sex { get; set; } = null!;
        public string? MaritalStatus { get; set; }

        [Required(ErrorMessage = "Street field is required.")]
        public string? Street { get; set; }

        [Required(ErrorMessage = "Neighborhood field is required.")]
        public string? Neighborhood { get; set; }

        [Required(ErrorMessage = "ZIP Code field is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "ZIP Code must be greater than 0.")]
        public int ZipCode { get; set; }

        public string? City { get; set; }

        [Required(ErrorMessage = "Phone1 is required.")]
        [RegularExpression(@"^\d+$", ErrorMessage = "Phone number must contain only numbers.")]
        public string? Phone1 { get; set; }

        [RegularExpression(@"^\d+$", ErrorMessage = "Phone number must contain only numbers.")]
        public string? Phone2 { get; set; }

        public string? Email { get; set; }
        public string? EducationLevel { get; set; }
        public string? Specialization { get; set; }

        [Required(ErrorMessage = "Please select a Department.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a Department")]
        public int FkDepartment { get; set; }

        [Required(ErrorMessage = "Please select a Position.")]
        [Range(1, int.MaxValue, ErrorMessage = "Please select a Position.")]
        public int FkPosition { get; set; }

        [Required(ErrorMessage = "Zip Code SAT is required.")]
        [Range(1, int.MaxValue, ErrorMessage = "Zip Code SAT must be greater than 0.")]
        public int ZipCodesat { get; set; }

        public string? AssignSupervisorCode { get; set; }
        public string? Supervisor { get; set; }

        [ValidateNever]
        public IEnumerable<CtDepartment> Departments { get; set; }
        [ValidateNever]
        public IEnumerable<CtPosition> Position { get; set; }
        [ValidateNever]
        public IEnumerable<SyHeadCount> SyHeadCount { get; set; }
    }
}
