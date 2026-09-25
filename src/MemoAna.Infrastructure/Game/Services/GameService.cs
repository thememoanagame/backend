using System.Security.Cryptography;
using System.Text;
using MemoAna.Application.Common.Abstractions;
using MemoAna.Application.Game.Abstractions;
using MemoAna.Application.Game.Dtos;
using MemoAna.Application.Game.Requests;
using MemoAna.Domain.Game;
using MemoAna.Infrastructure.Game.Options;

namespace MemoAna.Infrastructure.Game.Services;

/// <summary>Implements authoritative room and board operations.</summary>
public sealed class GameService(
    IRepository<Room> roomRepository,
    IRepository<Player> playerRepository,
    IRepository<Card> cardRepository,
    IRepository<Theme> themeRepository,
    IUnitOfWork unitOfWork,
    MqttOptions mqttOptions) : IGameService
{
    private const int CardsPerTheme = 15;

    public async Task<RoomSessionDto> CreateRoomAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var theme = await themeRepository.GetByIdAsync(request.ThemeId, cancellationToken)
            ?? throw new KeyNotFoundException("Game theme was not found.");

        if (theme.Cards.Count < CardsPerTheme)
            throw new InvalidOperationException("The selected theme must contain at least 15 cards.");

        var roomId = Guid.CreateVersion7().ToString();
        var playerId = Guid.CreateVersion7().ToString();
        var mqttPassword = GenerateSecret();

        var player = new Player(playerId, roomId)
        {
            PeerIdentifier = Guid.CreateVersion7().ToString("N"),
            Name = request.PlayerName,
            MqttUsername = $"room-{roomId}-player-{playerId}",
            MqttPasswordHash = HashSecret(mqttPassword)
        };

        var room = new Room
        {
            Id = roomId,
            Name = request.Name,
            ThemeId = theme.Id,
            Difficulty = request.Difficulty,
            RequirePassword = request.RequirePassword,
            JoinPasswordHash = request.RequirePassword ? HashSecret(request.Password!) : null,
            Status = GameStatus.WaitingForPlayers
        };

        await roomRepository.AddAsync(room, cancellationToken);
        await playerRepository.AddAsync(player, cancellationToken);

        return CreateSession(
            room,
            player,
            mqttPassword,
            board: null,
            playerCount: 1);
    }

    public async Task<IReadOnlyList<RoomSummaryDto>> ListRoomsAsync(
        CancellationToken cancellationToken = default)
    {
        var rooms = await roomRepository.ListAsync(
            x => x.Status == GameStatus.WaitingForPlayers,
            cancellationToken: cancellationToken);

        if (rooms.Count == 0)
            return [];

        var roomIds = rooms.Select(x => x.Id).ToHashSet();
        var players = await playerRepository.ListAsync(
            x => roomIds.Contains(x.RoomId),
            cancellationToken: cancellationToken);

        var playerCounts = players
            .GroupBy(x => x.RoomId)
            .ToDictionary(x => x.Key, x => x.Count());

        return rooms
            .OrderBy(x => x.CreatedAt)
            .Select(x => new RoomSummaryDto(
                x.Id,
                x.Name,
                x.ThemeId,
                x.Difficulty,
                x.Status.ToString(),
                x.RequirePassword,
                playerCounts.GetValueOrDefault(x.Id)))
            .ToList();
    }

    public async Task<RoomSessionDto> JoinRoomAsync(
        string roomId,
        JoinRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.FirstOrDefaultAsync(
            x => x.Id == roomId,
            tracking: true,
            cancellationToken: cancellationToken);

        if (room is null)
            throw new KeyNotFoundException("Game room was not found.");

        var existingPlayers = await playerRepository.ListAsync(
            x => x.RoomId == room.Id,
            tracking: true,
            cancellationToken: cancellationToken);

        if (room.Status != GameStatus.WaitingForPlayers || existingPlayers.Count >= 2)
            throw new InvalidOperationException("The game room is no longer available.");

        if (room.RequirePassword && !VerifySecret(request.Password, room.JoinPasswordHash))
            throw new UnauthorizedAccessException("The room password is invalid.");

        var theme = await themeRepository.GetByIdAsync(room.ThemeId, cancellationToken)
            ?? throw new KeyNotFoundException("Game theme was not found.");

        if (theme.Cards.Count < CardsPerTheme)
            throw new InvalidOperationException("The selected theme does not contain enough cards.");

        var playerId = Guid.CreateVersion7().ToString();
        var mqttPassword = GenerateSecret();
        var player = new Player(playerId, room.Id)
        {
            PeerIdentifier = Guid.CreateVersion7().ToString("N"),
            Name = request.PlayerName,
            MqttUsername = $"room-{room.Id}-player-{playerId}",
            MqttPasswordHash = HashSecret(mqttPassword)
        };

        room.Players = existingPlayers.ToList();
        room.AddPlayer(player);

        var imageIds = theme.Cards
            .Take(CardsPerTheme)
            .SelectMany(x => new[] { x.Id, x.Id })
            .ToArray();

        RandomNumberGenerator.Shuffle(imageIds);

        for (var position = 0; position < imageIds.Length; position++)
        {
            await cardRepository.AddAsync(
                new Card(Guid.CreateVersion7().ToString(), room.Id)
                {
                    Position = position,
                    LiteDbImageId = imageIds[position]
                },
                cancellationToken);
        }

        await playerRepository.AddAsync(player, cancellationToken);

        var allPlayers = existingPlayers.Append(player).ToList();
        var startingPlayer = allPlayers[RandomNumberGenerator.GetInt32(allPlayers.Count)];
        room.StartGame(startingPlayer.Id);

        var cards = await cardRepository.ListAsync(
            x => x.RoomId == room.Id,
            cancellationToken: cancellationToken);

        return CreateSession(
            room,
            player,
            mqttPassword,
            ToBoard(room, cards),
            allPlayers.Count);
    }

    public async Task<GameActionResultDto> SelectCardAsync(
        string roomId,
        string playerId,
        int position,
        CancellationToken cancellationToken = default)
    {
        var room = await LoadRoomAsync(roomId, cancellationToken);
        var cards = await cardRepository.ListAsync(
            x => x.RoomId == roomId,
            tracking: true,
            cancellationToken: cancellationToken);
        var players = await playerRepository.ListAsync(
            x => x.RoomId == roomId,
            tracking: true,
            cancellationToken: cancellationToken);

        room.Players = players;
        room.Cards = cards;

        if (cards.Count(x => x.IsFlipped && !x.IsMatched) >= 2)
            throw new InvalidOperationException("The current pair is still being resolved.");

        if (!room.CanFlipCard(playerId, position))
            throw new InvalidOperationException("The card cannot be selected by the current player.");

        var card = cards.First(x => x.Position == position);
        card.IsFlipped = true;

        var openCards = cards
            .Where(x => x.IsFlipped && !x.IsMatched)
            .OrderBy(x => x.Position)
            .ToList();

        if (openCards.Count < 2)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ToActionResult(room, cards, players, "card.flipped", false, null);
        }

        var first = openCards[0];
        var second = openCards[1];

        if (string.Equals(first.LiteDbImageId, second.LiteDbImageId, StringComparison.Ordinal))
        {
            first.IsMatched = true;
            second.IsMatched = true;

            var player = players.First(x => x.Id == playerId);
            player.Score++;

            if (cards.All(x => x.IsMatched))
            {
                room.CompleteGame();
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return ToActionResult(room, cards, players, "game.completed", false, null, true);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ToActionResult(room, cards, players, "pair.matched", false, null);
        }

        room.CurrentTurnPlayerId = players.First(x => x.Id != playerId).Id;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToActionResult(room, cards, players, "pair.mismatched", true, null);
    }

    public async Task<GameActionResultDto> ResolveMismatchAsync(
        string roomId,
        CancellationToken cancellationToken = default)
    {
        var room = await LoadRoomAsync(roomId, cancellationToken);
        var cards = await cardRepository.ListAsync(
            x => x.RoomId == roomId,
            tracking: true,
            cancellationToken: cancellationToken);
        var players = await playerRepository.ListAsync(
            x => x.RoomId == roomId,
            cancellationToken: cancellationToken);

        room.Players = players;
        room.Cards = cards;

        foreach (var card in cards.Where(x => x.IsFlipped && !x.IsMatched))
            card.IsFlipped = false;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToActionResult(room, cards, players, "pair.hidden", false, null);
    }

    private async Task<Room> LoadRoomAsync(
        string roomId,
        CancellationToken cancellationToken)
        => await roomRepository.FirstOrDefaultAsync(
            x => x.Id == roomId,
            tracking: true,
            cancellationToken)
            ?? throw new KeyNotFoundException("Game room was not found.");

    private RoomSessionDto CreateSession(
        Room room,
        Player player,
        string mqttPassword,
        GameBoardDto? board,
        int playerCount)
    {
        var summary = new RoomSummaryDto(
            room.Id,
            room.Name,
            room.ThemeId,
            room.Difficulty,
            room.Status.ToString(),
            room.RequirePassword,
            playerCount);

        var credentials = new MqttCredentialsDto(
            mqttOptions.Endpoint,
            mqttOptions.Port,
            player.PeerIdentifier,
            player.MqttUsername,
            mqttPassword,
            GetBoardTopic(room.Id),
            GetPlayerTopic(room.Id));

        return new RoomSessionDto(summary, player.Id, credentials, board);
    }

    private static GameBoardDto ToBoard(Room room, IReadOnlyList<Card> cards)
        => new(
            room.Id,
            room.ThemeId,
            cards
                .OrderBy(x => x.Position)
                .Select(x => new BoardCardDto(x.Position, x.LiteDbImageId, false, false))
                .ToList(),
            room.CurrentTurnPlayerId!);

    private static GameActionResultDto ToActionResult(
        Room room,
        IReadOnlyList<Card> cards,
        IReadOnlyList<Player> players,
        string eventName,
        bool resolveMismatchAfterDelay,
        string? message,
        bool gameOver = false)
        => new(
            room.Id,
            eventName,
            room.CurrentTurnPlayerId ?? string.Empty,
            players.Select(x => new PlayerScoreDto(x.Id, x.Name, x.Score)).ToList(),
            cards
                .OrderBy(x => x.Position)
                .Select(x => new BoardCardDto(x.Position, x.LiteDbImageId, x.IsFlipped, x.IsMatched))
                .ToList(),
            gameOver || room.Status == GameStatus.Finished,
            resolveMismatchAfterDelay,
            message);

    internal static string GetBoardTopic(string roomId) => $"/rooms/{roomId}/board";
    internal static string GetPlayerTopic(string roomId) => $"/rooms/{roomId}/player";

    private static string GenerateSecret()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-", StringComparison.Ordinal)
            .Replace("/", "_", StringComparison.Ordinal)
            .TrimEnd('=');

    private static string HashSecret(string value)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(value),
            salt,
            100_000,
            HashAlgorithmName.SHA256,
            32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    internal static bool VerifySecret(string? value, string? stored)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(stored))
            return false;

        var parts = stored.Split('.', 2);
        if (parts.Length != 2)
            return false;

        try
        {
            var salt = Convert.FromBase64String(parts[0]);
            var expected = Convert.FromBase64String(parts[1]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(value),
                salt,
                100_000,
                HashAlgorithmName.SHA256,
                expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
