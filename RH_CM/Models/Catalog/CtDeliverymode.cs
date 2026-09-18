using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtDeliverymode
    {
        public int PkDeliverymode { get; set; }
        public string DescriptionDeliverymode { get; set; } = null!;
        public string Createuser { get; set; } = null!;
        public string Createdate { get; set; } = null!;
        public string Lastupdateuser { get; set; } = null!;
        public string Lastupatedate { get; set; } = null!;
        public int Available { get; set; }
    }
}
