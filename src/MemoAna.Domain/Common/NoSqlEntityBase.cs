namespace MemoAna.Domain.Common;

/// <summary>
/// Provides common persistence metadata for NoSQL domain entities.
/// </summary>
public abstract class NoSqlEntityBase<TId> : INoSqlEntityBase<TId> where TId : notnull, new()
{
    public TId Id { get; set; } 
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string UpdatedBy { get; set; } = string.Empty;

    public NoSqlEntityBase() => (Id, CreatedAt) = (new(), DateTime.UtcNow);
    public NoSqlEntityBase(TId id) : this() => Id = id ?? new();
    public NoSqlEntityBase(TId id, string by) : this(id) => CreatedBy = by ?? string.Empty;
    public NoSqlEntityBase(TId id, DateTime? at, string by) : this(id, by) => CreatedAt = at ?? DateTime.UtcNow;

}
