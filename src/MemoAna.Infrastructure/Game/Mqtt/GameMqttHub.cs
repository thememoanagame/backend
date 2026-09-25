using System.Text;
using System.Collections.Concurrent;
using System.Text.Json;
using MemoAna.Application.Common.Abstractions;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MemoAna.Application.Game.Abstractions;
using MemoAna.Domain.Game;
using MemoAna.Application.Game.Dtos;
using MemoAna.Infrastructure.Game.Services;

namespace MemoAna.Infrastructure.Game.Mqtt;

/// <summary>Hosts the MQTT game hub and enforces room-scoped transport authorization.</summary>
public sealed class GameMqttHub(
    IServiceScopeFactory scopeFactory,
    ILogger<GameMqttHub> logger) : IGamePublisher
{
    private MqttServer? server;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string ServiceClientId = "memoana-game-service";
    private readonly ConcurrentDictionary<string, SemaphoreSlim> roomLocks = new(StringComparer.Ordinal);

    public void Configure(MqttServer mqttServer)
    {
        ArgumentNullException.ThrowIfNull(mqttServer);

        if (server is not null)
            throw new InvalidOperationException("The MQTT game hub has already been configured.");

        server = mqttServer;
        server.ValidatingConnectionAsync += ValidateConnectionAsync;
        server.InterceptingSubscriptionAsync += InterceptSubscriptionAsync;
        server.InterceptingPublishAsync += InterceptPublishAsync;
    }

    public async Task PublishRoomStartedAsync(
        RoomSessionDto session,
        CancellationToken cancellationToken = default)
    {
        if (session.Board is null)
            return;

        await PublishAsync(
            GameService.GetBoardTopic(session.Room.Id),
            session.Board,
            retain: true,
            cancellationToken);

        await PublishAsync(
            GameService.GetPlayerTopic(session.Room.Id),
            new
            {
                @event = "game.started",
                roomId = session.Room.Id,
                currentPlayerId = session.Board.CurrentPlayerId
            },
            retain: true,
            cancellationToken);
    }

    public Task PublishGameStateAsync(
        GameActionResultDto state,
        CancellationToken cancellationToken = default)
        => PublishAsync(
            GameService.GetPlayerTopic(state.RoomId),
            state,
            retain: true,
            cancellationToken);

    private async Task ValidateConnectionAsync(ValidatingConnectionEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(args.UserName) ||
            string.IsNullOrWhiteSpace(args.Password))
        {
            args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var playerRepository = scope.ServiceProvider.GetRequiredService<IRepository<Player>>();

        var player = await playerRepository.FirstOrDefaultAsync(
            x => x.MqttUsername == args.UserName,
            cancellationToken: args.CancellationToken);

        if (player is null ||
            !GameService.VerifySecret(args.Password, player.MqttPasswordHash))
        {
            args.ReasonCode = MqttConnectReasonCode.BadUserNameOrPassword;
            return;
        }

        args.SessionItems["PlayerId"] = player.Id;
        args.SessionItems["RoomId"] = player.RoomId;
    }

    private Task InterceptSubscriptionAsync(InterceptingSubscriptionEventArgs args)
    {
        if (!TryGetSessionRoom(args.SessionItems, out var roomId) ||
            !IsRoomTopic(args.TopicFilter.Topic, roomId))
        {
            args.Response.ReasonCode = MqttSubscribeReasonCode.NotAuthorized;
        }

        return Task.CompletedTask;
    }

    private async Task InterceptPublishAsync(InterceptingPublishEventArgs args)
    {
        if (!TryGetSessionRoom(args.SessionItems, out var roomId) ||
            !string.Equals(
                args.ApplicationMessage.Topic,
                GameService.GetPlayerTopic(roomId),
                StringComparison.Ordinal))
        {
            args.ProcessPublish = false;
            args.Response.ReasonCode = MqttPubAckReasonCode.NotAuthorized;
            return;
        }

        PlayerAction? action;
        try
        {
            action = JsonSerializer.Deserialize<PlayerAction>(
                args.ApplicationMessage.Payload.ToArray(),
                JsonOptions);
        }
        catch (JsonException)
        {
            action = null;
        }

        if (action is null ||
            !string.Equals(action.Action, "card.select", StringComparison.OrdinalIgnoreCase) ||
            action.Position is < 0 or > 29)
        {
            args.ProcessPublish = false;
            args.Response.ReasonCode = MqttPubAckReasonCode.PayloadFormatInvalid;
            return;
        }

        args.ProcessPublish = false;

        var roomLock = roomLocks.GetOrAdd(roomId, static _ => new SemaphoreSlim(1, 1));
        await roomLock.WaitAsync(args.CancellationToken);

        try
        {
            using var scope = scopeFactory.CreateScope();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

            var state = await gameService.SelectCardAsync(
                roomId,
                args.SessionItems["PlayerId"]?.ToString() ?? string.Empty,
                action.Position.Value,
                args.CancellationToken);

            await PublishGameStateAsync(state, args.CancellationToken);

            if (state.ResolveMismatchAfterDelay)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(800), args.CancellationToken);

                var resolved = await gameService.ResolveMismatchAsync(
                    roomId,
                    args.CancellationToken);

                await PublishGameStateAsync(resolved, args.CancellationToken);
            }
        }
        catch (OperationCanceledException) when (args.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Rejected MQTT game action from client {ClientId}.",
                args.ClientId);

            await PublishAsync(
                GameService.GetPlayerTopic(roomId),
                new
                {
                    @event = "error",
                    message = exception.Message
                },
                retain: false,
                args.CancellationToken);
        }
        finally
        {
            roomLock.Release();
        }
    }

    private static bool TryGetSessionRoom(
        System.Collections.IDictionary sessionItems,
        out string roomId)
    {
        roomId = sessionItems["RoomId"]?.ToString() ?? string.Empty;
        return Guid.TryParse(roomId, out _);
    }

    private static bool IsRoomTopic(string topicFilter, string roomId)
    {
        var prefix = $"/rooms/{roomId}/";
        return topicFilter.StartsWith(prefix, StringComparison.Ordinal) &&
               topicFilter[prefix.Length..] is "board" or "player" or "#";
    }

    private async Task PublishAsync<T>(
        string topic,
        T payload,
        bool retain,
        CancellationToken cancellationToken)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(JsonSerializer.Serialize(payload, JsonOptions))
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
            .WithRetainFlag(retain)
            .Build();

        var mqttServer = server
            ?? throw new InvalidOperationException("The MQTT game hub has not been configured.");

        await mqttServer.InjectApplicationMessage(
            new InjectedMqttApplicationMessage(message)
            {
                SenderClientId = ServiceClientId
            });
    }

    private sealed record PlayerAction(string Action, int? Position);
}
