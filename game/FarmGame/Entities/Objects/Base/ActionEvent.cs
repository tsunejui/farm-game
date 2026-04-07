namespace FarmGame.Entities.Objects;

/// <summary>
/// Represents an action event for an object (move, attack, skill).
/// Enqueued into the object's per-instance ActionQueue by AI or input systems.
/// Processed during the object's update cycle.
/// </summary>
public record ActionEvent(
    /// <summary>Action type: "move", "attack", "skill".</summary>
    string ActionType,
    /// <summary>Target tile X.</summary>
    int TargetX,
    /// <summary>Target tile Y.</summary>
    int TargetY);
