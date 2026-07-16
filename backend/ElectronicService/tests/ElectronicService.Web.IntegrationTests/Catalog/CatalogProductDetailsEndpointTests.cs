using System.Net;
using System.Text.Json;
using ElectronicService.Web.IntegrationTests.Data;
using ElectronicService.Web.IntegrationTests.Fixtures;
using ElectronicService.Web.IntegrationTests.Infrastructure;

namespace ElectronicService.Web.IntegrationTests.Catalog;

[Collection(WebIntegrationDefinition.Name)]
public sealed class CatalogProductDetailsEndpointTests
{
    private readonly WebIntegrationFixture _fixture;

    public CatalogProductDetailsEndpointTests(
        WebIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    // Проверяет карточку товара через настоящий HTTP endpoint.
    [Fact]
    public async Task DetailsEndpointReturnsCharacteristicsAndAliases()
    {
        // Arrange
        using var client = _fixture.CreateClient();

        var marker = WebCatalogTestDataFactory.CreateMarker();
        WebCatalogScenario scenario;

        await using (var dbContext = _fixture.CreateDbContext())
        {
            scenario =
                await WebCatalogTestDataFactory.CreateScenarioAsync(
                    dbContext,
                    marker,
                    TestContext.Current.CancellationToken,
                    ratedCurrent: 16.5m);
        }

        var endpoint =
            EndpointRouteResolver.ResolveCatalogGetEndpoint(
                _fixture.Services,
                CatalogEndpointKind.Details,
                scenario.Source.Id);

        // Act
        using var response = await client.GetAsync(
            endpoint,
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(body);

        Assert.Equal(
            scenario.Source.Id,
            JsonResponseAssertions.GetProductId(
                document.RootElement));

        Assert.Equal(
            scenario.Source.Article.Value,
            JsonResponseAssertions.GetRequiredString(
                document.RootElement,
                "article"));

        Assert.Equal(
            scenario.Source.Name.Value,
            JsonResponseAssertions.GetRequiredString(
                document.RootElement,
                "name"));

        var characteristics =
            JsonResponseAssertions.GetRequiredProperty(
                document.RootElement,
                "characteristics");

        var aliases =
            JsonResponseAssertions.GetRequiredProperty(
                document.RootElement,
                "aliases");

        Assert.True(
            characteristics.GetArrayLength() >= 4);

        Assert.True(
            JsonResponseAssertions.ContainsString(
                aliases,
                string.Concat(marker, " alias")));

        Assert.Contains(
            "\"16.5\"",
            body,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "16.5000",
            body,
            StringComparison.Ordinal);
    }

    // Проверяет HTTP 404 для неизвестного товара.
    [Fact]
    public async Task DetailsEndpointReturnsNotFoundForUnknownProduct()
    {
        // Arrange
        using var client = _fixture.CreateClient();

        var endpoint =
            EndpointRouteResolver.ResolveCatalogGetEndpoint(
                _fixture.Services,
                CatalogEndpointKind.Details,
                Guid.NewGuid());

        // Act
        using var response = await client.GetAsync(
            endpoint,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }
}
