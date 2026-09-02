using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Manufacturers;

public static class ManufacturerErrors
{
    public static DomainError ManufacturerAlreadyExists(string normalizedName)
    {
        return new DomainError(
            "catalog.manufacturer.already_exists",
            $"Производитель с нормализованным именем '{normalizedName}' уже существует.");
    }

    public static DomainError ManufacturerNameConflictsWithAlias(string normalizedName)
    {
        return new DomainError(
            "catalog.manufacturer.name_conflicts_with_alias",
            $"Каноническое имя производителя '{normalizedName}' уже используется как активный псевдоним другого производителя.");
    }

    public static DomainError ManufacturerNameConflictsWithNoisePhrase(string normalizedName)
    {
        return new DomainError(
            "catalog.manufacturer.name_conflicts_with_noise_phrase",
            $"Производителя с именем '{normalizedName}' нельзя создать, потому что эта фраза помечена Technical-пользователем как активный шум.");
    }
}