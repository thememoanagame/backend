namespace MemoAna.Domain.Common;

/// <summary>
/// Defines persistence metadata for an entity.
/// </summary>
public interface IEntityBase
{
    /// <summary>Gets the entity identifier.</summary>
    string Id { get; set; }

    /// <summary>Gets the creation timestamp.</summary>
    DateTime CreatedAt { get; set; }

    /// <summary>Gets the creator identifier.</summary>
    string CreatedBy { get; set; }

    /// <summary>Gets the last update timestamp.</summary>
    DateTime UpdatedAt { get; set; }

    /// <summary>Gets the last updater identifier.</summary>
    string UpdatedBy { get; set; }
}
