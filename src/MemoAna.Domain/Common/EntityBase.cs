namespace MemoAna.Domain.Common;

/// <summary>
/// Provides common persistence metadata.
/// </summary>
public abstract class EntityBase(string id = "",
    DateTime? createdAt = null) : IEntityBase
{
    /// <inheritdoc />
    public string Id { get; set; } = id == string.Empty ?
        Guid.CreateVersion7().ToString() : id;

    /// <inheritdoc />
    public DateTime CreatedAt { get; set; } =
        createdAt ?? DateTime.UtcNow;

    /// <inheritdoc />
    public string CreatedBy { get; set; } =
        string.Empty;

    /// <inheritdoc />
    public DateTime UpdatedAt { get; set; } =
        createdAt ?? DateTime.UtcNow;

    /// <inheritdoc />
    public string UpdatedBy { get; set; } =
        string.Empty;
}
