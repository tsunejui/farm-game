namespace FarmGame.Models;

/// <summary>
/// Map state model used by game logic.
/// Represents a persisted map state without DB-specific attributes.
/// </summary>
public class MapStateModel
{
    public string Id { get; set; }
    public string MapId { get; set; }
    public string StateJson { get; set; }
    public long TtlUtc { get; set; }
}
