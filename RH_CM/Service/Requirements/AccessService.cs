using RH_CM.Service.Requirements;
using RH_CM.Service.SQLSMS;

public class AccessService : IAccessService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UnitOfWork _UnitOfWork;

    public AccessService(IHttpContextAccessor httpContextAccessor, UnitOfWork unitOfWork)
    {
        _httpContextAccessor = httpContextAccessor;
        _UnitOfWork = unitOfWork;
    }

    public async Task<bool> HasAccessAsync(string userId, string controller, string action)
    {   bool hasAccess = false;

        var parameters = new Dictionary<string, object>
            {
                { "@pUserId", userId },
                { "@pController", controller },
                { "@pAction", action }
            };

        List<HasAcessDTOs> hasAcessDTOs = await _UnitOfWork.ExecuteStoredProcedureToListAsync<HasAcessDTOs>("[sp_AccessService_HasAccess]", parameters);

        // No matching permission row (e.g. a controller/action not yet registered) — fail closed instead of crashing.
        hasAccess = hasAcessDTOs.Count > 0 && hasAcessDTOs[0].HasAcess;

        return hasAccess;
    }
}
