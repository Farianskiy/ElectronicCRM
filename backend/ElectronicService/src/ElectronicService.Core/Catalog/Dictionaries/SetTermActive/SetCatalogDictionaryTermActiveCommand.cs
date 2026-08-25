namespace ElectronicService.Core.Catalog.Dictionaries.SetTermActive;

public sealed record SetCatalogDictionaryTermActiveCommand(Guid TermId, bool IsActive, string? Reason);