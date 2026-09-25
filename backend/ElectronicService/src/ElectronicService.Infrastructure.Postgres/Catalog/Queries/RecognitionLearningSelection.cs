using ElectronicService.Domain.Catalog.Recognition;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Queries;

internal static class RecognitionLearningSelection
{
    public static IQueryable<CatalogRecognitionTrainingExample> ConfirmedExamples(
        this IQueryable<CatalogRecognitionTrainingExample> query, IQueryable<CatalogRecognitionFeedback> feedback) =>
        query.Where(x => x.RevokedAtUtc == null && feedback.Any(f => f.Id == x.SourceFeedbackId && f.ExcludedAtUtc == null));

    public static IQueryable<CatalogRecognitionTrainingExample> InScope(
        this IQueryable<CatalogRecognitionTrainingExample> query, Guid? manufacturerId, Guid? productTypeId, Guid? characteristicId)
    {
        if (manufacturerId.HasValue) query = query.Where(x => x.ManufacturerId == manufacturerId.Value);
        if (productTypeId.HasValue) query = query.Where(x => x.ProductTypeId == productTypeId.Value);
        if (characteristicId.HasValue) query = query.Where(x => x.CharacteristicDefinitionId == characteristicId.Value);
        return query;
    }

    public static IQueryable<CatalogRecognitionFeedback> ReviewedFeedback(
        this IQueryable<CatalogRecognitionFeedback> query, DateTime cutoff) => query.Where(x =>
            x.ExcludedAtUtc == null && x.Status == CatalogRecognitionFeedbackStatus.Finalized && x.IsTrainingEligible &&
            x.FinalizedAtUtc.HasValue && x.FinalizedAtUtc.Value <= cutoff);
}
