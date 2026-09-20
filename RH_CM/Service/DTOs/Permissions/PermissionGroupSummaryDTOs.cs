namespace RH_CM.Service.DTOs.Permissions
{
    /// <summary>
    /// One row of the Permissions management list: enough detail (controller + how many roles
    /// already have access) to actually understand what a group is, instead of just its name.
    /// </summary>
    public class PermissionGroupSummaryDTOs
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string? ControllerName { get; set; }
        public int RoleCount { get; set; }
        public string GroupKey => $"{GroupId} - {GroupName}";
    }
}
