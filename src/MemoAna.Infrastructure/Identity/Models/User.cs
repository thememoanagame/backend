using MemoAna.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace MemoAna.Infrastructure.Identity.Models;

/// <summary>
/// Represents an MemoAna application user.
/// </summary>
public class User(
    string? userName = null)
    : IdentityUser<string>(
        userName ?? string.Empty),
      IEntityBase,
      ISoftDeletable
{
    /// <summary>
    /// Gets or sets the identity identifier.
    /// </summary>
    public override string Id { get; set; } =
        Guid.CreateVersion7().ToString();

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string? DisplayName { get; set; } =
        string.Empty;

    /// <summary>
    /// Gets or sets the first name.
    /// </summary>
    public string? FirstName { get; set; } =
        string.Empty;

    /// <summary>
    /// Gets or sets the surname.
    /// </summary>
    public string? SurName { get; set; } =
        string.Empty;

    /// <summary>
    /// Gets or sets whether the user is deleted.
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
