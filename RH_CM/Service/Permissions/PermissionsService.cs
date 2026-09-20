using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.Permissions;
using RH_CM.Service.SQLSMS;
using RH_CM.Messages.Permissions;

namespace RH_CM.Service.Permissions
{
    public class PermissionsService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly db_abcd61_rhchdbContext _context;

        public PermissionsService(UnitOfWork unitOfWork,
                                    db_abcd61_rhchdbContext rhchdbContext)
        {
            _unitOfWork = unitOfWork;
            _context = rhchdbContext;
        }

        /// <summary>
        /// One row per permission group, with its controller name and how many roles already
        /// have access — enough to actually understand the list at a glance, instead of the
        /// old plain "id - name" dropdown.
        /// </summary>
        public async Task<List<PermissionGroupSummaryDTOs>> GetGroupsSummaryAsync()
        {
            var groups = await _context.CtPermissionsgroups.AsNoTracking().ToListAsync();
            var roleCountByGroup = await _context.CtPermissions
                .AsNoTracking()
                .GroupBy(p => p.FkPermissionGroup)
                .Select(g => new { GroupId = g.Key, Count = g.Count() })
                .ToListAsync();

            var roleCounts = roleCountByGroup.ToDictionary(x => x.GroupId, x => x.Count);

            return groups
                .OrderBy(g => g.GroupName)
                .Select(g => new PermissionGroupSummaryDTOs
                {
                    GroupId = g.PkPermissionGroup,
                    GroupName = g.GroupName,
                    ControllerName = g.ControllerName,
                    RoleCount = roleCounts.TryGetValue(g.PkPermissionGroup, out var count) ? count : 0
                })
                .ToList();
        }

        /// <summary>
        /// Creates a brand-new permission group (a new CT_PERMISSIONSGROUPS row) for a
        /// controller that doesn't have one yet. No stored procedure covers this — it used to
        /// require a manual DB INSERT. This writes directly (parameterized) through the same
        /// connection the rest of the app already uses; no schema change.
        /// </summary>
        public async Task<ServiceAnswerPermissionsAdd> CreateGroupAsync(string groupName, string? controllerName)
        {
            var serviceAnswer = new ServiceAnswerPermissionsAdd();

            if (string.IsNullOrWhiteSpace(groupName))
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = PermissionsMessages.GroupNameIsRequired;
                return serviceAnswer;
            }

            var trimmedGroupName = groupName.Trim();
            var trimmedControllerName = string.IsNullOrWhiteSpace(controllerName) ? null : controllerName.Trim();

            var parameters = new Dictionary<string, object>
            {
                { "@pGroupName", trimmedGroupName },
                { "@pControllerName", (object?)trimmedControllerName ?? DBNull.Value }
            };

            string newIdRaw = await _unitOfWork.QuerySingleScalarAsync(
                "INSERT INTO CT_PERMISSIONSGROUPS (GroupName, ControllerName) " +
                "OUTPUT INSERTED.PK_PermissionGroup VALUES (@pGroupName, @pControllerName)",
                parameters);

            if (!int.TryParse(newIdRaw, out int newGroupId))
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = PermissionsMessages.GroupCouldNotBeCreated;
                return serviceAnswer;
            }

            serviceAnswer.GroupKey = $"{newGroupId} - {trimmedGroupName}";
            serviceAnswer.MessageType = ServiceAnswer.MessageType_Success;
            serviceAnswer.Message = PermissionsMessages.GroupSuccessfullyCreated;
            return serviceAnswer;
        }

        public async Task<PermissionsDetailDTOs> GetGroupDetailAsync(string GroupKey)
        {
            PermissionsDetailDTOs permissionsDetail = new();
            string[] splitGroupKey = (GroupKey ?? string.Empty).Split(" - ");

            if (splitGroupKey.Length < 2 || !int.TryParse(splitGroupKey[0], out int groupId))
            {
                // Invalid or tampered GroupKey — return an empty detail instead of crashing.
                return permissionsDetail;
            }

            var parameters = new Dictionary<string, object>
            {
                { "@pGroupID", groupId }
            };

            List<RolesDTOs> roles = await _unitOfWork.ExecuteStoredProcedureToListAsync<RolesDTOs>("[sp_Permissions_Detail_Get]", parameters);

            List<RolesDTOs> rolesAvailable = await _unitOfWork.ExecuteStoredProcedureToListAsync<RolesDTOs>("[sp_Permissions_DetailAvailable_Get]", parameters);

            permissionsDetail.Roles = roles;

            permissionsDetail.GroupId = groupId;

            permissionsDetail.GroupName = splitGroupKey[1];

            permissionsDetail.RolesAvailable = rolesAvailable;

            return permissionsDetail;
        }

        public async Task<ServiceAnswerPermissionsAdd> PostAddRoleToGroup(int GroupId, string RoleId)
        {
            ServiceAnswerPermissionsAdd serviceAnswer = new();

            var parameters = new Dictionary<string, object>
            {
                { "@pGroupID", GroupId },
                { "@pRoleID", RoleId }
            };

            string result = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_Permissions_AddRoleToGroup_Post]", parameters);

            var parameters2 = new Dictionary<string, object>
                {
                    { "@pGroupID", GroupId }
                };

            serviceAnswer.GroupKey = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_Permissions_GroupKey_Get]", parameters2);

            if (result == "Completed")
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Success;
                serviceAnswer.Message = PermissionsMessages.RoleSuccessfullyAddedToGroup;
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = PermissionsMessages.RoleNotAddedError;
            }
                return serviceAnswer;
        }

        //PostDeleteRoleFromGroup

        public async Task<ServiceAnswerPermissionsAdd> PostDeleteRoleFromGroup(int GroupId, string RoleId)
        {
            ServiceAnswerPermissionsAdd serviceAnswer = new();

            var parameters = new Dictionary<string, object>
            {
                { "@pGroupID", GroupId },
                { "@pRoleID", RoleId }
            };

            string result = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_Permissions_DeleteRoleFromGroup_Post]", parameters);

            var parameters2 = new Dictionary<string, object>
                {
                    { "@pGroupID", GroupId }
                };

            serviceAnswer.GroupKey = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_Permissions_GroupKey_Get]", parameters2);

            if (result == "Completed")
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Success;
                serviceAnswer.Message = PermissionsMessages.RoleSuccessfullyDeleteFromGroup;
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = PermissionsMessages.RoleCouldNotBeDeletedFromThe;
            }
            return serviceAnswer;
        }

    }
}
