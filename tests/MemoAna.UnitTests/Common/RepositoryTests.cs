using MemoAna.Domain.Game;
using MemoAna.Infrastructure.Common.Repository;
using MemoAna.Infrastructure.Persistence;
using MemoAna.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;

namespace MemoAna.UnitTests.Common;

/// <summary>Tests the relational repository against the existing EF test provider.</summary>
public sealed class RepositoryTests
{
    [Fact]
    public async Task RepositorySupportsCrudListAndMissingEntities()
    {
        await using SQLiteDbContext context = CreateContext();
        Repository<Theme> repository = new(context);
        Theme first = new()
        {
            Name = "theme-1",
            ThumbnailId = Guid.CreateVersion7().ToString(),
            Description = "description-1",
            Cards = new List<(string Id, string Name)>
            {
                (Guid.CreateVersion7().ToString(), "card-1"),
                (Guid.CreateVersion7().ToString(), "card-2")
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            Id = Guid.CreateVersion7().ToString()
        };
        Theme second = new()
        {
            Name = "theme-2",
            ThumbnailId = Guid.CreateVersion7().ToString(),
            Description = "description-2",
            Cards = new List<(string Id, string Name)>
            {
                (Guid.CreateVersion7().ToString(), "card-3"),
                (Guid.CreateVersion7().ToString(), "card-4")
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            Id = Guid.CreateVersion7().ToString()
        };

        await repository.AddAsync(first, TestContext.Current.CancellationToken);
        await repository.AddAsync(second, TestContext.Current.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(first, await repository.GetByIdAsync(first.Id, TestContext.Current.CancellationToken));
        Assert.Equal(2, (await repository.ListAsync(x => true, false, TestContext.Current.CancellationToken)).Count);

        first.Name = "theme-1-updated";
        Assert.True(await repository.UpdateAsync(first, TestContext.Current.CancellationToken));
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(
            "theme-1-updated",
            (await repository.GetByIdAsync(first.Id, TestContext.Current.CancellationToken))?.Name);

        Assert.True(await repository.RemoveAsync(second.Id, TestContext.Current.CancellationToken));
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Null(await repository.GetByIdAsync(second.Id, TestContext.Current.CancellationToken));
        Assert.False(await repository.RemoveAsync(second.Id, TestContext.Current.CancellationToken));
        Assert.Single(await repository.ListAsync(
            theme => theme.ThumbnailId == "manifest-updated", false, TestContext.Current.CancellationToken));
    }

    private static SQLiteDbContext CreateContext()
    {
        DbContextOptions<SQLiteDbContext> options =
            new DbContextOptionsBuilder<SQLiteDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        return new SQLiteDbContext(options);
    }
}
