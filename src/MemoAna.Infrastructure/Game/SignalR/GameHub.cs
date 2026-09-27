using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using MemoAna.Application.Game.Abstractions;
using MemoAna.Application.Game.Dtos;
using MemoAna.Domain.Game;

namespace MemoAna.Infrastructure.Game.SignalR;

/// <summary>Client contract for authoritative game state notifications.</summary>
public interface IGameClient
{
    /// <summary>Notifies the client with the current board state.</summary>
    Task GameState(GameBoardDto state);

    /// <summary>Notifies the client that an authoritative game event occurred.</summary>
    Task GameUpdated(GameActionResultDto state);
}

/// <summary>SignalR hub for authoritative MemoAna gameplay.</summary>
[Authorize]
public sealed class GameHub(
    IGameService gameService,
    IGameModeServiceResolver modeServiceResolver,
    IGameRoomExecutionCoordinator executionCoordinator) : Hub<IGameClient>
{
    private const string RoomItem = "GameRoomId";
    private const string PlayerItem = "GamePlayerId";

    /// <summary>Connects the authenticated client to a game room.</summary>
    public async Task<GameBoardDto> JoinGame(
        string roomId,
        string playerId,
        CancellationToken cancellationToken = default)
    {
        if (Context.Items.ContainsKey(RoomItem))
            throw new InvalidOperationException("The connection is already associated with a game room.");

        var state = await gameService.ConnectGameAsync(roomId, playerId, cancellationToken);

        Context.Items[RoomItem] = roomId;
        Context.Items[PlayerItem] = playerId;

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(roomId), cancellationToken);
        await Clients.Group(GetGroupName(roomId)).GameState(state);

        return state;
    }

    /// <summary>Selects a card for the player associated with this connection.</summary>
    public async Task<GameActionResultDto> SelectCard(
        int position,
        CancellationToken cancellationToken = default)
    {
        var (roomId, playerId) = GetConnectionGame();
        return await executionCoordinator.ExecuteAsync(
            roomId,
            token => SelectCardCoreAsync(roomId, playerId, position, token),
            cancellationToken);
    }

    private async Task<GameActionResultDto> SelectCardCoreAsync(
        string roomId,
        string playerId,
        int position,
        CancellationToken cancellationToken)
    {
        var mode = await gameService.GetGameModeAsync(roomId, cancellationToken);
        var service = modeServiceResolver.Resolve(mode);

        var state = await service.SelectCardAsync(
            roomId,
            playerId,
            position,
            cancellationToken);

        await Clients.Group(GetGroupName(roomId)).GameUpdated(state);

        if (state.GameOver)
            return state;

        if (state.ResolveMismatchAfterDelay)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(800), cancellationToken);

            state = await service.ResolveMismatchAsync(roomId, cancellationToken);
            await Clients.Group(GetGroupName(roomId)).GameUpdated(state);

            if (state.GameOver)
                return state;
        }

        if (mode == GameMode.PlayerVsAi &&
            state.CurrentPlayerId != playerId)
        {
            var aiStates = await modeServiceResolver
                .Resolve(GameMode.PlayerVsAi)
                .RunAutomaticTurnAsync(roomId, cancellationToken);

            foreach (var aiState in aiStates)
            {
                if (aiState.Event is "card.flipped")
                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                else if (aiState.Event is "pair.mismatched")
                    await Task.Delay(TimeSpan.FromMilliseconds(800), cancellationToken);

                await Clients.Group(GetGroupName(roomId)).GameUpdated(aiState);
            }

            if (aiStates.Count > 0)
                state = aiStates[^1];
        }

        return state;
    }

    private (string RoomId, string PlayerId) GetConnectionGame()
    {
        if (Context.Items.TryGetValue(RoomItem, out var roomValue) &&
            Context.Items.TryGetValue(PlayerItem, out var playerValue) &&
            roomValue is string roomId &&
            playerValue is string playerId &&
            !string.IsNullOrWhiteSpace(roomId) &&
            !string.IsNullOrWhiteSpace(playerId))
        {
            return (roomId, playerId);
        }

        throw new HubException("The connection is not associated with a game room.");
    }

    private static string GetGroupName(string roomId) => $"game:{roomId}";
}
