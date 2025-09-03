using RH_CM.Service.ExternalEvidence;
using RH_CM.Service.SQLSMS;

public static class ServiceExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddTransient<ExternalEvidenceService>();
        services.AddScoped<UnitOfWork>();
        // etc...
    }
}
