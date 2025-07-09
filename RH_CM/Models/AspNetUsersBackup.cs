using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class AspNetUsersBackup
    {
        public string Id { get; set; } = null!;
        public string? UserName { get; set; }
        public string? NormalizedUserName { get; set; }
        public string? Email { get; set; }
        public string? NormalizedEmail { get; set; }
        public bool EmailConfirmed { get; set; }
        public string? PasswordHash { get; set; }
        public string? SecurityStamp { get; set; }
        public string? ConcurrencyStamp { get; set; }
        public string? PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public int? Available { get; set; }
        public DateTime? CreateDate { get; set; }
        public string Discriminator { get; set; } = null!;
        public string? EmployeeNumber { get; set; }
        public string? LastName { get; set; }
        public string? Names { get; set; }
        public string? Ntuser { get; set; }
    }
}
