namespace ElectronicService.Core.Abstractions;

public interface ICurrentUserProvider
{
    Guid? UserId { get; }
}