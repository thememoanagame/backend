namespace MemoAna.Domain.Game;
/// <summary>Represents a game room.</summary>
public class Room : EntityBase
{
    /// <summary>EF Core Navigation.</summary>
    public string ThemeId { get; set; } = string.Empty;
    /// <summary>Game status of the room.</summary>
    public GameStatus Status { get; set; } = GameStatus.WaitingForPlayers;

    /// <summary>EF Core Navigation.</summary>
    public string? CurrentTurnPlayerId { get; set; }
    /// <summary>EF Core Navigation.</summary>
    public Player? CurrentTurnPlayer { get; set; }
    /// <summary>EF Core Navigation.</summary>
    public Theme? Theme { get; set; }

    /// <summary>EF Core relationship.</summary>
    public ICollection<Player> Players { get; set; } = [];
    /// <summary>EF Core relationship.</summary>
    public ICollection<Card> Cards { get; set; } = [];

    /// <summary>Domain behavior to check if a card can be flipped.</summary>
    public bool CanFlipCard(string playerId, int position)
    {
        if (Status != GameStatus.InProgress) return false;
        if (CurrentTurnPlayerId != playerId) return false;

        var card = Cards.FirstOrDefault(c => c.Position == position);
        if (card == null) return false;

        // cannot flip a card that is already matched or already flipped
        return !card.IsMatched && !card.IsFlipped;
    }

    /// <summary>Domain behavior to add a player to the room.</summary>
    public void AddPlayer(Player player)
    {
        if (Players.Count >= 2) throw new InvalidOperationException("A sala já está cheia.");
        Players.Add(player);

        if (Players.Count == 2)
        {
            Status = GameStatus.InProgress;
            CurrentTurnPlayerId = Players.First().Id; // O The creator or the first player added starts the game
        }
    }

}