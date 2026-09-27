using MemoAna.Application.Game.Abstractions;
using MemoAna.Application.Game.Dtos;
using MemoAna.Domain.Game;

namespace MemoAna.Infrastructure.Game.Services;

/// <summary>Authoritative service for player-versus-AI games.</summary>
public sealed class AiGameService(IGameService gameService) : GameModeServiceBase(gameService)
{
    public override GameMode Mode => GameMode.PlayerVsAi;

    public override async Task<IReadOnlyList<GameActionResultDto>> RunAutomaticTurnAsync(string roomId, CancellationToken cancellationToken = default)
    {
        await EnsureModeAsync(roomId, cancellationToken);
        return await gameService.RunAutomaticTurnAsync(roomId, cancellationToken);
    }
}
