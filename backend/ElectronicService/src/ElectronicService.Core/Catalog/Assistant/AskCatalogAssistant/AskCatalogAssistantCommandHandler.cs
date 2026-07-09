using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Assistant.Abstractions;
using ElectronicService.Core.Catalog.Products.Abstractions;
using ElectronicService.Core.Catalog.Products.GetReplacements;
using ElectronicService.Core.Catalog.Products.SearchProducts;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;

public sealed class AskCatalogAssistantCommandHandler
{
    private const int SourceProductPage = 1;
    private const int SourceProductPageSize = 1;

    private readonly ICatalogAssistantMessageParser _messageParser;
    private readonly ICatalogProductsReader _catalogProductsReader;
    private readonly ICatalogProductReplacementsReader _replacementsReader;

    public AskCatalogAssistantCommandHandler(
        ICatalogAssistantMessageParser messageParser,
        ICatalogProductsReader catalogProductsReader,
        ICatalogProductReplacementsReader replacementsReader)
    {
        _messageParser = messageParser;
        _catalogProductsReader = catalogProductsReader;
        _replacementsReader = replacementsReader;
    }

    public async Task<Result<CatalogAssistantResult, DomainError>> Handle(
    AskCatalogAssistantCommand command,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Message))
        {
            return GeneralErrors.ValueIsInvalid(nameof(command.Message));
        }

        var parsedRequest = await _messageParser
            .ParseAsync(command.Message, cancellationToken)
            .ConfigureAwait(false);

        if (parsedRequest.Clarification is not null)
        {
            return new CatalogAssistantResult(
                parsedRequest.Intent,
                parsedRequest.Clarification.Question,
                parsedRequest,
                [],
                null,
                []);
        }

        return parsedRequest.Intent switch
        {
            CatalogAssistantIntent.SearchReplacements => await SearchReplacementsAsync(
                parsedRequest,
                command,
                cancellationToken).ConfigureAwait(false),

            CatalogAssistantIntent.SearchProducts => await SearchProductsAsync(
                parsedRequest,
                command,
                cancellationToken).ConfigureAwait(false),

            _ => new CatalogAssistantResult(
                CatalogAssistantIntent.Unknown,
                "Не удалось понять запрос. Попробуйте написать, какой товар нужно найти или для какого товара подобрать замену.",
                parsedRequest,
                [],
                null,
                [])
        };
    }

    private async Task<CatalogAssistantResult> SearchProductsAsync(
        CatalogAssistantParsedRequest parsedRequest,
        AskCatalogAssistantCommand command,
        CancellationToken cancellationToken)
    {
        var products = await _catalogProductsReader
            .SearchProductsAsync(
                new SearchProductsQuery(
                    parsedRequest.Search,
                    parsedRequest.ProductTypeCode,
                    parsedRequest.Manufacturer,
                    parsedRequest.Characteristics,
                    command.Page,
                    command.PageSize),
                cancellationToken)
            .ConfigureAwait(false);

        var answer = products.TotalCount == 0
            ? "Товары по запросу не найдены."
            : $"Найдено товаров: {products.TotalCount}.";

        return new CatalogAssistantResult(
            CatalogAssistantIntent.SearchProducts,
            answer,
            parsedRequest,
            products.Items,
            null,
            []);
    }

    private async Task<CatalogAssistantResult> SearchReplacementsAsync(
        CatalogAssistantParsedRequest parsedRequest,
        AskCatalogAssistantCommand command,
        CancellationToken cancellationToken)
    {
        var sourceProducts = await _catalogProductsReader
            .SearchProductsAsync(
                new SearchProductsQuery(
                    parsedRequest.Search,
                    parsedRequest.ProductTypeCode,
                    parsedRequest.Manufacturer,
                    parsedRequest.Characteristics,
                    SourceProductPage,
                    SourceProductPageSize),
                cancellationToken)
            .ConfigureAwait(false);

        var sourceProduct = sourceProducts.Items.FirstOrDefault();

        if (sourceProduct is null)
        {
            return new CatalogAssistantResult(
                CatalogAssistantIntent.SearchReplacements,
                "Исходный товар для подбора замен не найден.",
                parsedRequest,
                [],
                null,
                []);
        }

        var replacements = await _replacementsReader
            .GetReplacementsAsync(
                new GetProductReplacementsQuery(
                    sourceProduct.Id,
                    command.OnlyInStock,
                    command.MinimumScore,
                    command.Page,
                    command.PageSize),
                cancellationToken)
            .ConfigureAwait(false);

        if (replacements is null || replacements.TotalCount == 0)
        {
            return new CatalogAssistantResult(
                CatalogAssistantIntent.SearchReplacements,
                "Исходный товар найден, но подходящие замены не найдены.",
                parsedRequest,
                [],
                sourceProduct,
                []);
        }

        return new CatalogAssistantResult(
            CatalogAssistantIntent.SearchReplacements,
            $"Найдено замен: {replacements.TotalCount}.",
            parsedRequest,
            [],
            sourceProduct,
            replacements.Items);
    }
}