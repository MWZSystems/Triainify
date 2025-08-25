using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtCourse
    {
        public CtCourse()
        {
            CtCourseLevelMaterials = new HashSet<CtCourseLevelMaterial>();
        }

        public int PkCourse { get; set; }
        public string ManagementSystem { get; set; } = null!;
        public string Idcourse { get; set; } = null!;
        public string CourseName { get; set; } = null!;
        public int CourseValidityDays { get; set; }
        public int Revision { get; set; }
        public string CreateUser { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string LastUpdateUser { get; set; } = null!;
        public DateTime LastUpdateDate { get; set; }
        public int Available { get; set; }

        public virtual ICollection<CtCourseLevelMaterial> CtCourseLevelMaterials { get; set; }
    }
}
