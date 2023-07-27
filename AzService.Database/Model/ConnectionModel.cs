namespace AzService.Database.Model
{
    public class ConnectionModel
    {
        public readonly string AdminName;
        public readonly string AdminPassword;

        public ConnectionModel(string adminName, string password)
        {
            AdminName = adminName;
            AdminPassword = password;
        }

    }
}
