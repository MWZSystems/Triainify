using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RH_CM.Data;
using RH_CM.Service.ExternalEvidence;
using RH_CM.Service.SQLSMS;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServices();

builder.Services.AddDbContext<db_abcd61_rhchdbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSQL")));

builder.Services.AddDbContext<ApplicationDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("ConexionSQL")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews();

// ✅ Blazor Server + Blazor.Bootstrap (para usar <component> y PdfViewer)
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazorBootstrap();   // Blazor.Bootstrap 3.4.0

// ✅ HttpClientFactory (útil si haces proxy de PDFs externos)
builder.Services.AddHttpClient();

// (Opcional) compresión para SignalR/Blazor
// builder.Services.AddResponseCompression(opts =>
// {
//     opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/octet-stream" });
// });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = new PathString("/Cuentas/Acceso");
    options.AccessDeniedPath = new PathString("/Cuentas/Denegado");
});

// SOLO aquí, una vez
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();                 // ✅ recomendado en prod
}

app.UseHttpsRedirection();         // ✅ recomendado
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// SOLO una vez y antes de MapControllerRoute
app.UseSession();

// (Opcional) compresión
// app.UseResponseCompression();

// ✅ Hub de Blazor Server (imprescindible para componentes interactivos)
app.MapBlazorHub();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Cuentas}/{action=Acceso}/{id?}");

app.Run();
