using ElectronicService.Infrastructure.IntegrationTests.Data;
using ElectronicService.Infrastructure.IntegrationTests.Fixtures;
using ElectronicService.Infrastructure.Postgres.Catalog.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

[Collection(PostgreSqlIntegrationDefinition.Name)]
public sealed class CatalogProductMetadataRepositoryTests
    : PostgreSqlIntegrationTest
{
    public CatalogProductMetadataRepositoryTests(
        PostgreSqlFixture fixture)
        : base(fixture)
    {
    }

    // Проверяет настоящий GetProductTypeByIdAsync:
    // ProductType загружается вместе с разрешёнными характеристиками
    // и остаётся Detached из-за AsNoTracking.
    [Fact]
    public async Task GetProductTypeByIdAsyncLoadsCharacteristicsWithoutTracking()
    {
        // Arrange
        var graph =
            PostgreSqlTestDataFactory.CreateCatalogProductGraph();

        await SaveGraphAsync(graph);

        DbContext.ChangeTracker.Clear();

        var repository =
            new CatalogProductMetadataRepository(DbContext);

        // Act
        var productType =
            await repository.GetProductTypeByIdAsync(
                graph.ProductType.Id,
                TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(productType);

        var relation =
            Assert.Single(productType.Characteristics);

        Assert.Equal(
            graph.Definition.Id,
            relation.CharacteristicDefinitionId);

        Assert.Equal(
            EntityState.Detached,
            DbContext.Entry(productType).State);

        Assert.Equal(
            EntityState.Detached,
            DbContext.Entry(relation).State);
    }

    // Проверяет настоящий поиск определения по коду:
    // repository обрезает пробелы, приводит код к верхнему регистру
    // и возвращает объект без tracking.
    [Fact]
    public async Task GetDefinitionByCodeAsyncNormalizesCodeWithoutTracking()
    {
        // Arrange
        var graph =
            PostgreSqlTestDataFactory.CreateCatalogProductGraph();

        await SaveGraphAsync(graph);

        DbContext.ChangeTracker.Clear();

        var repository =
            new CatalogProductMetadataRepository(DbContext);

        var rawCode =
            $"  {graph.Definition.Code.Replace(
                "RATED_CURRENT",
                "rated_current",
                StringComparison.Ordinal)}  ";

        // Act
        var definition =
            await repository.GetCharacteristicDefinitionByCodeAsync(
                rawCode,
                TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(definition);
        Assert.Equal(graph.Definition.Id, definition.Id);
        Assert.Equal(graph.Definition.Code, definition.Code);

        Assert.Equal(
            EntityState.Detached,
            DbContext.Entry(definition).State);
    }
}
