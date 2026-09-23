using MemoAna.Application.Common.Abstractions;
using MemoAna.Application.Game.Abstractions;
using MemoAna.Application.Game.Dtos;
using MemoAna.Domain.Game;
using MemoAna.Infrastructure.Game.Exceptions;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace MemoAna.Infrastructure.Game.Services;

public class ThemeService(IRepository<Theme> repository, ICardsRepository imageRepository, ILogger<ThemeService> logger) : IThemeService
{
    public async Task<GameThemeDto> AddThemeAsync(
        string name,
        Stream logoStream,
        string logoFilename,
        IEnumerable<(string Filename, Stream Stream)> cardStreams,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Theme theme = new()
            {
                Cards = [],
                Name = name,
                Id = Guid.CreateVersion7().ToString(),
                ThumbnailId = Guid.CreateVersion7().ToString()
            };

            // Common LiteDB metadata
            var metadata = new Dictionary<string, object>
            {
                { "ThemeId", theme.Id },
                { "ThemeName", theme.Name },
                { "Type", "Logo" }
            };

            // Thumbnail upload
            await imageRepository.UploadImageAsync($"{theme.ThumbnailBasePath}{logoFilename}", logoFilename, logoStream, metadata);

            // Cards upload
            theme.Cards = [];
            metadata["Type"] = "Card";

            foreach (var (filename, stream) in cardStreams)
            {
                var cardImageId = Guid.CreateVersion7().ToString();
                await imageRepository.UploadImageAsync($"{theme.CardsBasePath}/{cardImageId}/{filename}", filename, stream, metadata);
                theme.Cards.Add((cardImageId, filename));
            }

            // 4. Salva no SQLite
            await repository.AddAsync(theme, cancellationToken);

            return GameThemeDto.FromTheme(theme);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error adding theme '{Name}': {Message}", name, e.Message);
            throw;
        }
    }

    public async Task<GameThemeDto> UpdateThemeAsync(string id, string name, CancellationToken cancellationToken = default)
    {
        // Obs: Se a edição de tema permitir alterar as imagens, você deve orquestrar
        // a exclusão dos IDs antigos no LiteDB e o upload das novas streams aqui.
        // Para atualizar apenas os metadados (ex: Nome do Tema):
        try
        {
            logger.LogDebug("Updating theme '{Id}' with name '{Name}'", id, name);
            var theme = await repository.GetByIdAsync(id, cancellationToken);
            if (theme == null)
            {
                logger.LogWarning("Theme '{Id}' not found for update.", id);
                throw new KeyNotFoundException($"Theme with ID '{id}' not found.");
            }
            theme.Name = name;
           var result = await repository.UpdateAsync(theme, cancellationToken);

            return result ? GameThemeDto.FromTheme(theme) : throw new GameException("Failed to update theme.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error updating theme '{ThemeId}': {Message}", id, e.Message);
            throw;
        }
    }

    public async Task<IEnumerable<GameThemeDto>> FindThemesAsync(
        Expression<Func<Theme, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return (await repository.ListAsync(predicate, false, cancellationToken)).Select(GameThemeDto.FromTheme);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error finding themes: {Message}", e.Message);
            throw;
        }
    }

    public async Task<bool> DeleteThemeAsync(string themeId, CancellationToken cancellationToken = default)
    {
        try
        {
            var theme = await repository.GetByIdAsync(themeId, cancellationToken);
            if (theme == null) return false;

            if (!string.IsNullOrEmpty(theme.ThumbnailId) && !string.IsNullOrWhiteSpace(theme.ThumbnailId))
            {
                await imageRepository.DeleteImageAsync(theme.ThumbnailBasePath);
            }
            else
            {
                logger.LogWarning("Theme '{ThemeId}' has no thumbnail ID to delete.", themeId);
            }

            for (int i = 0; i < theme.Cards.Count; i++)
            {
                (string Id, string Name) cardId = theme.Cards[i];
                await imageRepository.DeleteImageAsync(theme.GetCardsPath(i));
            }

            return await repository.RemoveAsync(themeId, cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error deleting theme '{ThemeId}': {Message}", themeId, e.Message);
            throw;
        }
    }
}