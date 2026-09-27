namespace MemoAna.Application.Game.Abstractions;

/// <summary>Serializes authoritative operations per game room.</summary>
public interface IGameRoomExecutionCoordinator
{
    /// <summary>Executes an operation while holding the room-specific lock.</summary>
    Task<T> ExecuteAsync<T>(string roomId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
