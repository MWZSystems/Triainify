using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtDepartment
    {
        public int PkDepartment { get; set; }
        public string NameDeparment { get; set; } = null!;
        public string Createuser { get; set; } = null!;
        public DateTime Createdate { get; set; }
        public string Lastupdateuser { get; set; } = null!;
        public DateTime Lastupdatedate { get; set; }
        public int Available { get; set; }
    }
}
