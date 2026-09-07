using CSharpFunctionalExtensions;
using ElectronicService.Core.Abstractions.Data;
using ElectronicService.Core.Catalog.Manufacturers.Abstractions;
using ElectronicService.Core.Catalog.PriceLists.Abstractions;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.Errors;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.UploadCatalogPriceList;

public sealed class UploadCatalogPriceListCommandHandler
{
    private const int BufferSize = 81_920;

    private readonly ICatalogPriceListRepository _priceListRepository;

    private readonly IManufacturerRepository _manufacturerRepository;

    private readonly IUserRepository _userRepository;

    private readonly IUnitOfWork _unitOfWork;

    public UploadCatalogPriceListCommandHandler(
        ICatalogPriceListRepository priceListRepository,
        IManufacturerRepository manufacturerRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(priceListRepository);
        ArgumentNullException.ThrowIfNull(manufacturerRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _priceListRepository = priceListRepository;
        _manufacturerRepository = manufacturerRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<
        UploadCatalogPriceListResult,
        DomainError>> Handle(
            UploadCatalogPriceListCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.ManufacturerId == Guid.Empty)
        {
            return Result.Failure<
                UploadCatalogPriceListResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.ManufacturerId)));
        }

        if (command.CreatedByUserId == Guid.Empty)
        {
            return Result.Failure<
                UploadCatalogPriceListResult,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (!command.FileStream.CanRead)
        {
            return Result.Failure<
                UploadCatalogPriceListResult,
                DomainError>(
                    CatalogPriceListErrors.FileCannotBeRead());
        }

        var currentUser =
            await _userRepository
                .GetByIdAsync(
                    command.CreatedByUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<
                UploadCatalogPriceListResult,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanManageCatalogPriceLists())
        {
            return Result.Failure<
                UploadCatalogPriceListResult,
                DomainError>(
                    CatalogPriceListErrors.UserCannotUploadPriceList());
        }

        var manufacturerExists =
            await _manufacturerRepository
                .ExistsByIdAsync(
                    command.ManufacturerId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (!manufacturerExists)
        {
            return Result.Failure<
                UploadCatalogPriceListResult,
                DomainError>(
                    CatalogErrors.ManufacturerNotFound(
                        command.ManufacturerId));
        }

        var contentResult =
            await ReadContentAsync(
                    command.FileStream,
                    cancellationToken)
                .ConfigureAwait(false);

        if (contentResult.IsFailure)
        {
            return Result.Failure<
                UploadCatalogPriceListResult,
                DomainError>(
                    contentResult.Error);
        }

        var priceListResult =
            CatalogPriceList.Create(
                command.ManufacturerId,
                command.CreatedByUserId,
                command.FileName,
                command.ContentType,
                contentResult.Value);

        if (priceListResult.IsFailure)
        {
            return Result.Failure<
                UploadCatalogPriceListResult,
                DomainError>(
                    priceListResult.Error);
        }

        var priceList =
            priceListResult.Value;

        _priceListRepository.Add(priceList);

        await _unitOfWork
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return Result.Success<
            UploadCatalogPriceListResult,
            DomainError>(
                new UploadCatalogPriceListResult(
                    priceList.Id,
                    priceList.ManufacturerId,
                    priceList.OriginalFileName,
                    priceList.FileSizeBytes,
                    priceList.Status));
    }

    private static async Task<Result<
        byte[],
        DomainError>> ReadContentAsync(
            Stream fileStream,
            CancellationToken cancellationToken)
    {
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
                > CatalogPriceList.MaximumFileSizeBytes)
            {
                return Result.Failure<
                    byte[],
                    DomainError>(
                        CatalogPriceListErrors.FileIsTooLarge(
                            CatalogPriceList.MaximumFileSizeBytes));
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