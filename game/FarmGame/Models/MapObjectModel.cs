namespace FarmGame.Models;

/// <summary>
/// Map object model used by game logic.
/// Represents a persisted object on a map without DB-specific attributes.
/// </summary>
public class MapObjectModel
{
    public string Id { get; set; }
    public string ItemId { get; set; }
    public string Category { get; set; }
    public int TileX { get; set; }
    public int TileY { get; set; }
    public int Hp { get; set; }
    public string StateJson { get; set; }
}
