using AzService.Database.Util;
using System.ComponentModel;

namespace AzService.Database.Model
{
    public class MigrationConfig
    {
        public List<DBConfig> DatabaseConfigs { get; set; } = null!;
        public CloudStorageConfig Source { get; set; } = null!;
        public CloudStorageConfig Target { get; set; } = null!;
    }

    public class CloudStorageConfig
    {
        public string ResourceGroup { get; set; } = null!;
        public string StorageKey { get; private set; } = null!;
        public string StorageType { get; set; } = "StorageAccessKey";
        public string AuthType { get; set; } = "SQL";
        public string Subscription { get; set; } = null!;
        public string StorageAccountUrl { get; set; } = null!;
        public string Container { get; set; } = null!;

        public string StorageUrl
        {
            get
            {
                var url = StorageAccountUrl + (StorageAccountUrl[^1] == '/' ? string.Empty : "/");
                return $"{url}{Container}/";
            }
        }

        public string StorageName => Helper.ExtractName(StorageAccountUrl, ".blob");

        public void SetStorageKey(string key) => StorageKey = key;

    }

    public class DBConfig
    {
        public string SourceResourceGroup { get; set; } = null!;
        public string SourceServer { get; set; } = null!;
        public string SourceDB { get; set; } = null!;
        public string TargetResourceGroup { get; set; } = null!;
        public string TargetServer { get; set; } = null!;
        public string TargetDB { get; set; } = null!;

        public string GetBlobUrl(string storageUrl)
        {
            return $"{storageUrl}{SourceResourceGroup}-{SourceDB}.bacpac";
        }

        #region Computed Properties

        public string SourceServerName => Helper.ExtractName(SourceServer, ".database");
        public string TargetServerName => Helper.ExtractName(TargetServer, ".database");

        #endregion
        
    }
}
