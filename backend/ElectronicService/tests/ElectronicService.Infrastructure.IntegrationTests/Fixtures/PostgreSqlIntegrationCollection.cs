namespace ElectronicService.Infrastructure.IntegrationTests.Fixtures;

/// <summary>
/// Объединяет все PostgreSQL integration-тесты в одну xUnit collection.
/// Благодаря этому один контейнер используется несколькими тестовыми классами,
/// а сами классы внутри collection не выполняются параллельно.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgreSqlIntegrationDefinition
    : ICollectionFixture<PostgreSqlFixture>
{
    public const string Name = "PostgreSQL integration tests";
}
