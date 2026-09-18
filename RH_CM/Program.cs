using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.ExternalEvidence;
using RH_CM.Service.SQLSMS;
using BootstrapBlazor.Components;
using RH_CM.Service.AccessGroups;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;

// Checks that the process can write to a folder (creates a probe file and deletes it).
// If it can't, builds a message that says exactly which folder failed and which Windows
// identity is running the process, instead of letting the application fail later with a
// generic "access denied" error that's hard to trace back to the real cause (IIS App Pool permissions).
static void EnsureFolderIsWritable(string path, string purpose)
{
    try
    {
        Directory.CreateDirectory(path);
        var probeFile = Path.Combine(path, $".write-check-{Guid.NewGuid():N}.tmp");
        File.WriteAllText(probeFile, "ok");
        File.Delete(probeFile);
    }
    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
    {
        string identity;
        try
        {
#pragma warning disable CA1416 // The application is only deployed on IIS on Windows.
            identity = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
#pragma warning restore CA1416
        }
        catch { identity = "(could not determine the process's Windows identity)"; }

        throw new InvalidOperationException(
            $"The user the application is running as ('{identity}') does not have write permission on the " +
            $"folder required for {purpose}: '{path}'. If the application runs under IIS: open IIS Manager > " +
            "Application Pools > (this site's Pool) > Advanced Settings, check which account is used as " +
            "'Identity', and grant that account (or the IIS_IUSRS group) 'Modify' permission on that folder. " +
            $"Original detail: {ex.Message}", ex);
    }
}

// Translates the error code returned by SQL Server into a concrete explanation of what to check
// in appsettings.json, instead of leaving only the raw ADO.NET technical message (which almost
// never says which part of the connection string is wrong).
static string DescribeSqlConnectionError(SqlException ex, string server, string database)
{
    return ex.Number switch
    {
        18456 => $"Server '{server}' responded, but rejected the user or password configured in " +
                 "'ConexionSQL' (User Id / Password in appsettings.json). Verify they are correct for this environment.",

        4060 => $"The user was able to connect to server '{server}', but failed to open database " +
                $"'{database}'. Verify that the name in 'Database=' in appsettings.json is exactly correct " +
                 "and that this user has access permission on that specific database.",

        18452 => $"Server '{server}' expected a Windows login (Trusted_Connection), but the connection " +
                  "string carries a SQL Server username/password. Check whether 'ConexionSQL' should use " +
                  "'Trusted_Connection=True' instead of 'User Id'/'Password' for this environment.",

        -2 => $"Server '{server}' did not respond in time (the 'Connection Timeout' was exceeded). This usually " +
               "means a firewall is blocking the connection, or the server name belongs to another environment " +
               "(dev/prod) that is not reachable from here.",

        53 or 2 or 11001 or 10061 =>
              $"Could not reach server '{server}' over the network (the name was not found, or it refused the " +
               "connection on the SQL Server port). Verify that the name/address in 'Server=' is spelled correctly, " +
               "that the server is powered on, and that no firewall is blocking port 1433 between this machine and the server.",

        _ => $"The SQL Server engine at '{server}' returned an error while trying to open database '{database}'. " +
              "Check the code and the detail below to identify the exact cause."
    };
}

var builder = WebApplication.CreateBuilder(args);

// Keep logging portable across IIS, services, containers and local validation.
// The default Windows EventLog provider can throw when the process identity
// cannot write to the Event Log, which must never abort an HTTP request.
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// Our own services
builder.Services.AddServices();

// ✅ Read the connection string once
var connectionString = builder.Configuration.GetConnectionString("ConexionSQL");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "The connection string 'ConexionSQL' was not found. " +
        "Please configure it in appsettings.json under ConnectionStrings.");
}

// ✅ Show which server/database it's connecting to (without exposing the username/password),
// so the target environment can be confirmed at a glance from appsettings.json.
string dbServerDisplay;
string dbNameDisplay;
try
{
    var parsedConnection = new SqlConnectionStringBuilder(connectionString);
    dbServerDisplay = parsedConnection.DataSource;
    dbNameDisplay = parsedConnection.InitialCatalog;
}
catch (Exception ex)
{
    throw new InvalidOperationException(
        "The 'ConexionSQL' connection string in appsettings.json is not in a valid format. " +
        $"Make sure the server, database, and access credentials are correctly written. Detail: {ex.Message}", ex);
}

if (string.IsNullOrWhiteSpace(dbServerDisplay) || string.IsNullOrWhiteSpace(dbNameDisplay))
{
    throw new InvalidOperationException(
        "The 'ConexionSQL' connection string in appsettings.json is missing the server and/or the database name " +
        $"(Server='{dbServerDisplay}', Database='{dbNameDisplay}'). Fill it in as 'Server=...;Database=...;...'.");
}

System.Console.WriteLine("==================================================================");
System.Console.WriteLine($"[Startup] Configured database -> Server: {dbServerDisplay} | Database: {dbNameDisplay}");

try
{
    using var testConnection = new SqlConnection(connectionString);
    await testConnection.OpenAsync();
    System.Console.WriteLine("[Startup] Database connection verified successfully.");
}
catch (SqlException ex)
{
    System.Console.WriteLine("[Startup] ERROR: could not connect to the database configured above.");
    System.Console.WriteLine($"  {DescribeSqlConnectionError(ex, dbServerDisplay, dbNameDisplay)}");
    System.Console.WriteLine($"  SQL error code: {ex.Number}. Original detail: {ex.Message}");
}
catch (Exception ex)
{
    System.Console.WriteLine("[Startup] ERROR: could not connect to the database configured above.");
    System.Console.WriteLine("  This was not an error from the SQL Server engine itself, but something earlier " +
        "(for example, the connection string has an unrecognized option, or TLS/certificate negotiation failed).");
    System.Console.WriteLine($"  Detail: {ex.Message}");
}
System.Console.WriteLine("==================================================================");

// ✅ Register DbContexts with the same connection string
builder.Services.AddDbContext<db_abcd61_rhchdbContext>(opt =>
    opt.UseSqlServer(connectionString));

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(connectionString));

// ✅ Identity
builder.Services
    .AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

// ✅ Data Protection: the keys that encrypt session cookies and anti-forgery tokens are stored
// in an explicit folder under the site (instead of letting .NET pick a default location that
// may not be accessible under IIS). Write access is validated BEFORE startup, so that an IIS
// App Pool permissions problem fails right here with a clear message, instead of showing up
// later as sessions/cookies that fail without explanation.
var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys");
EnsureFolderIsWritable(dataProtectionKeysPath, "the data protection keys (session cookies, login, anti-forgery tokens)");

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
    .SetApplicationName("RH_CM");

// ✅ Blazor Server + BootstrapBlazor
builder.Services.AddServerSideBlazor();
builder.Services.AddBootstrapBlazor();

// ✅ HttpClientFactory
builder.Services.AddHttpClient();

// Authentication cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = new PathString("/Cuentas/Acceso");
    options.AccessDeniedPath = new PathString("/Cuentas/Denegado");

    // ⏱ Login expiration
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

    // 🔄 Renew the time if the user is still active
    options.SlidingExpiration = true;

    // 🚪 When it expires, send them to login with an indicator
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Redirect("/Cuentas/Acceso?expired=true");
        return Task.CompletedTask;
    };
});

// ✅ Session (registered once)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// Per-controller authorization.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ViewAccess", policy =>
        policy.Requirements.Add(new ViewAccessRequirement()));
});

// ✅ Response compression (gzip) to reduce the weight of HTML/CSS/JS over the network.
// Gzip only: Brotli (the default provider modern browsers negotiate) was corrupting large,
// dynamic HTML responses (e.g. /Trainify/Diagnostic with 11+ questions), causing
// ERR_CONTENT_DECODING_FAILED in the browser. Gzip does not have this problem.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Clear();
    options.Providers.Add<GzipCompressionProvider>();
});

var app = builder.Build();

// Global error-handling middleware for production
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseResponseCompression();

// ✅ 7-day cache for static assets (wwwroot/Assets, wwwroot/lib, etc.)
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cache-Control", "public,max-age=604800");
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// Blazor Server Hub
app.MapBlazorHub();

// MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cuentas}/{action=Acceso}/{id?}");

await app.RunAsync();

public partial class Program
{
    // Non-public constructor: nothing instantiates this class directly — it only exists so that
    // WebApplicationFactory<Program> (in the tests) can reference the startup assembly.
    protected Program() { }
}
