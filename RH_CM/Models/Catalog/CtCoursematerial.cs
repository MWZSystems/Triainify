using System;
using System.ComponentModel.DataAnnotations;

namespace RH_CM.Models
{
    public partial class CtCoursematerial
    {
        [Required]
        public int PkCoursematerial { get; set; }

        public string? NameMaterial { get; set; }
        public string? MaterialType { get; set; }
        public byte[]? File { get; set; }
        public string? UrlPath { get; set; }
        public string? CreateUser { get; set; }
        public DateTime? CreateDate { get; set; }
        public string? LastUpdateUser { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public int? Available { get; set; }
    }
}
