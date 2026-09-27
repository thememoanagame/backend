using MemoAna.Application.Game.Dtos;
using MemoAna.Domain.Game;

namespace MemoAna.Application.Game.Abstractions;

/// <summary>Provides authoritative gameplay operations for a specific game mode.</summary>
public interface IGameModeService
{
    /// <summary>Gets the game mode handled by this service.</summary>
    GameMode Mode { get; }

    /// <summary>Selects a card for the current player.</summary>
    Task<GameActionResultDto> SelectCardAsync(string roomId, string playerId, int position, CancellationToken cancellationToken = default);

    /// <summary>Resolves the currently visible mismatched pair.</summary>
    Task<GameActionResultDto> ResolveMismatchAsync(string roomId, CancellationToken cancellationToken = default);

    /// <summary>Runs server-controlled automatic turns when the mode supports them.</summary>
    Task<IReadOnlyList<GameActionResultDto>> RunAutomaticTurnAsync(string roomId, CancellationToken cancellationToken = default);
}
