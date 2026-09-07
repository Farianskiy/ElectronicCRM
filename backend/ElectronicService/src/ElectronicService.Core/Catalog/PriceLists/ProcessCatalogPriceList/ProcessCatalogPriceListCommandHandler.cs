using CSharpFunctionalExtensions;
using ElectronicService.Core.Catalog.PriceLists.Import;
using ElectronicService.Core.Users;
using ElectronicService.Domain.Catalog.PriceLists;
using ElectronicService.Domain.Common;

namespace ElectronicService.Core.Catalog.PriceLists.ProcessCatalogPriceList;

public sealed class ProcessCatalogPriceListCommandHandler
{
    private readonly IUserRepository _userRepository;

    private readonly ICatalogPriceListProcessor _priceListProcessor;

    public ProcessCatalogPriceListCommandHandler(
        IUserRepository userRepository,
        ICatalogPriceListProcessor priceListProcessor)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(priceListProcessor);

        _userRepository = userRepository;
        _priceListProcessor = priceListProcessor;
    }

    public async Task<Result<
        CatalogPriceListProcessingResult,
        DomainError>> Handle(
            ProcessCatalogPriceListCommand command,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.PriceListId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceListProcessingResult,
                DomainError>(
                    GeneralErrors.ValueIsInvalid(
                        nameof(command.PriceListId)));
        }

        if (command.CurrentUserId == Guid.Empty)
        {
            return Result.Failure<
                CatalogPriceListProcessingResult,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        var currentUser =
            await _userRepository
                .GetByIdAsync(
                    command.CurrentUserId,
                    cancellationToken)
                .ConfigureAwait(false);

        if (currentUser is null)
        {
            return Result.Failure<
                CatalogPriceListProcessingResult,
                DomainError>(
                    CatalogPriceListErrors.CurrentUserNotFound());
        }

        if (!currentUser.CanManageCatalogPriceLists())
        {
            return Result.Failure<
                CatalogPriceListProcessingResult,
                DomainError>(
                    CatalogPriceListErrors.UserCannotProcessPriceList());
        }

        return await _priceListProcessor
            .ProcessAsync(
                command.PriceListId,
                cancellationToken)
            .ConfigureAwait(false);
    }
}