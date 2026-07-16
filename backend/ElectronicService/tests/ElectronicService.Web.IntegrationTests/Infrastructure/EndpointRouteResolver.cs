using System.Globalization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;
using Microsoft.AspNetCore.Routing.Patterns;

namespace ElectronicService.Web.IntegrationTests.Infrastructure;

internal enum CatalogEndpointKind
{
    Search = 1,
    Details = 2,
    Replacements = 3
}

/// <summary>
/// Находит фактические controller routes в запущенном ASP.NET Core приложении.
/// Это позволяет тестам не дублировать route templates production-кода.
/// </summary>
internal static class EndpointRouteResolver
{
    public static Uri ResolveCatalogGetEndpoint(
        IServiceProvider serviceProvider,
        CatalogEndpointKind endpointKind,
        Guid? productId = null,
        IReadOnlyDictionary<string, string?>? query = null)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var endpoints = serviceProvider
            .GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(routeEndpoint => new ControllerRoute(
                routeEndpoint,
                routeEndpoint.Metadata
                    .GetMetadata<ControllerActionDescriptor>()))
            .Where(item => item.ActionDescriptor is not null)
            .Where(item => SupportsGet(item.RouteEndpoint))
            .ToList();

        var selected = endpoints.FirstOrDefault(item =>
            MatchesEndpoint(item, endpointKind));

        if (selected is null)
        {
            throw new XunitException(
                BuildEndpointNotFoundMessage(
                    endpointKind,
                    endpoints));
        }

        var path = BuildPath(
            selected.RouteEndpoint.RoutePattern,
            productId);

        if (query is not null && query.Count > 0)
        {
            var queryValues = query
                .Where(item => item.Value is not null)
                .ToDictionary(
                    item => item.Key,
                    item => item.Value,
                    StringComparer.Ordinal);

            path = QueryHelpers.AddQueryString(
                path,
                queryValues);
        }

        return new Uri(path, UriKind.Relative);
    }

    private static bool SupportsGet(RouteEndpoint routeEndpoint)
    {
        var methods = routeEndpoint.Metadata
            .GetMetadata<IHttpMethodMetadata>()?
            .HttpMethods;

        return methods is null
            || methods.Any(method =>
                string.Equals(
                    method,
                    "GET",
                    StringComparison.OrdinalIgnoreCase));
    }

    private static bool MatchesEndpoint(
        ControllerRoute route,
        CatalogEndpointKind endpointKind)
    {
        return endpointKind switch
        {
            CatalogEndpointKind.Search =>
                IsSearchEndpoint(route),

            CatalogEndpointKind.Details =>
                IsDetailsEndpoint(route),

            CatalogEndpointKind.Replacements =>
                IsReplacementsEndpoint(route),

            _ => false
        };
    }

    private static bool IsSearchEndpoint(
        ControllerRoute route)
    {
        var descriptor = route.ActionDescriptor!;

        if (!Contains(
                descriptor.ControllerName,
                "CatalogProduct")
            || IsReplacementsEndpoint(route)
            || IsDetailsEndpoint(route))
        {
            return false;
        }

        if (route.RouteEndpoint.RoutePattern.Parameters.Count > 0)
        {
            return false;
        }

        var actionSuggestsSearch =
            Contains(descriptor.ActionName, "Search")
            || Contains(descriptor.ActionName, "GetProduct")
            || string.Equals(
                descriptor.ActionName,
                "Get",
                StringComparison.OrdinalIgnoreCase);

        var parameterSuggestsSearch = descriptor.Parameters.Any(
            parameter =>
                Contains(parameter.Name, "search")
                || Contains(parameter.Name, "onlyInStock")
                || Contains(
                    parameter.ParameterType.Name,
                    "SearchProductsQuery"));

        return actionSuggestsSearch || parameterSuggestsSearch;
    }

    private static bool IsDetailsEndpoint(
        ControllerRoute route)
    {
        var descriptor = route.ActionDescriptor!;

        if (!Contains(
                descriptor.ControllerName,
                "CatalogProduct")
            || IsReplacementsEndpoint(route))
        {
            return false;
        }

        return Contains(descriptor.ControllerName, "Detail")
            || Contains(descriptor.ActionName, "Detail")
            || Contains(descriptor.ActionName, "GetById")
            || route.RouteEndpoint.RoutePattern.Parameters.Count > 0;
    }

    private static bool IsReplacementsEndpoint(
        ControllerRoute route)
    {
        var descriptor = route.ActionDescriptor!;
        var rawRoute = route.RouteEndpoint.RoutePattern.RawText;

        return Contains(
                descriptor.ControllerName,
                "Replacement")
            || Contains(
                descriptor.ActionName,
                "Replacement")
            || Contains(
                rawRoute,
                "replacement");
    }

    private static string BuildPath(
        RoutePattern routePattern,
        Guid? productId)
    {
        var path = routePattern.RawText
            ?? throw new XunitException(
                "Controller endpoint does not have a route pattern.");

        foreach (var parameter in routePattern.Parameters)
        {
            if (parameter.IsOptional)
            {
                path = ReplaceRouteParameter(
                    path,
                    parameter.Name,
                    string.Empty);

                continue;
            }

            if (productId is null)
            {
                throw new XunitException(
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"Route '{routePattern.RawText}' requires parameter '{parameter.Name}', but productId was not supplied."));
            }

            path = ReplaceRouteParameter(
                path,
                parameter.Name,
                productId.Value.ToString());
        }

        path = path.Replace("//", "/", StringComparison.Ordinal);

        if (!path.StartsWith('/'))
        {
            path = string.Concat("/", path);
        }

        return path;
    }

    private static string ReplaceRouteParameter(
        string path,
        string parameterName,
        string value)
    {
        var startToken = string.Concat(
            "{",
            parameterName);

        var startIndex = path.IndexOf(
            startToken,
            StringComparison.OrdinalIgnoreCase);

        if (startIndex < 0)
        {
            return path;
        }

        var endIndex = path.IndexOf(
            '}',
            startIndex);

        if (endIndex < 0)
        {
            throw new XunitException(
                $"Invalid route template: '{path}'.");
        }

        var tokenLength = endIndex - startIndex + 1;

        return string.Concat(
            path.AsSpan(0, startIndex),
            value,
            path.AsSpan(startIndex + tokenLength));
    }

    private static string BuildEndpointNotFoundMessage(
        CatalogEndpointKind endpointKind,
        IReadOnlyCollection<ControllerRoute> routes)
    {
        var availableRoutes = routes
            .Select(item =>
            {
                var descriptor = item.ActionDescriptor!;

                return string.Create(
                    CultureInfo.InvariantCulture,
                    $"{descriptor.ControllerName}.{descriptor.ActionName} -> {item.RouteEndpoint.RoutePattern.RawText}");
            })
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();

        var routesText = availableRoutes.Count == 0
            ? "<controller endpoints were not found>"
            : string.Join(
                Environment.NewLine,
                availableRoutes);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Catalog endpoint '{endpointKind}' was not found.{Environment.NewLine}Available controller endpoints:{Environment.NewLine}{routesText}");
    }

    private static bool Contains(
        string? value,
        string fragment)
    {
        return value?.Contains(
            fragment,
            StringComparison.OrdinalIgnoreCase) is true;
    }

    private sealed record ControllerRoute(
        RouteEndpoint RouteEndpoint,
        ControllerActionDescriptor? ActionDescriptor);
}
