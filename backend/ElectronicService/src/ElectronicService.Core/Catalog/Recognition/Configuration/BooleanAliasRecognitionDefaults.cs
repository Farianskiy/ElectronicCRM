namespace ElectronicService.Core.Catalog.Recognition.Configuration;

public static class BooleanAliasRecognitionDefaults
{
    public static BooleanAliasRecognitionSettings ThermalRelease { get; } = new(
        Array.AsReadOnly(
            new[]
            {
                "с ТР",
                "с тепловым расцепителем",
                "тепловой расцепитель"
            }),
        Array.AsReadOnly(
            new[]
            {
                "без ТР",
                "нет ТР",
                "без теплового расцепителя"
            }));
}