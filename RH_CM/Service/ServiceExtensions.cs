using Microsoft.AspNetCore.Authorization;
using RH_CM.Service.AccessGroups;
using RH_CM.Service.ExternalEvidence;
using RH_CM.Service.Permissions;
using RH_CM.Service.Requirements;
using RH_CM.Service.SQLSMS;
using RH_CM.Service.UserTestEvidence;

public static class ServiceExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddTransient<ExternalEvidenceService>();
        services.AddScoped<UnitOfWork>();
        services.AddTransient<UserTestEvidenceService>();
        services.AddTransient<PermissionsService>();
        services.AddScoped<AccessGroupsService>();
        services.AddScoped<IAccessService, AccessService>();
        services.AddScoped<IAuthorizationHandler, ViewAccessHandler>();
        services.AddHttpContextAccessor(); // necesario si usas IHttpContextAccessor
        // etc...
    }
}
