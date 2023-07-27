using AzMigration.Verify.Interface;
using AzService.Database.Interface;
using Microsoft.Extensions.DependencyInjection;

namespace AzMigration.Copy
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IAzService, AzService.Database.Implementation.AzService>();
            services.AddSingleton<IContextCredential, ContextCredential>();
            services.AddSingleton<IVerify, Verify.Implementation.Verify>();
            services.AddSingleton<ILogger, Logger>();
        }
    }
}
