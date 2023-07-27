using AzMigration.Verify.Interface;
using Microsoft.Data.SqlClient;

namespace AzMigration.Verify.Implementation
{
    public class Verify : IVerify
    {
        public async Task<Dictionary<string, int>> GetCountAsync(string server, string catalog, string user, string password, string[] queries)
        {
            var rtn = new Dictionary<string, int>();
            var connection = new SqlConnection(GetConnectionString(server, catalog, user, password));

            var command = new SqlCommand("", connection);
            await command.Connection.OpenAsync();

            try
            {
                foreach (var query in queries)
                {
                    command.CommandText = query;
                    var result = Convert.ToInt16(await command.ExecuteScalarAsync());
                    rtn.Add(query, result);
                }
            }
            finally { command.Connection.Close(); }

            return rtn;
        }

        #region Private

        private string GetConnectionString(string server, string catalog, string user, string password) =>
            $"Server={server};Initial Catalog={catalog};Persist Security Info=False;User ID={user};Password={password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;";

        #endregion
    }
}
