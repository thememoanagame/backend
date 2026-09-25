using MemoAna.Application.Game.Dtos;

namespace MemoAna.Application.Game.Abstractions;

/// <summary>Publishes authoritative game events through the realtime transport.</summary>
public interface IGamePublisher
{
    Task PublishRoomStartedAsync(RoomSessionDto session, CancellationToken cancellationToken = default);
    Task PublishGameStateAsync(GameActionResultDto state, CancellationToken cancellationToken = default);
}
