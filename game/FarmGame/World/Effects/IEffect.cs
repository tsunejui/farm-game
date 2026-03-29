// =============================================================================
// IEffect.cs — Interface for stackable object effects
//
// Each effect has a unique string ID. Effects can modify damage, stats,
// or behavior. TTL=0 means permanent (never expires).
// =============================================================================

namespace FarmGame.World.Effects;

public interface IEffect
{
    // Unique effect identifier (e.g. "indestructible")
    string Id { get; }

    // Display name for UI
    string DisplayName { get; }

    // Called when a damage amount is about to be applied.
    // Returns the modified damage amount. Return 0 to negate all damage.
    int ModifyDamage(WorldObject obj, int damage) => damage;

    // Called every effect refresh tick (~1 second). Used for aura/DoT effects.
    // The owner is the object holding this effect; map provides nearby lookups.
    void OnTick(WorldObject owner, GameMap map) { }
}
