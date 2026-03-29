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

    // Primary attributes (persisted as individual columns)
    [Column("max_hp")]
    public int MaxHp { get; set; }

    [Column("current_hp")]
    public int CurrentHp { get; set; }

    [Column("strength")]
    public float Strength { get; set; }

    [Column("dexterity")]
    public float Dexterity { get; set; }

    [Column("weapon_atk")]
    public float WeaponAtk { get; set; }

    [Column("buff_percent")]
    public float BuffPercent { get; set; }

    [Column("crit_rate")]
    public float CritRate { get; set; }

    [Column("crit_damage")]
    public float CritDamage { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; }

    [Column("updated_at")]
    public string UpdatedAt { get; set; }
}
