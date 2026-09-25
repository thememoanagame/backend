using MemoAna.Application.Game.Dtos;
using MemoAna.Application.Game.Requests;

namespace MemoAna.Application.Game.Abstractions;

/// <summary>Provides authoritative multiplayer game operations.</summary>
public interface IGameService
{
    Task<RoomSessionDto> CreateRoomAsync(CreateRoomRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RoomSummaryDto>> ListRoomsAsync(CancellationToken cancellationToken = default);
    Task<RoomSessionDto> JoinRoomAsync(string roomId, JoinRoomRequest request, CancellationToken cancellationToken = default);
    Task<GameActionResultDto> SelectCardAsync(string roomId, string playerId, int position, CancellationToken cancellationToken = default);
    Task<GameActionResultDto> ResolveMismatchAsync(string roomId, CancellationToken cancellationToken = default);
}
