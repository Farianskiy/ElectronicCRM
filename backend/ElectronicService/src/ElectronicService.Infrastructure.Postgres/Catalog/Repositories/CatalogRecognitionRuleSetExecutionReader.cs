using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Core.Catalog.Recognition.Training;
using ElectronicService.Domain.Catalog.Recognition;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class CatalogRecognitionRuleSetExecutionReader
    : ICatalogRecognitionRuleSetExecutionReader
{
    private readonly ElectronicDbContext _dbContext;

    public CatalogRecognitionRuleSetExecutionReader(ElectronicDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        _dbContext = dbContext;
    }

    public async Task<Result<CatalogRecognitionRuleSetExecutionSnapshot, DomainError>> ReadAsync(
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        if (versionId == Guid.Empty)
        {
            return new DomainError("training.invalid_request", "Укажите версию правил.");
        }

        var version = await _dbContext.CatalogRecognitionRuleSetVersions
            .AsNoTracking()
            .Include(item => item.Entries)
            .SingleOrDefaultAsync(item => item.Id == versionId, cancellationToken)
            .ConfigureAwait(false);

        if (version is null)
        {
            return new DomainError("training.not_found", "Версия правил не найдена.");
        }

        if (version.Entries.Count == 0 || version.Entries.Count > 100)
        {
            return new DomainError("training.invalid_data", "Некорректный размер состава версии.");
        }

        var literalIds = new List<Guid>();
        var numericIds = new List<Guid>();
        var multiNumericIds = new List<Guid>();

        foreach (var entry in version.Entries)
        {
            switch (entry.Kind)
            {
                case CatalogRecognitionRuleKind.Literal
                    when entry.LiteralDraftId is Guid literalId &&
                         literalId != Guid.Empty &&
                         entry.IntegerDraftId is null &&
                         entry.MultiIntegerDraftId is null:
                    literalIds.Add(literalId);
                    break;

                case CatalogRecognitionRuleKind.NumericCapture
                    when entry.IntegerDraftId is Guid numericId &&
                         numericId != Guid.Empty &&
                         entry.LiteralDraftId is null &&
                         entry.MultiIntegerDraftId is null:
                    numericIds.Add(numericId);
                    break;

                case CatalogRecognitionRuleKind.MultipleNumericCaptures
                    when entry.MultiIntegerDraftId is Guid multiNumericId &&
                         multiNumericId != Guid.Empty &&
                         entry.LiteralDraftId is null &&
                         entry.IntegerDraftId is null:
                    multiNumericIds.Add(multiNumericId);
                    break;

                default:
                    return new DomainError(
                        "training.invalid_data",
                        "Состав версии содержит некорректную ссылку или неизвестный вид шаблона.");
            }
        }

        var literalDrafts = await _dbContext.CatalogRecognitionLiteralDrafts
            .AsNoTracking()
            .Where(draft =>
                literalIds.Contains(draft.Id) &&
                draft.ManufacturerId == version.ManufacturerId &&
                draft.ProductTypeId == version.ProductTypeId)
            .OrderBy(draft => draft.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var numericDrafts = await _dbContext.CatalogRecognitionIntegerDrafts
            .AsNoTracking()
            .Where(draft =>
                numericIds.Contains(draft.Id) &&
                draft.ManufacturerId == version.ManufacturerId &&
                draft.ProductTypeId == version.ProductTypeId)
            .Include(draft => draft.Suffixes)
            .OrderBy(draft => draft.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var multiNumericDrafts = await _dbContext.CatalogRecognitionMultiIntegerDrafts
            .AsNoTracking()
            .Where(draft =>
                multiNumericIds.Contains(draft.Id) &&
                draft.ManufacturerId == version.ManufacturerId &&
                draft.ProductTypeId == version.ProductTypeId)
            .Include(draft => draft.Parts)
            .OrderBy(draft => draft.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        if (literalDrafts.Length != literalIds.Count ||
            numericDrafts.Length != numericIds.Count ||
            multiNumericDrafts.Length != multiNumericIds.Count)
        {
            return new DomainError(
                "training.invalid_data",
                "Не удалось загрузить полный состав версии: есть повторные ссылки, отсутствующие черновики или несовпадение производителя и типа товара.");
        }

        var literalRules = literalDrafts
            .Select(draft => new CatalogRecognitionLiteralExecutionRule(
                draft.Id,
                draft.CharacteristicDefinitionId,
                draft.GeneratorVersion,
                draft.Literal,
                draft.NormalizedValue))
            .ToArray();

        var numericRules = numericDrafts
            .Select(draft => new CatalogRecognitionNumericExecutionRule(
                draft.Id,
                draft.CharacteristicDefinitionId,
                draft.GeneratorVersion,
                new CatalogRecognitionIntegerAlternativesPattern(
                    draft.Prefix,
                    draft.Suffixes
                        .OrderBy(suffix => suffix.Position)
                        .Select(suffix => suffix.Text)
                        .ToArray())))
            .ToArray();

        var multiNumericRules = multiNumericDrafts
            .Select(draft => new CatalogRecognitionMultiNumericExecutionRule(
                draft.Id,
                draft.GeneratorVersion,
                new CatalogRecognitionMultiIntegerPattern(
                    draft.Parts
                        .OrderBy(part => part.Position)
                        .Select(part => new CatalogRecognitionMultiIntegerPart(
                            part.Literal,
                            part.CharacteristicDefinitionId))
                        .ToArray())))
            .ToArray();

        return new CatalogRecognitionRuleSetExecutionSnapshot(
            version.Id,
            version.ManufacturerId,
            version.ProductTypeId,
            version.VersionNumber,
            literalRules,
            numericRules,
            multiNumericRules);
    }
}