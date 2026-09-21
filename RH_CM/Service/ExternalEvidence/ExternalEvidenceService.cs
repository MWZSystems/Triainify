using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.DTOs;
using RH_CM.Service.SQLSMS;
using System.Linq;
using RH_CM.Messages.ExternalEvidence;

namespace RH_CM.Service.ExternalEvidence
{
    
    public class ExternalEvidenceService
    {
        // Field that lives for the lifetime of the class instance
        private readonly UnitOfWork _unitOfWork;
        private readonly db_abcd61_rhchdbContext _context;
                                         
        // Instantiated in the constructor
        public ExternalEvidenceService(UnitOfWork unitOfWork, db_abcd61_rhchdbContext context)
        {
            // Dependency injection
            _unitOfWork = unitOfWork;
            _context = context;
        }

        /// <summary>
        /// Gets the external evidence records for the index table.
        /// </summary>
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
        /// Gets the dropdown options (users, courses, levels) for the create-evidence form.
        /// </summary>
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
            ServiceAnswerAndFeedbackDTOs invalidRequest = new();

            string[] splitCourse = (DTOs.SelectedCourse ?? string.Empty).Split(" - ");
            if (splitCourse.Length < 2 || !int.TryParse(splitCourse[0], out int courseID))
            {
                invalidRequest.ServiceAnswer = new(false, "ErrorMessage", ExternalEvidenceMessages.SelectAValidCourse);
                return invalidRequest;
            }
            string courseName = splitCourse[1];

            string query = "SELECT [PK_LEVELCOURSE] FROM [CT_LEVELCOURSE] WHERE [DESCRIPCTION_LEVEL] = @pLevel";
            string levelResult = await _unitOfWork.QuerySingleScalarAsync(query, new Dictionary<string, object>
            {
                { "@pLevel", DTOs.SelectedLevel ?? string.Empty }
            });

            if (!int.TryParse(levelResult, out int level))
            {
                invalidRequest.ServiceAnswer = new(false, "ErrorMessage", ExternalEvidenceMessages.SelectAValidLevel);
                return invalidRequest;
            }

            byte[] fileBytes;
            int controlNumber;
            string positionName;
            bool feedBackRequired = false;
            string fullName;
            List<ExternalEvidenceFeedbackItemDTOs> feedbackItems = new();

            ServiceAnswerAndFeedbackDTOs serviceAnswerAndFeedback = new();

            if (DTOs.UploadedFile == null || !PDFValidation(DTOs.UploadedFile))
            {
                serviceAnswerAndFeedback.ServiceAnswer = new(false, "ErrorMessage", ExternalEvidenceMessages.FileEmptyOrPdfFormatNotCorrect);
                return serviceAnswerAndFeedback;
            }

            string evidenceFileName = DTOs.UploadedFile.FileName;

            //Save file in Binary
            using (var memoryStream = new MemoryStream())
            {
                DTOs.UploadedFile.CopyTo(memoryStream);
                fileBytes = memoryStream.ToArray();
            }

            // Validates that this level actually exists in the CourseAssignment; otherwise, error to the user
            // (not every course has all 4 levels, hence this validation)
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
                serviceAnswerAndFeedback.ServiceAnswer = new(false, "ErrorMessage", string.Format(ExternalEvidenceMessages.CourseDoesNotHaveExternalLevelFormat, courseName, DTOs.SelectedLevel));
                return serviceAnswerAndFeedback;
            }

            foreach (var line in DTOs.SelectedUsers ?? Enumerable.Empty<string>())
            {
                //Gets employee Data.
                string[] employeeData = (line ?? string.Empty).Split(" - ");
                if (employeeData.Length < 3 || !int.TryParse(employeeData[0], out controlNumber))
                {
                    feedBackRequired = true;
                    continue;
                }
                fullName = employeeData[1];
                positionName = employeeData[2];

                //Revisa si es internal, External, o si el Position-Course no existe en absoluto
                string AssignmentExistsQuery = @"         DECLARE  @pResultado VARCHAR(20)
                                                        SET @pResultado =
                                                        (SELECT CASE
                                                                    WHEN A.FK_DeliveryMode = 1 THEN 'Internal'
                                                                    WHEN A.FK_DeliveryMode = 2 THEN 'External'
                                                                     END AS Answer

                                                         FROM [dbo].[CT_COURSEASSIGNMENTS] A
                                                            LEFT JOIN dbo.CT_POSITION B ON A.FK_Position = B.PK_POSITION
                                                         WHERE A.FK_Course = @pCourseId
                                                         AND A.FK_RequiredCourseLevels = @pLevel
                                                         AND B.NAME_POSITION = @pPositionName
                                                         )

                                                         SELECT COALESCE(@pResultado, 'Empty') As resultado";

                string AssignmentExists = await _unitOfWork.QuerySingleScalarAsync(AssignmentExistsQuery, new Dictionary<string, object>
                {
                    { "@pCourseId", courseID },
                    { "@pLevel", level },
                    { "@pPositionName", positionName }
                });

                if (AssignmentExists == "Internal")
                {
                    feedbackItems.Add(new ExternalEvidenceFeedbackItemDTOs
                    {
                        ControlNumber = controlNumber,
                        FullName = fullName,
                        PositionName = positionName,
                        CourseID = courseID,
                        CourseName = courseName,
                        Level = DTOs.SelectedLevel ?? string.Empty,
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
                        Level = DTOs.SelectedLevel ?? string.Empty,
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
                    { "@pScore", DTOs.Score ?? (object)DBNull.Value },
                    { "@pPositionName", positionName },
                    { "@pCourseID", courseID },
                    { "@pLevel", level },
                    { "@pUser", DTOs.UserName ?? string.Empty },
                    { "@pEvidenceFile", fileBytes },
                    { "@pEvidenceFileName", evidenceFileName }
                };

                string result = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[dbo].[sp_ExternalEvidence_Create_Post]", parameters);

                if (result != "Completed")
                {
                    // The stored procedure prefixes a caught SQL error with "Failed: " (see its
                    // CATCH block) so a real reason reaches the user instead of a dead end.
                    const string FailedPrefix = "Failed: ";
                    string detail = result.StartsWith(FailedPrefix)
                        ? $"This record could not be added: {result[FailedPrefix.Length..]}"
                        : "This record could not be added. Please review it in detail.";

                    feedbackItems.Add(new ExternalEvidenceFeedbackItemDTOs
                    {
                        ControlNumber = controlNumber,
                        FullName = fullName,
                        PositionName = positionName,
                        CourseID = courseID,
                        CourseName = courseName,
                        Level = DTOs.SelectedLevel ?? string.Empty,
                        EvidenceFileName = "",
                        Success = false,
                        FeedBackComment = detail
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
                    Level = DTOs.SelectedLevel ?? string.Empty,
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
                serviceAnswerAndFeedback.ServiceAnswer = new(true, "SuccessMessage", ExternalEvidenceMessages.EvidenceAddedToEverySelectedRecord);
            }

            return serviceAnswerAndFeedback;
        }

        /// <summary>
        /// Gets an evidence file's bytes, optionally with its file name (<paramref name="choose"/> = 1).
        /// </summary>
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

            string currentValue = await _unitOfWork.QuerySingleScalarAsync(FirstQuery);
            if (!int.TryParse(currentValue, out int status))
            {
                serviceAnswer.Success = false;
                serviceAnswer.MessageType = "ErrorMessage";
                serviceAnswer.Message = ExternalEvidenceMessages.RecordNotFound;
                return serviceAnswer;
            }

            // Flip the current status for the toggle
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
                serviceAnswer.Message = ExternalEvidenceMessages.RecordToggledSuccessfully;
            }
            else
            {
                serviceAnswer.Success = true;
                serviceAnswer.MessageType = "ErrorMessage";
                serviceAnswer.Message = ExternalEvidenceMessages.ToggleFailed;
            }

                return serviceAnswer;
        }

        public async Task<EditExternalEvidenceDTOs?> GetUpdateRecordAsync(int? id)
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
                serviceAnswer.Message = ExternalEvidenceMessages.FileEmptyOrPdfFormatNotCorrect;
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
                    { "@pPK_ExternalEvidence", PK_ExternalEvidence ?? (object)DBNull.Value },
                    { "@pFile", fileBinary },
                    { "@pFileName", fileNamePDF },
                    { "@pUserName", userName ?? string.Empty }
                };

            string result = await _unitOfWork.ExecuteStoredProcedureScalarAsync("[dbo].[sp_ExternalEvidence_Edit_Post]", parameters);

            if (result != "Completed")
            {
                serviceAnswer.MessageType = "ErrorMessage";
                serviceAnswer.Message = ExternalEvidenceMessages.RecordCouldNotBeUpdatedPleaseTry;
                return serviceAnswer;
            }

            serviceAnswer.MessageType = "SuccessMessage";
            serviceAnswer.Message = ExternalEvidenceMessages.EvidenceWasUpdatedSuccessfully;

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
                serviceAnswer.Message = string.Format(ExternalEvidenceMessages.RecordSuccessfullyDeletedFormat, id);
            }
            else
            {
                serviceAnswer.MessageType = ServiceAnswer.MessageType_Error;
                serviceAnswer.Message = ExternalEvidenceMessages.RecordCouldNotBeDeleted;
            }

                return serviceAnswer;
        }

        /// <summary>
        /// Validates that the file is not empty and has a .pdf extension.
        /// </summary>
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
