using AzService.Database.Model;

namespace AzService.Database.Interface
{
    public interface IAzService
    {
        Task LogIn();
        Task Copy(string source, string target);
        Task SelectSubscription(string subscriptionName);
        Task<List<StorageKeys>> GetStorageKeys(string resourceGroupName, string storageAccountName);
        Task<string> GenerateBlobStorageSASUrl(string storageAccountName, string storageAccessKey, string storageUrl, string containerName = null!);
        Task<List<string>> GetFileListInContainer(string storageAccountName, string storageAccessKey, string storageUrl, string containerName);
        Task Export(string adminName, string adminPassword, string storageKey, string storageKeyType, string storageUri,
                            string authType, string dbName, string resourceGroup, string serverName, string subscription);
        Task Import(string adminName, string adminPassword, string storageKey, string storageKeyType, string storageUri,
                            string authType, string dbName, string resourceGroup, string serverName, string subscription);
    }
}
