namespace AzService.Database.Util
{
    internal static class Helper
    {
        internal static string ExtractName(string url, string keyword)
        {
            var storageAccountName = url;

            if (url.Contains("http"))
            {
                var myUri = new Uri(url);
                storageAccountName = myUri.Host;
            }

            var index = storageAccountName.IndexOf(keyword);
            return storageAccountName.Substring(0, index);
        }
    }
}
