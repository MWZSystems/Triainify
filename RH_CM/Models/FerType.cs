using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class FerType
    {
        public int? FkCourse { get; set; }
        public int? FkRequiredCourseLevels { get; set; }
        public string? TypeCourse { get; set; }
    }
}
