using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceCalculations.GetCatalogPriceCalculation;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceCalculations.ExportCatalogPriceCalculation;

public sealed class ExportCatalogPriceCalculationQueryHandler
{
    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private readonly GetCatalogPriceCalculationQueryHandler _calculationQueryHandler;
    private readonly ICatalogPriceCalculationWorkbookExporter _workbookExporter;

    public ExportCatalogPriceCalculationQueryHandler(GetCatalogPriceCalculationQueryHandler calculationQueryHandler, ICatalogPriceCalculationWorkbookExporter workbookExporter)
    {
        ArgumentNullException.ThrowIfNull(calculationQueryHandler);
        ArgumentNullException.ThrowIfNull(workbookExporter);
        _calculationQueryHandler = calculationQueryHandler;
        _workbookExporter = workbookExporter;
    }

    public async Task<Result<ExportCatalogPriceCalculationResult, DomainError>> Handle(ExportCatalogPriceCalculationQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var calculationResult = await _calculationQueryHandler.Handle(
            new GetCatalogPriceCalculationQuery(query.CalculationId, query.CurrentUserId),
            cancellationToken).ConfigureAwait(false);

        if (calculationResult.IsFailure)
        {
            return Result.Failure<ExportCatalogPriceCalculationResult, DomainError>(calculationResult.Error);
        }

        var content = _workbookExporter.Export(calculationResult.Value, DateTime.UtcNow);
        var fileName = $"price-calculation-{query.CalculationId:N}.xlsx";

        return Result.Success<ExportCatalogPriceCalculationResult, DomainError>(
            new ExportCatalogPriceCalculationResult(content, ExcelContentType, fileName));
    }
}