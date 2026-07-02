using ElectronicService.Core.Users.BlockUser;
using ElectronicService.Core.Users.CreateRegularUser;
using ElectronicService.Core.Users.CreateTechnicalUser;
using ElectronicService.Core.Users.MakeUserRegular;
using ElectronicService.Core.Users.MakeUserTechnical;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}