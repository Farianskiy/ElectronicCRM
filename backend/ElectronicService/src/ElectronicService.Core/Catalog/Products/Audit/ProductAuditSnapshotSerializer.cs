using System.Text.Json;

namespace ElectronicService.Core.Catalog.Products.Audit;

public static class ProductAuditSnapshotSerializer
{
    private static readonly JsonSerializerOptions
        SerializerOptions =
            new(JsonSerializerDefaults.Web);

    public static string Serialize(
        ProductAuditSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return JsonSerializer.Serialize(
            snapshot,
            SerializerOptions);
    }

    public static bool TryDeserialize(
        string? json,
        out ProductAuditSnapshot? snapshot)
    {
        snapshot = null;

        /*
         * null допустим для будущих операций
         * создания или удаления товара.
         */
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            var deserialized =
                JsonSerializer.Deserialize<
                    ProductAuditSnapshot>(
                        json,
                        SerializerOptions);

            if (deserialized is null)
            {
                return false;
            }

            /*
             * Не пытаемся интерпретировать
             * неизвестную будущую версию.
             */
            if (deserialized.SnapshotVersion
                is not ProductAuditSnapshotVersions.Legacy
                and not ProductAuditSnapshotVersions.Current)
            {
                return false;
            }

            /*
             * JSON может быть синтаксически корректным,
             * но структурно неполным.
             */
            if (deserialized.Characteristics is null
                || deserialized.Aliases is null)
            {
                return false;
            }

            snapshot = deserialized;

            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }
}