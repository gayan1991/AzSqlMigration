using AzService.Database.Model;

namespace AzMigration.Copy
{
    public interface IContextCredential
    {
        public ConnectionModel GetCredential();
    }
}
