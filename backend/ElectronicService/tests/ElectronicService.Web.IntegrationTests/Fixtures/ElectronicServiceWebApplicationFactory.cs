using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ElectronicService.Web.IntegrationTests.Fixtures;

/// <summary>
/// Запускает настоящее ASP.NET Core приложение в памяти
/// и подменяет только внешнюю PostgreSQL-конфигурацию.
/// </summary>
public sealed class ElectronicServiceWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public ElectronicServiceWebApplicationFactory(
        string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException(
                "Connection string is required.",
                nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (_, configurationBuilder) =>
            {
                var testConfiguration =
                    new Dictionary<string, string?>(
                        StringComparer.Ordinal)
                    {
                        ["ConnectionStrings:Database"] =
                            _connectionString,

                        ["Jwt:Issuer"] =
                            "ElectronicService.Web.IntegrationTests",

                        ["Jwt:Audience"] =
                            "ElectronicService.Web.IntegrationTests",

                        ["Jwt:SecretKey"] =
                            "electronic-service-web-integration-tests-secret-key-2026"
                    };

                configurationBuilder.AddInMemoryCollection(
                    testConfiguration);
            });
    }
}
