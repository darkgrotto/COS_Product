using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace CountOrSell.Tests.Integration.Updates;

// Helper for building test update package ZIP streams.
internal static class PackageBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static MemoryStream Build(
        object? treatments = null,
        object? sets = null,
        object? cards = null,
        object? sealedCategories = null,
        object? sealedProducts = null)
    {
        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            if (treatments != null) AddJsonEntry(archive, "treatments.json", treatments);
            if (sets != null) AddJsonEntry(archive, "sets.json", sets);
            if (cards != null) AddJsonEntry(archive, "cards.json", cards);
            if (sealedCategories != null) AddJsonEntry(archive, "sealed_product_categories.json", sealedCategories);
            if (sealedProducts != null) AddJsonEntry(archive, "sealed_products.json", sealedProducts);
        }
        ms.Position = 0;
        return ms;
    }

    private static void AddJsonEntry(ZipArchive archive, string name, object data)
    {
        var entry = archive.CreateEntry(name);
        using var stream = entry.Open();
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        stream.Write(bytes, 0, bytes.Length);
    }
}
