using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.ImportBatches.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.ImportBatches;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.ImportBatches.CreateCatalogImportBatch;

public sealed class
    CreateCatalogImportBatchCommandHandler
{
    private const int BufferSize =
        81_920;

    private readonly ICatalogImportBatchRepository
        _importBatchRepository;

    private readonly IUserRepository
        _userRepository;

    public CreateCatalogImportBatchCommandHandler(
        ICatalogImportBatchRepository
            importBatchRepository,
        IUserRepository userRepository)
    {
        _importBatchRepository =
            importBatchRepository;

        _userRepository =
            userRepository;
    }

    public async Task<Result<
        CreateCatalogImportBatchResult,
        DomainError>> Handle(
            CreateCatalogImportBatchCommand command,
            CancellationToken cancellationToken =
                default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.CreatedByUserId == Guid.Empty)
        {
            return Result.Failure<
                CreateCatalogImportBatchResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(
                            command.CreatedByUserId)));
        }

        if (!command.FileStream.CanRead)
        {
            return Result.Failure<
                CreateCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors.FileCannotBeRead());
        }

        /*
         * Не доверяем одной только роли из JWT.
         *
         * Загружаем актуального пользователя
         * из базы и проверяем доменное permission.
         */
        var currentUser =
            await _userRepository
                .GetByIdAsync(
                    command.CreatedByUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<
                CreateCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .CurrentUserNotFound());
        }

        if (!currentUser
                .CanCreateCatalogImport())
        {
            return Result.Failure<
                CreateCatalogImportBatchResult,
                DomainError>(
                    CatalogImportErrors
                        .UserCannotCreateCatalogImport());
        }

        /*
         * Читаем поток с жёстким ограничением.
         *
         * Нельзя просто вызвать CopyToAsync,
         * а потом проверить Length:
         * слишком большой файл уже оказался бы
         * полностью загружен в память.
         */
        var contentResult =
            await ReadContentAsync(
                    command.FileStream,
                    cancellationToken)
                .ConfigureAwait(false);

        if (contentResult.IsFailure)
        {
            return Result.Failure<
                CreateCatalogImportBatchResult,
                DomainError>(
                    contentResult.Error);
        }

        var batchResult =
            CatalogImportBatch.Create(
                command.CreatedByUserId,
                command.FileName,
                command.ContentType,
                contentResult.Value);

        if (batchResult.IsFailure)
        {
            return Result.Failure<
                CreateCatalogImportBatchResult,
                DomainError>(
                    batchResult.Error);
        }

        var batch = batchResult.Value;

        _importBatchRepository.Add(batch);

        /*
         * Batch и исходный Excel сохраняются
         * одной транзакцией.
         *
         * Частичного состояния:
         *
         * batch без файла
         * или
         * файл без batch
         *
         * возникнуть не должно.
         */
        await _importBatchRepository
            .SaveChangesAsync(
                cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<
            CreateCatalogImportBatchResult,
            DomainError>(
                new CreateCatalogImportBatchResult(
                    batch.Id,
                    batch.Status));
    }

    private static async Task<Result<
        byte[],
        DomainError>> ReadContentAsync(
            Stream fileStream,
            CancellationToken cancellationToken)
    {
        /*
         * Новый поток из IFormFile начинается
         * с позиции 0. Эта проверка также делает
         * handler удобнее для unit-тестов.
         */
        if (fileStream.CanSeek)
        {
            fileStream.Position = 0;
        }

        using var output =
            new MemoryStream();

        var buffer =
            new byte[BufferSize];

        while (true)
        {
            var bytesRead =
                await fileStream
                    .ReadAsync(
                        buffer.AsMemory(
                            0,
                            buffer.Length),
                        cancellationToken)
                    .ConfigureAwait(false);

            if (bytesRead == 0)
            {
                break;
            }

            if (output.Length + bytesRead
                > CatalogImportBatch
                    .MaximumFileSizeBytes)
            {
                return Result.Failure<
                    byte[],
                    DomainError>(
                        CatalogImportErrors
                            .FileIsTooLarge(
                                CatalogImportBatch
                                    .MaximumFileSizeBytes));
            }

            await output
                .WriteAsync(
                    buffer.AsMemory(
                        0,
                        bytesRead),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return Result.Success<
            byte[],
            DomainError>(
                output.ToArray());
    }
}