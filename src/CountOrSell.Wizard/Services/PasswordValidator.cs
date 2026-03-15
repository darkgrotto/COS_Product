using CountOrSell.Wizard.Models;

namespace CountOrSell.Wizard.Services;

public static class PasswordValidator
{
    private const int MinimumLength = 15;

    public static ValidationResult Validate(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinimumLength)
        {
            return new ValidationResult(false,
                $"Password must be at least {MinimumLength} characters. Please try again.");
        }

        return new ValidationResult(true, string.Empty);
    }
}
