using FluentValidation;
using Hika.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Hika.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserProfileService, UserProfileService>();

        return services;
    }
}
