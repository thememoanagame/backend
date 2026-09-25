namespace MemoAna.Application.Game.Requests;

public sealed record GameDataRequest(
    string ThemeName,
    bool IsDefault,
    IReadOnlyList<string> Base64Images);

public sealed record CreateGameThemeRequest(
    string ThemeName);

public sealed record CreateRoomRequest(
    string Name,
    string ThemeId,
    int Difficulty,
    bool RequirePassword,
    string? Password,
    string PlayerName,
    MemoAna.Domain.Game.GameMode Mode = MemoAna.Domain.Game.GameMode.PlayerVsPlayer,
    int? TimeLimitSeconds = null);

public sealed record JoinRoomRequest(
    bool HasPassword,
    string? Password,
    string PlayerName);