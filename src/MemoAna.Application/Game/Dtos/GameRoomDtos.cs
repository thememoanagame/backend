namespace MemoAna.Application.Game.Dtos;

public sealed record MqttCredentialsDto(
    string Endpoint,
    int Port,
    string ClientId,
    string Username,
    string Password,
    string BoardTopic,
    string PlayerTopic);

public sealed record RoomSummaryDto(
    string Id,
    string Name,
    string ThemeId,
    int Difficulty,
    string Mode,
    string Status,
    bool RequirePassword,
    int PlayerCount,
    int TimeLimitSeconds);

public sealed record RoomSessionDto(
    RoomSummaryDto Room,
    string PlayerId,
    MqttCredentialsDto Credentials,
    GameBoardDto? Board);

public sealed record GameBoardDto(
    string RoomId,
    string ThemeId,
    string Mode,
    IReadOnlyList<BoardCardDto> Cards,
    string CurrentPlayerId,
    long? RemainingMilliseconds);

public sealed record BoardCardDto(
    int Position,
    string? ImageUrl,
    bool IsFlipped,
    bool IsMatched);

public sealed record GameActionResultDto(
    string RoomId,
    string Event,
    string Mode,
    string CurrentPlayerId,
    IReadOnlyList<PlayerScoreDto> Players,
    IReadOnlyList<BoardCardDto> Cards,
    bool GameOver,
    bool ResolveMismatchAfterDelay,
    long? RemainingMilliseconds,
    string? Message);

public sealed record PlayerScoreDto(
    string PlayerId,
    string Name,
    bool IsAi,
    int Score,
    int CorrectPairs,
    int Errors,
    int Moves,
    int CurrentStreak);