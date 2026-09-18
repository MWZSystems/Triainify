using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtVideomaterial
    {
        public int PkVideomaterial { get; set; }
        public string? NameVideo { get; set; }
        public string? UrlPath { get; set; }
        public string? Createuser { get; set; }
        public DateTime? Createdate { get; set; }
        public string? Lastupdateuser { get; set; }
        public DateTime? Lastupdatedate { get; set; }
        public int? Available { get; set; }
    }
}
