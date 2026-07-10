using ElectronicService.Core.Catalog.Metadata.GetManufacturers;
using ElectronicService.Core.Catalog.Metadata.GetProductTypeCharacteristics;
using ElectronicService.Core.Catalog.Metadata.GetProductTypes;
using ElectronicService.Core.Catalog.Products.AddAlias;
using ElectronicService.Core.Catalog.Products.GetProductById;
using ElectronicService.Core.Catalog.Products.GetProducts;
using ElectronicService.Core.Catalog.Products.GetReplacements;
using ElectronicService.Core.Catalog.Products.SearchProducts;
using ElectronicService.Core.Catalog.Products.SetCharacteristic;
using ElectronicService.Core.Catalog.Products.UpdatePrice;
using ElectronicService.Core.Catalog.Products.UpdateStock;
using ElectronicService.Core.Catalog.Products.SearchReplacements;
using ElectronicService.Core.Users.BlockUser;
using ElectronicService.Core.Users.CreateRegularUser;
using ElectronicService.Core.Users.CreateTechnicalUser;
using ElectronicService.Core.Users.MakeUserRegular;
using ElectronicService.Core.Users.MakeUserTechnical;
using Microsoft.Extensions.DependencyInjection;
using ElectronicService.Core.Catalog.Dictionaries.GetTerms;
using ElectronicService.Core.Catalog.Dictionaries.AddTerm;
using ElectronicService.Core.Catalog.Assistant.AskCatalogAssistant;
using ElectronicService.Core.Catalog.Assistant.Parsing;
using ElectronicService.Core.Catalog.Assistant.Abstractions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.CreateSuggestion;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.GetSuggestions;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.ApproveSuggestion;
using ElectronicService.Core.Catalog.Assistant.DictionarySuggestions.RejectSuggestion;

namespace ElectronicService.Core;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddScoped<CreateRegularUserCommandHandler>();
        services.AddScoped<CreateTechnicalUserCommandHandler>();
        services.AddScoped<MakeUserTechnicalCommandHandler>();
        services.AddScoped<MakeUserRegularCommandHandler>();
        services.AddScoped<BlockUserCommandHandler>();
        services.AddScoped<GetCatalogProductsQueryHandler>();
        services.AddScoped<GetCatalogProductByIdQueryHandler>();
        services.AddScoped<SearchProductsQueryHandler>();
        services.AddScoped<GetProductReplacementsQueryHandler>();
        services.AddScoped<UpdateProductPriceCommandHandler>();
        services.AddScoped<UpdateProductStockCommandHandler>();
        services.AddScoped<SetProductCharacteristicCommandHandler>();
        services.AddScoped<AddProductAliasCommandHandler>();
        services.AddScoped<GetCatalogProductTypesQueryHandler>();
        services.AddScoped<GetCatalogProductTypeCharacteristicsQueryHandler>();
        services.AddScoped<GetCatalogManufacturersQueryHandler>();
        services.AddScoped<SearchProductReplacementsQueryHandler>();
        services.AddScoped<GetCatalogDictionaryTermsQueryHandler>();
        services.AddScoped<AddCatalogDictionaryTermCommandHandler>();
        services.AddScoped<AskCatalogAssistantCommandHandler>();
        services.AddScoped<ICatalogAssistantMessageParser, RuleBasedCatalogAssistantMessageParser>();
        services.AddScoped<CreateCatalogAssistantDictionarySuggestionCommandHandler>();
        services.AddScoped<GetCatalogAssistantDictionarySuggestionsQueryHandler>();
        services.AddScoped<ApproveCatalogAssistantDictionarySuggestionCommandHandler>();
        services.AddScoped<RejectCatalogAssistantDictionarySuggestionCommandHandler>();

        return services;
    }
}