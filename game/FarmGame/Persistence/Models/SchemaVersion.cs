using SQLite;

namespace FarmGame.Persistence.Models;

[Table("schema_version")]
public class SchemaVersion
{
    [Column("version")]
    public int Version { get; set; }

    [Column("applied_at")]
    public string AppliedAt { get; set; }

    [Column("description")]
    public string Description { get; set; }
}
