namespace ElectronicService.Web.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class WebIntegrationDefinition
    : ICollectionFixture<WebIntegrationFixture>
{
    public const string Name = "Web API integration tests";
}
