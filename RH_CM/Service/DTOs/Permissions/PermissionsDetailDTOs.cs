namespace RH_CM.Service.DTOs.Permissions
{
    public class PermissionsDetailDTOs
    {
        public int GroupId { get; set; }
        public string? GroupName { get; set; }
        public List<RolesDTOs> Roles { get; set; } = new();
        public List<RolesDTOs> RolesAvailable { get; set; } = new();
    }
}
