namespace MemoAna.Application.Game.Requests;

/// <summary>Represents the payload used to create or update game data.</summary>
/// <param name="ThemeName">The display name of the card theme.</param>
/// <param name="IsDefault">Whether the theme is the default theme.</param>
/// <param name="Base64Images">The theme images encoded as Base64.</param>
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
    string PlayerName);

public sealed record JoinRoomRequest(
    bool HasPassword,
    string? Password,
    string PlayerName);
