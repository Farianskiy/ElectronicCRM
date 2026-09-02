using System.Globalization;
using System.Text.Json;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Datasets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace ElectronicService.Web.Controllers.Catalog.Recognition.Datasets;

[ApiController]
[Authorize(Roles = "Technical")]
[Route("api/catalog/recognition/datasets")]
public sealed class ExportCatalogRecognitionDatasetBundleController : ControllerBase
{
    private const string ZipContentType = "application/zip";

    private const string FormatVersionHeader = "X-Dataset-Format-Version";

    private const string FinalizedUntilUtcHeader = "X-Dataset-Finalized-Until-Utc";

    private const string ExportedAtUtcHeader = "X-Dataset-Exported-At-Utc";

    private const string ExampleCountHeader = "X-Dataset-Example-Count";

    private const string Sha256Header = "X-Dataset-SHA256";

    private const string BundleFormatVersionHeader = "X-Dataset-Bundle-Format-Version";

    private const string SplitAlgorithmVersionHeader = "X-Dataset-Split-Algorithm-Version";

    private const string ProductGroupCountHeader = "X-Dataset-Product-Group-Count";

    private const string BundleManifestHeader = "X-Dataset-Bundle-Manifest";

    private static readonly JsonSerializerOptions BundleManifestSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ICatalogRecognitionDatasetBundleExporter _datasetBundleExporter;

    public ExportCatalogRecognitionDatasetBundleController(ICatalogRecognitionDatasetBundleExporter datasetBundleExporter)
    {
        ArgumentNullException.ThrowIfNull(datasetBundleExporter);

        _datasetBundleExporter = datasetBundleExporter;
    }

    [HttpGet("export-bundle")]
    [Produces(ZipContentType)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExportBundle([FromQuery] DateTimeOffset? finalizedUntilUtc, CancellationToken cancellationToken = default)
    {
        var requestStartedAtUtc = DateTime.UtcNow;
        var cutoffUtc = finalizedUntilUtc?.UtcDateTime ?? requestStartedAtUtc;

        if (cutoffUtc > requestStartedAtUtc)
        {
            return BadRequest(CreateFutureCutoffProblemDetails());
        }

        await using var temporaryStream = CreateTemporaryStream();

        var metadata = await _datasetBundleExporter.ExportAsync(temporaryStream, cutoffUtc, cancellationToken).ConfigureAwait(false);

        temporaryStream.Position = 0;

        WriteMetadataHeaders(metadata);

        Response.StatusCode = StatusCodes.Status200OK;
        Response.ContentType = ZipContentType;
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
            Title = "Не удалось экспортировать замороженный датасет.",
            Detail = "Момент отсечения датасета не может находиться в будущем."
        };
    }

    private static FileStream CreateTemporaryStream()
    {
        var temporaryFilePath = Path.Combine(
            Path.GetTempPath(),
            $"electroniccrm-catalog-recognition-bundle-{Path.GetRandomFileName()}");

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

    private void WriteMetadataHeaders(CatalogRecognitionDatasetBundleExportMetadata metadata)
    {
        var manifest = metadata.Manifest;
        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, BundleManifestSerializerOptions);

        Response.Headers[BundleFormatVersionHeader] = manifest.BundleFormatVersion;
        Response.Headers[FormatVersionHeader] = manifest.DatasetFormatVersion;
        Response.Headers[FinalizedUntilUtcHeader] = manifest.FinalizedUntilUtc.ToString("O", CultureInfo.InvariantCulture);
        Response.Headers[ExportedAtUtcHeader] = metadata.ExportedAtUtc.ToString("O", CultureInfo.InvariantCulture);
        Response.Headers[ExampleCountHeader] = manifest.ExampleCount.ToString(CultureInfo.InvariantCulture);
        Response.Headers[Sha256Header] = manifest.DatasetSha256;
        Response.Headers[SplitAlgorithmVersionHeader] = manifest.SplitAlgorithmVersion;
        Response.Headers[ProductGroupCountHeader] = manifest.ProductGroupCount.ToString(CultureInfo.InvariantCulture);
        Response.Headers[BundleManifestHeader] = Convert.ToBase64String(manifestBytes);
    }
}