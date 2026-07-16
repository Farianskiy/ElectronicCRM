using ElectronicService.Core.Abstractions;

namespace ElectronicService.Core.UnitTests.TestDoubles;

/// <summary>
/// Предсказуемая реализация хеширования пароля.
/// Настоящая криптография здесь не нужна: тестируется orchestration handler,
/// а не алгоритм хеширования.
/// </summary>
internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string HashResult { get; set; } = "test-password-hash";

    public bool VerifyResult { get; set; } = true;

    public int HashCallsCount { get; private set; }

    public int VerifyCallsCount { get; private set; }

    public string? LastPasswordToHash { get; private set; }

    public string? LastPasswordToVerify { get; private set; }

    public string? LastPasswordHashToVerify { get; private set; }

    public string Hash(string password)
    {
        HashCallsCount++;
        LastPasswordToHash = password;

        return HashResult;
    }

    public bool Verify(string password, string passwordHash)
    {
        VerifyCallsCount++;
        LastPasswordToVerify = password;
        LastPasswordHashToVerify = passwordHash;

        return VerifyResult;
    }
}
