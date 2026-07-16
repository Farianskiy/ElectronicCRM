using System.Net;
using System.Text.Json;
using ElectronicService.Web.IntegrationTests.Data;
using ElectronicService.Web.IntegrationTests.Fixtures;
using ElectronicService.Web.IntegrationTests.Infrastructure;

namespace ElectronicService.Web.IntegrationTests.Catalog;

[Collection(WebIntegrationDefinition.Name)]
public sealed class CatalogProductReplacementsEndpointTests
{
    private readonly WebIntegrationFixture _fixture;

    public CatalogProductReplacementsEndpointTests(
        WebIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    // Проверяет подбор аналогов и OnlyInStock через HTTP.
    [Fact]
    public async Task ReplacementsEndpointReturnsOnlyAvailableCandidate()
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
                    TestContext.Current.CancellationToken);
        }

        var query = new Dictionary<string, string?>(
            StringComparer.Ordinal)
        {
            ["onlyInStock"] = bool.TrueString,
            ["minimumScore"] = "100",
            ["page"] = "1",
            ["pageSize"] = "20"
        };

        var endpoint =
            EndpointRouteResolver.ResolveCatalogGetEndpoint(
                _fixture.Services,
                CatalogEndpointKind.Replacements,
                scenario.Source.Id,
                query);

        // Act
        using var response = await client.GetAsync(
            endpoint,
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(body);

        var items = JsonResponseAssertions.GetItems(
            document.RootElement);

        var item = Assert.Single(items.EnumerateArray());

        Assert.Equal(
            scenario.AvailableReplacement.Id,
            JsonResponseAssertions.GetProductId(item));

        Assert.NotEqual(
            scenario.Source.Id,
            JsonResponseAssertions.GetProductId(item));

        Assert.NotEqual(
            scenario.UnavailableReplacement.Id,
            JsonResponseAssertions.GetProductId(item));

        Assert.True(
            JsonResponseAssertions.GetRequiredDecimal(
                item,
                "stockQuantity") > 0);
    }

    // Проверяет HTTP 404 для неизвестного исходного товара.
    [Fact]
    public async Task ReplacementsEndpointReturnsNotFoundForUnknownProduct()
    {
        // Arrange
        using var client = _fixture.CreateClient();

        var query = new Dictionary<string, string?>(
            StringComparer.Ordinal)
        {
            ["onlyInStock"] = bool.TrueString,
            ["minimumScore"] = "0",
            ["page"] = "1",
            ["pageSize"] = "20"
        };

        var endpoint =
            EndpointRouteResolver.ResolveCatalogGetEndpoint(
                _fixture.Services,
                CatalogEndpointKind.Replacements,
                Guid.NewGuid(),
                query);

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
