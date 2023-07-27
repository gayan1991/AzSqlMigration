namespace AzService.Database.Interface
{
    public interface ILogger
    {
        string? ReadLine();
        string ReadSecret();
        void WriteLine(string message, bool log = true);
    }
}
