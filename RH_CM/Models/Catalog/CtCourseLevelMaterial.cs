using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtCourseLevelMaterial
    {
        public int PkCourseLevelMaterial { get; set; }
        public int FkCourse { get; set; }
        public int FkLevelCourse { get; set; }
        public int FkCourseMaterial { get; set; }
        public string? CreateUser { get; set; }
        public DateTime? CreateDate { get; set; }
        public string? LastUpdateUser { get; set; }
        public DateTime? LastUpdateDate { get; set; }
        public int Available { get; set; }
    }
}
