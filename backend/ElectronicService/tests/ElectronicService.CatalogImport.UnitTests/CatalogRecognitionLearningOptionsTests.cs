using ElectronicService.Infrastructure.Postgres;
using ElectronicService.Infrastructure.Postgres.Catalog.Recognition.Learning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ElectronicService.CatalogImport.UnitTests;

public sealed class CatalogRecognitionLearningOptionsTests
{
    [Fact]
    public void DefaultOptionsEnableLearningEveryThirtySecondsWithFiveHundredRows()
    {
        using var provider = Provider(null, null);
        var options = provider.GetRequiredService<IOptions<CatalogRecognitionLearningOptions>>().Value;
        Assert.True(options.Enabled);
        Assert.Equal(TimeSpan.FromSeconds(30), options.PollInterval);
        Assert.Equal(500, options.BatchSize);
    }

    [Theory]
    [InlineData("00:00:00", "500")]
    [InlineData("2.00:00:00", "500")]
    [InlineData("00:00:30", "0")]
    [InlineData("00:00:30", "5001")]
    public async Task InvalidLearningOptionsFailStartupValidation(string interval, string batchSize)
    {
        await using var provider = Provider(interval, batchSize);
        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IStartupValidator>().Validate());
    }

    private static ServiceProvider Provider(string? interval, string? batchSize)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (interval is not null)
        {
            values["CatalogRecognitionLearning:PollInterval"] = interval;
            values["CatalogRecognitionLearning:BatchSize"] = batchSize;
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        services.AddInfrastructurePostgres(configuration);
        return services.BuildServiceProvider();
    }
}
