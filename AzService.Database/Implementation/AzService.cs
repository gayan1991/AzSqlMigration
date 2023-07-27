using AzService.Database.Interface;
using AzService.Database.Model;
using AzService.Database.Util;
using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace AzService.Database.Implementation
{
    public class AzService : IAzService
    {
        private readonly ILogger _logger;

        public AzService(ILogger logger)
        {
            _logger = logger;
        }

        public Task LogIn()
        {
            return Command.Execute("az login");
        }

        public Task SelectSubscription(string subscriptionName)
        {
            return Command.Execute($"az account set --subscription {subscriptionName}");
        }

        public async Task<List<StorageKeys>> GetStorageKeys(string resourceGroupName, string storageAccountName)
        {
            var result = await Command.ExecuteWithOutput($"az storage account keys list --resource-group {resourceGroupName} --account-name {storageAccountName}");

            if (string.IsNullOrEmpty(result))
            {
                _logger.WriteLine("Storage Keys: empty output");
                return null!;
            }

            var rtnObj = Newtonsoft.Json.JsonConvert.DeserializeObject<List<StorageKeys>>(result);
            return rtnObj!;
        }

        public Task Export(string adminName, string adminPassword, string storageKey, string storageKeyType, string storageUri,
                            string authType, string dbName, string resourceGroup, string serverName, string subscription)
        {
            var script = "az sql db export ";
            script += $"--admin-password {adminPassword} ";
            script += $"--admin-user {adminName} ";
            script += $"--storage-key {storageKey} ";
            script += $"--storage-key-type {storageKeyType} ";
            script += $"--storage-uri {storageUri} ";
            script += $"--auth-type {authType} ";
            script += $"--name {dbName} ";
            script += $"--no-wait ";
            script += $"--resource-group {resourceGroup} ";
            script += $"--server {serverName} ";
            script += $"--subscription {subscription}";

            return Command.Execute(script);
        }

        public Task Import(string adminName, string adminPassword, string storageKey, string storageKeyType, string storageUri,
                            string authType, string dbName, string resourceGroup, string serverName, string subscription)
        {
            try
            {
                var script = "az sql db import ";
                script += $"--admin-password {adminPassword} ";
                script += $"--admin-user {adminName} ";
                script += $"--storage-key {storageKey} ";
                script += $"--storage-key-type {storageKeyType} ";
                script += $"--storage-uri {storageUri} ";
                script += $"--auth-type {authType} ";
                script += $"--name {dbName} ";
                script += $"--no-wait ";
                script += $"--resource-group {resourceGroup} ";
                script += $"--server {serverName} ";
                script += $"--subscription {subscription}";

                return Command.Execute(script);
            }
            finally
            {
                _logger.WriteLine($"Import is completed in {serverName}.{dbName}");
            }
        }

        public Task Copy(string source, string target)
        {
            var copyScript = $"azcopy copy \"{source}\" \"{target}\" --recursive=true --overwrite=false";
            _logger.WriteLine(copyScript);
            return Command.Execute(copyScript, true);
        }

        public async Task<string> GenerateBlobStorageSASUrl(string storageAccountName, string storageAccessKey, string storageUrl, string containerName = null!)
        {
            var urlStr = string.Empty;
            var accountUri = new Uri(storageUrl);
            var sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, storageAccessKey);

            var client = new BlobServiceClient(accountUri, sharedKeyCredential);
            try
            {
                var permissionString = "racwl";
                var startsOn = DateTimeOffset.UtcNow.AddHours(-1);
                var expiresOn = DateTime.Now.AddDays(1);

                if (string.IsNullOrWhiteSpace(containerName))
                {
                    var sasBuilder = new AccountSasBuilder()
                    {
                        StartsOn = startsOn,
                        ExpiresOn = expiresOn,
                        ResourceTypes = AccountSasResourceTypes.All,
                        Services = AccountSasServices.Blobs,
                    };
                    sasBuilder.SetPermissions(permissionString);
                    var sasUri = client.GenerateAccountSasUri(sasBuilder);
                    urlStr = sasUri.AbsoluteUri;
                }
                else
                {
                    var container = client.GetBlobContainerClient(containerName);
                    if (!await container.ExistsAsync())
                    {
                        _logger.WriteLine($"Container {containerName} does not exists");
                        _logger.WriteLine($"Creating container {containerName}");
                        container.CreateIfNotExists();
                        await Task.Delay(1000);
                    }

                    var blobSasBuilder = new BlobSasBuilder()
                    {
                        BlobContainerName = containerName,
                        ExpiresOn = expiresOn,
                        StartsOn = startsOn,
                        Protocol = SasProtocol.Https,
                    };
                    blobSasBuilder.SetPermissions(permissionString);
                    var sasUri = container.GenerateSasUri(blobSasBuilder);
                    urlStr = sasUri.AbsoluteUri;
                }

                _logger.WriteLine($"SAS URI for blob is: {urlStr}");
            }
            catch (RequestFailedException e)
            {
                _logger.WriteLine(e.ErrorCode!);
                _logger.WriteLine(e.Message);
            }

            return urlStr;
        }

        public async Task<List<string>> GetFileListInContainer(string storageAccountName, string storageAccessKey, string storageUrl, string containerName)
        {
            var lst = new List<string>();
            var accountUri = new Uri(storageUrl);
            var sharedKeyCredential = new StorageSharedKeyCredential(storageAccountName, storageAccessKey);

            var client = new BlobServiceClient(accountUri, sharedKeyCredential);

            try
            {
                var container = client.GetBlobContainerClient(containerName);
                var resultSegment = container.GetBlobsAsync().AsPages(default, null);

                await foreach (Page<BlobItem> blobPage in resultSegment)
                {
                    foreach (BlobItem blobItem in blobPage.Values)
                    {
                        lst.Add(blobItem.Name);
                    }
                }
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }

            return lst;
        }
    }
}
