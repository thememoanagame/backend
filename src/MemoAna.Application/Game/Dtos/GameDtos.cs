using MemoAna.Domain.Game;
using System.Linq.Expressions;

namespace MemoAna.Application.Game.Dtos;

public sealed record GameThemeDto(
    string Id,
    string Name,
    string ThumbnailId,
    IReadOnlyList<(string Filename, string Name)> Cards)
{
    public static GameThemeDto FromTheme(Theme theme)
    {
        ArgumentNullException.ThrowIfNull(theme, nameof(theme));
        return new (
            Id: theme.Id,
            Name: theme.Name,
            ThumbnailId: theme.ThumbnailId,
            Cards: theme.Cards
        );
    }
}
 
 /// <summary>Dto for files from LiteDB.</summary>
 /// <param name="Content">Stream of file content</param>
 /// <param name="ContentType">ContentType of file</param>
public record FileDto(Stream Content, string ContentType);
public record FilesDto(IReadOnlyList<FileDto> Files);