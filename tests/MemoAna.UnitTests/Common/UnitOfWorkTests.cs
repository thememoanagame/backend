using MemoAna.Infrastructure.Common.UnitOfWork;
using MemoAna.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MemoAna.UnitTests.Common;

/// <summary>Tests database unit-of-work transactions.</summary>
public sealed class UnitOfWorkTests
{
    [Fact]
    public async Task UnitOfWork_CommitsAndRollsBackTransactions()
    {
        await using SqliteConnection connection =
            new("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        DbContextOptions<SQLiteDbContext> options =
            new DbContextOptionsBuilder<SQLiteDbContext>()
                .UseSqlite(connection)
                .Options;
        await using SQLiteDbContext context =
            new(options);
        _ = await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await using UnitOfWork unitOfWork =
            new(context);

        await unitOfWork.CommitTransactionAsync(
            TestContext.Current.CancellationToken);
        await unitOfWork.RollbackTransactionAsync(
            TestContext.Current.CancellationToken);
        Assert.Equal(0, await unitOfWork.SaveChangesAsync(
            TestContext.Current.CancellationToken));

        await unitOfWork.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        await unitOfWork.CommitTransactionAsync(
            TestContext.Current.CancellationToken);

        await unitOfWork.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        await unitOfWork.RollbackTransactionAsync(
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UnitOfWork_SecondBegin_DoesNotReplaceTransaction()
    {
        await using SqliteConnection connection =
            new("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        DbContextOptions<SQLiteDbContext> options =
            new DbContextOptionsBuilder<SQLiteDbContext>()
                .UseSqlite(connection)
                .Options;
        await using SQLiteDbContext context =
            new(options);
        _ = await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        await using UnitOfWork unitOfWork =
            new(context);

        await unitOfWork.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        await unitOfWork.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        await unitOfWork.RollbackTransactionAsync(
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UnitOfWork_DisposeRollsBackActiveTransaction()
    {
        await using SqliteConnection connection =
            new("Data Source=:memory:");
        await connection.OpenAsync(TestContext.Current.CancellationToken);
        DbContextOptions<SQLiteDbContext> options =
            new DbContextOptionsBuilder<SQLiteDbContext>()
                .UseSqlite(connection)
                .Options;
        await using SQLiteDbContext context =
            new(options);
        _ = await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
        UnitOfWork unitOfWork = new(context);

        await unitOfWork.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        await unitOfWork.DisposeAsync();
    }
}
