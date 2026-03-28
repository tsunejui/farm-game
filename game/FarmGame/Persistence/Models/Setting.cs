using SQLite;

namespace FarmGame.Persistence.Models;

[Table("setting")]
public class Setting
{
    [PrimaryKey]
    [Column("key")]
    public string Key { get; set; }

    [Column("value")]
    public string Value { get; set; }
}
