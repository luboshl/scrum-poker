using System.Net;
using AwesomeAssertions;
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
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AliveEndpoint_ReturnsHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/alive");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
