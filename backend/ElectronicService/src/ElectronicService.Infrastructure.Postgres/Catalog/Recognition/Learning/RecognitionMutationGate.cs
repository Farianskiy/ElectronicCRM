using ElectronicService.Core.Catalog.Recognition.Abstractions;
using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ElectronicService.Infrastructure.Postgres.Catalog.Recognition.Learning;

// The same DB session owns the application lock and any trigger-acquired transaction lock.
public sealed class RecognitionMutationGate(ElectronicDbContext db) : IRecognitionMutationGate
{
    public const long LockKey = 72639482022;
    private bool _entered;

    public async Task<IAsyncDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        if (_entered || db.Database.CurrentTransaction is not null || db.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Acquire the learning mutation gate once, before loading mutable state or opening a transaction.");
        await db.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        try
        {
            await using var command = new NpgsqlCommand("SELECT pg_advisory_lock(@key)", connection);
            command.Parameters.AddWithValue("key", LockKey);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            db.ChangeTracker.Clear();
            _entered = true;
            return new Release(this, connection);
        }
        catch
        {
            await db.Database.CloseConnectionAsync().ConfigureAwait(false);
            throw;
        }
    }

    private sealed class Release(RecognitionMutationGate owner, NpgsqlConnection connection) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            try
            {
                await using var command = new NpgsqlCommand("SELECT pg_advisory_unlock(@key)", connection);
                command.Parameters.AddWithValue("key", LockKey);
                await command.ExecuteNonQueryAsync(CancellationToken.None).ConfigureAwait(false);
            }
            finally
            {
                owner._entered = false;
                await owner.CloseAsync().ConfigureAwait(false);
            }
        }
    }

    private Task CloseAsync() => db.Database.CloseConnectionAsync();
}
