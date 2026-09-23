using LiteDB;
using MemoAna.Infrastructure.Persistence.Options;
using Microsoft.Extensions.Options;

namespace MemoAna.Infrastructure.Persistence.Contexts;

public sealed class LiteDbContext(LiteDatabase database) : IDisposable
{
    private readonly LiteDatabase _database = database ?? throw new ArgumentNullException(nameof(database));

    /// <summary> Base instance.</summary>
    public ILiteDatabase Database => _database;

    /// <summary>FileStorage for files.</summary>
    public ILiteStorage<string> FileStorage => _database.FileStorage;

    /// <summary>
    /// Initialize the database.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task InitializeAsync()
    {
        await Task.Run(() =>
        {
            // Ensure that the database file is created and accessible
            var collections = _database.GetCollectionNames();
            foreach (var collectionName in collections)
            {
                var collection = _database.GetCollection(collectionName);
                collection.EnsureIndex("_id");
            }
        });
    }

    /// <summary>
    /// Generic method to get collections of documents (JSON).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="collectionName"></param>
    /// <returns></returns>
    public ILiteCollection<T> GetCollection<T>()
    {
        return _database.GetCollection<T>(typeof(T).Name);
    }

    /// <summary>
    /// Free up resources.
    /// </summary>
    public void Dispose()
    {
        // Ensures that the phisical file be released when the application is closed
        _database?.Dispose();
        GC.SuppressFinalize(this);
    }
}