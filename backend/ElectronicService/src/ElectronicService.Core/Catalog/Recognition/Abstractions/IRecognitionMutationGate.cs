namespace ElectronicService.Core.Catalog.Recognition.Abstractions;

public interface IRecognitionMutationGate
{
    Task<IAsyncDisposable> EnterAsync(CancellationToken cancellationToken);
}
