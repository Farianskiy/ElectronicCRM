using System.Text.Json;
using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.PriceLists;

public sealed class CatalogPriceListRow : ElectronicService.Domain.Abstractions.Entity
{
    public const int MaximumArticleLength = 100;

    public const int MinimumNameLength = 2;

    public const int MaximumNameLength = 500;

    public const int MaximumProductUrlLength = 2_048;

    public const int MaximumUnitLength = 50;

    private const string ArticleRequiredIssueCode = "article.required";

    private const string ArticleTooLongIssueCode = "article.too_long";

    private const string NameRequiredIssueCode = "name.required";

    private const string NameTooShortIssueCode = "name.too_short";

    private const string NameTooLongIssueCode = "name.too_long";

    private const string BasePriceRequiredIssueCode = "base_price.required";

    private const string BasePriceNegativeIssueCode = "base_price.negative";

    private const string MrcPriceNegativeIssueCode = "mrc_price.negative";

    private const string ProductUrlInvalidIssueCode = "product_url.invalid";

    private const string ProductUrlTooLongIssueCode = "product_url.too_long";

    private const string UnitTooLongIssueCode = "unit.too_long";

    private const string ProductNotFoundIssueCode = "product.not_found";

    private const string ProductAmbiguousIssueCode = "product.ambiguous";

    private static readonly JsonSerializerOptions IssuesSerializerOptions =
        new(JsonSerializerDefaults.Web);

    private CatalogPriceListRow(
        Guid id,
        Guid priceListId,
        int rowNumber,
        string article,
        string normalizedArticle,
        string name,
        string normalizedName,
        decimal? basePriceAmount,
        decimal? mrcPriceAmount,
        Uri? productUrl,
        string? unit,
        CatalogPriceListRowStatus status,
        string issuesJson)
        : base(id)
    {
        PriceListId = priceListId;
        RowNumber = rowNumber;
        Article = article;
        NormalizedArticle = normalizedArticle;
        Name = name;
        NormalizedName = normalizedName;
        BasePriceAmount = basePriceAmount;
        MrcPriceAmount = mrcPriceAmount;
        ProductUrl = productUrl;
        Unit = unit;
        Status = status;
        IssuesJson = issuesJson;
        MatchStatus = CatalogPriceListRowMatchStatus.Pending;
    }

    private CatalogPriceListRow()
    {
    }

    public Guid PriceListId
    {
        get;
        private set;
    }

    public int RowNumber
    {
        get;
        private set;
    }

    public string Article
    {
        get;
        private set;
    } = string.Empty;

    public string NormalizedArticle
    {
        get;
        private set;
    } = string.Empty;

    public string Name
    {
        get;
        private set;
    } = string.Empty;

    public string NormalizedName
    {
        get;
        private set;
    } = string.Empty;

    public decimal? BasePriceAmount
    {
        get;
        private set;
    }

    public decimal? MrcPriceAmount
    {
        get;
        private set;
    }

    public Uri? ProductUrl
    {
        get;
        private set;
    }

    public string? Unit
    {
        get;
        private set;
    }

    public CatalogPriceListRowStatus Status
    {
        get;
        private set;
    }

    public string IssuesJson
    {
        get;
        private set;
    } = "[]";

    public Guid? ProductId
    {
        get;
        private set;
    }

    public CatalogPriceListRowMatchStatus MatchStatus
    {
        get;
        private set;
    }

    public decimal? MatchConfidencePercent
    {
        get;
        private set;
    }

    public bool IsMatched =>
        ProductId.HasValue
        && MatchStatus is
            CatalogPriceListRowMatchStatus.MatchedByArticle
            or CatalogPriceListRowMatchStatus.MatchedByName
            or CatalogPriceListRowMatchStatus.MatchedManually;

    public bool HasErrors =>
        Status == CatalogPriceListRowStatus.Error;

    public static Result<CatalogPriceListRow, DomainError> Create(
        Guid priceListId,
        int rowNumber,
        string article,
        string name,
        decimal? basePriceAmount,
        decimal? mrcPriceAmount,
        string? productLink,
        string? unit)
    {
        if (priceListId == Guid.Empty)
        {
            return GeneralErrors.ValueIsInvalid(nameof(priceListId));
        }

        if (rowNumber <= 0)
        {
            return GeneralErrors.ValueIsInvalid(nameof(rowNumber));
        }

        var issues =
            new List<CatalogPriceListRowIssue>();

        var sourceArticle =
            string.IsNullOrWhiteSpace(article)
                ? string.Empty
                : article.Trim();

        var normalizedArticle =
            string.Empty;

        if (sourceArticle.Length == 0)
        {
            issues.Add(
                new CatalogPriceListRowIssue(
                    ArticleRequiredIssueCode,
                    nameof(Article),
                    "В строке прайса не указан артикул."));
        }
        else if (sourceArticle.Length > MaximumArticleLength)
        {
            issues.Add(
                new CatalogPriceListRowIssue(
                    ArticleTooLongIssueCode,
                    nameof(Article),
                    $"Артикул содержит больше {MaximumArticleLength} символов."));
        }
        else
        {
            normalizedArticle =
                NormalizeArticle(sourceArticle);
        }

        var sourceName =
            string.IsNullOrWhiteSpace(name)
                ? string.Empty
                : name.Trim();

        var normalizedName =
            string.Empty;

        if (sourceName.Length == 0)
        {
            issues.Add(
                new CatalogPriceListRowIssue(
                    NameRequiredIssueCode,
                    nameof(Name),
                    "В строке прайса не указано наименование."));
        }
        else if (sourceName.Length < MinimumNameLength)
        {
            issues.Add(
                new CatalogPriceListRowIssue(
                    NameTooShortIssueCode,
                    nameof(Name),
                    $"Наименование должно содержать не менее {MinimumNameLength} символов."));
        }
        else if (sourceName.Length > MaximumNameLength)
        {
            issues.Add(
                new CatalogPriceListRowIssue(
                    NameTooLongIssueCode,
                    nameof(Name),
                    $"Наименование содержит больше {MaximumNameLength} символов."));
        }
        else
        {
            normalizedName =
                NormalizeName(sourceName);
        }

        if (!basePriceAmount.HasValue)
        {
            issues.Add(
                new CatalogPriceListRowIssue(
                    BasePriceRequiredIssueCode,
                    nameof(BasePriceAmount),
                    "В строке прайса не указана базовая цена."));
        }
        else if (basePriceAmount.Value < 0m)
        {
            issues.Add(
                new CatalogPriceListRowIssue(
                    BasePriceNegativeIssueCode,
                    nameof(BasePriceAmount),
                    "Базовая цена не может быть отрицательной."));
        }

        if (mrcPriceAmount.HasValue
            && mrcPriceAmount.Value < 0m)
        {
            issues.Add(
                new CatalogPriceListRowIssue(
                    MrcPriceNegativeIssueCode,
                    nameof(MrcPriceAmount),
                    "МРЦ не может быть отрицательной."));
        }

        var normalizedProductLink =
            NormalizeOptionalValue(productLink);

        Uri? productUrl = null;

        if (normalizedProductLink is not null)
        {
            if (normalizedProductLink.Length > MaximumProductUrlLength)
            {
                issues.Add(
                    new CatalogPriceListRowIssue(
                        ProductUrlTooLongIssueCode,
                        nameof(ProductUrl),
                        $"Ссылка на товар содержит больше {MaximumProductUrlLength} символов."));
            }
            else if (!Uri.TryCreate(
                         normalizedProductLink,
                         UriKind.Absolute,
                         out var parsedProductUrl)
                     || !HasSupportedScheme(parsedProductUrl))
            {
                issues.Add(
                    new CatalogPriceListRowIssue(
                        ProductUrlInvalidIssueCode,
                        nameof(ProductUrl),
                        "Ссылка на товар должна быть абсолютной HTTP- или HTTPS-ссылкой."));
            }
            else
            {
                productUrl = parsedProductUrl;
            }
        }

        var normalizedUnit =
            NormalizeOptionalValue(unit);

        if (normalizedUnit is not null
            && normalizedUnit.Length > MaximumUnitLength)
        {
            issues.Add(
                new CatalogPriceListRowIssue(
                    UnitTooLongIssueCode,
                    nameof(Unit),
                    $"Единица измерения содержит больше {MaximumUnitLength} символов."));
        }

        var status =
            issues.Count == 0
                ? CatalogPriceListRowStatus.Pending
                : CatalogPriceListRowStatus.Error;

        var issuesJson =
            SerializeIssues(issues);

        return new CatalogPriceListRow(
            Guid.CreateVersion7(),
            priceListId,
            rowNumber,
            sourceArticle,
            normalizedArticle,
            sourceName,
            normalizedName,
            basePriceAmount,
            mrcPriceAmount,
            productUrl,
            normalizedUnit,
            status,
            issuesJson);
    }

    public IReadOnlyList<CatalogPriceListRowIssue>
    GetIssues()
    {
        return DeserializeIssues();
    }

    public UnitResult<DomainError> ApplyCorrection(
    string article,
    string name,
    decimal? basePriceAmount,
    decimal? mrcPriceAmount,
    string? productLink,
    string? unit,
    Guid? productId)
    {
        var correctedRowResult =
            Create(
                PriceListId,
                RowNumber,
                article,
                name,
                basePriceAmount,
                mrcPriceAmount,
                productLink,
                unit);

        if (correctedRowResult.IsFailure)
        {
            return UnitResult.Failure(
                correctedRowResult.Error);
        }

        var correctedRow =
            correctedRowResult.Value;

        Article = correctedRow.Article;
        NormalizedArticle =
            correctedRow.NormalizedArticle;
        Name = correctedRow.Name;
        NormalizedName =
            correctedRow.NormalizedName;
        BasePriceAmount =
            correctedRow.BasePriceAmount;
        MrcPriceAmount =
            correctedRow.MrcPriceAmount;
        ProductUrl =
            correctedRow.ProductUrl;
        Unit =
            correctedRow.Unit;
        Status =
            correctedRow.Status;
        IssuesJson =
            correctedRow.IssuesJson;
        ProductId = null;
        MatchStatus =
            CatalogPriceListRowMatchStatus.Pending;
        MatchConfidencePercent = null;

        if (!productId.HasValue)
        {
            MarkProductNotFound();

            return UnitResult.Success<DomainError>();
        }

        return MarkMatchedManually(
            productId.Value);
    }

    public UnitResult<DomainError> MarkMatchedByArticle(
        Guid productId)
    {
        var productValidationResult =
            ValidateProductId(productId);

        if (productValidationResult.IsFailure)
        {
            return productValidationResult;
        }

        ProductId = productId;
        MatchStatus =
            CatalogPriceListRowMatchStatus.MatchedByArticle;
        MatchConfidencePercent = 100m;

        RemoveMatchingIssuesAndRefreshStatus();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> MarkMatchedByName(
        Guid productId,
        decimal confidencePercent)
    {
        var productValidationResult =
            ValidateProductId(productId);

        if (productValidationResult.IsFailure)
        {
            return productValidationResult;
        }

        if (confidencePercent < 0m
            || confidencePercent > 100m)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.InvalidMatchConfidence());
        }

        ProductId = productId;
        MatchStatus =
            CatalogPriceListRowMatchStatus.MatchedByName;
        MatchConfidencePercent = confidencePercent;

        RemoveMatchingIssuesAndRefreshStatus();

        return UnitResult.Success<DomainError>();
    }

    public UnitResult<DomainError> MarkMatchedManually(
        Guid productId)
    {
        var productValidationResult =
            ValidateProductId(productId);

        if (productValidationResult.IsFailure)
        {
            return productValidationResult;
        }

        ProductId = productId;
        MatchStatus =
            CatalogPriceListRowMatchStatus.MatchedManually;
        MatchConfidencePercent = 100m;

        RemoveMatchingIssuesAndRefreshStatus();

        return UnitResult.Success<DomainError>();
    }

    public void MarkAmbiguous()
    {
        ProductId = null;
        MatchStatus =
            CatalogPriceListRowMatchStatus.Ambiguous;
        MatchConfidencePercent = null;

        RemoveMatchingIssues();

        AddIssue(
            new CatalogPriceListRowIssue(
                ProductAmbiguousIssueCode,
                nameof(ProductId),
                "По данным строки найдено несколько товаров. Необходимо выбрать товар вручную."));
    }

    public void MarkProductNotFound()
    {
        ProductId = null;
        MatchStatus =
            CatalogPriceListRowMatchStatus.ProductNotFound;
        MatchConfidencePercent = null;

        RemoveMatchingIssues();

        AddIssue(
            new CatalogPriceListRowIssue(
                ProductNotFoundIssueCode,
                nameof(ProductId),
                "Товар не найден ни по артикулу, ни по точному наименованию."));
    }

    public void ResetMatch()
    {
        ProductId = null;
        MatchStatus =
            CatalogPriceListRowMatchStatus.Pending;
        MatchConfidencePercent = null;

        RemoveMatchingIssuesAndRefreshStatus();
    }

    private static UnitResult<DomainError> ValidateProductId(
        Guid productId)
    {
        if (productId == Guid.Empty)
        {
            return UnitResult.Failure(
                CatalogPriceListErrors.ProductIdRequiredForMatch());
        }

        return UnitResult.Success<DomainError>();
    }

    private void AddIssue(
        CatalogPriceListRowIssue issue)
    {
        var issues =
            DeserializeIssues();

        var issueAlreadyExists =
            issues.Any(
                existingIssue =>
                    string.Equals(
                        existingIssue.Code,
                        issue.Code,
                        StringComparison.Ordinal)
                    && string.Equals(
                        existingIssue.Field,
                        issue.Field,
                        StringComparison.Ordinal));

        if (!issueAlreadyExists)
        {
            issues.Add(issue);
        }

        IssuesJson =
            SerializeIssues(issues);

        RefreshStatus(issues);
    }

    private void RemoveMatchingIssues()
    {
        var issues =
            DeserializeIssues();

        issues.RemoveAll(
            issue =>
                string.Equals(
                    issue.Code,
                    ProductNotFoundIssueCode,
                    StringComparison.Ordinal)
                || string.Equals(
                    issue.Code,
                    ProductAmbiguousIssueCode,
                    StringComparison.Ordinal));

        IssuesJson =
            SerializeIssues(issues);

        RefreshStatus(issues);
    }

    private void RemoveMatchingIssuesAndRefreshStatus()
    {
        RemoveMatchingIssues();
    }

    private void RefreshStatus(
        List<CatalogPriceListRowIssue> issues)
    {
        if (issues.Count > 0)
        {
            Status =
                CatalogPriceListRowStatus.Error;

            return;
        }

        Status =
            IsMatched
                ? CatalogPriceListRowStatus.Valid
                : CatalogPriceListRowStatus.Pending;
    }

    private List<CatalogPriceListRowIssue> DeserializeIssues()
    {
        if (string.IsNullOrWhiteSpace(IssuesJson))
        {
            return [];
        }

        return JsonSerializer.Deserialize<
                   List<CatalogPriceListRowIssue>>(
                   IssuesJson,
                   IssuesSerializerOptions)
               ?? [];
    }

    private static string SerializeIssues(
        List<CatalogPriceListRowIssue> issues)
    {
        return JsonSerializer.Serialize(
            issues,
            IssuesSerializerOptions);
    }

    private static bool HasSupportedScheme(
        Uri productUrl)
    {
        return productUrl.IsAbsoluteUri
               && (string.Equals(
                       productUrl.Scheme,
                       Uri.UriSchemeHttp,
                       StringComparison.OrdinalIgnoreCase)
                   || string.Equals(
                       productUrl.Scheme,
                       Uri.UriSchemeHttps,
                       StringComparison.OrdinalIgnoreCase));
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

    private static string NormalizeName(
        string name)
    {
        return name
            .Trim()
            .ToUpperInvariant()
            .Replace(
                "Ё",
                "Е",
                StringComparison.Ordinal);
    }

    private static string? NormalizeOptionalValue(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}