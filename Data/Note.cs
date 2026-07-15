namespace BesqlPersistencePoc.Data;

// One trivial entity — deliberately minimal. No JSON HasConversion mappings, no audit fields.
public class Note
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
