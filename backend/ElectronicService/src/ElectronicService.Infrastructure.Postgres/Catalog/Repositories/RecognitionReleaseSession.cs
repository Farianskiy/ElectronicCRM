using System.Data;
using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.Recognition.Evaluation;
using ElectronicService.Domain.Common;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Repositories;

public sealed class RecognitionReleaseSession(ElectronicDbContext db, RecognitionEvaluationAccess access) : IRecognitionReleaseSession
{
    public Task<Result<Guid, DomainError>> AuthorizeAsync(CancellationToken ct) => access.AuthorizeAsync(ct);
    public async Task<IRecognitionReleaseTransaction> BeginAsync(CancellationToken ct) =>
        new ReleaseTransaction(await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct).ConfigureAwait(false));
    private sealed class ReleaseTransaction(IDbContextTransaction transaction) : IRecognitionReleaseTransaction
    {
        public Task CommitAsync(CancellationToken ct) => transaction.CommitAsync(ct);
        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
