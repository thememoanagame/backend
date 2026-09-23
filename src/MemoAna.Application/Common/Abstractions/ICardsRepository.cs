namespace MemoAna.Application.Common.Abstractions;

public interface ICardsRepository
{
    Task<Stream> GetThemeImageStreamAsync(string imageId);
    Task<IReadOnlyList<Stream>> GetThemeImageStreamsAsync(string themeId);
    Task DeleteImageAsync(string path);
    Task UploadImageAsync(string path, string filename, Stream imageStream, Dictionary<string, object> metadata);
}