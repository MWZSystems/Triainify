using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class AspNetRolesBackup
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public string? NormalizedName { get; set; }
        public string? ConcurrencyStamp { get; set; }
    }
}
