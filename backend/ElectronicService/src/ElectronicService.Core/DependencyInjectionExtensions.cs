using ElectronicService.Core.Users.BlockUser;
using ElectronicService.Core.Users.CreateRegularUser;
using ElectronicService.Core.Users.CreateTechnicalUser;
using ElectronicService.Core.Users.MakeUserRegular;
using ElectronicService.Core.Users.MakeUserTechnical;
using Microsoft.Extensions.DependencyInjection;
using ElectronicService.Core.Catalog.Products.GetProducts;
using ElectronicService.Core.Catalog.Products.GetProductById;
using ElectronicService.Core.Catalog.Products.SearchProducts;
using ElectronicService.Core.Catalog.Products.GetReplacements;
using ElectronicService.Core.Catalog.Products.UpdatePrice;
using ElectronicService.Core.Catalog.Products.UpdateStock;

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

        return services;
    }
}