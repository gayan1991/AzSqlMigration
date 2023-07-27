using AzMigration.Copy;
using AzMigration.Copy.Util;
using Microsoft.Extensions.DependencyInjection;

var host = MigrationHostBuilder.Build(args);

var app = host.Services.GetRequiredService<Migration>();
await app.Run();
