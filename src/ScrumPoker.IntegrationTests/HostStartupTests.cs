using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ScrumPoker.IntegrationTests;

/// <summary>
/// Smoke tests that verify the ASP.NET Core host starts successfully and
/// responds on the health endpoints configured via service defaults.
/// </summary>
public class HostStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HostStartupTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AliveEndpoint_ReturnsHealthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/alive");

        response.EnsureSuccessStatusCode();
    }
}
