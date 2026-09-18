using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtSupervisor
    {
        public int PkSupervisorId { get; set; }
        public int FkHeadcount { get; set; }
        public int FkDepartment { get; set; }
        public int FkPosition { get; set; }
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupdatedate { get; set; }
        public int Available { get; set; }
    }
}
