using ElectronicService.Infrastructure.Postgres.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ElectronicService.Web.IntegrationTests.Fixtures;

/// <summary>
/// Управляет временным PostgreSQL и тестовым ASP.NET Core приложением.
/// Один контейнер используется всей Web integration collection.
/// </summary>
public sealed class WebIntegrationFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18-alpine")
            .WithDatabase("electronic_service_web_tests")
            .WithUsername("electronic_web_test_user")
            .WithPassword("electronic_web_test_password")
            .Build();

    private ElectronicServiceWebApplicationFactory? _application;

    public IServiceProvider Services => Application.Services;
    public HttpClient CreateClient()
    {
        return Application.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    public ElectronicDbContext CreateDbContext()
    {
        var options =
            new DbContextOptionsBuilder<ElectronicDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .EnableDetailedErrors()
                .Options;

        return new ElectronicDbContext(options);
    }

    public async ValueTask InitializeAsync()
    {
        var cancellationToken =
            TestContext.Current.CancellationToken;

        await _container.StartAsync(cancellationToken);

        await using var dbContext = CreateDbContext();

        await dbContext.Database.MigrateAsync(cancellationToken);

        _application =
            new ElectronicServiceWebApplicationFactory(
                _container.GetConnectionString());
    }

    public async ValueTask DisposeAsync()
    {
        if (_application is not null)
        {
            await _application.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    private ElectronicServiceWebApplicationFactory Application =>
        _application
        ?? throw new InvalidOperationException(
            "Web application factory is not initialized.");
}
