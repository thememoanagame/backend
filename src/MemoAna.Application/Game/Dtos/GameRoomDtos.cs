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
    string Status,
    bool RequirePassword,
    int PlayerCount);

public sealed record RoomSessionDto(
    RoomSummaryDto Room,
    string PlayerId,
    MqttCredentialsDto Credentials,
    GameBoardDto? Board);

public sealed record GameBoardDto(
    string RoomId,
    string ThemeId,
    IReadOnlyList<BoardCardDto> Cards,
    string CurrentPlayerId);

public sealed record BoardCardDto(
    int Position,
    string ImageId,
    bool IsFlipped,
    bool IsMatched);

public sealed record GameActionResultDto(
    string RoomId,
    string Event,
    string CurrentPlayerId,
    IReadOnlyList<PlayerScoreDto> Players,
    IReadOnlyList<BoardCardDto> Cards,
    bool GameOver,
    bool ResolveMismatchAfterDelay,
    string? Message);

public sealed record PlayerScoreDto(
    string PlayerId,
    string Name,
    int Score);
