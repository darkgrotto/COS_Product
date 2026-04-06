using CountOrSell.Domain.Services;
using System.Security.Cryptography;

namespace CountOrSell.Api.Services;

public class PackageVerifier : IPackageVerifier
{
    // expectedChecksum format from per-package manifest: "sha256:<hex_lowercase>"
    public bool VerifyFileChecksum(byte[] fileBytes, string expectedChecksum)
    {
        if (!expectedChecksum.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
            return false;

        var expectedHex = expectedChecksum["sha256:".Length..];
        var hash = SHA256.HashData(fileBytes);
        var actual = Convert.ToHexString(hash);
        return string.Equals(actual, expectedHex, StringComparison.OrdinalIgnoreCase);
    }
}
