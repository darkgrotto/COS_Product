using CountOrSell.Domain.Services;
using System.Security.Cryptography;

namespace CountOrSell.Api.Services;

public class PackageVerifier : IPackageVerifier
{
    public bool VerifyChecksum(Stream packageStream, string expectedSha256)
    {
        var originalPosition = packageStream.CanSeek ? packageStream.Position : 0;
        if (packageStream.CanSeek) packageStream.Position = 0;

        var hash = SHA256.HashData(packageStream);
        var actual = Convert.ToHexString(hash);

        // Reset stream so caller can re-read it
        if (packageStream.CanSeek) packageStream.Position = originalPosition == 0 ? 0 : originalPosition;

        return string.Equals(actual, expectedSha256, StringComparison.OrdinalIgnoreCase);
    }
}
