using CountOrSell.Wizard.Services;
using Xunit;

namespace CountOrSell.Tests.WizardTests;

public class PasswordValidationTests
{
    [Theory]
    [InlineData("short")]
    [InlineData("14charpassword")]
    [InlineData("")]
    public void DatabaseAdminPassword_UnderMinimum_IsRejected(string password)
    {
        var result = PasswordValidator.Validate(password);
        Assert.False(result.IsValid);
        Assert.Contains("15", result.ErrorMessage);
    }

    [Theory]
    [InlineData("validpassword123")]
    [InlineData("thisislongerthan15")]
    public void DatabaseAdminPassword_AtOrAboveMinimum_IsAccepted(string password)
    {
        var result = PasswordValidator.Validate(password);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("14charpassword")]
    [InlineData("")]
    public void ProductAdminPassword_UnderMinimum_IsRejected(string password)
    {
        var result = PasswordValidator.Validate(password);
        Assert.False(result.IsValid);
        Assert.Contains("15", result.ErrorMessage);
    }

    [Theory]
    [InlineData("validpassword123")]
    [InlineData("thisislongerthan15")]
    public void ProductAdminPassword_AtOrAboveMinimum_IsAccepted(string password)
    {
        var result = PasswordValidator.Validate(password);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("short")]
    [InlineData("14charpassword")]
    [InlineData("")]
    public void GeneralUserPassword_UnderMinimum_IsRejected(string password)
    {
        var result = PasswordValidator.Validate(password);
        Assert.False(result.IsValid);
        Assert.Contains("15", result.ErrorMessage);
    }

    [Theory]
    [InlineData("validpassword123")]
    [InlineData("thisislongerthan15")]
    public void GeneralUserPassword_AtOrAboveMinimum_IsAccepted(string password)
    {
        var result = PasswordValidator.Validate(password);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Password_ExactlyFifteenChars_IsAccepted()
    {
        var password = "123456789012345"; // exactly 15 chars
        Assert.Equal(15, password.Length);
        var result = PasswordValidator.Validate(password);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Password_FourteenChars_IsRejected()
    {
        var password = "12345678901234"; // exactly 14 chars
        Assert.Equal(14, password.Length);
        var result = PasswordValidator.Validate(password);
        Assert.False(result.IsValid);
        Assert.Contains("15", result.ErrorMessage);
    }

    [Fact]
    public void Password_Null_IsRejected()
    {
        var result = PasswordValidator.Validate(null!);
        Assert.False(result.IsValid);
        Assert.Contains("15", result.ErrorMessage);
    }
}
