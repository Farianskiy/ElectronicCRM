using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using ElectronicService.Web.IntegrationTests.Data;
using ElectronicService.Web.IntegrationTests.Fixtures;
using ElectronicService.Web.IntegrationTests.Infrastructure;

namespace ElectronicService.Web.IntegrationTests.Catalog;

[Collection(WebIntegrationDefinition.Name)]
public sealed class CatalogProductsEndpointTests
{
    private static readonly Uri SearchEndpoint =
        new(
            "/api/catalog/products/search",
            UriKind.Relative);

    private readonly WebIntegrationFixture _fixture;

    public CatalogProductsEndpointTests(
        WebIntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    // Проверяет полный HTTP-путь поиска:
    // query string -> model binding -> reader -> PostgreSQL -> JSON.
    [Fact]
    public async Task SearchEndpointReturnsProductsMatchingSearch()
    {
        // Arrange
        using var client = _fixture.CreateClient();

        var marker = WebCatalogTestDataFactory.CreateMarker();

        await using (var dbContext = _fixture.CreateDbContext())
        {
            await WebCatalogTestDataFactory.CreateScenarioAsync(
                dbContext,
                marker,
                TestContext.Current.CancellationToken);
        }

        var request = CreateSearchRequest(
            marker,
            onlyInStock: null);

        using var response = await client.PostAsJsonAsync(
            SearchEndpoint,
            request,
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(body);

        Assert.Equal(
            3,
            JsonResponseAssertions.GetTotalCount(
                document.RootElement));

        var items = JsonResponseAssertions.GetItems(
            document.RootElement);

        Assert.Equal(3, items.GetArrayLength());

        Assert.All(
            items.EnumerateArray(),
            item => Assert.Contains(
                marker,
                JsonResponseAssertions.GetRequiredString(
                    item,
                    "name"),
                StringComparison.Ordinal));
    }

    // Проверяет все три состояния nullable фильтра наличия.
    [Fact]
    public async Task SearchEndpointAppliesOnlyInStockFilter()
    {
        // Arrange
        using var client = _fixture.CreateClient();

        var marker = WebCatalogTestDataFactory.CreateMarker();

        await using (var dbContext = _fixture.CreateDbContext())
        {
            await WebCatalogTestDataFactory.CreateScenarioAsync(
                dbContext,
                marker,
                TestContext.Current.CancellationToken);
        }

        var onlyAvailableRequest = CreateSearchRequest(
            marker,
            onlyInStock: true);

        var onlyUnavailableRequest = CreateSearchRequest(
            marker,
            onlyInStock: false);

        var allRequest = CreateSearchRequest(
            marker,
            onlyInStock: null);

        // Act
        using var availableResponse =
            await client.PostAsJsonAsync(
                SearchEndpoint,
                onlyAvailableRequest,
                TestContext.Current.CancellationToken);

        using var unavailableResponse =
            await client.PostAsJsonAsync(
                SearchEndpoint,
                onlyUnavailableRequest,
                TestContext.Current.CancellationToken);

        using var allResponse =
            await client.PostAsJsonAsync(
                SearchEndpoint,
                allRequest,
                TestContext.Current.CancellationToken);

        var availableBody =
            await availableResponse.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        var unavailableBody =
            await unavailableResponse.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        var allBody =
            await allResponse.Content.ReadAsStringAsync(
                TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            availableResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            unavailableResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.OK,
            allResponse.StatusCode);

        using var availableDocument =
            JsonDocument.Parse(availableBody);

        using var unavailableDocument =
            JsonDocument.Parse(unavailableBody);

        using var allDocument =
            JsonDocument.Parse(allBody);

        Assert.Equal(
            1,
            JsonResponseAssertions.GetTotalCount(
                availableDocument.RootElement));

        Assert.Equal(
            2,
            JsonResponseAssertions.GetTotalCount(
                unavailableDocument.RootElement));

        Assert.Equal(
            3,
            JsonResponseAssertions.GetTotalCount(
                allDocument.RootElement));

        var availableItem = Assert.Single(
            JsonResponseAssertions.GetItems(
                    availableDocument.RootElement)
                .EnumerateArray());

        Assert.True(
            JsonResponseAssertions.GetRequiredDecimal(
                availableItem,
                "stockQuantity") > 0);

        Assert.All(
            JsonResponseAssertions.GetItems(
                    unavailableDocument.RootElement)
                .EnumerateArray(),
            item => Assert.Equal(
                0m,
                JsonResponseAssertions.GetRequiredDecimal(
                    item,
                    "stockQuantity")));
    }


        private sealed record SearchRequestPayload(
        string? Search,
        string? ProductTypeCode,
        string? Manufacturer,
        SearchCharacteristicPayload[] Characteristics,
        int Page,
        int PageSize,
        bool? OnlyInStock);

    private sealed record SearchCharacteristicPayload(
        string Code,
        string Value);

    private static SearchRequestPayload CreateSearchRequest(
        string marker,
        bool? onlyInStock)
    {
        return new SearchRequestPayload(
            Search: marker,
            ProductTypeCode: null,
            Manufacturer: null,
            Characteristics: [],
            Page: 1,
            PageSize: 20,
            OnlyInStock: onlyInStock);
    }
}
