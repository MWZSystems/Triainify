using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtPermissionsgroup
    {
        public int PkPermissionGroup { get; set; }
        public string GroupName { get; set; } = null!;
        public string? ControllerName { get; set; }
    }
}
