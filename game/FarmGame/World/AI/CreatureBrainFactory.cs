namespace FarmGame.World.AI;

/// <summary>
/// Creates a CreatureBrain with properly wired FSM states for a creature.
/// </summary>
public static class CreatureBrainFactory
{
    public static CreatureBrain Create(WorldObject creature)
    {
        // Create states (circular references resolved via properties)
        var hostileState = new HostileState();
        var neutralState = new NeutralState();
        var idleState = new IdleState(neutralState, hostileState);

        // Wire cross-references
        neutralState.IdleState = idleState;
        neutralState.HostileState = hostileState;

        // Pick initial state based on default behavior
        ICreatureState initial = creature.State.Behavior switch
        {
            BehaviorState.Idle => idleState,
            BehaviorState.Hostile => hostileState,
            _ => neutralState,
        };

        return new CreatureBrain(creature, initial);
    }
}
