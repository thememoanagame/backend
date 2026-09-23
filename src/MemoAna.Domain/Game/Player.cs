namespace MemoAna.Domain.Game;

/// <summary>Player representation class.</summary>
public class Player(string id = "", string roomId = "") : EntityBase(id)
{
    /// <summary>ID of the room that the player is in.</summary>
    public string RoomId { get; set; } = roomId ?? Guid.CreateVersion7().ToString();
    /// <summary>ID real do usuário ou ConnectionId.</summary>
    public string PeerIdentifier { get; set; } = string.Empty;
    /// <summary>Player Name.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Player Score.</summary>
    public int Score { get; set; }

    /// <summary>EF Core Navigation.</summary>
    public Room? Room { get; set; }
}