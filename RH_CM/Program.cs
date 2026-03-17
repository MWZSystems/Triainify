using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.ExternalEvidence;
using RH_CM.Service.SQLSMS;
using BootstrapBlazor.Components;
using RH_CM.Service.AccessGroups;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Servicios propios
builder.Services.AddServices();

// ✅ Leer una sola vez la cadena de conexión
var connectionString = builder.Configuration.GetConnectionString("ConexionSQL");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "The connection string 'ConexionSQL' was not found. " +
        "Please configure it in appsettings.json under ConnectionStrings.");
}

// ✅ Registrar DbContexts con la misma cadena
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

// ✅ Blazor Server + BootstrapBlazor
builder.Services.AddServerSideBlazor();
builder.Services.AddBootstrapBlazor();

// ✅ HttpClientFactory
builder.Services.AddHttpClient();

// Cookies de autenticación
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = new PathString("/Cuentas/Acceso");
    options.AccessDeniedPath = new PathString("/Cuentas/Denegado");

    // ⏱ Expiración del login
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

    // 🔄 Si el usuario sigue activo se renueva el tiempo
    options.SlidingExpiration = true;

    // 🚪 Cuando expire lo manda al login con indicador
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.Redirect("/Cuentas/Acceso?expired=true");
        return Task.CompletedTask;
    };
});

// ✅ Session (una sola vez)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();

// Autorización por Controller.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ViewAccess", policy =>
        policy.Requirements.Add(new ViewAccessRequirement()));
});

var app = builder.Build();

// Middleware de manejo de errores global para producción
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// Blazor Server Hub
app.MapBlazorHub();

// Rutas MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cuentas}/{action=Acceso}/{id?}");

app.Run();
