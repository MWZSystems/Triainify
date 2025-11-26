using System;
using System.Collections.Generic;

namespace RH_CM.Models
{
    public partial class CtPermission
    {
        public int PkPermission { get; set; }
        public int FkPermissionGroup { get; set; }
        public string FkRoleId { get; set; } = null!;
    }
}
