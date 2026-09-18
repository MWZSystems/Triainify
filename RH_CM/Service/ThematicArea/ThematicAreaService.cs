using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.OcupationKey;
using RH_CM.Service.DTOs.ThematicArea;
using RH_CM.Service.SQLSMS;
using RH_CM.Messages.ThematicArea;

namespace RH_CM.Service.ThematicArea
{
    public class ThematicAreaService
    {
        private readonly UnitOfWork _unitOfWork;

        public ThematicAreaService(UnitOfWork unitOfWork) 
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<ThematicAreaDTOs>> IndexGet_async()
        {

            List<ThematicAreaDTOs> thematicAreaDTOs = await _unitOfWork.ExecuteStoredProcedureToListAsync<ThematicAreaDTOs>("[sp_ThematicArea_Index_Get]");

            return thematicAreaDTOs;
        }

        public async Task<ServiceAnswer> AddThematicAreaAsync(string ThematicAreaName, int ThematicAreaCode, string UserName)
        {
            ServiceAnswer serviceAnswer = new();


            var parameters = new Dictionary<string, object>
            {
                { "@pAreaName", ThematicAreaName },
                { "@pAreaCode", ThematicAreaCode},
                { "@pUsername", UserName }
            };

            string answer = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_ThematicArea_AddRecord_Post]", parameters);

            if (answer == "Completed")
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Success;
                serviceAnswer.Message = ThematicAreaMessages.RecordSuccessfullyAdded;
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = answer;
            }

            return serviceAnswer;
        }

        //EditOcupationCodeAsync
        public async Task<ThematicAreaDTOs?> GetEditThematicAreaAsync(int id)
        {

            var parameters = new Dictionary<string, object>
            {
                { "@pPKThematicArea", id }
            };

            List<ThematicAreaDTOs> thematicList = await _unitOfWork.ExecuteStoredProcedureToListAsync<ThematicAreaDTOs>("[sp_ThematicArea_EditRecord_Get]", parameters);

            return thematicList.FirstOrDefault();
        }

        public async Task<ServiceAnswer> PostEditThematicAreaAsync(ThematicAreaDTOs Answer, string UserName)
        {

            ServiceAnswer serviceAnswer = new();


            var parameters = new Dictionary<string, object>
            {
                { "@pPK_ThematicArea", Answer.Id },
                { "@pThematicName", Answer.ThematicName ?? string.Empty },
                { "@pThematicCode", Answer.ThematicCode },
                { "@pUserName", UserName }
            };

            string answer = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_ThematicArea_EditRecord_Post]", parameters);

            if (answer == "Completed")
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Success;
                serviceAnswer.Message = ThematicAreaMessages.RecordSuccessfullyUpdated;
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = answer;
            }

            return serviceAnswer;
        }



        public async Task<ServiceAnswer> PostToggleThematicAreaAsync(int id)
        {

            ServiceAnswer serviceAnswer = new();


            var parameters = new Dictionary<string, object>
            {
                { "@pPK_ThematicArea", id }
            };

            string answer = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_ThematicArea_ToggleRecord_Post]", parameters);

            if (answer == "Completed")
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Success;
                serviceAnswer.Message = ThematicAreaMessages.RecordSuccessfullyToggled;
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = answer;
            }

            return serviceAnswer;
        }

        public async Task<ServiceAnswer> PostDeleteThematicAreaAsync(int id)
        {

            ServiceAnswer serviceAnswer = new();


            var parameters = new Dictionary<string, object>
            {
                { "@pPK_ThematicArea", id }
            };

            string answer = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_ThematicArea_DeleteRecord_Post]", parameters);

            if (answer == "Completed")
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Success;
                serviceAnswer.Message = ThematicAreaMessages.RecordSuccessfullyDeleted;
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = answer;
            }

            return serviceAnswer;
        }

    }
}
