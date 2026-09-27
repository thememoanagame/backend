using MemoAna.Domain.Game;

namespace MemoAna.Application.Game.Abstractions;

/// <summary>Resolves the authoritative gameplay service for a game mode.</summary>
public interface IGameModeServiceResolver
{
    /// <summary>Gets the service responsible for the supplied mode.</summary>
    IGameModeService Resolve(GameMode mode);
}
