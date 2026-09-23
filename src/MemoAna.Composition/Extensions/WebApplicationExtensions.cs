using MemoAna.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Scalar.AspNetCore;

namespace MemoAna.Composition.Extensions;
/// <summary>WebApplication extension methods class.</summary>
public static class WebApplicationExtensions
{
    extension(WebApplication app)
    {
        /// <summary>
        /// Overload Extension Method to setup http pipeline and run the app. 
        /// </summary>
        public async Task RunMemoAnaAsync<T>() where T : Microsoft.AspNetCore.Components.IComponent
        {
            _ = app.UseForwardedHeaders();
            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                _ = app.UseExceptionHandler("/Error", createScopeForErrors: true);
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                _ = app.UseHsts();
            }
            else
            {
                app.UseHttpsRedirection();
                _ = app.MapOpenApi("ma/{v1}.json").AllowAnonymous();
                _ = app.MapScalarApiReference("ma/scalar", async options =>
                {
                    _ = options.WithOpenApiRoutePattern("/ma/{documentName}.json");
                    _ = options.WithTitle($"MemoAna Backend: [{app.Environment.EnvironmentName}]");
                    
                });
            }
            _ = app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            _ = app.UseAuthentication();
            _ = app.UseAuthorization();
            _ = app.UseAntiforgery();

            _ = app.MapStaticAssets();
            _ = app.MapControllers();
            _ = app.MapRazorComponents<T>()
                .AddInteractiveServerRenderMode();
            await app.ApplyDatabaseMigrationsAsync();
            await app.RunAsync();
        }

        private async Task ApplyDatabaseMigrationsAsync()
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<WebApplication>>();
            try
            {
                var context = services.GetRequiredService<SQLiteDbContext>();

                if (context.Database.GetPendingMigrations().Any())
                {
                    logger.LogInformation("Applying pending migrations for SQLite...");
                    await context.Database.MigrateAsync();
                    await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ocorreu um erro crítico ao tentar aplicar as migrações do SQLite: {Message}", ex.Message);
                throw;
            }
        }
    }   
}
