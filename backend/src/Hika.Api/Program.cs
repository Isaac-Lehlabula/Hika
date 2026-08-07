using System.Text;
using Hika.Api.Middleware;
using Hika.Application;
using Hika.Infrastructure;
using Hika.Infrastructure.Persistence;
using Hika.Infrastructure.Security;
using Hika.Infrastructure.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    const string CorsPolicyName = "Frontend";
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(CorsPolicyName, policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            }
        });
    });

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();

    builder.Services
        .AddControllers(options => options.Filters.Add<ValidationActionFilter>());

    builder.Services.AddOpenApi();

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();

    // Configured via the options pipeline (resolving IOptions<JwtOptions> lazily through DI)
    // rather than reading IConfiguration directly in top-level code. That distinction matters:
    // top-level code here runs against `builder.Configuration` *before* WebApplicationFactory
    // (used by integration tests) has merged its configuration overrides in, which would
    // otherwise sign tokens with one key and validate them with another. Resolving lazily
    // guarantees this always sees the same fully-finalized configuration JwtTokenGenerator does.
    builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
        .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) =>
        {
            var jwt = jwtOptions.Value;

            // Without this, ASP.NET Core remaps well-known JWT claim names (e.g. "sub" ->
            // ClaimTypes.NameIdentifier) on the way in, which would silently break any code
            // (ours included) that reads claims by their original JWT registered name.
            bearerOptions.MapInboundClaims = false;

            bearerOptions.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30),
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services
        .AddHealthChecks()
        .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
        .AddNpgSql(
            // Resolved lazily per-check from IServiceProvider (not read eagerly here) for the
            // same reason as AddInfrastructure's DbContext registration — see the comment there.
            sp => sp.GetRequiredService<IConfiguration>().GetConnectionString("Default")
                ?? throw new InvalidOperationException("Missing required configuration: ConnectionStrings:Default"),
            name: "postgres",
            tags: ["ready"]);

    var app = builder.Build();

    // Request logging goes outermost so its completion log reflects the *final* status code
    // that UseExceptionHandler resolves an exception to, instead of logging its own separate
    // "unhandled exception" entry on the way through.
    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseHttpsRedirection();

    // Served from the same resolved path LocalFileStorage writes to, at the web root (no
    // "/uploads" prefix) — matches the URLs LocalFileStorage.SaveAsync returns.
    var uploadsRootPath = LocalFileStorage.ResolveAbsoluteRootPath(
        app.Services.GetRequiredService<IOptions<LocalFileStorageOptions>>().Value.RootPath, app.Environment);
    Directory.CreateDirectory(uploadsRootPath);
    app.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(uploadsRootPath) });

    app.UseCors(CorsPolicyName);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/health/live", new()
    {
        Predicate = check => check.Tags.Contains("live"),
    });
    app.MapHealthChecks("/health/ready", new()
    {
        Predicate = check => check.Tags.Contains("ready"),
    });

    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Hika API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>Entry point partial class, exposed for WebApplicationFactory in integration tests.</summary>
public partial class Program;
