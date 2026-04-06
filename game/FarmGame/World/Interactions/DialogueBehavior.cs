// =============================================================================
// DialogueBehavior.cs — Shows a dialogue panel when player interacts
//
// Triggered via Z key when player faces an adjacent interactable object.
// Dialogue lines are locale keys resolved from the "dialogues" module:
//   dialogue_lines: ["signpost_farm_portal_right", "signpost_farm_portal_left"]
// Each key is looked up via LocaleManager.Get("dialogues", key).
// ChargeTime is 0 — instant trigger (no overlap timer needed).
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using FarmGame.Core;
using FarmGame.Core.Managers;

namespace FarmGame.World.Interactions;

public class DialogueBehavior : IInteractionBehavior
{
    public float ChargeTime => 0f;

    // Raw locale keys from YAML properties
    private readonly List<string> _lineKeys;

    public DialogueBehavior(List<string> lineKeys)
    {
        _lineKeys = lineKeys;
    }

    public InteractionRequest Execute(WorldObject source, WorldObject player)
    {
        return null;
    }

    // Resolve locale keys to display strings
    public List<string> GetLines()
    {
        return _lineKeys
            .Select(key => LocaleManager.Get("dialogues", key, key))
            .ToList();
    }
}
