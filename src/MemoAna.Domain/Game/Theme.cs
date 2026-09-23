namespace MemoAna.Domain.Game;
/// <summary>Represents a game theme.</summary>
/// <param name="id"></param>
/// <param name="name"></param>
public class Theme(string id = "", string name = "") : EntityBase(id)
{
    /// <summary>Theme name.</summary>
    public string Name { get; set; } = name;
    /// <summary>Thumbnail image reference (00.webp) at LiteDB</summary>
    public string ThumbnailId { get; set; } = string.Empty;
    /// <summary>Theme description.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Cards base path.</summary>
    public string CardsBasePath { get => $"{ThemeBasePath}/cards"; }
    /// <summary>Theme base path.</summary>
    public string ThemeBasePath { get  => $"themes/{Id}"; }
    /// <summary>Thumbnail path.</summary>
    public string ThumbnailBasePath { get => $"{ThemeBasePath}/thumbnail/{ThumbnailId}"; }
    /// <summary>Image cards references from 01.webp to 15.webp) at LiteDB.</summary>
    public List<(string Id, string Name)> Cards { get; set; } = [];
    /// <summary>EF Core navigation.(One theme many rooms)</summary>
    public ICollection<Room> Rooms { get; set; } = [];

    public string GetCardsPath(int position)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position, nameof(position));
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(position, Cards.Count, nameof(position));
        return $"{CardsBasePath}/{Cards[position].Id}/{Cards[position].Name}";
    }
}