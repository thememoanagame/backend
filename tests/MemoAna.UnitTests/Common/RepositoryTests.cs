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
        Repository<CardThemeEntity> repository = new(context);
        CardThemeEntity first = new()
        {
            ManifestId = "manifest-1"
        };
        CardThemeEntity second = new()
        {
            ManifestId = "manifest-2"
        };

        await repository.AddAsync(first, TestContext.Current.CancellationToken);
        await repository.AddAsync(second, TestContext.Current.CancellationToken);
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        Assert.Equal(first, await repository.GetByIdAsync(first.Id, TestContext.Current.CancellationToken));
        Assert.Equal(2, (await repository.ListAsync(x => true, false, TestContext.Current.CancellationToken)).Count);

        first.ManifestId = "manifest-updated";
        Assert.True(await repository.UpdateAsync(first, TestContext.Current.CancellationToken));
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Equal(
            "manifest-updated",
            (await repository.GetByIdAsync(first.Id, TestContext.Current.CancellationToken))?.ManifestId);

        Assert.True(await repository.RemoveAsync(second.Id, TestContext.Current.CancellationToken));
        _ = await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        Assert.Null(await repository.GetByIdAsync(second.Id, TestContext.Current.CancellationToken));
        Assert.False(await repository.RemoveAsync(second.Id, TestContext.Current.CancellationToken));
        Assert.Single(await repository.ListAsync(
            theme => theme.ManifestId == "manifest-updated", false, TestContext.Current.CancellationToken));
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
