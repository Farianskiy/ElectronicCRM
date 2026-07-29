namespace ElectronicService.Infrastructure.Postgres.Catalog.ImportBatches.Cleanup;

public sealed class CatalogImportCleanupOptions
{
    public const string SectionName = "CatalogImportCleanup";

    public bool Enabled { get; set; } = true;

    public int RetentionDays { get; set; } = 30;

    public int InitialDelayMinutes { get; set; } = 5;

    public int IntervalHours { get; set; } = 24;

    public int BatchSize { get; set; } = 100;
}