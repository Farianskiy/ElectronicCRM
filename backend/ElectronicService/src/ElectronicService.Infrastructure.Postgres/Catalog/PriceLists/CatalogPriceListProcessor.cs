using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceLists.Import;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.PriceLists;

public sealed class CatalogPriceListProcessor
    : ICatalogPriceListProcessor
{
    private const int WriteBatchSize = 500;

    private readonly ElectronicDbContext _dbContext;

    private readonly ICatalogPriceListWorkbookReader _workbookReader;

    public CatalogPriceListProcessor(
        ElectronicDbContext dbContext,
        ICatalogPriceListWorkbookReader workbookReader)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(workbookReader);

        _dbContext = dbContext;
        _workbookReader = workbookReader;
    }

    public async Task<
        Result<
            CatalogPriceListProcessingResult,
            DomainError>> ProcessAsync(
                Guid priceListId,
                CancellationToken cancellationToken = default)
    {
        if (priceListId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceListProcessingResult,
                DomainError>(
                    CatalogPriceListErrors.PriceListNotFound(
                        priceListId));
        }

        var priceList =
            await _dbContext.CatalogPriceLists
                .Include(item => item.File)
                .SingleOrDefaultAsync(
                    item => item.Id == priceListId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (priceList is null)
        {
            return Result.Failure<
                CatalogPriceListProcessingResult,
                DomainError>(
                    CatalogPriceListErrors.PriceListNotFound(
                        priceListId));
        }

        var startResult =
            priceList.StartProcessing();

        if (startResult.IsFailure)
        {
            return Result.Failure<
                CatalogPriceListProcessingResult,
                DomainError>(
                    startResult.Error);
        }

        var originalFileName =
            priceList.OriginalFileName;

        var fileContent =
            priceList.File.Content;

        var manufacturerId =
            priceList.ManufacturerId;

        await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        await _dbContext.CatalogPriceListRows
            .Where(row =>
                row.PriceListId == priceListId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        _dbContext.ChangeTracker.Clear();

        var productLookups =
            await LoadProductLookupsAsync(
                    manufacturerId,
                    cancellationToken)
                .ConfigureAwait(false);

        var articleIndex =
            BuildArticleIndex(productLookups);

        var sourceRows =
            new List<CatalogPriceListSourceRow>(
                WriteBatchSize);

        var validRowsCount = 0;
        var errorRowsCount = 0;
        var savedRowsCount = 0;

        async Task<UnitResult<DomainError>> ConsumeRowAsync(
            CatalogPriceListSourceRow sourceRow,
            CancellationToken rowCancellationToken)
        {
            sourceRows.Add(sourceRow);

            if (sourceRows.Count < WriteBatchSize)
            {
                return UnitResult.Success<DomainError>();
            }

            var saveResult =
                await SaveRowsAsync(
                        priceListId,
                        sourceRows,
                        articleIndex,
                        rowCancellationToken)
                    .ConfigureAwait(false);

            if (saveResult.IsFailure)
            {
                return UnitResult.Failure(
                    saveResult.Error);
            }

            validRowsCount +=
                saveResult.Value.ValidRowsCount;

            errorRowsCount +=
                saveResult.Value.ErrorRowsCount;

            savedRowsCount +=
                saveResult.Value.SavedRowsCount;

            sourceRows.Clear();

            return UnitResult.Success<DomainError>();
        }

        async Task<UnitResult<DomainError>>
            ReportProgressAsync(
                CatalogPriceListWorkbookReadProgress progress,
                CancellationToken progressCancellationToken)
        {
            var estimatedRowsCount =
                Math.Max(
                    progress.EstimatedRowsCount,
                    progress.ReadRowsCount);

            var updatedAtUtc =
                DateTime.UtcNow;

            var updatedRowsCount =
                await _dbContext.CatalogPriceLists
                    .Where(item =>
                        item.Id == priceListId
                        && item.Status
                        == CatalogPriceListStatus.Processing)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(
                                item => item.EstimatedRowsCount,
                                estimatedRowsCount)
                            .SetProperty(
                                item => item.ReadRowsCount,
                                progress.ReadRowsCount)
                            .SetProperty(
                                item => item.SavedRowsCount,
                                savedRowsCount)
                            .SetProperty(
                                item => item.UpdatedAtUtc,
                                updatedAtUtc),
                        progressCancellationToken)
                    .ConfigureAwait(false);

            if (updatedRowsCount == 0)
            {
                return UnitResult.Failure(
                    CatalogPriceListErrors
                        .PriceListProcessingFailed());
            }

            return UnitResult.Success<DomainError>();
        }

        var readingResult =
            await _workbookReader
                .ReadAsync(
                    originalFileName,
                    fileContent,
                    ConsumeRowAsync,
                    ReportProgressAsync,
                    cancellationToken)
                .ConfigureAwait(false);

        if (readingResult.IsFailure)
        {
            await MarkFailedAsync(
                    priceListId,
                    readingResult.Error.Message,
                    cancellationToken)
                .ConfigureAwait(false);

            return Result.Failure<
                CatalogPriceListProcessingResult,
                DomainError>(
                    readingResult.Error);
        }

        if (sourceRows.Count > 0)
        {
            var finalSaveResult =
                await SaveRowsAsync(
                        priceListId,
                        sourceRows,
                        articleIndex,
                        cancellationToken)
                    .ConfigureAwait(false);

            if (finalSaveResult.IsFailure)
            {
                await MarkFailedAsync(
                        priceListId,
                        finalSaveResult.Error.Message,
                        cancellationToken)
                    .ConfigureAwait(false);

                return Result.Failure<
                    CatalogPriceListProcessingResult,
                    DomainError>(
                        finalSaveResult.Error);
            }

            validRowsCount +=
                finalSaveResult.Value.ValidRowsCount;

            errorRowsCount +=
                finalSaveResult.Value.ErrorRowsCount;

            savedRowsCount +=
                finalSaveResult.Value.SavedRowsCount;

            sourceRows.Clear();
        }

        _dbContext.ChangeTracker.Clear();

        var processedPriceList =
            await _dbContext.CatalogPriceLists
                .SingleOrDefaultAsync(
                    item => item.Id == priceListId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (processedPriceList is null)
        {
            return Result.Failure<
                CatalogPriceListProcessingResult,
                DomainError>(
                    CatalogPriceListErrors.PriceListNotFound(
                        priceListId));
        }

        var completionResult =
            processedPriceList.CompleteProcessing(
                readingResult.Value.EffectiveDate,
                savedRowsCount,
                validRowsCount,
                errorRowsCount);

        if (completionResult.IsFailure)
        {
            await MarkFailedAsync(
                    priceListId,
                    completionResult.Error.Message,
                    cancellationToken)
                .ConfigureAwait(false);

            return Result.Failure<
                CatalogPriceListProcessingResult,
                DomainError>(
                    completionResult.Error);
        }

        await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<
            CatalogPriceListProcessingResult,
            DomainError>(
                new CatalogPriceListProcessingResult(
                    processedPriceList.Id,
                    savedRowsCount,
                    validRowsCount,
                    errorRowsCount,
                    processedPriceList.Status));
    }

    private async Task<
        Result<
            CatalogPriceListBatchSaveResult,
            DomainError>> SaveRowsAsync(
                Guid priceListId,
                List<CatalogPriceListSourceRow> sourceRows,
                Dictionary<string, Guid[]> articleIndex,
                CancellationToken cancellationToken)
    {
        var rows =
            new List<CatalogPriceListRow>(
                sourceRows.Count);

        var validRowsCount = 0;
        var errorRowsCount = 0;

        foreach (var sourceRow in sourceRows)
        {
            var rowResult =
                CatalogPriceListRow.Create(
                    priceListId,
                    sourceRow.RowNumber,
                    sourceRow.Article,
                    sourceRow.Name,
                    sourceRow.BasePriceAmount,
                    sourceRow.MrcPriceAmount,
                    sourceRow.ProductLink,
                    sourceRow.Unit);

            if (rowResult.IsFailure)
            {
                return Result.Failure<
                    CatalogPriceListBatchSaveResult,
                    DomainError>(
                        rowResult.Error);
            }

            var row =
                rowResult.Value;

            MatchRow(
                row,
                articleIndex);

            if (row.Status != CatalogPriceListRowStatus.Valid)
            {
                continue;
            }

            validRowsCount++;
            rows.Add(row);
        }

        _dbContext.CatalogPriceListRows
            .AddRange(rows);

        try
        {
            await _dbContext
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();

            return Result.Failure<
                CatalogPriceListBatchSaveResult,
                DomainError>(
                    CatalogPriceListErrors
                        .PriceListProcessingFailed());
        }

        _dbContext.ChangeTracker.Clear();

        return Result.Success<
            CatalogPriceListBatchSaveResult,
            DomainError>(
                new CatalogPriceListBatchSaveResult(
                    rows.Count,
                    validRowsCount,
                    errorRowsCount));
    }

    private static void MatchRow(
        CatalogPriceListRow row,
        Dictionary<string, Guid[]> articleIndex)
    {
        if (articleIndex.TryGetValue(
                row.NormalizedArticle,
                out var articleMatches))
        {
            ApplyArticleMatches(
                row,
                articleMatches);

            return;
        }

        row.MarkProductNotFound();
    }

    private static void ApplyArticleMatches(
        CatalogPriceListRow row,
        Guid[] productIds)
    {
        if (productIds.Length == 1)
        {
            var matchResult =
                row.MarkMatchedByArticle(
                    productIds[0]);

            if (matchResult.IsFailure)
            {
                row.MarkProductNotFound();
            }

            return;
        }

        row.MarkAmbiguous();
    }

    private async Task<ProductLookup[]>
        LoadProductLookupsAsync(
            Guid manufacturerId,
            CancellationToken cancellationToken)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(product =>
                product.ManufacturerId
                == manufacturerId)
            .Select(product =>
                new ProductLookup(
                    product.Id,
                    product.Article.Value))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private static Dictionary<string, Guid[]>
        BuildArticleIndex(
            ProductLookup[] products)
    {
        return products
            .GroupBy(
                product =>
                    NormalizeArticle(
                        product.Article),
                StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(product =>
                        product.Id)
                    .Distinct()
                    .ToArray(),
                StringComparer.Ordinal);
    }

    private static string NormalizeArticle(
        string article)
    {
        return article
            .Trim()
            .ToUpperInvariant()
            .Replace(
                "Ё",
                "Е",
                StringComparison.Ordinal);
    }

    private async Task MarkFailedAsync(
        Guid priceListId,
        string failureReason,
        CancellationToken cancellationToken)
    {
        _dbContext.ChangeTracker.Clear();

        var priceList =
            await _dbContext.CatalogPriceLists
                .SingleOrDefaultAsync(
                    item => item.Id == priceListId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (priceList is null)
        {
            return;
        }

        var failureResult =
            priceList.MarkFailed(
                failureReason);

        if (failureResult.IsFailure)
        {
            return;
        }

        await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    private sealed record ProductLookup(
        Guid Id,
        string Article);

    private sealed record CatalogPriceListBatchSaveResult(
        int SavedRowsCount,
        int ValidRowsCount,
        int ErrorRowsCount);
}
