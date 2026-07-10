using System.Text;
using ElectronicService.Core;
using ElectronicService.Core.Abstractions;
using ElectronicService.Infrastructure.Postgres;
using ElectronicService.Infrastructure.Postgres.Catalog.Seeding;
using ElectronicService.Infrastructure.Postgres.Data;
using ElectronicService.Web.Auth;
using ElectronicService.Web.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Нативный OpenAPI .NET 9/10
builder.Services.AddOpenApi();

// MVC controllers
builder.Services.AddControllers();

// Core layer
builder.Services.AddCore();

// Infrastructure layer
builder.Services.AddInfrastructurePostgres(builder.Configuration);

// Current user provider.
// Сейчас он умеет брать UserId из JWT claims,
// а также из X-User-Id как dev fallback.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserProvider, HttpContextCurrentUserProvider>();

// JWT options
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

var jwtOptions = builder.Configuration
    .GetSection("Jwt")
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT options are not configured.");

builder.Services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ElectronicDbContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<CatalogDataSeeder>();

    await seeder.SeedAsync();
}

app.MapGet("/", () => "ElectronicService is running!");

app.MapHealthChecks("/health");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

await app.RunAsync();