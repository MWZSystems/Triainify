using RH_CM.Data;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.Permissions;
using RH_CM.Service.SQLSMS;

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

        public async Task<List<PermissionsIndexDTOs>> GetIndexAsync()
        {
            List<PermissionsIndexDTOs> groups = await _unitOfWork.ExecuteStoredProcedureToListAsync<PermissionsIndexDTOs>("[sp_Permissions_Index_Get]");

            return groups;
        }

        public async Task<PermissionsDetailDTOs> GetGroupDetailAsync(string GroupKey)
        {
            PermissionsDetailDTOs permissionsDetail = new();
            string[] splitGroupKey = GroupKey.Split(" - ");

            var parameters = new Dictionary<string, object>
            {
                { "@pGroupID", int.Parse(splitGroupKey[0]) }
            };

            List<RolesDTOs> roles = await _unitOfWork.ExecuteStoredProcedureToListAsync<RolesDTOs>("[sp_Permissions_Detail_Get]", parameters);

            List<RolesDTOs> rolesAvailable = await _unitOfWork.ExecuteStoredProcedureToListAsync<RolesDTOs>("[sp_Permissions_DetailAvailable_Get]", parameters);

            permissionsDetail.Roles = roles;

            permissionsDetail.GroupId = int.Parse(splitGroupKey[0]);

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
                serviceAnswer.Message = "Role successfully added to Group!";
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = "Role not added. Error";
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
                serviceAnswer.Message = "Role successfully Delete from Group!";
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = "Role could not be deleted from the group. Error";
            }
            return serviceAnswer;
        }

    }
}
