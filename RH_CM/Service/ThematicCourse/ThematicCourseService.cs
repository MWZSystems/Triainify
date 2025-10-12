using DocumentFormat.OpenXml.Office2010.Excel;
using RH_CM.Service.DTOs;
using RH_CM.Service.DTOs.ThematicArea;
using RH_CM.Service.SQLSMS;

namespace RH_CM.Service.ThematicCourse
{
    public class ThematicCourseService
    {
        private readonly UnitOfWork _unitOfWork;
        public ThematicCourseService(UnitOfWork unitOfWork) 
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ThematicCourseDTOs> IndexGet_async()
        {
            ThematicCourseDTOs thematicCourseDTOs = new ThematicCourseDTOs();

            thematicCourseDTOs.ThematicCourses = await _unitOfWork.ExecuteStoredProcedureToListAsync<ThematicCourseList>("[sp_ThematicCourse_Index_Get]");
            thematicCourseDTOs.Courses = await _unitOfWork.ExecuteStoredProcedureToListAsync<Course>("[sp_ThematicCourse_Index_Course_Get]");
            thematicCourseDTOs.Thematics = await _unitOfWork.ExecuteStoredProcedureToListAsync<Thematic>("[sp_ThematicCourse_Index_Thematic_Get]");


            return thematicCourseDTOs;
        }

        public async Task<ServiceAnswer> AddThematicCourseAsync(string ThematicArea, string Course, string UserName)
        {
            ServiceAnswer serviceAnswer = new();

            int pkThematicCode = int.Parse(ThematicArea.Split(" - ")[0]);
            int pkCourse = int.Parse(Course.Split(" - ")[0]);

            var parameters = new Dictionary<string, object>
            {
                { "@pPKThematicCode", pkThematicCode },
                { "@pPkCourse", pkCourse},
                { "@pUsername", UserName }
            };

            string answer = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_ThematicCourse_AddRecord_Post]", parameters);

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
        public async Task<ThematicCourseDTOs> GetEditThematicCourseAsync(int id)
        {

            var parameters = new Dictionary<string, object>
            {
                { "@pPKThematicCourse", id }
            };

            ThematicCourseDTOs thematicCourseDTOs = new();

            thematicCourseDTOs.ThematicCourses = await _unitOfWork.ExecuteStoredProcedureToListAsync<ThematicCourseList>("[sp_ThematicCourse_Edit_Get]", parameters);
            
            thematicCourseDTOs.Courses = await _unitOfWork.ExecuteStoredProcedureToListAsync<Course>("[sp_ThematicCourse_Index_Course_Get]");
            
            thematicCourseDTOs.Thematics = await _unitOfWork.ExecuteStoredProcedureToListAsync<Thematic>("[sp_ThematicCourse_Index_Thematic_Get]");

            return thematicCourseDTOs;
        }

        
        public async Task<ServiceAnswer> PostEditThematicCourseAsync(int id, string ThematicArea,string Course, string UserName)
        {

            ServiceAnswer serviceAnswer = new();

            int thematicCode = int.Parse(ThematicArea.Split(" - ")[0]);
            int pkCourse = int.Parse(Course.Split(" - ")[0]);

            var parameters = new Dictionary<string, object>
            {
                { "@pPK_ThematicCourse", id },
                { "@pThematicCode", thematicCode },
                { "@pPkCourse", pkCourse },
                { "@pUserName", UserName }
            };

            string answer = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_ThematicCourse_EditRecord_Post]", parameters);

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


        //PostToggleThematicCourseAsync
        public async Task<ServiceAnswer> PostToggleThematicCourseAsync(int id)
        {

            ServiceAnswer serviceAnswer = new();


            var parameters = new Dictionary<string, object>
            {
                { "@pPK_ThematicCourse", id }
            };

            string answer = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_ThematicCourse_ToggleRecord_Post]", parameters);

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

        public async Task<ServiceAnswer> PostDeleteThematicCourseAsync(int id)
        {

            ServiceAnswer serviceAnswer = new();


            var parameters = new Dictionary<string, object>
            {
                { "@pPK_ThematicCourse", id }
            };

            string answer = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[sp_ThematicCourse_DeleteRecord_Post]", parameters);

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
