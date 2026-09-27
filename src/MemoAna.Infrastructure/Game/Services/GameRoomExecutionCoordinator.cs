using System.Collections.Concurrent;
using MemoAna.Application.Game.Abstractions;

namespace MemoAna.Infrastructure.Game.Services;

/// <inheritdoc />
public sealed class GameRoomExecutionCoordinator : IGameRoomExecutionCoordinator, IDisposable
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new(StringComparer.Ordinal);

    public async Task<T> ExecuteAsync<T>(string roomId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var gate = locks.GetOrAdd(roomId, static _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await operation(cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose()
    {
        foreach (var gate in locks.Values)
            gate.Dispose();
        locks.Clear();
    }
}
