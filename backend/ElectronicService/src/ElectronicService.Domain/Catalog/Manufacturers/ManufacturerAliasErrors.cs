using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Manufacturers;

public static class ManufacturerAliasErrors
{
    public static DomainError StatusIsInvalid(ManufacturerAliasStatus status)
    {
        return new DomainError(
            "catalog.manufacturer_alias.status_invalid",
            $"Статус псевдонима производителя '{status}' некорректен.");
    }

    public static DomainError SourceIsInvalid(ManufacturerAliasSource source)
    {
        return new DomainError(
            "catalog.manufacturer_alias.source_invalid",
            $"Источник псевдонима производителя '{source}' некорректен.");
    }

    public static DomainError RejectedAliasCannotBeApproved(Guid manufacturerAliasId)
    {
        return new DomainError(
            "catalog.manufacturer_alias.rejected_cannot_be_approved",
            $"Отклонённый псевдоним производителя '{manufacturerAliasId}' нельзя подтвердить.");
    }

    public static DomainError ApprovedAliasCannotBeRejected(Guid manufacturerAliasId)
    {
        return new DomainError(
            "catalog.manufacturer_alias.approved_cannot_be_rejected",
            $"Подтверждённый псевдоним производителя '{manufacturerAliasId}' нельзя отклонить.");
    }

    public static DomainError AliasAlreadyExists(string normalizedPhrase)
    {
        return new DomainError(
            "catalog.manufacturer_alias.already_exists",
            $"Псевдоним производителя '{normalizedPhrase}' уже существует.");
    }

    public static DomainError AliasNotFound(Guid manufacturerAliasId)
    {
        return new DomainError(
            "catalog.manufacturer_alias.not_found",
            $"Псевдоним производителя '{manufacturerAliasId}' не найден.");
    }

    public static DomainError AliasConflictsWithManufacturerName(string normalizedPhrase)
    {
        return new DomainError(
            "catalog.manufacturer_alias.conflicts_with_manufacturer_name",
            $"Псевдоним производителя '{normalizedPhrase}' совпадает с каноническим именем существующего производителя.");
    }

    public static DomainError AliasConflictsWithNoisePhrase(string normalizedPhrase)
    {
        return new DomainError(
            "catalog.manufacturer_alias.conflicts_with_noise_phrase",
            $"Псевдоним производителя '{normalizedPhrase}' нельзя создать, потому что эта фраза помечена Technical-пользователем как активный шум.");
    }
}