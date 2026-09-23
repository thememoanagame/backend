using Infisical.Sdk;
using Infisical.Sdk.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration; 

namespace MemoAna.Infrastructure.Persistence.Contexts;

public class SQLiteDbContextFactory : IDesignTimeDbContextFactory<SQLiteDbContext>
{
    public SQLiteDbContext CreateDbContext(string[] args)
    {
        string basePath = Directory.GetCurrentDirectory();


        Task.Run(async() =>
        {
            string? cid = Environment.GetEnvironmentVariable("ICID");
            string? cs = Environment.GetEnvironmentVariable("ICS");
            string? sp = Environment.GetEnvironmentVariable("ISP") ?? "/";
            string? pid = Environment.GetEnvironmentVariable("IPID");
            string env = Environment.GetEnvironmentVariable("IENV") ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            IConfigurationRoot configuration = null!; 
        
            if (!string.IsNullOrEmpty(cid) && !string.IsNullOrWhiteSpace(cid) && !string.IsNullOrEmpty(pid) && !string.IsNullOrWhiteSpace(pid) && !string.IsNullOrEmpty(cs)! && !string.IsNullOrWhiteSpace(cs))
            {

                var settings = new InfisicalSdkSettingsBuilder().Build();
                var client = new InfisicalClient(settings);

                MachineIdentityCredential credential = await client.Auth().UniversalAuth().LoginAsync(cid, cs);

                var options = new ListSecretsOptions
                {
                    SetSecretsAsEnvironmentVariables = true,
                    EnvironmentSlug = env,
                    SecretPath = sp,
                    Recursive = true,
                    ExpandSecretReferences = true,
                    ProjectId = pid ?? "",
                    ViewSecretValue = true,
                };

                Secret[] secrets = await client.Secrets().ListAsync(options)
                    ?? throw new InvalidOperationException("Failed to fetch secrets from Infisical");

                configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.Development.json", optional: true)
                    .AddEnvironmentVariables() 
                    .AddInMemoryCollection(secrets.ToDictionary(
                        s => s.SecretKey.Replace("__", ":"),
                        s => s.SecretValue
                    )!)
                    .Build();
                string? connectionString = configuration.GetConnectionString("SQLite");

                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("The 'SQLite' ConnectionString was not found at appsettings.json.");
                }

                return new SQLiteDbContext(
                    new DbContextOptionsBuilder<SQLiteDbContext>()
                    .UseSqlite(connectionString, options => options.CommandTimeout(90))
                    .Options);
            }
            else
            {
                
                System.Diagnostics.Debug.Write("Infisical Machine Identity credentials are missing. Please set ICID, ICS, and IPID environment variables.");
         
                configuration = new ConfigurationBuilder()
                    .SetBasePath(basePath)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.Development.json", optional: true)
                    .AddEnvironmentVariables() 
                    .Build();
                string? connectionString = configuration.GetConnectionString("SQLite");
                if (string.IsNullOrEmpty(connectionString))
                {
                    throw new InvalidOperationException("The 'SQLite' ConnectionString was not found at appsettings.json.");
                }

                return new SQLiteDbContext(
                    new DbContextOptionsBuilder<SQLiteDbContext>()
                    .UseSqlite(connectionString, options => options.CommandTimeout(90))
                    .Options);
            }
        });
        return new SQLiteDbContext(
            new DbContextOptionsBuilder<SQLiteDbContext>()
            .UseSqlite(Guid.CreateVersion7().ToString(), options => options.CommandTimeout(90))
            .Options);
    }
 
}
