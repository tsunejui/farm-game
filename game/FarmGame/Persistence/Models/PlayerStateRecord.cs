using SQLite;

namespace FarmGame.Persistence.Models;

[Table("player")]
public class PlayerStateRecord
{
    [PrimaryKey, AutoIncrement]
    [Column("id")]
    public int Id { get; set; }

    [Unique]
    [Column("player_uuid")]
    public string PlayerUuid { get; set; }

    [Column("state_json")]
    public string StateJson { get; set; }

    [Column("game_version")]
    public string GameVersion { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; }

    [Column("updated_at")]
    public string UpdatedAt { get; set; }
}
