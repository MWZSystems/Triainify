// RH_CM/ViewModels/SyCourseCompletedVM.cs
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RH_CM.ViewModels
{
    public class SyCourseCompletedVM
    {
        public int PkCourseCompleted { get; set; }

        [Display(Name = "Course Assignment")]
        [Required]
        public int FkCourseAssignment { get; set; }

        [Display(Name = "Course Status")]
        [Required]
        public int FkCourseStatus { get; set; }

        [Display(Name = "Delivery Mode")]
        public int FkDeliveryMode { get; set; } // Permitimos 0 = Not Assigned

        [Display(Name = "Employee (Headcount)")]
        [Required]
        public int FkHeadcount { get; set; }

        [Display(Name = "Created By")]
        public string CreateUser { get; set; } = string.Empty;

        [Display(Name = "Created At")]
        public DateTime CreateDate { get; set; }

        [Display(Name = "Updated By")]
        public string LastUpdateUser { get; set; } = string.Empty;

        [Display(Name = "Updated At")]
        public DateTime LastUpdateDate { get; set; }

        [Display(Name = "Available")]
        public int Avaialble { get; set; } = 1; // 1=active, 0=inactive

        // Catalogs (dropdown options)
        public SelectList CourseAssignments { get; set; } = default!;
        public SelectList CourseStatuses { get; set; } = default!;
        public SelectList DeliveryModes { get; set; } = default!;
        public SelectList Headcounts { get; set; } = default!;
    }
}
