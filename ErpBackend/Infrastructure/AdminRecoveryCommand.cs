using ErpBackend.Data;
using ErpBackend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ErpBackend.Infrastructure;

internal static class AdminRecoveryCommand
{
    public static async Task<int> RunAsync(string[] args, IConfiguration configuration)
    {
        if (!TryParseArguments(args, out var username, out var operatorIdentity))
        {
            WriteUsage();
            return 2;
        }

        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Administrator recovery requires an interactive terminal.");
            return 2;
        }

        Console.Write($"Type RECOVER {username} to confirm: ");
        if (!string.Equals(Console.ReadLine(), $"RECOVER {username}", StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Confirmation did not match; no changes were made.");
            return 2;
        }

        Console.WriteLine("Enter a new password of at least 16 characters, including letters and numbers. Input is hidden.");
        var newPassword = ReadHiddenPassword("New password: ");
        var confirmation = ReadHiddenPassword("Confirm password: ");
        if (!string.Equals(newPassword, confirmation, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("Passwords did not match; no changes were made.");
            return 2;
        }

        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? configuration.GetConnectionString("ErpGeneral");
        if (string.IsNullOrWhiteSpace(connectionString)
            || connectionString.StartsWith("${", StringComparison.Ordinal))
            throw new InvalidOperationException("DB_CONNECTION_STRING is required for administrator recovery.");

        var options = new DbContextOptionsBuilder<ErpGeneralDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var db = new ErpGeneralDbContext(options);
        await using var transaction = await db.Database.BeginTransactionAsync();
        var authService = new AuthService(db, configuration);
        var recovered = await authService.RecoverAdminPasswordAsync(username, newPassword, operatorIdentity);
        if (!recovered)
        {
            Console.Error.WriteLine("Recovery was not applied. Verify the account is an active administrator and the password is new and meets policy.");
            return 1;
        }

        await transaction.CommitAsync();
        Console.WriteLine("Recovery completed. The account must change its password at next login; existing sessions have been invalidated.");
        return 0;
    }

    private static bool TryParseArguments(string[] args, out string username, out string operatorIdentity)
    {
        username = "";
        operatorIdentity = "";
        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < args.Length; index++)
        {
            var option = args[index];
            if (option == "--confirm" && seen.Add(option))
                continue;

            if (option is not ("--username" or "--operator")
                || index + 1 >= args.Length
                || !seen.Add(option))
                return false;

            var value = args[++index];
            if (string.IsNullOrWhiteSpace(value) || value.StartsWith("--", StringComparison.Ordinal))
                return false;

            if (option == "--username")
                username = value;
            else
                operatorIdentity = value;
        }

        return seen.SetEquals(["--confirm", "--username", "--operator"])
            && !string.IsNullOrWhiteSpace(username)
            && username.Length <= 50
            && !username.Any(char.IsControl)
            && operatorIdentity.Length <= 80
            && !operatorIdentity.Any(char.IsControl);
    }

    private static string ReadHiddenPassword(string prompt)
    {
        Console.Write(prompt);
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return password.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                    password.Length--;
                continue;
            }

            if (!char.IsControl(key.KeyChar))
                password.Append(key.KeyChar);
        }
    }

    private static void WriteUsage() =>
        Console.Error.WriteLine(
            "Usage: dotnet ErpBackend.dll recover-admin --username <active-admin> --operator <ticket-or-operator-id> --confirm");
}
