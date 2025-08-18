using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class SyCousemovement
    {
        public int PkMovementCourse { get; set; }
        public int FkCourseCompleted { get; set; }
        public int FkCourseStatus { get; set; }
        public int FkDeliveryMode { get; set; }
        public int FkHeadcount { get; set; }
        public string CreateUser { get; set; } = null!;
        public DateTime CreateDate { get; set; }
        public string LastUpdateUser { get; set; } = null!;
        public DateTime LastUpdateDate { get; set; }
        public int Avaialble { get; set; }
    }
}
