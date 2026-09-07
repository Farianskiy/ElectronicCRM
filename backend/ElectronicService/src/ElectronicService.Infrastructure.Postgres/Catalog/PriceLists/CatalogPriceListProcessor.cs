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

    private const decimal ExactNameMatchConfidencePercent = 95m;

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

        var nameIndex =
            BuildNameIndex(productLookups);

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
                        nameIndex,
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

            savedRowsCount += sourceRows.Count;

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
                        nameIndex,
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
                readingResult.Value.RowsCount,
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
                    readingResult.Value.RowsCount,
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
                Dictionary<string, Guid[]> nameIndex,
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
                articleIndex,
                nameIndex);

            if (row.Status == CatalogPriceListRowStatus.Valid)
            {
                validRowsCount++;
            }
            else
            {
                errorRowsCount++;
            }

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
                    validRowsCount,
                    errorRowsCount));
    }

    private static void MatchRow(
        CatalogPriceListRow row,
        Dictionary<string, Guid[]> articleIndex,
        Dictionary<string, Guid[]> nameIndex)
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

        if (nameIndex.TryGetValue(
                row.NormalizedName,
                out var nameMatches))
        {
            ApplyNameMatches(
                row,
                nameMatches);

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

    private static void ApplyNameMatches(
        CatalogPriceListRow row,
        Guid[] productIds)
    {
        if (productIds.Length == 1)
        {
            var matchResult =
                row.MarkMatchedByName(
                    productIds[0],
                    ExactNameMatchConfidencePercent);

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
                    product.Article.Value,
                    product.Name.NormalizedValue))
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

    private static Dictionary<string, Guid[]>
        BuildNameIndex(
            ProductLookup[] products)
    {
        return products
            .GroupBy(
                product =>
                    product.NormalizedName,
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
        string Article,
        string NormalizedName);

    private sealed record CatalogPriceListBatchSaveResult(
        int ValidRowsCount,
        int ErrorRowsCount);
}