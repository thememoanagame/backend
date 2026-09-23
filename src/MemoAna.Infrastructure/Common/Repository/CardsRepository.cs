using System.Collections.Immutable;
using LiteDB;
using MemoAna.Application.Common.Abstractions;
using MemoAna.Domain.Exceptions;
using MemoAna.Infrastructure.Persistence.Contexts;
using Microsoft.Extensions.Logging;

namespace MemoAna.Infrastructure.Common.Repository;

public class CardsRepository(LiteDbContext liteDbContext, ILogger<CardsRepository> logger) : ICardsRepository
{
    private readonly ILiteStorage<string> storage = liteDbContext.Database.GetStorage<string>();
    private readonly ILogger<CardsRepository> logger = logger;

    public Task DeleteImageAsync(string imageId)
    {
        return Task.Run(() =>
        {
            try
            {
                storage.Delete(imageId);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error deleting image from repository: {Message}", e.Message);
                throw;
            }
        });
    }

    public async Task<Stream> GetThemeImageStreamAsync(string imageId)
    {
        // No LiteDB, a leitura de arquivos não é estritamente async na API atual (v5),
        // mas envolvemos em Task.Run para não bloquear a thread HTTP do ASP.NET.
        return await Task.Run(() =>
        {
            try
            {
                var fileInfo = storage.FindById(imageId) ??
                    throw new CardNotFoundException();
                return fileInfo.OpenRead();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error getitng image from repository: {Message}", e.Message);
                throw;
            }
        });
    }

    public async Task<IReadOnlyList<Stream>> GetThemeImageStreamsAsync(string themeId)
    {
        return await Task.Run<IReadOnlyList<LiteFileStream<string>>>(() =>
        {
            try
            {
                var files = storage.Find(x => x.Metadata.ContainsKey("ThemeId") && x.Metadata["ThemeId"] == themeId);
                if (!files.Any()) throw new CardNotFoundException();
                return [.. files.Select(f => f.OpenRead())];
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error getitng images from repository: {Message}", e.Message);
                throw;
            }
        }); 
    }

    public async Task UploadImageAsync(string imageId, string filename, Stream stream, Dictionary<string, object> metadata)
    {
        await Task.Run(() =>
        {
            try
            {
                // O id (themeId) será exatamente o LiteDbImageId que você mapeou no SQLite
                // ex: "animais/01.webp" ou um Guid gerado.
                LiteFileInfo<string> info = storage.Upload(imageId, filename, stream, [.. metadata.Select<
                    KeyValuePair<string, object>, 
                    KeyValuePair<string, BsonValue>>(
                        x => new(x.Key, new BsonValue(x.Value)))
                            .ToDictionary(x => x.Key, x => x.Value)]);
                if (info is null) throw new Exception("Failed to upload image to LiteDB.");
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error uploading image to repository: {Message}", e.Message);
                throw;
            }
        });
    }
}