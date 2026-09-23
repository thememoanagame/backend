namespace MemoAna.Domain.Common;

/// <summary>
/// Defines the common contract for domain entities persisted in a NoSQL store.
/// </summary>
public interface INoSqlEntityBase<TId> where TId : notnull, new()
{
    /// <summary>Gets the entity identifier.</summary>
    TId Id { get; set; }

    /// <summary>Gets the creation timestamp.</summary>
    DateTime CreatedAt { get; }

    /// <summary>Gets the creator identifier.</summary>
    string CreatedBy { get; }

    /// <summary>Gets the last update timestamp.</summary>
    DateTime UpdatedAt { get; set; }

    /// <summary>Gets the last updater identifier.</summary>
    string UpdatedBy { get; set; }

}
