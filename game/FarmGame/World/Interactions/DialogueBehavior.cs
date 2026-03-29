// =============================================================================
// DialogueBehavior.cs — Shows a dialogue panel when player interacts
//
// Triggered via Z key when player faces an adjacent interactable object.
// Dialogue lines are read from the object's instance properties:
//   dialogue_lines: ["line1", "line2", ...]
// ChargeTime is 0 — instant trigger (no overlap timer needed).
// =============================================================================

using System;
using System.Collections.Generic;

namespace FarmGame.World.Interactions;

public class DialogueBehavior : IInteractionBehavior
{
    public float ChargeTime => 0f;

    private readonly List<string> _lines;

    public DialogueBehavior(List<string> lines)
    {
        _lines = lines;
    }

    public InteractionRequest Execute(WorldObject source, WorldObject player)
    {
        // Dialogue is handled by the OnInteract callback in AttackAction,
        // not via InteractionRequest. Return null.
        return null;
    }

    public List<string> GetLines() => _lines;
}
