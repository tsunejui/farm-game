using SQLite;

namespace FarmGame.Persistence.Entities;

[Table("map_state")]
public class MapStateRecord
{
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; }   // UUID

    [Column("map_id")]
    public string MapId { get; set; }

    [Column("state_json")]
    public string StateJson { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; }

    [Column("updated_at")]
    public string UpdatedAt { get; set; }

    /// <summary>
    /// Unix timestamp (seconds) when this map state expires. 0 = active/no expiry.
    /// </summary>
    [Column("TtlUtc")]
    public long TtlUtc { get; set; }
}
