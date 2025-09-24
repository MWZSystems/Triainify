using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.DTOs.UserTestEvidence;
using RH_CM.Service.SQLSMS;

namespace RH_CM.Service.UserTestEvidence
{
    public class UserTestEvidenceService
    {
        private readonly UnitOfWork _unitOfWork;
        private readonly db_abcd61_rhchdbContext _context;

        public UserTestEvidenceService(UnitOfWork unitOfWork,
                                        db_abcd61_rhchdbContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }
        

        public async Task<List<UserDTOs>> GetIndexAsync()
        {

            List<UserDTOs> user = await _unitOfWork.ExecuteStoredProcedureToListAsync<UserDTOs>("[sp_UserTestEvidenceService_Index_Get]");

            return user;
        }

        public async Task<DetailDTOs> GetDetailUserAsync(string ControlNumber)
        { 

            DetailDTOs detailDTOs = new();

            var parameters = new Dictionary<string, object>
                {
                    { "@pControlNumber", ControlNumber }
                };

            List<DetailUserExamDTOs> user = await _unitOfWork.ExecuteStoredProcedureToListAsync<DetailUserExamDTOs>("[sp_UserTestEvidenceService_Detail_Get]", parameters);

            detailDTOs.Details = user;

            detailDTOs.ControlNumber = ControlNumber;

            detailDTOs.FullName = await _context.SyHeadcounts
                                    .Where(h => h.ControlNumber == int.Parse(ControlNumber))
                                    .Select(h =>
                                        h.Names
                                        + " " + (h.LastName ?? "")
                                        + " " + (h.SecondName ?? "")
                                    )
                                    .FirstOrDefaultAsync();

            detailDTOs.Position = await _context.SyHeadcounts
                            .Where(h => h.ControlNumber == int.Parse(ControlNumber))
                            .Join(
                                _context.CtPositions,
                                h => h.FkPosition,
                                p => p.PkPosition,
                                (h, p) => p.NamePosition
                            )
                            .FirstOrDefaultAsync();



            return detailDTOs;
        }

    }
}
