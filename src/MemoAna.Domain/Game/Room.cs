namespace MemoAna.Domain.Game;

/// <summary>Represents a multiplayer game room.</summary>
public class Room : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string ThemeId { get; set; } = string.Empty;
    public int Difficulty { get; set; }
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

        if (Players.Count >= 2)
            throw new InvalidOperationException("The room is full.");

        Players.Add(player);
    }

    public void StartGame(string startingPlayerId)
    {
        if (Players.Count != 2)
            throw new InvalidOperationException("A game requires exactly two players.");

        if (Players.All(p => p.Id != startingPlayerId))
            throw new InvalidOperationException("The starting player must belong to the room.");

        Status = GameStatus.InProgress;
        CurrentTurnPlayerId = startingPlayerId;
    }

    public void CompleteGame()
    {
        Status = GameStatus.Completed;
        CurrentTurnPlayerId = null;
    }
}
