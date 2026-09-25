namespace ElectronicService.Infrastructure.Postgres.Catalog.Recognition.Learning;

public sealed class CatalogRecognitionLearningOptions
{
    public const string SectionName = "CatalogRecognitionLearning";

    public bool Enabled { get; set; } = true;

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(30);

    public int BatchSize { get; set; } = 500;
}
