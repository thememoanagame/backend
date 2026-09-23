using MemoAna.Application.Game.Dtos;
using MemoAna.Application.Game.Requests;
using MemoAna.Domain.Game;
using System.Linq.Expressions;

namespace MemoAna.Application.Game.Abstractions;

public interface IThemeService
{
    Task<GameThemeDto> AddThemeAsync(string name, Stream logoStream, string logoFilename, IEnumerable<(string Filename, Stream Stream)> cardStreams, CancellationToken cancellationToken = default);
    Task<GameThemeDto> UpdateThemeAsync(string id, string name, CancellationToken cancellationToken = default);
    Task<IEnumerable<GameThemeDto>> FindThemesAsync(Expression<Func<Theme, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> DeleteThemeAsync(string themeId, CancellationToken cancellationToken = default);
}