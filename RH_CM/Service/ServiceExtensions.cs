using Microsoft.AspNetCore.Authorization;
using RH_CM.Service.AccessGroups;
using RH_CM.Service.Catalog;
using RH_CM.Service.DC3Service;
using RH_CM.Service.ExternalEvidence;
using RH_CM.Service.OcupationKey;
using RH_CM.Service.Permissions;
using RH_CM.Service.Requirements;
using RH_CM.Service.SQLSMS;
using RH_CM.Service.ThematicArea;
using RH_CM.Service.ThematicCourse;
using RH_CM.Service.Trainify;
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
        services.AddHttpContextAccessor();
        services.AddTransient<OcupationKeyService>();
        services.AddTransient<ThematicAreaService>();
        services.AddTransient<ThematicCourseService>();
        services.AddTransient<DC3Service>();
        // Scoped because they use the (scoped) DbContext directly, same as AccessGroupsService.
        services.AddScoped<DiagnosticExamService>();
        services.AddScoped<TestCatalogService>();
        services.AddScoped<CatalogIntegrityService>();
        // etc...
    }
}
