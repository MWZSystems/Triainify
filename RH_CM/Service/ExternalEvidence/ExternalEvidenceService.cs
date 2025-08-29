using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.DTOs;
using RH_CM.Service.SQLSMS;

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
        public async Task<List<GetExternalEvidenceDTOs>> GetIndex()
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

            List<GetExternalEvidenceDTOs> result = await _unitOfWork.QueryListAsync<GetExternalEvidenceDTOs>(sql);

            return result;

        }


    }
}
