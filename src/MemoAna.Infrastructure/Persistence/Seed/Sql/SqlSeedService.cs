using System.Security.Claims; 
using MemoAna.Application.Common.Contracts;
using MemoAna.Application.Seed.Abstractions;
using MemoAna.Application.Seed.Dtos;
using MemoAna.Application.Seed.Exceptions;
using MemoAna.Infrastructure.Identity.Models;
using MemoAna.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MemoAna.Infrastructure.Persistence.Seed.Sql;

/// <summary>
/// Seeds Identity and the relational references shared with LiteDB card assets.
/// </summary>
public sealed class SqlSeedService(
    SQLiteDbContext dbContext,
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IConfiguration configuration,
    ILogger<SqlSeedService> logger) : ISqlSeedService
{
    private readonly string SeedUserEmail = configuration["Seed:App:User"]! ?? throw new ArgumentNullException("appuser");
    private readonly string SeedActor = configuration["Seed:App:Actor"]!;

    private static readonly RoleDefinition[] Roles =
    [
        new(IdentityRoles.Administrator, "system.admin"),
        new(IdentityRoles.User, "system.user")
    ];

    /// <inheritdoc />
    public async Task<SeedStatusDto> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        User? user = await userManager.FindByEmailAsync(SeedUserEmail);
        bool applicationUserExists = user is not null;
        bool administratorRoleLinked = applicationUserExists
            && await userManager.IsInRoleAsync(user!, IdentityRoles.Administrator);
        bool rolesExist = await AreRolesReadyAsync(cancellationToken);
         

        List<string> missing = [];
        if (!applicationUserExists)
        {
            missing.Add("Application user is missing.");
        }

        if (!rolesExist)
        {
            missing.Add("One or more application roles or permissions are missing.");
        }

        if (!administratorRoleLinked)
        {
            missing.Add("The seed user is not linked to the administrator role.");
        }

        return new SeedStatusDto(
            applicationUserExists,
            rolesExist,
            administratorRoleLinked,
            missing.Count > 0,
            missing);
    }

    /// <inheritdoc />
    public async Task<SeedOperationResultDto> SeedAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            SeedStatusDto currentStatus = await GetStatusAsync(cancellationToken);
            if (!currentStatus.SeedRequired)
            {
               return new(true, await roleManager.Roles.Select(x => x.Name!).ToListAsync(cancellationToken), currentStatus);
            }

            List<string> rolesCreated = [];
            
            bool userCreated = await EnsureIdentityAsync(
                rolesCreated);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            SeedStatusDto finalStatus = await GetStatusAsync(cancellationToken);
            if (finalStatus.SeedRequired)
            {
                var e = new SeedException("The seed completed but the application is still missing prerequisites.", finalStatus.MissingRequirements);
                logger.LogError(e, "{Message}", e.Message);
                throw e;
            }

            logger.LogInformation(
                "Application seed completed. UserCreated={UserCreated}, RolesCreated={RolesCreated}",
                userCreated,
                rolesCreated.Count);

            return new SeedOperationResultDto(
                userCreated,
                rolesCreated,
                finalStatus);
        }
        catch (InvalidOperationException exception)
        {
            var e = new SeedException(
                "The application seed could not be completed.",
                [exception.Message],
                exception);
            logger.LogError(e, "{Message}", e.Message);
            throw e;
        }
        catch (DbUpdateException exception)
        {
            var e = new SeedException(
                "The application seed could not persist PostgreSQL data.",
                [exception.GetBaseException().Message],
                exception);
            logger.LogError(e, "{Message}", e.Message);
            throw e;
        }
    }

    private async Task<bool> EnsureIdentityAsync(
        List<string> rolesCreated)
    {
        foreach (RoleDefinition definition in Roles)
        {
            Role? role = await roleManager.FindByNameAsync(definition.Name);
            if (role is null)
            {
                role = new Role(definition.Name);
                IdentityResult createRoleResult = await roleManager.CreateAsync(role);
                EnsureIdentitySuccess(
                    createRoleResult,
                    $"Could not create role '{definition.Name}'.");
                rolesCreated.Add(definition.Name);
            }

            IList<Claim> claims = await roleManager.GetClaimsAsync(role);
            if (!claims.Any(claim =>
                    claim.Type == IdentityClaimTypes.Permission
                    && claim.Value == definition.Permission))
            {
                IdentityResult claimResult = await roleManager.AddClaimAsync(
                    role,
                    new Claim(IdentityClaimTypes.Permission, definition.Permission));
                EnsureIdentitySuccess(
                    claimResult,
                    $"Could not configure role '{definition.Name}'.");
            }
        }

        User? user = await userManager.FindByEmailAsync(SeedUserEmail);
        bool created = user is null;
        if (user is null)
        {
            user = new User(SeedUserEmail)
            {
                Email = SeedUserEmail,
                EmailConfirmed = true,
                DisplayName = SeedUserEmail,
                CreatedBy = SeedActor,
                UpdatedBy = SeedActor
            };
            IdentityResult createUserResult = await userManager.CreateAsync(user);
            EnsureIdentitySuccess(
                createUserResult,
                $"Could not create application user '{SeedUserEmail}'.");
        }

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            IdentityResult updateUserResult = await userManager.UpdateAsync(user);
            EnsureIdentitySuccess(
                updateUserResult,
                $"Could not confirm application user '{SeedUserEmail}'.");
        }

        if (!await userManager.IsInRoleAsync(user, IdentityRoles.Administrator))
        {
            IdentityResult roleResult = await userManager.AddToRoleAsync(
                user,
                IdentityRoles.Administrator);
            EnsureIdentitySuccess(
                roleResult,
                $"Could not link '{SeedUserEmail}' to the administrator role.");
        }

        return created;
    }

    private async Task<bool> AreRolesReadyAsync(
        CancellationToken cancellationToken)
    {
        foreach (RoleDefinition definition in Roles)
        {
            Role? role = await roleManager.FindByNameAsync(definition.Name);
            if (role is null)
            {
                return false;
            }

            IList<Claim> claims = await roleManager.GetClaimsAsync(role);
            if (!claims.Any(claim =>
                    claim.Type == IdentityClaimTypes.Permission
                    && claim.Value == definition.Permission))
            {
                return false;
            }
        }

        await dbContext.Database.CanConnectAsync(cancellationToken);
        return true;
    }
    
    private static void EnsureIdentitySuccess(
        IdentityResult result,
        string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        throw new SeedException(
            message,
            [.. result.Errors.Select(error => error.Description)]);
    }

    private sealed record RoleDefinition(string Name, string Permission);
}
