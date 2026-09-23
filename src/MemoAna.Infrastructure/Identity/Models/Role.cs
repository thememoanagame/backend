using MemoAna.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace MemoAna.Infrastructure.Identity.Models;

/// <summary>
/// Represents an MemoAna application role.
/// </summary>
public class Role
    : IdentityRole<string>,
      IEntityBase,
      ISoftDeletable
{
    protected Role() : base() => Id = Guid.CreateVersion7().ToString();
    
    public Role(string? name = null) : base(name ?? "user") => Id = Guid.CreateVersion7().ToString();
    /// <summary>
    /// Gets or sets whether the role is deleted.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the deletion timestamp.
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } =
        DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the creator identifier.
    /// </summary>
    public string CreatedBy { get; set; } =
        string.Empty;

    /// <summary>
    /// Gets or sets the last update timestamp.
    /// </summary>
    public DateTime UpdatedAt { get; set; } =
        DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last updater identifier.
    /// </summary>
    public string UpdatedBy { get; set; } =
        string.Empty;
}
