using System.Security.Claims;
using System.Text.Json;
using ElectronicService.Domain.Users.Enums;
using ElectronicService.Web.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ElectronicService.Infrastructure.IntegrationTests.Catalog;

public sealed partial class RecognitionEvaluationReleaseTests
{
    // Opt-in host for browser QA. Uses the fixture's disposable database, never Program.cs or production seeding.
    [Fact]
    public async Task WorkspaceBrowserHarness()
    {
        var output = Environment.GetEnvironmentVariable("LEARNING_BROWSER_SESSION_FILE");
        if (string.IsNullOrWhiteSpace(output)) Assert.Skip("Set LEARNING_BROWSER_SESSION_FILE to run the isolated browser host.");
        var ct = TestContext.Current.CancellationToken;
        var rules = await SeedAsync(ct);
        var dictionary = await SeedDictionaryAsync(ct);
        await using var app = await StartAsync(ct, configure: services =>
        {
            services.AddCors();
            services.AddAuthorization(options =>
            {
                foreach (var permission in Enum.GetValues<UserPermissionCode>())
                    options.AddPolicy(PermissionPolicy.For(permission), policy => policy.RequireAuthenticatedUser());
            });
        }, configureApp: server =>
        {
            server.UseCors(policy => policy.WithOrigins("http://127.0.0.1:3300").AllowAnyHeader().AllowAnyMethod());
            // Browser uses the same explicit test identity as the real-HTTP tests.
            server.Use(async (context, next) =>
            {
                if (Guid.TryParse(context.Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.Ordinal), out var id))
                    context.Request.Headers["X-Test-User"] = id.ToString();
                await next(context);
            });
        });
        await File.WriteAllTextAsync(output, JsonSerializer.Serialize(new { url = app.Urls.Single(), rules = new { rules.UserId, manufacturerId = rules.Graph.Manufacturer.Id, productTypeId = rules.Graph.ProductType.Id, rules.VersionId, rules.BatchId }, dictionary = new { dictionary.Base.UserId, manufacturerId = dictionary.Base.Graph.Manufacturer.Id, productTypeId = dictionary.Base.Graph.ProductType.Id, suggestionId = dictionary.Command.SuggestionId } }), ct);
        var until = DateTime.UtcNow.AddMinutes(30);
        while (File.Exists(output) && DateTime.UtcNow < until) await Task.Delay(TimeSpan.FromSeconds(1), ct);
    }
}
