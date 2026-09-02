using System.Globalization;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Datasets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.Datasets;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/recognition/datasets")]
public sealed class ExportCatalogRecognitionDatasetController : ControllerBase
{
    private const string JsonLinesContentType = "application/x-ndjson";

    private const string FormatVersionHeader = "X-Dataset-Format-Version";

    private const string FinalizedUntilUtcHeader = "X-Dataset-Finalized-Until-Utc";

    private const string ExportedAtUtcHeader = "X-Dataset-Exported-At-Utc";

    private const string ExampleCountHeader = "X-Dataset-Example-Count";

    private const string ExampleCountsHeader = "X-Dataset-Example-Counts";

    private const string Sha256Header = "X-Dataset-SHA256";

    private readonly ICatalogRecognitionDatasetExporter _datasetExporter;

    public ExportCatalogRecognitionDatasetController(ICatalogRecognitionDatasetExporter datasetExporter)
    {
        ArgumentNullException.ThrowIfNull(datasetExporter);

        _datasetExporter = datasetExporter;
    }

    [HttpGet("export")]
    [Produces(JsonLinesContentType)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Export([FromQuery] DateTimeOffset? finalizedUntilUtc, CancellationToken cancellationToken = default)
    {
        var requestStartedAtUtc = DateTime.UtcNow;
        var cutoffUtc = finalizedUntilUtc?.UtcDateTime ?? requestStartedAtUtc;

        if (cutoffUtc > requestStartedAtUtc)
        {
            return BadRequest(CreateFutureCutoffProblemDetails());
        }

        await using var temporaryStream = CreateTemporaryStream();

        var metadata = await _datasetExporter.ExportAsync(temporaryStream, cutoffUtc, cancellationToken).ConfigureAwait(false);

        temporaryStream.Position = 0;

        WriteMetadataHeaders(metadata);

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = JsonLinesContentType;
        Response.ContentLength = temporaryStream.Length;
        Response.Headers[HeaderNames.ContentDisposition] = $"attachment; filename=\"{metadata.FileName}\"";
        Response.Headers[HeaderNames.CacheControl] = "private, no-store";
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";

        await temporaryStream.CopyToAsync(Response.Body, 65536, cancellationToken).ConfigureAwait(false);

        return new EmptyResult();
    }

    private static ProblemDetails CreateFutureCutoffProblemDetails()
    {
        return new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Не удалось экспортировать датасет.",
            Detail = "Момент отсечения датасета не может находиться в будущем."
        };
    }

    private static FileStream CreateTemporaryStream()
    {
        var temporaryFilePath = Path.Combine(
            Path.GetTempPath(),
            $"electroniccrm-catalog-recognition-{Path.GetRandomFileName()}");

        return new FileStream(
            temporaryFilePath,
            new FileStreamOptions
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.ReadWrite,
                Share = FileShare.Read | FileShare.Delete,
                BufferSize = 65536,
                Options = FileOptions.Asynchronous |
                    FileOptions.SequentialScan |
                    FileOptions.DeleteOnClose
            });
    }

    private void WriteMetadataHeaders(CatalogRecognitionDatasetExportMetadata metadata)
    {
        Response.Headers[FormatVersionHeader] = metadata.FormatVersion;
        Response.Headers[FinalizedUntilUtcHeader] = metadata.FinalizedUntilUtc.ToString("O", CultureInfo.InvariantCulture);
        Response.Headers[ExportedAtUtcHeader] = metadata.ExportedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        Response.Headers[ExampleCountHeader] = metadata.ExampleCount.ToString(CultureInfo.InvariantCulture);
        Response.Headers[ExampleCountsHeader] = FormatExampleCounts(metadata.ExampleCounts);
        Response.Headers[Sha256Header] = metadata.Sha256;
    }

    private static string FormatExampleCounts(IReadOnlyDictionary<string, long> exampleCounts)
    {
        return string.Join(
            ";",
            exampleCounts
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value.ToString(CultureInfo.InvariantCulture)}"));
    }
}