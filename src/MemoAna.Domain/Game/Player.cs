namespace MemoAna.Domain.Game;

/// <summary>Represents a player participating in a game room.</summary>
public class Player(string id = "", string roomId = "") : EntityBase(id)
{
    public string RoomId { get; set; } = roomId ?? Guid.CreateVersion7().ToString();
    public string PeerIdentifier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
    public string MqttUsername { get; set; } = string.Empty;
    public string MqttPasswordHash { get; set; } = string.Empty;
    public Room? Room { get; set; }
    public bool IsAi { get; set; }
    public int CurrentStreak { get; set; }
    public int Moves { get; set; }
    public int CorrectPairs { get; set; }
    public int Errors { get; set; }
    public string AiMemoryJson { get; set; } = string.Empty;
}
