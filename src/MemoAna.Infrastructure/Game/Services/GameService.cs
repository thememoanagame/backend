using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using MemoAna.Application.Common.Abstractions;
using MemoAna.Application.Game.Abstractions;
using MemoAna.Application.Game.Dtos;
using MemoAna.Application.Game.Requests;
using MemoAna.Domain.Game;
using MemoAna.Infrastructure.Game.Options;

namespace MemoAna.Infrastructure.Game.Services;

public sealed class GameService(
    IRepository<Room> roomRepository,
    IRepository<Player> playerRepository,
    IRepository<Card> cardRepository,
    IRepository<Theme> themeRepository,
    ICardsRepository imageRepository,
    IUnitOfWork unitOfWork,
    MqttOptions mqttOptions,
    IDataProtectionProvider dataProtectionProvider) : IGameService
{
    private const int CardsPerTheme = 15;
    private const int BoardCardCount = CardsPerTheme * 2;
    private const int DefaultTimeLimitSeconds = 120;
    private static readonly TimeSpan ImageTokenLifetime = TimeSpan.FromMinutes(5);
    private readonly ITimeLimitedDataProtector imageTokenProtector =
        dataProtectionProvider
            .CreateProtector("MemoAna", "Game", "CardImage")
            .ToTimeLimitedDataProtector();

    public async Task<RoomSessionDto> CreateRoomAsync(
        CreateRoomRequest request,
        CancellationToken cancellationToken = default)
    {
        var theme = await GetValidThemeAsync(request.ThemeId, cancellationToken);

        if (request.Mode == GameMode.PlayerVsTime &&
            request.TimeLimitSeconds is <= 0)
        {
            request = request with { TimeLimitSeconds = DefaultTimeLimitSeconds };
        }

        var roomId = Guid.CreateVersion7().ToString();
        var playerId = Guid.CreateVersion7().ToString();
        var mqttPassword = GenerateSecret();

        var player = CreatePlayer(roomId, playerId, request.PlayerName, mqttPassword);
        var room = new Room
        {
            Id = roomId,
            Name = request.Name,
            ThemeId = theme.Id,
            Difficulty = request.Difficulty,
            Mode = request.Mode,
            TimeLimitSeconds = request.Mode == GameMode.PlayerVsTime
                ? request.TimeLimitSeconds!.Value
                : 0,
            RequirePassword = request.Mode == GameMode.PlayerVsPlayer && request.RequirePassword,
            JoinPasswordHash = request.Mode == GameMode.PlayerVsPlayer && request.RequirePassword
                ? HashSecret(request.Password!)
                : null,
            Status = GameStatus.WaitingForPlayers
        };

        await roomRepository.AddAsync(room, cancellationToken);
        await playerRepository.AddAsync(player, cancellationToken);

        if (request.Mode != GameMode.PlayerVsPlayer)
        {
            if (request.Mode == GameMode.PlayerVsAi)
            {
                var ai = new Player(Guid.CreateVersion7().ToString(), roomId)
                {
                    PeerIdentifier = Guid.CreateVersion7().ToString("N"),
                    Name = "IA",
                    IsAi = true,
                    MqttUsername = $"ai-{roomId}-{Guid.CreateVersion7():N}",
                    MqttPasswordHash = HashSecret(GenerateSecret())
                };

                await playerRepository.AddAsync(ai, cancellationToken);
            }

            await CreateBoardAsync(room, theme, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            room.Players = await playerRepository.ListAsync(
                x => x.RoomId == room.Id,
                tracking: true,
                cancellationToken: cancellationToken);

            room.StartGame(player.Id);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        var board = request.Mode == GameMode.PlayerVsPlayer
            ? null
            : await BuildBoardAsync(room, cancellationToken);

        return CreateSession(room, player, mqttPassword, board, request.Mode == GameMode.PlayerVsAi ? 2 : 1);
    }

    public async Task<IReadOnlyList<RoomSummaryDto>> ListRoomsAsync(
        CancellationToken cancellationToken = default)
    {
        var rooms = await roomRepository.ListAsync(
            x => x.Status == GameStatus.WaitingForPlayers &&
                 x.Mode == GameMode.PlayerVsPlayer,
            cancellationToken: cancellationToken);

        if (rooms.Count == 0)
            return [];

        var roomIds = rooms.Select(x => x.Id).ToHashSet();
        var players = await playerRepository.ListAsync(
            x => roomIds.Contains(x.RoomId),
            cancellationToken: cancellationToken);

        var counts = players
            .GroupBy(x => x.RoomId)
            .ToDictionary(x => x.Key, x => x.Count());

        return rooms
            .OrderBy(x => x.CreatedAt)
            .Select(x => new RoomSummaryDto(
                x.Id,
                x.Name,
                x.ThemeId,
                x.Difficulty,
                x.Mode.ToString(),
                x.Status.ToString(),
                x.RequirePassword,
                counts.GetValueOrDefault(x.Id),
                x.TimeLimitSeconds))
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
            cancellationToken: cancellationToken)
            ?? throw new KeyNotFoundException("Game room was not found.");

        if (room.Mode != GameMode.PlayerVsPlayer ||
            room.Status != GameStatus.WaitingForPlayers)
        {
            throw new InvalidOperationException("Only waiting player-versus-player rooms can be joined.");
        }

        var existingPlayers = await playerRepository.ListAsync(
            x => x.RoomId == room.Id,
            tracking: true,
            cancellationToken: cancellationToken);

        if (existingPlayers.Count >= 2)
            throw new InvalidOperationException("The game room is full.");

        if (room.RequirePassword && !VerifySecret(request.Password, room.JoinPasswordHash))
            throw new UnauthorizedAccessException("The room password is invalid.");

        var theme = await GetValidThemeAsync(room.ThemeId, cancellationToken);
        var mqttPassword = GenerateSecret();
        var player = CreatePlayer(room.Id, Guid.CreateVersion7().ToString(), request.PlayerName, mqttPassword);

        room.Players = existingPlayers.ToList();
        room.AddPlayer(player);

        await playerRepository.AddAsync(player, cancellationToken);
        await CreateBoardAsync(room, theme, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var allPlayers = existingPlayers.Append(player).ToList();
        room.Players = allPlayers;

        var startingPlayer = allPlayers[RandomNumberGenerator.GetInt32(allPlayers.Count)];
        room.StartGame(startingPlayer.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var board = await BuildBoardAsync(room, cancellationToken);
        return CreateSession(room, player, mqttPassword, board, allPlayers.Count);
    }

    public async Task<GameActionResultDto> SelectCardAsync(
        string roomId,
        string playerId,
        int position,
        CancellationToken cancellationToken = default)
    {
        if (position is < 0 or >= BoardCardCount)
            throw new ArgumentOutOfRangeException(nameof(position));

        var room = await LoadRoomAsync(roomId, cancellationToken);
        var cards = await cardRepository.ListAsync(
            x => x.RoomId == roomId,
            tracking: true,
            cancellationToken: cancellationToken);
        var players = await playerRepository.ListAsync(
            x => x.RoomId == roomId,
            tracking: true,
            cancellationToken: cancellationToken);

        room.Players = [.. players];
        room.Cards = [.. cards];

        if (room.Status != GameStatus.InProgress)
            throw new InvalidOperationException("The game is not in progress.");

        if (room.Mode == GameMode.PlayerVsTime && IsTimeExpired(room))
        {
            room.CompleteGame();
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ToActionResult(room, cards, players, "time.expired", false, "Time limit reached.", true);
        }

        if (cards.Count(x => x.IsFlipped && !x.IsMatched) >= 2)
            throw new InvalidOperationException("The current pair is still being resolved.");

        if (!room.CanFlipCard(playerId, position))
            throw new InvalidOperationException("The card cannot be selected by the current player.");

        var player = players.FirstOrDefault(x => x.Id == playerId)
            ?? throw new UnauthorizedAccessException("The player does not belong to the room.");

        var card = cards.First(x => x.Position == position);
        card.IsFlipped = true;
        player.Moves++;

        UpdateAiMemory(players, cards);

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
            player.CorrectPairs++;
            player.CurrentStreak++;
            player.Score = CalculateScore(room.Mode, player.Score, player.CurrentStreak);

            if (cards.All(x => x.IsMatched))
            {
                room.CompleteGame();
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return ToActionResult(room, cards, players, "game.completed", false, null, true);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ToActionResult(room, cards, players, "pair.matched", false, null);
        }

        player.Errors++;
        player.CurrentStreak = 0;

        if (room.Mode == GameMode.PlayerVsPlayer)
        {
            room.CurrentTurnPlayerId = players
                .FirstOrDefault(x => x.Id != playerId)?.Id
                ?? throw new InvalidOperationException("The room does not have an opponent.");
        }
        else if (room.Mode == GameMode.PlayerVsAi)
        {
            var ai = players.FirstOrDefault(x => x.IsAi)
                ?? throw new InvalidOperationException("The AI player is missing.");
            room.CurrentTurnPlayerId = ai.Id;
        }

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
            tracking: true,
            cancellationToken: cancellationToken);

        room.Players = [.. players];
        room.Cards = [.. cards];

        foreach (var card in cards.Where(x => x.IsFlipped && !x.IsMatched))
            card.IsFlipped = false;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToActionResult(room, cards, players, "pair.hidden", false, null);
    }

    public async Task<IReadOnlyList<GameActionResultDto>> RunAutomaticTurnAsync(
        string roomId,
        CancellationToken cancellationToken = default)
    {
        var results = new List<GameActionResultDto>();

        while (true)
        {
            var room = await LoadRoomAsync(roomId, cancellationToken);
            if (room.Status != GameStatus.InProgress || room.Mode != GameMode.PlayerVsAi)
                break;

            var players = await playerRepository.ListAsync(
                x => x.RoomId == roomId,
                tracking: true,
                cancellationToken: cancellationToken);
            var ai = players.FirstOrDefault(x => x.IsAi);

            if (ai is null || room.CurrentTurnPlayerId != ai.Id)
                break;

            var cards = await cardRepository.ListAsync(
                x => x.RoomId == roomId,
                cancellationToken: cancellationToken);

            var available = cards
                .Where(x => !x.IsMatched && !x.IsFlipped)
                .Select(x => x.Position)
                .ToList();

            if (available.Count < 2)
                break;

            var memory = DeserializeAiMemory(ai.AiMemoryJson);
            var first = ChooseAiPosition(available, memory);
            var second = ChooseAiPartnerPosition(first, available, memory);

            results.Add(await SelectCardAsync(roomId, ai.Id, first, cancellationToken));

            if (results[^1].GameOver)
                break;

            results.Add(await SelectCardAsync(roomId, ai.Id, second, cancellationToken));

            if (results[^1].ResolveMismatchAfterDelay)
            {
                results.Add(await ResolveMismatchAsync(roomId, cancellationToken));
            }

            if (results[^1].GameOver)
                break;
        }

        return results;
    }

    public async Task<FileDto> GetCardImageAsync(
        string roomId,
        int position,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedAccessException("An image access token is required.");

        ImageTokenPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<ImageTokenPayload>(
                imageTokenProtector.Unprotect(token))
                ?? throw new UnauthorizedAccessException("Invalid image access token.");
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            throw new UnauthorizedAccessException("Invalid or expired image access token.", ex);
        }

        if (!string.Equals(payload.RoomId, roomId, StringComparison.Ordinal) ||
            payload.Position != position)
        {
            throw new UnauthorizedAccessException("The image token is not valid for this card.");
        }

        var card = await cardRepository.FirstOrDefaultAsync(
            x => x.RoomId == roomId && x.Position == position,
            cancellationToken: cancellationToken)
            ?? throw new KeyNotFoundException("Game card was not found.");

        if (!card.IsFlipped && !card.IsMatched)
            throw new UnauthorizedAccessException("The requested card is not currently revealed.");

        if (!string.Equals(payload.ImageId, card.LiteDbImageId, StringComparison.Ordinal))
            throw new UnauthorizedAccessException("The image token does not match the current card.");

        var stream = await imageRepository.GetThemeImageStreamAsync(card.LiteDbImageId);
        var contentType = (stream as LiteDB.LiteFileStream<string>)?.FileInfo.MimeType
            ?? "application/octet-stream";

        return new FileDto(stream, contentType);
    }

    private async Task<Theme> GetValidThemeAsync(
        string themeId,
        CancellationToken cancellationToken)
    {
        var theme = await themeRepository.GetByIdAsync(themeId, cancellationToken)
            ?? throw new KeyNotFoundException("Game theme was not found.");

        if (theme.Cards.Count < CardsPerTheme)
            throw new InvalidOperationException("The selected theme must contain at least 15 cards.");

        return theme;
    }

    private async Task CreateBoardAsync(
        Room room,
        Theme theme,
        CancellationToken cancellationToken)
    {
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
    }

    private async Task<GameBoardDto> BuildBoardAsync(
        Room room,
        CancellationToken cancellationToken)
    {
        var cards = await cardRepository.ListAsync(
            x => x.RoomId == room.Id,
            cancellationToken: cancellationToken);

        return ToBoard(room, cards);
    }

    private static Player CreatePlayer(
        string roomId,
        string playerId,
        string name,
        string mqttPassword)
        => new(playerId, roomId)
        {
            PeerIdentifier = Guid.CreateVersion7().ToString("N"),
            Name = name,
            MqttUsername = $"room-{roomId}-player-{playerId}",
            MqttPasswordHash = HashSecret(mqttPassword)
        };

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
            room.Mode.ToString(),
            room.Status.ToString(),
            room.RequirePassword,
            playerCount,
            room.TimeLimitSeconds);

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

    private GameBoardDto ToBoard(Room room, IReadOnlyList<Card> cards)
        => new(
            room.Id,
            room.ThemeId,
            room.Mode.ToString(),
            cards
                .OrderBy(x => x.Position)
                .Select(x => new BoardCardDto(
                    x.Position,
                    null,
                    false,
                    false))
                .ToList(),
            room.CurrentTurnPlayerId ?? string.Empty,
            GetRemainingMilliseconds(room));

    private GameActionResultDto ToActionResult(
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
            room.Mode.ToString(),
            room.CurrentTurnPlayerId ?? string.Empty,
            players.Select(x => new PlayerScoreDto(
                x.Id,
                x.Name,
                x.IsAi,
                x.Score,
                x.CorrectPairs,
                x.Errors,
                x.Moves,
                x.CurrentStreak)).ToList(),
            cards
                .OrderBy(x => x.Position)
                .Select(x => new BoardCardDto(
                    x.Position,
                    x.IsFlipped || x.IsMatched
                        ? CreateImageUrl(room.Id, x.Position, x.LiteDbImageId)
                        : null,
                    x.IsFlipped,
                    x.IsMatched))
                .ToList(),
            gameOver || room.Status == GameStatus.Finished,
            resolveMismatchAfterDelay,
            GetRemainingMilliseconds(room),
            message);

    private string CreateImageUrl(string roomId, int position, string imageId)
    {
        var token = imageTokenProtector.Protect(
            JsonSerializer.Serialize(new ImageTokenPayload(roomId, position, imageId)),
            ImageTokenLifetime);

        return $"/api/v1/game/rooms/{roomId}/cards/{position}/image?token={Uri.EscapeDataString(token)}";
    }

    private static long? GetRemainingMilliseconds(Room room)
    {
        if (room.Mode != GameMode.PlayerVsTime ||
            room.StartedAt is null)
            return null;

        var remaining = TimeSpan.FromSeconds(room.TimeLimitSeconds) -
                        (DateTime.UtcNow - room.StartedAt.Value);

        return Math.Max(0, (long)remaining.TotalMilliseconds);
    }

    private static bool IsTimeExpired(Room room)
        => GetRemainingMilliseconds(room) is 0;

    private static int CalculateScore(GameMode mode, int score, int streak)
        => mode is GameMode.PlayerVsTime or GameMode.PlayerVsAi
            ? (score + 1) * streak
            : score + 1;

    private static void UpdateAiMemory(
        IReadOnlyList<Player> players,
        IReadOnlyList<Card> cards)
    {
        var ai = players.FirstOrDefault(x => x.IsAi);
        if (ai is null)
            return;

        var memory = DeserializeAiMemory(ai.AiMemoryJson);

        foreach (var card in cards.Where(x => x.IsFlipped || x.IsMatched))
            memory[card.Position] = card.LiteDbImageId;

        ai.AiMemoryJson = JsonSerializer.Serialize(memory);
    }

    private static Dictionary<int, string> DeserializeAiMemory(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        try
        {
            return JsonSerializer.Deserialize<Dictionary<int, string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static int ChooseAiPosition(
        IReadOnlyCollection<int> available,
        IReadOnlyDictionary<int, string> memory)
    {
        var knownPairPosition = memory
            .Where(x => available.Contains(x.Key))
            .GroupBy(x => x.Value, StringComparer.Ordinal)
            .Where(x => x.Count() >= 2)
            .SelectMany(x => x.Take(2))
            .Select(x => (int?)x.Key)
            .FirstOrDefault();

        return knownPairPosition ??
               available.ElementAt(RandomNumberGenerator.GetInt32(available.Count));
    }

    private static int ChooseAiPartnerPosition(
        int first,
        IReadOnlyCollection<int> available,
        IReadOnlyDictionary<int, string> memory)
    {
        if (memory.TryGetValue(first, out var imageId))
        {
            var partner = memory
                .Where(x =>
                    x.Key != first &&
                    available.Contains(x.Key) &&
                    string.Equals(x.Value, imageId, StringComparison.Ordinal))
                .Select(x => (int?)x.Key)
                .FirstOrDefault();

            if (partner.HasValue)
                return partner.Value;
        }

        var candidates = available.Where(x => x != first).ToArray();
        return candidates[RandomNumberGenerator.GetInt32(candidates.Length)];
    }

    private async Task<Room> LoadRoomAsync(
        string roomId,
        CancellationToken cancellationToken)
        => await roomRepository.FirstOrDefaultAsync(
            x => x.Id == roomId,
            tracking: true,
            cancellationToken)
            ?? throw new KeyNotFoundException("Game room was not found.");

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

    private sealed record ImageTokenPayload(
        string RoomId,
        int Position,
        string ImageId);
}
