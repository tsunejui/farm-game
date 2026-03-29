using Serilog;

namespace FarmGame.World.AI;

/// <summary>
/// Manages the finite state machine for a single creature.
/// Holds the current state and transitions between states each frame.
/// </summary>
public class CreatureBrain
{
    private ICreatureState _currentState;
    private readonly WorldObject _creature;

    public string CurrentStateName => _currentState?.Name ?? "none";

    public CreatureBrain(WorldObject creature, ICreatureState initialState)
    {
        _creature = creature;
        _currentState = initialState;
        _currentState?.Enter(creature, null);
    }

    public void Update(GameMap map, float deltaTime)
    {
        if (_currentState == null || !_creature.State.IsAlive) return;

        var nextState = _currentState.Update(_creature, map, deltaTime);
        if (nextState != _currentState)
        {
            Log.Debug("Creature {Id} at ({X},{Y}): {From} → {To}",
                _creature.ItemId, _creature.TileX, _creature.TileY,
                _currentState.Name, nextState.Name);
            _currentState = nextState;
            _currentState.Enter(_creature, map);
        }
    }
}
