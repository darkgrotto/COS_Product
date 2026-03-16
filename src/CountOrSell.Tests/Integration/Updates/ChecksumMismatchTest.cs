using CountOrSell.Api.Services;
using Xunit;

namespace CountOrSell.Tests.Integration.Updates;

public class ChecksumMismatchTest
{
    [Fact]
    public void VerifyChecksum_ReturnsFalse_WhenHashDoesNotMatch()
    {
        var verifier = new PackageVerifier();
        var data = "test package content"u8.ToArray();
        using var stream = new MemoryStream(data);

        var result = verifier.VerifyChecksum(stream, "0000000000000000000000000000000000000000000000000000000000000000");

        Assert.False(result);
    }

    [Fact]
    public void VerifyChecksum_ReturnsTrue_WhenHashMatches()
    {
        var verifier = new PackageVerifier();
        var data = "test package content"u8.ToArray();
        using var stream = new MemoryStream(data);

        // Compute expected hash
        var expectedHash = System.Security.Cryptography.SHA256.HashData(data);
        var expectedHex = Convert.ToHexString(expectedHash);

        stream.Position = 0;
        var result = verifier.VerifyChecksum(stream, expectedHex);

        Assert.True(result);
    }

    [Fact]
    public void VerifyChecksum_ResetsStreamPositionToZero_AfterComputing()
    {
        var verifier = new PackageVerifier();
        var data = "test package content"u8.ToArray();
        using var stream = new MemoryStream(data);

        var expectedHash = System.Security.Cryptography.SHA256.HashData(data);
        var expectedHex = Convert.ToHexString(expectedHash);

        stream.Position = 0;
        verifier.VerifyChecksum(stream, expectedHex);

        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void VerifyChecksum_IsCaseInsensitive()
    {
        var verifier = new PackageVerifier();
        var data = "test package content"u8.ToArray();
        using var stream = new MemoryStream(data);

        var expectedHash = System.Security.Cryptography.SHA256.HashData(data);
        var expectedHexLower = Convert.ToHexString(expectedHash).ToLowerInvariant();

        stream.Position = 0;
        var result = verifier.VerifyChecksum(stream, expectedHexLower);

        Assert.True(result);
    }
}
