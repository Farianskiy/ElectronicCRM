using System.Net;
using ElectronicService.Web.IntegrationTests.Fixtures;

namespace ElectronicService.Web.IntegrationTests.Smoke;

[Collection(WebIntegrationDefinition.Name)]
public sealed class ApplicationSmokeTests
{
    private static readonly Uri RootEndpoint =
        new("/", UriKind.Relative);

    private static readonly Uri HealthEndpoint =
        new("/health", UriKind.Relative);

    private readonly WebIntegrationFixture _fixture;

    public ApplicationSmokeTests(
        WebIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    // Проверяет, что Program, DI, PostgreSQL, migrations и seeding
    // успешно запускаются как единое ASP.NET Core приложение.
    [Fact]
    public async Task RootEndpointReturnsRunningMessage()
    {
        // Arrange
        using var client = _fixture.CreateClient();

        // Act
        using var response = await client.GetAsync(
            RootEndpoint,
            TestContext.Current.CancellationToken);

        var content = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ElectronicService is running!", content);
    }

    // Проверяет настоящий health check DbContext
    // против временного PostgreSQL Testcontainer.
    [Fact]
    public async Task HealthEndpointReturnsHealthyStatus()
    {
        // Arrange
        using var client = _fixture.CreateClient();

        // Act
        using var response = await client.GetAsync(
            HealthEndpoint,
            TestContext.Current.CancellationToken);

        var content = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", content);
    }
}