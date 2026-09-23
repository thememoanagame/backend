namespace MemoAna.Domain.Game;

/// <summary>Card representation class.</summary>
public class Card(string id = "", string roomId = "") : EntityBase(id)
{
    /// <summary>ID of the room that the card is in.</summary>
    public string RoomId { get; set; } = roomId ?? Guid.CreateVersion7().ToString();
    /// <summary>Posição na grid do front-end (ex: 0 a 29)</summary>
    public int Position { get; set; }
    /// <summary>Referência real da imagem armazenada no LiteDB</summary>
    public string LiteDbImageId { get; set; } = string.Empty;
    /// <summary>Authoritative flag to indicate if the card is flipped</summary>
    public bool IsFlipped { get; set; }
    /// <summary>Authoritative flag to indicate if the card is matched</summary>
    public bool IsMatched { get; set; }
    /// <summary>EF Core navigation</summary>
    public Room? Room { get; set; }
}