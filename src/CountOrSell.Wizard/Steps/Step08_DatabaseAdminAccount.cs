using CountOrSell.Wizard.Models;
using CountOrSell.Wizard.Services;

namespace CountOrSell.Wizard.Steps;

public static class Step08_DatabaseAdminAccount
{
    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 8 of 17: Database Admin Account");
        Console.WriteLine("--------------------------------------");
        Console.WriteLine("Create the database administrator account.");
        Console.WriteLine("Minimum password length: 15 characters.");
        Console.WriteLine();

        while (true)
        {
            Console.Write("Database admin username: ");
            var username = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(username))
            {
                config.DbAdminUsername = username;
                break;
            }
            Console.WriteLine("Username cannot be empty.");
        }

        while (true)
        {
            Console.Write("Database admin password (min 15 chars): ");
            var password = ReadPassword();
            var result = PasswordValidator.Validate(password);
            if (result.IsValid)
            {
                config.DbAdminPassword = password;
                break;
            }
            Console.WriteLine(result.ErrorMessage);
        }

        Console.WriteLine("Database admin account configured.");
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
