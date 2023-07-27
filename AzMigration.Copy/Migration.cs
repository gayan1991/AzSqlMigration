using AzMigration.Verify.Interface;
using AzService.Database.Interface;
using AzService.Database.Model;
using AzService.Database.Util;
using Newtonsoft.Json;

namespace AzMigration.Copy
{
    public class Migration
    {
        private readonly string[] queries = null!;
        private readonly MigrationConfig config = null!;
        private const string fileName = "Migration.Config.json";
        private const string verificationFileName = "VerificationQueries.json";
        private readonly ILogger _logger;
        private readonly IVerify _verify;
        private readonly IAzService _azService;
        private readonly IContextCredential _contextCredential;

        public Migration(IAzService azService, IVerify verify, ILogger logger, IContextCredential contextCredential)
        {
            _verify = verify;
            _logger = logger;
            _azService = azService;
            _contextCredential = contextCredential;

            var file = System.IO.File.ReadAllText(fileName);
            _logger.WriteLine($"{fileName} is requested");

            if (string.IsNullOrEmpty(file))
            {
                _logger.WriteLine($"{fileName} is not found");
                return;
            }

            config = JsonConvert.DeserializeObject<MigrationConfig>(file)!;
            _logger.WriteLine($"Configuration is set from Migration Config");

            file = System.IO.File.ReadAllText(verificationFileName);
            _logger.WriteLine($"{fileName} is requested");

            if (string.IsNullOrEmpty(file))
            {
                _logger.WriteLine($"{verificationFileName} is not found");
                return;
            }

            queries = JsonConvert.DeserializeObject<string[]>(file)!;
            _logger.WriteLine($"Configuration is set from verification");
        }

        internal async Task Run()
        {
            #region LogIn & Connection

            await _azService.LogIn();
            _logger.WriteLine($"Making a connection with {config.Source.StorageName}");
            config.Source.SetStorageKey(await GetStorageKey(config.Source.Subscription,
                                                            config.Source.ResourceGroup,
                                                            config.Source.StorageName));

            _logger.WriteLine($"Making a connection with {config.Target.StorageName}");
            config.Target.SetStorageKey(await GetStorageKey(config.Target.Subscription,
                                                                    config.Target.ResourceGroup,
                                                                    config.Target.StorageName));

            var credential = _contextCredential.GetCredential();

            #endregion

            #region Export

            foreach (var account in config.DatabaseConfigs)
            {
                await _azService.Export(credential.AdminName,
                                    credential.AdminPassword,
                                    config.Source.StorageKey,
                                    config.Source.StorageType,
                                    account.GetBlobUrl(config.Source.StorageUrl),
                                    config.Source.AuthType,
                                    account.SourceDB,
                                    account.SourceResourceGroup,
                                    account.SourceServerName,
                                    config.Source.Subscription);

                _logger.WriteLine($"Export request raised for {account.SourceServerName}.{account.SourceDB}");
            }

            _logger.WriteLine($"Waiting for export completion");
            await WaitingExportScreen();

            #endregion

            #region Az Copy

            _logger.WriteLine($"Making a connection with {config.Source.StorageName}.{config.Source.Container}");
            var sourceURL = await _azService.GenerateBlobStorageSASUrl(config.Source.StorageName,
                                                                        config.Source.StorageKey,
                                                                        config.Source.StorageAccountUrl,
                                                                        config.Source.Container);

            _logger.WriteLine($"Making a connection with {config.Target.StorageName}.{config.Source.Container}");
            var targetURL = await _azService.GenerateBlobStorageSASUrl(config.Target.StorageName,
                                                                       config.Target.StorageKey,
                                                                       config.Target.StorageAccountUrl,
                                                                       config.Source.Container);

            _logger.WriteLine($"Copying to target");
            await _azService.Copy(sourceURL, targetURL);

            #endregion

            #region Import

            var ls = new List<Task>();
            foreach (var account in config.DatabaseConfigs)
            {
                var tsk = _azService.Import(credential.AdminName,
                                    credential.AdminPassword,
                                    config.Target.StorageKey,
                                    config.Target.StorageType,
                                    account.GetBlobUrl(config.Target.StorageUrl),
                                    config.Target.AuthType,
                                    account.TargetDB,
                                    account.TargetResourceGroup,
                                    account.TargetServerName,
                                    config.Target.Subscription);

                ls.Add(tsk);
            }

            _logger.WriteLine($"Import opration begins");
            ProgressIndicator.Show();
            try
            {
                await Task.WhenAll(ls);
            }
            finally
            {
                ProgressIndicator.Hide();
            }

            #endregion

            #region Verification

            var notMatchedList = new List<string>();
            _logger.WriteLine($"Verification opration begins");
            ProgressIndicator.Show();
            try
            {
                notMatchedList = await VerifyAsync(credential);
            }
            finally
            {
                ProgressIndicator.Hide();
                _logger.WriteLine($"Verification is completed");
            }

            foreach (var notMatch in notMatchedList)
                _logger.WriteLine(notMatch);

            #endregion
        }

        #region Private

        private async Task<string> GetStorageKey(string subscription, string resourceGroupName, string accountName)
        {
            await _azService.SelectSubscription(subscription);
            var keys = await _azService.GetStorageKeys(resourceGroupName, accountName);

            if (keys != null && keys.Count > 0)
            {
                return keys.Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => x.Value ?? string.Empty).First();
            }

            return string.Empty;
        }

        private async Task WaitingExportScreen()
        {
            ProgressIndicator.Show();
            try
            {
                var count = 0;
                while (config.DatabaseConfigs.Count != count)
                {
                    await Task.Delay(5000);

                    var fileNames = await _azService.GetFileListInContainer(config.Source.StorageName,
                                                                            config.Source.StorageKey,
                                                                            config.Source.StorageAccountUrl,
                                                                            "migrationbackups");

                    count = fileNames.Count;
                }
            }
            finally
            {
                ProgressIndicator.Hide();
            }
        }

        private async Task<List<string>> VerifyAsync(ConnectionModel credential)
        {
            var ls = new List<Task>();
            var rtn = new List<string>();
            foreach (var account in config.DatabaseConfigs)
            {
                var tsk = Task.Run(async () =>
                {
                    var sourceResult = await _verify.GetCountAsync(account.SourceServer, account.SourceDB, credential.AdminName, credential.AdminPassword, queries);
                    var targetResult = await _verify.GetCountAsync(account.TargetServer, account.TargetDB, credential.AdminName, credential.AdminPassword, queries);

                    foreach (var query in queries)
                    {
                        if (sourceResult[query] != targetResult[query])
                        {
                            rtn.Add($"{query} count {account.SourceServer},{account.SourceDB} - {sourceResult[query]} is not matched with {account.TargetServer},{account.TargetDB} - {targetResult[query]}");
                        }
                    }
                });
                ls.Add(tsk);
            }

            await Task.WhenAll(ls);

            return rtn;
        }

        #endregion
    }
}
