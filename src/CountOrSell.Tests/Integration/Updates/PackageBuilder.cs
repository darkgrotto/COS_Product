using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CountOrSell.Domain.Dtos;
using CountOrSell.Domain.Dtos.Packages;

namespace CountOrSell.Tests.Integration.Updates;

// Helper for building test update package ZIP streams.
// Returns (stream, packageManifest) matching the real countorsell.com package format:
//   manifest.json
//   metadata/treatments.json
//   metadata/taxonomy.json
//   metadata/sets/{set_code}/set.json
//   metadata/sets/{set_code}/cards.json
//   metadata/sealed/{product_id}.json
internal static class PackageBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    public static (MemoryStream stream, PackageManifest manifest) Build(
        List<TreatmentDto>? treatments = null,
        List<SetDto>? sets = null,
        List<CardDto>? cards = null,
        TaxonomyDto? taxonomy = null,
        List<SealedProductDto>? sealedProducts = null)
    {
        var entries = new Dictionary<string, byte[]>();

        if (treatments != null)
            entries["metadata/treatments.json"] = Serialize(treatments);

        if (taxonomy != null)
            entries["metadata/taxonomy.json"] = Serialize(taxonomy);

        // Organize sets
        if (sets != null)
            foreach (var set in sets)
                entries[$"metadata/sets/{set.Code}/set.json"] = Serialize(set);

        // Organize cards per set
        if (cards != null)
        {
            foreach (var group in cards.GroupBy(c => c.SetCode))
            {
                var path = $"metadata/sets/{group.Key}/cards.json";
                entries[path] = Serialize(group.ToList());
            }
        }

        if (sealedProducts != null)
            foreach (var product in sealedProducts)
                entries[$"metadata/sealed/{product.Identifier}.json"] = Serialize(product);

        // Build checksums
        var checksums = entries.ToDictionary(
            kvp => kvp.Key,
            kvp => "sha256:" + ComputeSha256(kvp.Value));

        var packageManifest = new PackageManifest
        {
            PackageType = "full",
            GeneratedAt = DateTime.UtcNow,
            SchemaVersion = "1.0.0",
            ContentVersions = new Dictionary<string, ContentVersionEntry>
            {
                ["cards"] = new ContentVersionEntry { Version = "test-" + Guid.NewGuid().ToString("N")[..8] }
            },
            Checksums = checksums
        };

        var manifestBytes = Serialize(packageManifest);

        var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "manifest.json", manifestBytes);
            foreach (var (path, data) in entries)
                AddEntry(archive, path, data);
        }
        ms.Position = 0;
        return (ms, packageManifest);
    }

    private static byte[] Serialize<T>(T data)
        => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data, JsonOptions));

    private static string ComputeSha256(byte[] data)
    {
        var hash = SHA256.HashData(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static void AddEntry(ZipArchive archive, string path, byte[] data)
    {
        var entry = archive.CreateEntry(path);
        using var stream = entry.Open();
        stream.Write(data, 0, data.Length);
    }
}
