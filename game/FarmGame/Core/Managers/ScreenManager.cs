// =============================================================================
// ScreenManager.cs — Scene state tracker
//
// Tracks the current game state (screen). Actual screen lifecycle is
// managed by BackgroundController which owns the IScreen instances.
// =============================================================================

namespace FarmGame.Core.Managers;

/// <summary>
/// Tracks the current game state for screen transitions.
/// Owned by BackgroundController.
/// </summary>
public class ScreenManager
{
    public GameState CurrentState { get; private set; }

    /// <summary>Set the current state. Screen activation is handled by BackgroundController.</summary>
    public void TransitionTo(GameState target)
    {
        CurrentState = target;
    }
}
