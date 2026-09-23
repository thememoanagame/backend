namespace MemoAna.Infrastructure.Persistence.Options;

public sealed class ConnectionStringsOptions
{
    public const string SectionName = "ConnectionStrings";
    public required string Postgres { get; set; }
    public required string LiteDB { get; set; }
    public required string Mongo { get; set; }
    public required string SQLite { get; set; }

}
