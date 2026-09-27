using MemoAna.Application.Game.Abstractions;
using MemoAna.Application.Game.Dtos;
using MemoAna.Domain.Game;

namespace MemoAna.Infrastructure.Game.Services;

internal abstract class GameModeServiceBase(IGameService gameService) : IGameModeService
{
    public abstract GameMode Mode { get; }

    public async Task<GameActionResultDto> SelectCardAsync(string roomId, string playerId, int position, CancellationToken cancellationToken = default)
    {
        await EnsureModeAsync(roomId, cancellationToken);
        return await gameService.SelectCardAsync(roomId, playerId, position, cancellationToken);
    }

    public async Task<GameActionResultDto> ResolveMismatchAsync(string roomId, CancellationToken cancellationToken = default)
    {
        await EnsureModeAsync(roomId, cancellationToken);
        return await gameService.ResolveMismatchAsync(roomId, cancellationToken);
    }

    public virtual Task<IReadOnlyList<GameActionResultDto>> RunAutomaticTurnAsync(string roomId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<GameActionResultDto>>([]);

    protected async Task EnsureModeAsync(string roomId, CancellationToken cancellationToken)
    {
        var mode = await gameService.GetGameModeAsync(roomId, cancellationToken);
        if (mode != Mode)
            throw new InvalidOperationException($"The room is configured for {mode}, not {Mode}.");
    }
}
