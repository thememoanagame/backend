using MemoAna.Domain.Common;
using MemoAna.Domain.Game;
using MemoAna.Infrastructure.Identity.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace MemoAna.Infrastructure.Persistence.Contexts;

/// <summary>Represents the Postgres database context.</summary>
public sealed class SQLiteDbContext(DbContextOptions<SQLiteDbContext> options)
    : IdentityDbContext<User, Role, string>(options)
{
    public DbSet<Theme> Themes => Set<Theme>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Card> Cards => Set<Card>();
 
    /// <inheritdoc />
    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // foreach (IMutableEntityType entityType
        //     in builder.Model.GetEntityTypes())
        // {
        //     if (!typeof(ISoftDeletable).IsAssignableFrom(
        //         entityType.ClrType))
        //     {
        //         continue;
        //     }

        //     ParameterExpression parameter =
        //         Expression.Parameter(
        //             entityType.ClrType,
        //             "entity");
        //     MemberExpression property =
        //         Expression.Property(
        //             parameter,
        //             nameof(ISoftDeletable.IsDeleted));
        //     LambdaExpression filter =
        //         Expression.Lambda(
        //             Expression.Not(property),
        //             parameter);

        //     builder.Entity(entityType.ClrType)
        //         .HasQueryFilter(filter);
        // }

        _ = builder.ApplyConfigurationsFromAssembly(
            typeof(SQLiteDbContext).Assembly);
    }

    /// <inheritdoc />
    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
    {
        // ApplyEntityIdentifiers();
        // ApplySoftDelete();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override int SaveChanges()
    {
        // ApplyEntityIdentifiers();
        // ApplySoftDelete();
        return base.SaveChanges();
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        // ApplyEntityIdentifiers();
        // ApplySoftDelete();
        return base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        // ApplyEntityIdentifiers();
        // ApplySoftDelete();
        return base.SaveChangesAsync(
            cancellationToken);
    }

    private void ApplyEntityIdentifiers()
    {
        foreach (EntityEntry<IEntityBase> entry
            in ChangeTracker.Entries<IEntityBase>())
        {
            if (entry.State != EntityState.Added)
            {
                continue;
            }

            if (entry.Entity is User
                || entry.Entity is Role)
            {
                entry.Entity.Id =
                    Guid.CreateVersion7().ToString();
            }
        }
    }

    private void ApplySoftDelete()
    {
        foreach (EntityEntry<ISoftDeletable> entry
            in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt =
                DateTime.UtcNow;
        }
    }
}
