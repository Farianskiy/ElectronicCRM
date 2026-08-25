namespace ElectronicService.Domain.Catalog.Dictionaries;

public enum CatalogDictionarySuggestionSource
{
    None = 0,

    Assistant = 1,

    ImportRecognition = 2,

    UserCorrection = 3,

    RecognitionLearning = 4,

    MlRecognition = 5
}