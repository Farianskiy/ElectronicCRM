using ElectronicService.Core;
using ElectronicService.Infrastructure.Postgres;
using ElectronicService.Infrastructure.Postgres.Catalog.Seeding;
using ElectronicService.Infrastructure.Postgres.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Нативный OpenAPI .NET 9/10
builder.Services.AddOpenApi();

// Для тестового контроллера
builder.Services.AddControllers();

// Подключение слоя Core, это handlers, команды, запросы и т.д.
builder.Services.AddCore();

// Подключение инфраструктуры Postgres
builder.Services.AddInfrastructurePostgres(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ElectronicDbContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<CatalogDataSeeder>();

    await seeder.SeedAsync();
}

// Minimal API endpoints
app.MapGet("/", () => "ElectronicService is running!");

app.MapHealthChecks("/health");

// MVC контроллеры
app.MapControllers();

if (!app.Environment.IsProduction())
{
    app.MapOpenApi();              // /openapi/v1.json
    app.MapScalarApiReference();   // /scalar/v1
}

await app.RunAsync();
