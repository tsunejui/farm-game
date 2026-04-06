namespace FarmGame.Models;

/// <summary>
/// Object effect model used by game logic.
/// Represents a persisted effect on an object without DB-specific attributes.
/// </summary>
public class ObjectEffectModel
{
    public string EffectId { get; set; }
    public float Ttl { get; set; }
    public string UpdatedAt { get; set; }
}
