using SQLite;

namespace FarmGame.Persistence.Models;

[Table("map_object")]
public class MapObjectRecord
{
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; }   // UUID

    [Indexed]
    [Column("map_state_id")]
    public string MapStateId { get; set; }   // FK → map_state.id

    [Column("item_id")]
    public string ItemId { get; set; }

    [Column("category")]
    public string Category { get; set; }   // "item" or "creature"

    [Column("tile_x")]
    public int TileX { get; set; }

    [Column("tile_y")]
    public int TileY { get; set; }

    [Column("hp")]
    public int Hp { get; set; }

    [Column("state_json")]
    public string StateJson { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; }

    [Column("updated_at")]
    public string UpdatedAt { get; set; }
}
