using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Assistant.Abstractions;
using ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.GetProducts;
using ElectronicService.Core.Catalog.Products.SearchProducts;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Assistant.PreviewCatalogAssistantBatch;

public sealed record PreviewCatalogAssistantBatchCommand(string Message, bool OnlyInStock, int MatchesPerLine);

public sealed record CatalogAssistantBatchLineResult(int LineNumber, string SourceText, string SearchText, decimal? Quantity, string Status, string Message, CatalogAssistantParsedRequest? ParsedRequest, IReadOnlyCollection<CatalogProductListItemResult> Products);

public sealed record PreviewCatalogAssistantBatchResult(string? CommonText, int TotalLines, int MatchedLines, int RequiresAttentionLines, IReadOnlyCollection<CatalogAssistantBatchLineResult> Lines);

public sealed class PreviewCatalogAssistantBatchCommandHandler
{
    private readonly ICatalogAssistantMessageParser _messageParser;
    private readonly ICatalogProductsReader _catalogProductsReader;

    public PreviewCatalogAssistantBatchCommandHandler(ICatalogAssistantMessageParser messageParser, ICatalogProductsReader catalogProductsReader)
    {
        _messageParser = messageParser;
        _catalogProductsReader = catalogProductsReader;
    }

    public async Task<Result<PreviewCatalogAssistantBatchResult, DomainError>> Handle(PreviewCatalogAssistantBatchCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Message))
        {
            return GeneralErrors.ValueIsInvalid(nameof(command.Message));
        }

        if (command.MatchesPerLine is < 1 or > 20)
        {
            return GeneralErrors.ValueIsInvalid(nameof(command.MatchesPerLine));
        }

        var input = CatalogAssistantBatchMessageSplitter.Split(command.Message);

        if (input.Lines.Count == 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(command.Message));
        }

        var lineResults = new List<CatalogAssistantBatchLineResult>(input.Lines.Count);

        foreach (var line in input.Lines)
        {
            lineResults.Add(await PreviewLineAsync(line, command, cancellationToken).ConfigureAwait(false));
        }

        var matchedLines = lineResults.Count(line => string.Equals(line.Status, "Matched", StringComparison.Ordinal));

        return new PreviewCatalogAssistantBatchResult(input.CommonText, lineResults.Count, matchedLines, lineResults.Count - matchedLines, lineResults);
    }

    private async Task<CatalogAssistantBatchLineResult> PreviewLineAsync(CatalogAssistantBatchLineInput line, PreviewCatalogAssistantBatchCommand command, CancellationToken cancellationToken)
    {

        var parsedRequest = await _messageParser.ParseAsync(line.SearchText, null, cancellationToken).ConfigureAwait(false);

        if (parsedRequest.Clarification is not null)
        {
            return new CatalogAssistantBatchLineResult(line.LineNumber, line.SourceText, line.SearchText, line.Quantity, "NeedsClarification", parsedRequest.Clarification.Question, parsedRequest, []);
        }

        var products = await _catalogProductsReader.SearchProductsAsync(new SearchProductsQuery(parsedRequest.Search, parsedRequest.ProductTypeCode, parsedRequest.Manufacturer, parsedRequest.Characteristics, 1, command.MatchesPerLine, command.OnlyInStock ? true : null), cancellationToken).ConfigureAwait(false);

        if (line.Quantity is null or <= 0)
        {
            var missingQuantityMessage = products.TotalCount == 0
                ? "Подходящий товар не найден. Также укажите количество, например «4 шт»."
                : $"Найдено вариантов: {products.TotalCount}. Укажите количество, например «4 шт», затем повторите поиск.";

            return new CatalogAssistantBatchLineResult(line.LineNumber, line.SourceText, line.SearchText, line.Quantity, "MissingQuantity", missingQuantityMessage, parsedRequest, products.Items);
        }

        var status = products.TotalCount switch
        {
            0 => "NotFound",
            1 => "Matched",
            _ => "MultipleMatches"
        };

        var message = products.TotalCount switch
        {
            0 => "Подходящий товар не найден.",
            1 => "Товар сопоставлен.",
            _ => $"Найдено вариантов: {products.TotalCount}. Выберите нужный товар."
        };

        return new CatalogAssistantBatchLineResult(line.LineNumber, line.SourceText, line.SearchText, line.Quantity, status, message, parsedRequest, products.Items);
    }
}