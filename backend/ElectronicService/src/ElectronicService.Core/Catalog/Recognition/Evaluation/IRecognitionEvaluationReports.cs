using CSharpFunctionalExtensions;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.Recognition.Evaluation;

public interface IRecognitionEvaluationReports
{
    Task<Result<EvaluationPage, DomainError>> ReadAsync(Guid reportId, int page, bool replay, CancellationToken cancellationToken);
}
