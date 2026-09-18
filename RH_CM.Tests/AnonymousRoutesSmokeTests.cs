using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace RH_CM.Tests;

/// <summary>
/// Smoke tests de arranque de la app contra rutas anónimas únicamente, vía WebApplicationFactory.
/// No siembran ni modifican nada en la base de datos: como mucho hacen una verificación de
/// conectividad de solo lectura (CanConnectAsync, que ya ejecuta la propia página de login).
/// Sirven para detectar regresiones de wiring en Program.cs (compresión de respuesta, cache
/// de estáticos, DI, Identity) sin necesitar un usuario autenticado.
/// </summary>
public class AnonymousRoutesSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AnonymousRoutesSmokeTests(WebApplicationFactory<Program> factory)
    {
        // Windows Event Log requires elevated OS permissions and is unrelated to HTTP behavior.
        // Removing host log providers keeps these smoke tests deterministic in CI/sandboxes.
        _factory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureLogging(logging => logging.ClearProviders()));
    }

    [Fact]
    public async Task LoginPage_ReturnsSuccess_AndContainsLoginForm()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Cuentas/Acceso");
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("form", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginPage_IsServedCompressed_WhenClientAcceptsGzip()
    {
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/Cuentas/Acceso");
        request.Headers.Add("Accept-Encoding", "gzip");

        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        Assert.Contains("gzip", response.Content.Headers.ContentEncoding, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task StaticAsset_HasLongLivedCacheControlHeader()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/Assets/dist/css/light.css");
        response.EnsureSuccessStatusCode();

        Assert.NotNull(response.Headers.CacheControl);
        Assert.True(response.Headers.CacheControl!.Public);
        Assert.Equal(TimeSpan.FromDays(7), response.Headers.CacheControl.MaxAge);
    }
}
