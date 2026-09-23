using LiteDB;
using MemoAna.Application.Common.Abstractions;
using MemoAna.Domain.Common;
using MemoAna.Domain.Game;
using MemoAna.Infrastructure.Persistence.Contexts;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using System.Linq.Expressions;

namespace MemoAna.Infrastructure.Common.Repository;

/// <summary>
/// Provides LiteDB persistence operations for supported NoSQL documents.
/// </summary>
/// <typeparam name="TEntity">The supported NoSQL document type.</typeparam>
public sealed class NoRepository<TEntity>(
    LiteDbContext context) : INoRepository<TEntity, BsonValue>
    where TEntity : NoSqlEntityBase<BsonValue>
{
    private readonly ILiteCollection<TEntity> collection = context.GetCollection<TEntity>();
    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return await Task.Run(async () =>
        {
            BsonExpression expression = BsonExpression.Create("_id = $.id", new BsonValue(id));
            if (collection.FindOne(expression) is not TEntity result) throw new KeyNotFoundException($"Data not found for _id: {id}");
            return result;
        });
    }

    public async Task<TEntity?> FirstOrDefaultAsync(string predicate, 
        object[] parameters,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(parameters);
        return await Task.Run(async() =>
        {
            BsonExpression expression = BsonExpression.Create(predicate, [.. parameters.Select(p => new BsonValue(p))]);
            if (collection.FindOne(expression) is not TEntity result) return default(TEntity);
            return result;
        });
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> ListAsync(
        string? predicate,
        object[] parameters,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(async() =>
        {
            IEnumerable<TEntity> results = string.IsNullOrEmpty(predicate) || string.IsNullOrWhiteSpace(predicate) ?
                collection.FindAll() :
                collection.Find(BsonExpression.Create(predicate));
            return results.ToList().AsReadOnly();
        }); 
    }

    /// <inheritdoc />
    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        await Task.Run(async () =>
        {
            collection.Insert(entity);
        });
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            return collection.Update(entity.Id, entity);
        });
    }

    public async Task<bool> UpsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return await Task.Run(() =>
        {
            return collection.Upsert(entity.Id, entity);
        });
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            if (collection.Exists(entity.Id))   
                return collection.Delete(entity.Id);
            else 
                throw new KeyNotFoundException($"Data not found for _id: {entity.Id}");
        });
    } 
}
