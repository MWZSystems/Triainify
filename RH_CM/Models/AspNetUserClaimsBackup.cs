using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class AspNetUserClaimsBackup
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string? ClaimType { get; set; }
        public string? ClaimValue { get; set; }
    }
}
