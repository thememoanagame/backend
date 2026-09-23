using Infisical.Sdk;
using Infisical.Sdk.Model; 
using MemoAna.Infrastructure.Common.HealthChecks;
using MemoAna.Infrastructure.Identity.Options;
using MemoAna.Infrastructure.Persistence.Options; 
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.IdentityModel.Protocols.Configuration;
using Microsoft.Extensions.Hosting;
using DotNetEnv;

namespace MemoAna.Composition.Extensions;

/// <summary>Adds MemoAna services to the web host.</summary>
public static class WebApplicationBuilderExtensions
{
    extension(WebApplicationBuilder builder)
    {
        /// <summary>Runs the MemoAna web application.</summary>
        /// <typeparam name="TProgram">The program type.</typeparam>
        /// <typeparam name="TApp">The root component.</typeparam>
        /// <returns>A task for application startup.</returns>
        public async Task RunMemoAnaAsync<TProgram, TApp>(Action<WebApplicationBuilder> configurePresentationServices)
            where TProgram : class
            where TApp : IComponent
        {
            if (builder.Environment.IsDevelopment() && Environment.GetEnvironmentVariable("DOTNET_IS_RUNNING_ON_CONTAINER") != "true")
            {
                var currentDir = new DirectoryInfo(AppContext.BaseDirectory);
            
                while (currentDir != null && !File.Exists(Path.Combine(currentDir.FullName, ".env")))
                {
                    currentDir = currentDir.Parent;
                }
                
                if (currentDir is null)
                {
                    throw new InvalidOperationException("No .env found"); 
                }

                Env.Load(Path.Combine(currentDir.FullName, ".env"));
            } 
            _ = await builder.ConfigureSettings();
            _ = builder.ConfigurePresentation(configurePresentationServices);
            _ = builder.Services.ConfigureDatabase(builder.Configuration);
            _ = builder.Services.ConfigureIdentity();
            _ = builder.Services.ConfigureInfrastructureServices();
            _ = builder.Services.ConfigureAuth(builder.Configuration);
            _ = builder.Services.ConfigureApplicationServices();
            await builder.Build().RunMemoAnaAsync<TApp>();
        }
        
        private WebApplicationBuilder ConfigurePresentation(Action<WebApplicationBuilder> configurePresentationServices)
        {
            _ = builder.Services.AddCascadingAuthenticationState();
            _ = builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();
            _ = builder.Services.AddControllers();
            _ = builder.Services.AddOpenApi();
            configurePresentationServices?.Invoke(builder);
            _ = builder.Services
                .AddHealthChecks()
                .AddCheck<ApiCheck>("ApiCheck")
                .AddCheck<ApiDiskUsageCheck>("ApiDiskUsageCheck")
                .AddCheck<HostInfoCheck>("HostInfoCheck")
                .AddCheck<DatabaseCheck>("DatabaseCheck");
            return builder;
        }

        private async Task<WebApplicationBuilder> ConfigureSettings()
        {

            _ = builder.Configuration
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();

            //var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
            //builder.WebHost.UseUrls($"http://*:{port};https://*:{(int.Parse(port) + 443)}");

            string? cid = Environment.GetEnvironmentVariable("ICID");
            string? cs = Environment.GetEnvironmentVariable("ICS");
            string? sp = Environment.GetEnvironmentVariable("ISP") ?? "/";
            string? pid = Environment.GetEnvironmentVariable("IPID");
            string env = Environment.GetEnvironmentVariable("IENV")
                            ?? builder.Environment.EnvironmentName.ToLowerInvariant();

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

                builder.Configuration.AddInMemoryCollection(secrets.ToDictionary(
                    s => s.SecretKey.Replace("__", ":"),
                    s => s.SecretValue
                )!);
            }
            else
            {
                throw new InvalidConfigurationException("Infisical Machine Identity credentials are missing. Please set ICID, ICS, and IPID environment variables.");
            }
            _ = builder.Services.Configure<JwtOptions>(
                builder.Configuration.GetSection(
                    JwtOptions.SectionName));
            _ = builder.Services.Configure<ConnectionStringsOptions>(
                builder.Configuration.GetSection(
                    ConnectionStringsOptions.SectionName));
            _ = builder.Services.Configure<MongoDbOptions>(
                builder.Configuration.GetSection(
                    MongoDbOptions.SectionName));
            _ = builder.Services.Configure<LiteDbOptions>(
                builder.Configuration.GetSection(
                    LiteDbOptions.SectionName));
            _ = builder.Services.Configure<GooglePlayGamesOptions>(
                builder.Configuration.GetSection(
                    GooglePlayGamesOptions.SectionName));

            return builder;
        }
    }
}