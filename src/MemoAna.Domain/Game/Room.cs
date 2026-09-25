namespace MemoAna.Domain.Game;

public class Room : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string ThemeId { get; set; } = string.Empty;
    public int Difficulty { get; set; }
    public GameMode Mode { get; set; } = GameMode.PlayerVsPlayer;
    public int TimeLimitSeconds { get; set; }
    public DateTime? StartedAt { get; set; }
    public bool RequirePassword { get; set; }
    public string? JoinPasswordHash { get; set; }
    public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;

    public string? CurrentTurnPlayerId { get; set; }
    public Player? CurrentTurnPlayer { get; set; }
    public Theme? Theme { get; set; }

    public ICollection<Player> Players { get; set; } = [];
    public ICollection<Card> Cards { get; set; } = [];

    public bool CanFlipCard(string playerId, int position)
    {
        if (Status != GameStatus.InProgress || CurrentTurnPlayerId != playerId)
            return false;

        var card = Cards.FirstOrDefault(c => c.Position == position);
        return card is not null && !card.IsMatched && !card.IsFlipped;
    }

    public void AddPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (Mode == GameMode.PlayerVsTime)
            throw new InvalidOperationException("A time room accepts only its creator.");

        var maximumPlayers = Mode == GameMode.PlayerVsAi ? 2 : 2;
        if (Players.Count >= maximumPlayers)
            throw new InvalidOperationException("The room is full.");

        Players.Add(player);
    }

    public void StartGame(string startingPlayerId)
    {
        var requiredPlayers = Mode == GameMode.PlayerVsTime ? 1 : 2;
        if (Players.Count != requiredPlayers)
            throw new InvalidOperationException($"This game mode requires {requiredPlayers} player(s).");

        if (Players.All(p => p.Id != startingPlayerId))
            throw new InvalidOperationException("The starting player must belong to the room.");

        Status = GameStatus.InProgress;
        StartedAt = DateTime.UtcNow;
        CurrentTurnPlayerId = startingPlayerId;
    }

    public void CompleteGame()
    {
        Status = GameStatus.Finished;
        CurrentTurnPlayerId = null;
    }
}
