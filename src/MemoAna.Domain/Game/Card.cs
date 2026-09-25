namespace MemoAna.Domain.Game;

public class Card(string id = "", string roomId = "") : EntityBase(id)
{
    public string RoomId { get; set; } = roomId ?? Guid.CreateVersion7().ToString();
    public int Position { get; set; }
    public string LiteDbImageId { get; set; } = string.Empty;
    public bool IsFlipped { get; set; }
    public bool IsMatched { get; set; }
    public Room? Room { get; set; }
}