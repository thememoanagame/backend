namespace MemoAna.Infrastructure.Persistence.Options;

public class LiteDbOptions
{
    public const string SectionName = "LiteDb";

    /// <summary>
    /// Phisical file path (e.g. "Data/images.db")
    /// </summary>
    public string DatabasePath { get; set; } = string.Empty;
}
