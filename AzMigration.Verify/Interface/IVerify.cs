namespace AzMigration.Verify.Interface
{
    public interface IVerify
    {
        Task<Dictionary<string, int>> GetCountAsync(string server, string catalog, string user, string password, string[] queries);
    }
}
