using System.Globalization;
using System.Text.Json;
using Xunit.Sdk;

namespace ElectronicService.Web.IntegrationTests.Infrastructure;

internal static class JsonResponseAssertions
{
    public static JsonElement GetRequiredProperty(
        JsonElement element,
        string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(
                    property.Name,
                    propertyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        throw new XunitException(
            string.Create(
                CultureInfo.InvariantCulture,
                $"JSON property '{propertyName}' was not found. JSON: {element.GetRawText()}"));
    }

    public static JsonElement GetItems(
        JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root;
        }

        return GetRequiredProperty(root, "items");
    }

    public static int GetTotalCount(
        JsonElement root)
    {
        return GetRequiredProperty(
                root,
                "totalCount")
            .GetInt32();
    }

    public static string GetRequiredString(
        JsonElement element,
        string propertyName)
    {
        var value = GetRequiredProperty(
            element,
            propertyName);

        return value.GetString()
            ?? throw new XunitException(
                $"JSON property '{propertyName}' is null.");
    }

    public static decimal GetRequiredDecimal(
        JsonElement element,
        string propertyName)
    {
        return GetRequiredProperty(
                element,
                propertyName)
            .GetDecimal();
    }

    public static Guid GetProductId(
        JsonElement item)
    {
        foreach (var propertyName in new[]
                 {
                     "id",
                     "productId"
                 })
        {
            foreach (var property in item.EnumerateObject())
            {
                if (!string.Equals(
                        property.Name,
                        propertyName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (property.Value.ValueKind == JsonValueKind.String
                    && property.Value.TryGetGuid(out var id))
                {
                    return id;
                }
            }
        }

        throw new XunitException(
            $"Product identifier was not found. JSON: {item.GetRawText()}");
    }

    public static bool ContainsString(
        JsonElement element,
        string expected)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object =>
                element.EnumerateObject().Any(
                    property =>
                        ContainsString(
                            property.Value,
                            expected)),

            JsonValueKind.Array =>
                element.EnumerateArray().Any(
                    item => ContainsString(item, expected)),

            JsonValueKind.String =>
                string.Equals(
                    element.GetString(),
                    expected,
                    StringComparison.Ordinal),

            _ => false
        };
    }
}
