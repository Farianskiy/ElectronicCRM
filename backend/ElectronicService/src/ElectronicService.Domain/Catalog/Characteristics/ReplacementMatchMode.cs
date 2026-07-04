namespace ElectronicService.Domain.Catalog.Characteristics;

public enum ReplacementMatchMode
{
    None = 0,

    // Значение должно совпадать точно.
    Exact = 1,

    // Значение у замены должно быть больше или равно.
    GreaterOrEqual = 2,

    // Значение у замены должно быть меньше или равно.
    LessOrEqual = 3,

    // Значение может быть близким, например габариты шкафа.
    Near = 4,

    // Совместимое значение, например IP31 можно заменить на IP54.
    CompatibleOrHigher = 5,

    // Характеристика желательная, но не обязательная.
    Optional = 6
}