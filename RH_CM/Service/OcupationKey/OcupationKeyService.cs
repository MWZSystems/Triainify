using DocumentFormat.OpenXml.Office2010.Excel;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.OcupationKey;
using RH_CM.Service.SQLSMS;
using RH_CM.Messages.OcupationKey;

namespace RH_CM.Service.OcupationKey
{
    public class OcupationKeyService
    {
        private readonly UnitOfWork _unitOfWork;
        public OcupationKeyService(UnitOfWork unitOfWork) 
        { 
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gets the list of occupation codes for the CRUD table.
        /// </summary>
        public async Task<OcupationDTOs> IndexGet_async()
        {
            OcupationDTOs ocupationDTOs = new OcupationDTOs();


            ocupationDTOs.OcupationsLists = await _unitOfWork.ExecuteStoredProcedureToListAsync<OcupationsList>("[sp_OcupationKey_Index_Get]");

            ocupationDTOs.PositionsList = await _unitOfWork.ExecuteStoredProcedureToListAsync<Positions>("[sp_OcupationKey_Index_Positions_Get]");


            return ocupationDTOs;
        }

        public async Task<ServiceAnswer> AddOcupationCodeAsync(string Position, int OcupationCode, string UserName)
        {
            ServiceAnswer serviceAnswer = new();

            string[] splitPosition = (Position ?? string.Empty).Split(" - ");
            if (splitPosition.Length < 1 || !int.TryParse(splitPosition[0], out int PositionID))
            {
                serviceAnswer.MessageType = "ErrorMessage";
                serviceAnswer.Message = OcupationKeyMessages.SelectAValidPosition;
                return serviceAnswer;
            }

            var parameters = new Dictionary<string, object>
            {
                { "@pPositionID", PositionID },
                { "@pOcupationCode", OcupationCode},
                { "@pUsername", UserName }
            };

            string answer = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_OcupationKey_AddRecord_Post]", parameters);

            if (answer == "Completed")
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Success;
                serviceAnswer.Message = OcupationKeyMessages.RecordSuccessfullyAdded;
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = answer;
            }

                return serviceAnswer;
        }

        //EditOcupationCodeAsync
        public async Task<OcupationsList?> GetEditOcupationCodeAsync(int id)
        {

            var parameters = new Dictionary<string, object>
            {
                { "@pPK_OcupationCode", id }
            };

            List<OcupationsList> ocupationsList = await _unitOfWork.ExecuteStoredProcedureToListAsync<OcupationsList>("[sp_OcupationKey_EditRecord_Get]", parameters);

            return ocupationsList.FirstOrDefault();
        }

        public async Task<ServiceAnswer> PostEditOcupationCodeAsync(OcupationsList Answer, string UserName)
        {

            ServiceAnswer serviceAnswer = new();


            var parameters = new Dictionary<string, object>
            {
                { "@pPK_OcupationCode", Answer.Id },
                { "@pOcupationCode", Answer.OcupationCode },
                { "@pUserName", UserName }
            };

            string answer = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_OcupationKey_EditRecord_Post]", parameters);

            if (answer == "Completed")
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Success;
                serviceAnswer.Message = OcupationKeyMessages.RecordSuccessfullyUpdated;
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = answer;
            }

            return serviceAnswer;
        }



        public async Task<ServiceAnswer> PostToggleOcupationCodeAsync(int id)
        {

            ServiceAnswer serviceAnswer = new();


            var parameters = new Dictionary<string, object>
            {
                { "@pPK_OcupationCode", id }
            };

            string answer = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_OcupationKey_ToggleRecord_Post]", parameters);

            if (answer == "Completed")
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Success;
                serviceAnswer.Message = OcupationKeyMessages.RecordSuccessfullyToggled;
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = answer;
            }

            return serviceAnswer;
        }

        public async Task<ServiceAnswer> PostDeleteOcupationCodeAsync(int id)
        {

            ServiceAnswer serviceAnswer = new();


            var parameters = new Dictionary<string, object>
            {
                { "@pPK_OcupationCode", id }
            };

            string answer = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_OcupationKey_DeleteRecord_Post]", parameters);

            if (answer == "Completed")
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Success;
                serviceAnswer.Message = OcupationKeyMessages.RecordSuccessfullyDeleted;
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
