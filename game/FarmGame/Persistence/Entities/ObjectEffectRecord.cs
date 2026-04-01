using SQLite;

namespace FarmGame.Persistence.Entities;

[Table("object_effect")]
public class ObjectEffectRecord
{
    [PrimaryKey]
    [Column("id")]
    public string Id { get; set; }   // UUID

    [Indexed]
    [Column("object_id")]
    public string ObjectId { get; set; }   // FK → map_object.id

    [Column("effect_id")]
    public string EffectId { get; set; }   // e.g. "indestructible"

    [Column("ttl")]
    public float Ttl { get; set; }         // seconds; 0 = permanent

    [Column("created_at")]
    public string CreatedAt { get; set; }

    [Column("updated_at")]
    public string UpdatedAt { get; set; }
}
