using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtLevelcourse
    {
        public CtLevelcourse()
        {
            CtCourseLevelMaterials = new HashSet<CtCourseLevelMaterial>();
        }

        public int PkLevelcourse { get; set; }
        public string DescripctionLevel { get; set; } = null!;
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupatedate { get; set; }
        public int Available { get; set; }

        public virtual ICollection<CtCourseLevelMaterial> CtCourseLevelMaterials { get; set; }
    }
}
