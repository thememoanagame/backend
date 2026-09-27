using MemoAna.Application.Game.Abstractions;
using MemoAna.Domain.Game;

namespace MemoAna.Infrastructure.Game.Services;

/// <inheritdoc />
public sealed class GameModeServiceResolver(IEnumerable<IGameModeService> services) : IGameModeServiceResolver
{
    public IGameModeService Resolve(GameMode mode)
        => services.FirstOrDefault(x => x.Mode == mode)
            ?? throw new InvalidOperationException($"No game service is registered for mode {mode}.");
}
