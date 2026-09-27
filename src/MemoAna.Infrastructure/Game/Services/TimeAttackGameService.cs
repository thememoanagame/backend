using MemoAna.Application.Game.Abstractions;
using MemoAna.Domain.Game;

namespace MemoAna.Infrastructure.Game.Services;

/// <summary>Authoritative service for TimeAttack games.</summary>
public sealed class TimeAttackGameService(IGameService gameService) : GameModeServiceBase(gameService)
{
    public override GameMode Mode => GameMode.PlayerVsTime;
}
