using System.Text.RegularExpressions;

namespace CountOrSell.Domain;

public static class CardIdentifierValidator
{
    private static readonly Regex ValidBase =
        new(@"^[a-z0-9]{3,4}[0-9]{3,4}[a-z]?$", RegexOptions.Compiled);

    // Rejects zero-padded 4-digit suffixes (e.g. "eoe0123") - 4-digit suffix must be >= 1000.
    private static readonly Regex ZeroPaddedFourDigitSuffix =
        new(@"^[a-z0-9]{3,4}0[0-9]{3}[a-z]?$", RegexOptions.Compiled);

    public static bool IsValid(string identifier) =>
        ValidBase.IsMatch(identifier) && !ZeroPaddedFourDigitSuffix.IsMatch(identifier);
}
