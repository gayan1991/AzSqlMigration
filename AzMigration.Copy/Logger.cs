using AzService.Database.Interface;

namespace AzMigration.Copy
{
    public class Logger : ILogger
    {
        public void WriteLine(string message, bool log = true)
        {
            System.Console.WriteLine($"{(log ? $"{DateTimeOffset.UtcNow}: " : string.Empty)}{message}");
        }

        public string? ReadLine()
        {
            return System.Console.ReadLine();
        }

        public string ReadSecret()
        {
            var secret = string.Empty;

            ConsoleKey key;
            do
            {
                var keyInfo = System.Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && secret.Length > 0)
                {
                    System.Console.Write("\b \b");
                    secret = secret[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    System.Console.Write("*");
                    secret += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);

            if (key == ConsoleKey.Enter)
            {
                System.Console.Write(Environment.NewLine);
            }
            return secret;
        }
    }
}
