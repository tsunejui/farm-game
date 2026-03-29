namespace FarmGame.World.AI;

/// <summary>
/// Interface for creature FSM states. Each state handles its own
/// update logic and returns the next state (or itself to stay).
/// </summary>
public interface ICreatureState
{
    /// <summary>State identifier for debugging/logging.</summary>
    string Name { get; }

    /// <summary>Called once when entering this state.</summary>
    void Enter(WorldObject creature, GameMap map);

    /// <summary>Called every frame. Returns the next state (or this to stay).</summary>
    ICreatureState Update(WorldObject creature, GameMap map, float deltaTime);
}
