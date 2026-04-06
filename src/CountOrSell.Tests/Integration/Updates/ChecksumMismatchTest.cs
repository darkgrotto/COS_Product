using CountOrSell.Api.Services;
using Xunit;

namespace CountOrSell.Tests.Integration.Updates;

public class ChecksumMismatchTest
{
    [Fact]
    public void VerifyFileChecksum_ReturnsFalse_WhenHashDoesNotMatch()
    {
        var verifier = new PackageVerifier();
        var data = "test package content"u8.ToArray();

        var result = verifier.VerifyFileChecksum(data, "sha256:0000000000000000000000000000000000000000000000000000000000000000");

        Assert.False(result);
    }

    [Fact]
    public void VerifyFileChecksum_ReturnsTrue_WhenHashMatches()
    {
        var verifier = new PackageVerifier();
        var data = "test package content"u8.ToArray();

        var expectedHash = System.Security.Cryptography.SHA256.HashData(data);
        var expectedHex = Convert.ToHexString(expectedHash).ToLowerInvariant();

        var result = verifier.VerifyFileChecksum(data, $"sha256:{expectedHex}");

        Assert.True(result);
    }

    [Fact]
    public void VerifyFileChecksum_IsCaseInsensitive_OnHexPart()
    {
        var verifier = new PackageVerifier();
        var data = "test package content"u8.ToArray();

        var expectedHash = System.Security.Cryptography.SHA256.HashData(data);
        var expectedHexUpper = Convert.ToHexString(expectedHash).ToUpperInvariant();

        var result = verifier.VerifyFileChecksum(data, $"sha256:{expectedHexUpper}");

        Assert.True(result);
    }

    [Fact]
    public void VerifyFileChecksum_ReturnsFalse_WhenPrefixMissing()
    {
        var verifier = new PackageVerifier();
        var data = "test package content"u8.ToArray();

        var expectedHash = System.Security.Cryptography.SHA256.HashData(data);
        var expectedHex = Convert.ToHexString(expectedHash).ToLowerInvariant();

        // Missing "sha256:" prefix - should not match
        var result = verifier.VerifyFileChecksum(data, expectedHex);

        Assert.False(result);
    }
}
