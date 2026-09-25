using MemoAna.Domain.Game;

namespace MemoAna.Application.Game.Dtos;

public sealed record GameThemeCardDto(string Name);

public sealed record GameThemeDto(
    string Id,
    string Name,
    string ThumbnailId,
    IReadOnlyList<GameThemeCardDto> Cards)
{
    public GameThemeDto(
        string id,
        string name,
        string thumbnailId,
        IReadOnlyList<string> cardNames)
        : this(
            id,
            name,
            thumbnailId,
            cardNames.Select(x => new GameThemeCardDto(x)).ToList())
    {
    }

    public static GameThemeDto FromTheme(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme, nameof(theme));

        return new(
            theme.Id,
            theme.Name,
            theme.ThumbnailId,
            theme.Cards.Select(x => new GameThemeCardDto(x.Name)).ToList());
    }
}

public record FileDto(Stream Content, string ContentType);
public record FilesDto(IReadOnlyList<FileDto> Files);