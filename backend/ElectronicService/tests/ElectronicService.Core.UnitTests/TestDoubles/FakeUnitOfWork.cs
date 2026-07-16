using ElectronicService.Core.Abstractions.Data;

namespace ElectronicService.Core.UnitTests.TestDoubles;

/// <summary>
/// Фиксирует вызовы SaveChangesAsync.
/// Благодаря этому тест может доказать, что успешный handler сохраняет изменения,
/// а ошибочный сценарий не обращается к базе данных.
/// </summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallsCount { get; private set; }

    public int SaveChangesResult { get; set; } = 1;

    public CancellationToken LastCancellationToken { get; private set; }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SaveChangesCallsCount++;
        LastCancellationToken = cancellationToken;

        return Task.FromResult(SaveChangesResult);
    }
}
