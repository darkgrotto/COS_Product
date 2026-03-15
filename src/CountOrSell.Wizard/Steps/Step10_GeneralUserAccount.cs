using CountOrSell.Wizard.Models;
using CountOrSell.Wizard.Services;

namespace CountOrSell.Wizard.Steps;

public static class Step10_GeneralUserAccount
{
    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 10 of 17: General User Account");
        Console.WriteLine("-------------------------------------");
        Console.WriteLine("Create one general user account (local account).");
        Console.WriteLine("Additional users and OAuth configuration are set up post-setup.");
        Console.WriteLine("Minimum password length: 15 characters.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("General user username: ");
            var username = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(username))
            {
                config.GeneralUserUsername = username;
                break;
            }
            Console.WriteLine("Username cannot be empty.");
        }

        while (true)
        {
            Console.Write("General user password (min 15 chars): ");
            var password = ReadPassword();
            var result = PasswordValidator.Validate(password);
            if (result.IsValid)
            {
                config.GeneralUserPassword = password;
                break;
            }
            Console.WriteLine(result.ErrorMessage);
        }

        Console.WriteLine("General user account configured.");
        Console.WriteLine();
        return Task.CompletedTask;
    }

    private static string ReadPassword()
    {
        var sb = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }
            if (key.Key == ConsoleKey.Backspace && sb.Length > 0)
            {
                sb.Remove(sb.Length - 1, 1);
                Console.Write("\b \b");
            }
            else if (key.Key != ConsoleKey.Backspace)
            {
                sb.Append(key.KeyChar);
                Console.Write("*");
            }
        }
        return sb.ToString();
    }
}
