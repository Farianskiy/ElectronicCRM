using System.Text.Json;
using ElectronicService.Domain.Catalog.Products;

namespace ElectronicService.Core.Catalog.Products.Audit;

public static class ProductAuditSnapshotSerializer
{
    private static readonly JsonSerializerOptions
        SerializerOptions =
            new(JsonSerializerDefaults.Web);

    public static string Serialize(
        Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var snapshot =
            ProductAuditSnapshotFactory.Create(product);

        return JsonSerializer.Serialize(
            snapshot,
            SerializerOptions);
    }

    public static ProductAuditSnapshot? Deserialize(
        string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<
            ProductAuditSnapshot>(
                json,
                SerializerOptions);
    }
}