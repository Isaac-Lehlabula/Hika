using Hika.Application.Common.Events;
using Hika.Application.Common.Options;
using Hika.Application.Common.Persistence;
using Hika.Application.Common.Storage;
using Hika.Application.Payments.Ports;
using Hika.Application.Users.Ports;
using Hika.Infrastructure.Common.Events;
using Hika.Infrastructure.Identity;
using Hika.Infrastructure.Notifications;
using Hika.Infrastructure.Payments;
using Hika.Infrastructure.Persistence;
using Hika.Infrastructure.Security;
using Hika.Infrastructure.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hika.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Resolves IConfiguration fresh from the container (via the (sp, options) overload)
        // rather than capturing configuration.GetConnectionString(...) as a closure variable
        // here. That distinction matters for WebApplicationFactory-based tests: this method
        // runs before the test host merges its configuration overrides in, so an eager read
        // would silently connect to whatever ConnectionStrings:Default the *default* config
        // resolved to at that point, not the Testcontainers instance tests actually intend.
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
                ?? throw new InvalidOperationException("Missing required configuration: ConnectionStrings:Default");

            options.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                // Fast-fail shape rules (length, character classes) also live in
                // FluentValidation for immediate client feedback; Identity is the enforced
                // source of truth. Lockout protects against credential-stuffing/brute force.
                options.Password.RequiredLength = 10;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();
        // No .AddDefaultTokenProviders() — email/phone verification and password reset use
        // our own hashed-token entities (EmailVerificationToken, PhoneVerificationCode,
        // PasswordResetToken), not Identity's built-in token providers.

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<NotificationLinksOptions>()
            .Bind(configuration.GetSection(NotificationLinksOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<LocalFileStorageOptions>()
            .Bind(configuration.GetSection(LocalFileStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<IUserAccountService, UserAccountService>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<ISmsSender, LoggingSmsSender>();
        services.AddScoped<IPaymentGateway, MockPaymentGateway>();
        services.AddScoped<IFileStorage, LocalFileStorage>();
        // IHttpContextAccessor's concrete implementation lives in the full ASP.NET Core
        // shared framework, which this plain class library doesn't reference — registered in
        // Hika.Api's Program.cs (a Web SDK project) instead; this project only needs the
        // interface, from Microsoft.AspNetCore.Http.Abstractions.

        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }
}
