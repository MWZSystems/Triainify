using DocumentFormat.OpenXml.Office2010.Excel;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.OcupationKey;
using RH_CM.Service.SQLSMS;

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
        /// Gets the List of objets to display the crud table
        /// </summary>
        /// <returns></returns>
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

            int PositionID = int.Parse(Position.Split(" - ")[0]);


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
                serviceAnswer.Message = "Record Successfully Added!";
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = answer;
            }

                return serviceAnswer;
        }

        //EditOcupationCodeAsync
        public async Task<OcupationsList> GetEditOcupationCodeAsync(int id)
        {

            var parameters = new Dictionary<string, object>
            {
                { "@pPK_OcupationCode", id }
            };

            List<OcupationsList> ocupationsList = await _unitOfWork.ExecuteStoredProcedureToListAsync<OcupationsList>("[sp_OcupationKey_EditRecord_Get]", parameters);


            OcupationsList ocupation = ocupationsList[0];


            return ocupation;
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
                serviceAnswer.Message = "Record Successfully Updated!";
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
                serviceAnswer.Message = "Record Successfully Toggled!";
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
                serviceAnswer.Message = "Record Successfully Deleted!";
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
