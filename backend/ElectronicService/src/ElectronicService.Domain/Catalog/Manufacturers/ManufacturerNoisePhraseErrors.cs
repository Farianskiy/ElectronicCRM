using ElectronicService.Domain.Common;

namespace ElectronicService.Domain.Catalog.Manufacturers;

public static class ManufacturerNoisePhraseErrors
{
    public static DomainError ConflictsWithManufacturerName(string normalizedPhrase)
    {
        return new DomainError(
            "catalog.manufacturer_noise_phrase.conflicts_with_manufacturer_name",
            $"Фразу '{normalizedPhrase}' нельзя пометить как шум, потому что она совпадает с каноническим именем существующего производителя.");
    }

    public static DomainError ConflictsWithManufacturerAlias(string normalizedPhrase)
    {
        return new DomainError(
            "catalog.manufacturer_noise_phrase.conflicts_with_manufacturer_alias",
            $"Фразу '{normalizedPhrase}' нельзя пометить как шум, потому что она уже используется как активный псевдоним производителя.");
    }
}