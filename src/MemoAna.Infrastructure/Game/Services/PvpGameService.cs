using MemoAna.Application.Game.Abstractions;
using MemoAna.Domain.Game;

namespace MemoAna.Infrastructure.Game.Services;

/// <summary>Authoritative service for player-versus-player games.</summary>
public sealed class PvpGameService(IGameService gameService) : GameModeServiceBase(gameService)
{
    public override GameMode Mode => GameMode.PlayerVsPlayer;
}
