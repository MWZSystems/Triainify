using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.DTOs;
using RH_CM.Service.SQLSMS;
using System.Collections.Generic;
using System.Data;

namespace RH_CM.Service.ExternalEvidence
{
    
    public class ExternalEvidenceService
    {
        //Variable que vive durante la ejecucion de la clase
        private readonly db_abcd61_rhchdbContext _context;
        private readonly UnitOfWork _unitOfWork;
                                         
        //Instancia en el constructor
        public ExternalEvidenceService(db_abcd61_rhchdbContext context,
                                        UnitOfWork unitOfWork)
        {
            //Inyeccion de dependencia
            _context = context;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gets Data for index in object List<GetExternalEvidenceDTOs>
        /// </summary>
        /// <returns></returns>
        public async Task<List<ExternalEvidenceDTOs>> GetIndex()
        {
            //Gets data for preliminary Crud Table

            string sql = @"
                              SELECT EE.PK_ExternalEvidence AS ID
                                ,HC.NAMES + HC.LAST_NAME + HC.SECOND_NAME AS FullName
                                ,C.CourseName
                                ,LC.DESCRIPCTION_LEVEL AS LevelName
                                ,EE.Score
                                ,CASE WHEN EE.Available = 1 THEN 'Enabled' ELSE 'Disabled' END AS [Status]

                          FROM [dbo].[SY_EXTERNALEVIDENCE] EE
                            LEFT JOIN [dbo].[SY_COURSEMOVEMENTS] CM ON EE.FK_MovementCourse = CM.PK_MovementCourse
                            LEFT JOIN [dbo].[SY_COURSECOMPLETED] CC ON CC.PK_CourseCompleted = CM.FK_CourseCompleted
                            LEFT JOIN dbo.SY_HEADCOUNT HC ON HC.PK_HEADCOUNT = CC.FK_Headcount
                            LEFT JOIN dbo.CT_COURSEASSIGNMENTS CA ON CA.PK_CourseAssignment = CC.FK_CourseAssignment
                            LEFT JOIN dbo.CT_COURSE C ON C.PK_Course = CA.FK_Course
                            LEFT JOIN dbo.CT_LEVELCOURSE LC ON LC.PK_LEVELCOURSE = CA.FK_RequiredCourseLevels";

            List<ExternalEvidenceDTOs> result = await _unitOfWork.QueryListAsync<ExternalEvidenceDTOs>(sql);

            return result;

        }


        /// <summary>
        /// Returns from DB the List<object> needed for the view
        /// </summary>
        /// <returns></returns>
        public async Task<CreateExternalEvidenceDTOs> GetCreateExternalEvidence()
        {
            //Gets data for Combobox in CreateExternalEvidence

            CreateExternalEvidenceDTOs result = new();

            string query1 = @"

                            SELECT DISTINCT CAST(A.CONTROL_NUMBER AS VARCHAR(10)) + ' - ' + 
                                            CAST(A.NAMES AS VARCHAR(50)) + ' ' + 
                                            CAST(A.LAST_NAME AS VARCHAR(50)) + ' ' + 
                                            CAST(A.SECOND_NAME AS VARCHAR(50)) + ' - ' + 
                                            B.NAME_POSITION  AS [User]

                            FROM dbo.SY_HEADCOUNT A
                                LEFT JOIN dbo.CT_POSITION B ON A.FK_POSITION = B.PK_POSITION
                               WHERE A.AVAILABLE = 1";

            result.User = await _unitOfWork.QuerySingleColumnAsync<string>(query1);

            string query2 = @"
                                SELECT DISTINCT 
                                        CAST(A.PK_Course AS VARCHAR(10)) + ' - ' + A.CourseName As Course
                                FROM dbo.CT_COURSE A
                                    LEFT JOIN CT_COURSEASSIGNMENTS B ON A.PK_Course = B.FK_Course
                                WHERe A.Available = 1
                                --AND B.FK_DeliveryMode = 2 -- Comentado mientras emmanuel carga externos";

            result.Course = await _unitOfWork.QuerySingleColumnAsync<string>(query2);

            result.Level = new List<string> { "Introducción", "Básico", "Intermedio", "Avanzado" };


            return result;

        }

        public async Task<ServiceAnswer> PostCreateExternalEvidence(CreateExternalEvidenceInputDTOs model)
        {
            //Post data from view to db

            //Gets all the values.
            int courseID = int.Parse(model.SelectedCourse.Substring(0,model.SelectedCourse.IndexOf(" ")));
            string userName = model.UserName;
            int level = 0;
            byte[] fileBytes;
            int controlNumber;
            string positionName;
            ServiceAnswer serviceAnswer = new();

            switch (model.SelectedLevel)
            {
                case "Introducción":
                    level = 1;
                    break;

                case "Básico":
                    level = 2;
                    break;

                case "Intermedio":
                    level = 3;
                    break;

                case "Avanzado":
                    level = 4;
                    break;
            }

            using (var memoryStream = new MemoryStream())
            {
                model.UploadedFile.CopyTo(memoryStream);
                fileBytes = memoryStream.ToArray();
            }


            DataTable writeDt = new();

            //Validations
            if (model.UploadedFile == null || model.UploadedFile.Length == 0)
            {
                serviceAnswer = new(false, "ErrorMessage", "You must upload a PDF file.");
                return serviceAnswer;
            }



            var fileName = model.UploadedFile.FileName?.ToLowerInvariant() ?? "";

            if (!fileName.EndsWith(".pdf"))
            {
                serviceAnswer = new(false, "ErrorMessage", "Only PDF files are allowed (.pdf).");
                return serviceAnswer;
            }

            foreach(var line in model.SelectedUsers)
            {
                string[] userData = line.Split(" - ");
                controlNumber = int.Parse(userData[0]);
                positionName = userData[2];

                var parameters = new Dictionary<string, object>
                {
                    { "@pControlNumber", controlNumber },
                    { "@pScore", model.Score },
                    { "@pPositionName", positionName },
                    { "@pCourseID", courseID },
                    { "@pLevel", level },
                    { "@pUser", model.UserName },
                    { "@pEvidenceFile", fileBytes }
                };

                int filasAfectadas = await _unitOfWork.ExecuteStoredProcedureAsync("dbo.sp_PostCreateEvidenceMaterial", parameters);
            
            }




            serviceAnswer = new(false, "SuccessMessage", "Evidence Succesffully added");

            return serviceAnswer;

        }


    }
}
