using CSharpFunctionalExtensions;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Products
    .GetAuditHistory;

public sealed class
    GetProductAuditHistoryQueryHandler
{
    private const int MaximumPageSize = 100;

    private readonly IProductAuditHistoryReader
        _reader;

    public GetProductAuditHistoryQueryHandler(
        IProductAuditHistoryReader reader)
    {
        _reader = reader;
    }

    public async Task<Result<
        ProductAuditHistoryPageResult,
        DomainError>> Handle(
            GetProductAuditHistoryQuery query,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.ProductId == Guid.Empty)
        {
            return Result.Failure<
                ProductAuditHistoryPageResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.ProductId)));
        }

        if (query.PageNumber < 1)
        {
            return Result.Failure<
                ProductAuditHistoryPageResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.PageNumber)));
        }

        if (query.PageSize < 1
            || query.PageSize > MaximumPageSize)
        {
            return Result.Failure<
                ProductAuditHistoryPageResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(query.PageSize)));
        }

        var page = await _reader
            .ReadAsync(
                query.ProductId,
                query.PageNumber,
                query.PageSize,
                cancellationToken)
            .ConfigureAwait(false);

        if (page is null)
        {
            return Result.Failure<
                ProductAuditHistoryPageResult,
                DomainError>(
                    CatalogErrors.ProductNotFound(
                        query.ProductId.ToString()));
        }

        return Result.Success<
            ProductAuditHistoryPageResult,
            DomainError>(page);
    }
}