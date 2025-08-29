using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.ExternalEvidence;
using RH_CM.Service.SQLSMS;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddServices();

// Configurar la conexión a SQL Server para MATERIALES_CHDBContext
builder.Services.AddDbContext<db_abcd61_rhchdbContext>(Options =>
    Options.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSQL")));

// Configurar la conexión a SQL Server para ApplicationDbContext
builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSQL")));

// Configurar Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configuración de la URL de retorno al acceder
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = new PathString("/Cuentas/Acceso");
    options.AccessDeniedPath = new PathString("/Cuentas/Denegado");
});

// Configuración de sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de expiración de la sesión
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configurar el pipeline de solicitudes HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cuentas}/{action=Acceso}/{id?}");

app.Run();

