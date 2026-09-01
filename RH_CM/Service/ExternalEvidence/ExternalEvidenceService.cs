using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.DTOs;
using RH_CM.Service.SQLSMS;
using System.Linq;

namespace RH_CM.Service.ExternalEvidence
{
    
    public class ExternalEvidenceService
    {
        //Variable que vive durante la ejecucion de la clase
        private readonly UnitOfWork _unitOfWork;
        private readonly db_abcd61_rhchdbContext _context;
                                         
        //Instancia en el constructor
        public ExternalEvidenceService(UnitOfWork unitOfWork, db_abcd61_rhchdbContext context)
        {
            //Inyeccion de dependencia
            _unitOfWork = unitOfWork;
            _context = context;
        }

        /// <summary>
        /// Gets Data for index in object List<GetExternalEvidenceDTOs>
        /// </summary>
        /// <returns></returns>
        public async Task<List<ExternalEvidenceDTOs>> GetIndexAsync()
        {
            //Gets data for preliminary Crud Table

            string sql = @"
                                SELECT EE.PK_ExternalEvidence AS ID
                                ,HC.NAMES + ' ' + HC.LAST_NAME + ' ' + HC.SECOND_NAME AS FullName
                                ,C.CourseName
                                ,LC.DESCRIPCTION_LEVEL AS LevelName
                                ,EE.Score
                                ,EE.CreateDate
                                ,CASE WHEN EE.Available = 1 THEN 'Enabled' ELSE 'Disabled' END AS [Status]
                            FROM [dbo].[SY_EXTERNALEVIDENCE] EE
                            LEFT JOIN [dbo].[SY_COURSEMOVEMENTS] CM ON EE.FK_MovementCourse = CM.PK_MovementCourse
                            LEFT JOIN dbo.SY_HEADCOUNT HC ON HC.PK_HEADCOUNT = CM.FK_Headcount
                            LEFT JOIN dbo.CT_COURSEASSIGNMENTS CA ON CA.PK_CourseAssignment = CM.FK_CourseAssignment
                            LEFT JOIN dbo.CT_COURSE C ON C.PK_Course = CA.FK_Course
                            LEFT JOIN dbo.CT_LEVELCOURSE LC ON LC.PK_LEVELCOURSE = CA.FK_RequiredCourseLevels";

            List<ExternalEvidenceDTOs> result = await _unitOfWork.QueryListAsync<ExternalEvidenceDTOs>(sql);


            return result;

        }


        /// <summary>
        /// Returns from DB the List<object> needed for the view
        /// </summary>
        /// <returns></returns>
        public async Task<CreateExternalEvidenceDTOs> GetCreateExternalEvidenceAsync()
        {
            //Gets data for Combobox in CreateExternalEvidence

            CreateExternalEvidenceDTOs DTOs = new();

            string query1 = @"

                            SELECT DISTINCT CAST(A.CONTROL_NUMBER AS VARCHAR(10)) + ' - ' + 
                                            CAST(A.NAMES AS VARCHAR(50)) + ' ' + 
                                            CAST(A.LAST_NAME AS VARCHAR(50)) + ' ' + 
                                            CAST(A.SECOND_NAME AS VARCHAR(50)) + ' - ' + 
                                            B.NAME_POSITION  AS [User]

                            FROM dbo.SY_HEADCOUNT A
                                LEFT JOIN dbo.CT_POSITION B ON A.FK_POSITION = B.PK_POSITION
                               WHERE A.AVAILABLE = 1";

            DTOs.User = await _unitOfWork.QuerySingleColumnAsync<string>(query1);

            string query2 = @"
                                SELECT DISTINCT 
                                        CAST(A.PK_Course AS VARCHAR(10)) + ' - ' + A.CourseName As Course
                                FROM dbo.CT_COURSE A
                                    LEFT JOIN CT_COURSEASSIGNMENTS B ON A.PK_Course = B.FK_Course
                                WHERe A.Available = 1
                                --AND B.FK_DeliveryMode = 2 -- Comentado mientras emmanuel carga externos";

            DTOs.Course = await _unitOfWork.QuerySingleColumnAsync<string>(query2);



            string query3 = @"SELECT DESCRIPCTION_LEVEL
                                FROM [dbo].[CT_LEVELCOURSE] ";

            DTOs.Level = await _unitOfWork.QuerySingleColumnAsync<string>(query3);

            return DTOs;
        }


        public async Task<ServiceAnswerAndFeedbackDTOs> PostCreateExternalEvidenceAsync(CreateExternalEvidenceInputDTOs DTOs)
        {
            string[] splitCourse = DTOs.SelectedCourse.Split(" - ");
            int courseID = int.Parse(splitCourse[0]);
            string courseName = splitCourse[1];

            string query = $"SELECT [PK_LEVELCOURSE] FROM [CT_LEVELCOURSE] WHERE [DESCRIPCTION_LEVEL] = '{DTOs.SelectedLevel}' ";

            int level = int.Parse(await _unitOfWork.QuerySingleScalarAsync(query));

            byte[] fileBytes;
            int controlNumber;
            string positionName;
            bool feedBackRequired = false;
            string fullName;
            string evidenceFileName = DTOs.UploadedFile.FileName;
            List<ExternalEvidenceFeedbackItemDTOs> feedbackItems = new();

            ServiceAnswerAndFeedbackDTOs serviceAnswerAndFeedback = new();

            if (!PDFValidation(DTOs.UploadedFile))
            {
                serviceAnswerAndFeedback.ServiceAnswer = new(false, "ErrorMessage", "File Empty or .PDF format not correct");
                return serviceAnswerAndFeedback;
            }

            //Save file in Binary
            using (var memoryStream = new MemoryStream())
            {
                DTOs.UploadedFile.CopyTo(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            //Valida que si exista ese nivel en el CourseAssignment Si no, error al usuario
            //(No todos los cursos tienen los 4 niveles, para eso esta validacion)
            string LevelExistsQuery = @$" IF EXISTS (SELECT 1
                                                        FROM [dbo].[CT_COURSEASSIGNMENTS]
                                                        WHERE FK_Course = {courseID}
                                                            AND FK_RequiredCourseLevels = {level}
                                                            AND FK_DeliveryMode = 2
                                                            )
                                            BEGIN
                                                SELECT 'OK' as result
                                            END
                                         ELSE
                                            BEGIN
                                                SELECT 'Empty' as result
                                            END";

            string LevelExists = await _unitOfWork.QuerySingleScalarAsync(LevelExistsQuery);

            if (LevelExists != "OK")
            {
                serviceAnswerAndFeedback.ServiceAnswer = new(false, "ErrorMessage", $"The course '{courseName}' does not have a '{DTOs.SelectedLevel}' level with External delivery type in Course Assignments.");
                return serviceAnswerAndFeedback;
            }

            foreach (var line in DTOs.SelectedUsers)
            {
                //Gets employee Data.
                string[] employeeData = line.Split(" - ");
                controlNumber = int.Parse(employeeData[0]);
                fullName = employeeData[1];
                positionName = employeeData[2];

                //Revisa si es internal, External, o si el Position-Course no existe en absoluto
                string AssignmentExistsQuery = @$"         DECLARE  @pResultado VARCHAR(20)
                                                        SET @pResultado =
                                                        (SELECT CASE
                                                                    WHEN A.FK_DeliveryMode = 1 THEN 'Internal'
                                                                    WHEN A.FK_DeliveryMode = 2 THEN 'External'
                                                                     END AS Answer

                                                         FROM [dbo].[CT_COURSEASSIGNMENTS] A
                                                            LEFT JOIN dbo.CT_POSITION B ON A.FK_Position = B.PK_POSITION
                                                         WHERE A.FK_Course = {courseID}
                                                         AND A.FK_RequiredCourseLevels = {level}
                                                         AND B.NAME_POSITION = '{positionName}'
                                                         )

                                                         SELECT COALESCE(@pResultado, 'Empty') As resultado";

                string AssignmentExists = await _unitOfWork.QuerySingleScalarAsync(AssignmentExistsQuery);

                if (AssignmentExists == "Internal")
                {
                    feedbackItems.Add(new ExternalEvidenceFeedbackItemDTOs
                    {
                        ControlNumber = controlNumber,
                        FullName = fullName,
                        PositionName = positionName,
                        CourseID = courseID,
                        CourseName = courseName,
                        Level = DTOs.SelectedLevel,
                        EvidenceFileName = "",
                        Success = false,
                        FeedBackComment = $"The position '{positionName}' has course '{courseName}' (level '{DTOs.SelectedLevel}') assigned as INTERNAL, not External, in Course Assignments. The evidence was not added."
                    });
                    feedBackRequired = true;
                    continue;
                }
                else if (AssignmentExists == "Empty")
                {
                    feedbackItems.Add(new ExternalEvidenceFeedbackItemDTOs
                    {
                        ControlNumber = controlNumber,
                        FullName = fullName,
                        PositionName = positionName,
                        CourseID = courseID,
                        CourseName = courseName,
                        Level = DTOs.SelectedLevel,
                        EvidenceFileName = "",
                        Success = false,
                        FeedBackComment = $"The position '{positionName}' does not have course '{courseName}' assigned at level '{DTOs.SelectedLevel}' in Course Assignments. The evidence was not added."
                    });
                    feedBackRequired = true;
                    continue;
                }

                var parameters = new Dictionary<string, object>
                {
                    { "@pControlNumber", controlNumber },
                    { "@pScore", DTOs.Score },
                    { "@pPositionName", positionName },
                    { "@pCourseID", courseID },
                    { "@pLevel", level },
                    { "@pUser", DTOs.UserName },
                    { "@pEvidenceFile", fileBytes },
                    { "@pEvidenceFileName", evidenceFileName }
                };

                string result = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[dbo].[sp_ExternalEvidence_Create_Post]", parameters);

                if (result != "Completed")
                {
                    feedbackItems.Add(new ExternalEvidenceFeedbackItemDTOs
                    {
                        ControlNumber = controlNumber,
                        FullName = fullName,
                        PositionName = positionName,
                        CourseID = courseID,
                        CourseName = courseName,
                        Level = DTOs.SelectedLevel,
                        EvidenceFileName = "",
                        Success = false,
                        FeedBackComment = "This record could not be added. Please review it in detail."
                    });
                    feedBackRequired = true;
                    continue;
                }

                feedbackItems.Add(new ExternalEvidenceFeedbackItemDTOs
                {
                    ControlNumber = controlNumber,
                    FullName = fullName,
                    PositionName = positionName,
                    CourseID = courseID,
                    CourseName = courseName,
                    Level = DTOs.SelectedLevel,
                    EvidenceFileName = evidenceFileName,
                    Success = true,
                    FeedBackComment = "This record was successfully added with the evidence provided."
                });
            }

            if (feedBackRequired)
            {
                int errorCount = feedbackItems.Count(f => !f.Success);
                int totalCount = feedbackItems.Count;

                serviceAnswerAndFeedback.ServiceAnswer = new(
                    false,
                    "ErrorMessage",
                    $"{errorCount} of {totalCount} selected record(s) could not be assigned. Review the detail below.");
                serviceAnswerAndFeedback.FeedbackItems = feedbackItems;
            }
            else
            {
                serviceAnswerAndFeedback.ServiceAnswer = new(true, "SuccessMessage", "The evidence was successfully added to every selected record.");
            }

            return serviceAnswerAndFeedback;
        }

        /// <summary>
        /// Gets Material byte[] after receiving the ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="choose"> 0 -> For only byte[] | 1 -> For File name in ServiceAnswer.Message </param>
        /// <returns></returns>
        public async Task<ServiceAnswerAndFeedbackDTOs> GetEvidenceMaterialAsync(int id, int choose)
        {

            ServiceAnswerAndFeedbackDTOs serviceAnswerAndFeedbackDTOs = new ServiceAnswerAndFeedbackDTOs();
            string Query = @$"
                                SELECT EvidenceFile
                                FROM [dbo].[SY_EXTERNALEVIDENCE]
                                WHERE PK_ExternalEvidence = {id}";

            byte[] result = await _unitOfWork.QuerySingleBinaryAsync(Query);

            serviceAnswerAndFeedbackDTOs.FeedbackFile = result;

            if (choose == 1)
            {
                string Query2 = $@"SELECT REPLACE(EvidenceFileName,'.pdf','')
                                FROM [dbo].[SY_EXTERNALEVIDENCE]
                                WHERE PK_ExternalEvidence = {id}";

                string FileName = await _unitOfWork.QuerySingleScalarAsync(Query2);

                ServiceAnswer serviceAnswer = new(false, FileName, FileName);

                serviceAnswerAndFeedbackDTOs.ServiceAnswer = serviceAnswer;
            }

            return serviceAnswerAndFeedbackDTOs;

        }


        public async Task<ServiceAnswer> ToggleRecord(int id)
        {
            ServiceAnswer serviceAnswer = new();

            string FirstQuery = $@"SELECT Available
                                    FROM [dbo].[SY_EXTERNALEVIDENCE]
                                    WHERE PK_ExternalEvidence = {id}";

            int status = int.Parse(await _unitOfWork.QuerySingleScalarAsync(FirstQuery));

            //Invertimos el estatus actual para el toggle
            if (status == 0)
            {
                status = 1;
            }
            else
            {
                status = 0;
            }


                string Query = $@"BEGIN TRY
                                UPDATE [dbo].[SY_EXTERNALEVIDENCE]
                                SET Available = {status}
                                WHERE PK_ExternalEvidence = {id}
                                SELECT 'Completed' as result
                            END TRY
                            BEGIN CATCH
                                SELECT 'Error' as result
                            END CATCH";

            string result = await _unitOfWork.QuerySingleScalarAsync(Query);

            if (result == "Completed")
            {
                serviceAnswer.Success = true;
                serviceAnswer.MessageType = "SuccessMessage";
                serviceAnswer.Message = "Record Toggled successfully";
            }
            else
            {
                serviceAnswer.Success = true;
                serviceAnswer.MessageType = "ErrorMessage";
                serviceAnswer.Message = "Toggle Failed";
            }

                return serviceAnswer;
        }

        public async Task<EditExternalEvidenceDTOs> GetUpdateRecordAsync(int? id)
        {
            List<EditExternalEvidenceDTOs> result = new();


            string query = $@"   SELECT EV.PK_ExternalEvidence
		                                ,HC.CONTROL_NUMBER As ControlNumber
		                                ,HC.NAMES + ' ' + HC.LAST_NAME + ' ' + HC.SECOND_NAME AS FullName
		                                ,PO.NAME_POSITION As NamePosition
		                                ,CO.CourseName
		                                ,LC.DESCRIPCTION_LEVEL AS [Level]
		                                ,EV.EvidenceFile
		                                ,EV.Score

                                    FROM [dbo].[SY_EXTERNALEVIDENCE] EV
		                                LEFT JOIN dbo.SY_COURSEMOVEMENTS CM ON EV.FK_MovementCourse = CM.PK_MovementCourse
		                                LEFT JOIN dbo.SY_HEADCOUNT HC ON CM.FK_Headcount = HC.PK_HEADCOUNT
		                                LEFT JOIN dbo.CT_POSITION PO ON PO.PK_POSITION = HC.FK_POSITION
		                                LEFT JOIN dbo.CT_COURSEASSIGNMENTS CA ON Ca.PK_CourseAssignment = CM.FK_CourseAssignment
		                                LEFT JOIN dbo.CT_COURSE CO ON CO.PK_Course = CA.FK_Course
		                                LEFT JOIN [dbo].[CT_LEVELCOURSE] LC ON LC.PK_LEVELCOURSE = CA.FK_RequiredCourseLevels
	                                WHERE EV.PK_ExternalEvidence = {id}";

            result = await _unitOfWork.QueryListAsync<EditExternalEvidenceDTOs>(query);


            //PK evidence
            //Pdf
            //Score

            return result.FirstOrDefault();
        }

        public async Task<ServiceAnswer> PostUpdateRecordAsync(EditExternalEvidenceDTOs DTOs, IFormFile file)
        {
            ServiceAnswer serviceAnswer = new();
            int? PK_ExternalEvidence = DTOs.PK_ExternalEvidence;
            string? userName = DTOs.UserName;

            // Same check the Create flow uses (null/empty/extension), applied here too for
            // defense in depth — the controller already validates the file before calling this,
            // but the service should not trust its caller blindly.
            if (!PDFValidation(file))
            {
                serviceAnswer.MessageType = "ErrorMessage";
                serviceAnswer.Message = "File Empty or .PDF format not correct";
                return serviceAnswer;
            }

            byte[] fileBinary;
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                fileBinary = memoryStream.ToArray();
            }

            string fileNamePDF = file.FileName;

            var parameters = new Dictionary<string, object>
                {
                    { "@pPK_ExternalEvidence", PK_ExternalEvidence },
                    { "@pFile", fileBinary },
                    { "@pFileName", fileNamePDF },
                    { "@pUserName", userName }
                };

            string result = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[dbo].[sp_ExternalEvidence_Edit_Post]", parameters);

            if (result != "Completed")
            {
                serviceAnswer.MessageType = "ErrorMessage";
                serviceAnswer.Message = "The record could not be updated. Please try again or contact support.";
                return serviceAnswer;
            }

            serviceAnswer.MessageType = "SuccessMessage";
            serviceAnswer.Message = "The evidence was updated successfully.";

            return serviceAnswer;
        }

        public async Task<ServiceAnswer> PostDeleteRecordAsync(int? id)
        {
            ServiceAnswer serviceAnswer = new();

            string Query = @$"EXECUTE [dbo].[sp_ExternalEvidence_Delete_Post] {id}";

            string answer = await _unitOfWork.QuerySingleScalarAsync(Query);

            if (answer == "Completed")
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Success;
                serviceAnswer.Message = $"Record {id} Successfully Deleted";
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = $"Record could not be deleted";
            }

                return serviceAnswer;
        }

        /// <summary>
        /// Validates IFormFile not empty. And 'PDF' Extension
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private bool PDFValidation(IFormFile file)
        {
            //Validations
            if (file == null || file.Length == 0)
            {
                return false;
            }

            var fileName = file.FileName?.ToLowerInvariant() ?? "";

            if (!fileName.EndsWith(".pdf"))
            {
                return false;
            }
            return true;
        }

        private int ReturnLevelID(string levelDescription)
        {
            int idResult = 0;

            switch (levelDescription)
            {
                case "Introducción":
                    idResult = 1;
                    break;

                case "Básico":
                    idResult = 2;
                    break;

                case "Intermedio":
                    idResult = 3;
                    break;

                case "Avanzado":
                    idResult = 4;
                    break;
            }

            return idResult;
        }

    }
}
