using System.Linq.Expressions;
using MemoAna.Domain.Common;

namespace MemoAna.Application.Common.Abstractions;

/// <summary>
/// Defines persistence operations for supported NoSQL documents.
/// </summary>
/// <typeparam name="TEntity">The supported NoSQL entity type.</typeparam>
public interface INoRepository<TEntity, TId>
    where TEntity : class, INoSqlEntityBase<TId> 
    where TId : notnull, new()
{
    /// <summary>Gets a document by its stable string identifier.</summary>
    Task<TEntity?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the first document matching the supplied predicate.</summary>
    Task<TEntity?> FirstOrDefaultAsync(
        string predicate,
        object[] parameters,
        CancellationToken cancellationToken = default);

    /// <summary>Lists supported documents, optionally filtered by a predicate.</summary>
    Task<IReadOnlyList<TEntity>> ListAsync(
        string? predicate,
        object[] parameters,
        CancellationToken cancellationToken = default);

    /// <summary>Inserts a document.</summary>
    Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces an existing document.</summary>
    Task<bool> UpdateAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>Replaces a document matching the entity Id, inserting it when absent.</summary>
    Task<bool> UpsertAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a document by its stable string identifier.</summary>
    Task<bool> RemoveAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
}
