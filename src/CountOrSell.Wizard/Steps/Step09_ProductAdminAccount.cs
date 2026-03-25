using CountOrSell.Wizard.Models;
using CountOrSell.Wizard.Services;

namespace CountOrSell.Wizard.Steps;

public static class Step09_ProductAdminAccount
{
    public static Task RunAsync(WizardConfig config)
    {
        Console.WriteLine("Step 9 of 17: Product Admin Account");
        Console.WriteLine("-------------------------------------");
        Console.WriteLine("Create the CountOrSell product administrator account.");
        Console.WriteLine("This is always a local account. OAuth is configured post-setup.");
        Console.WriteLine("Minimum password length: 15 characters.");
        Console.WriteLine();

        config.ConfigValues.TryGetValue("product_admin_username", out var cfgProductAdminUsername);
        while (true)
        {
            if (!string.IsNullOrEmpty(cfgProductAdminUsername))
                Console.Write($"Product admin username [{cfgProductAdminUsername}]: ");
            else
                Console.Write("Product admin username: ");
            var usernameInput = Console.ReadLine()?.Trim();
            var username = string.IsNullOrEmpty(usernameInput) ? cfgProductAdminUsername : usernameInput;
            if (!string.IsNullOrEmpty(username))
            {
                config.ProductAdminUsername = username;
                break;
            }
            Console.WriteLine("Username cannot be empty.");
        }

        while (true)
        {
            Console.Write("Product admin password (min 15 chars): ");
            var password = ReadPassword();
            var result = PasswordValidator.Validate(password);
            if (result.IsValid)
            {
                config.ProductAdminPassword = password;
                break;
            }
            Console.WriteLine(result.ErrorMessage);
        }

        Console.WriteLine("Product admin account configured.");
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
