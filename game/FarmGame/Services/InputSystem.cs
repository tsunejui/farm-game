using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using FarmGame.Queues;
using FarmGame.Queues.Events;

namespace FarmGame.Services;

/// <summary>
/// Centralized input handling. Reads raw keyboard/gamepad state each frame
/// and translates it into semantic events published to QueueManager.
/// Game1 calls Process() once per frame instead of checking keys directly.
/// </summary>
public class InputSystem
{
    private readonly QueueManager _queue;

    /// <summary>True if Escape was pressed this frame (consumed after read).</summary>
    public bool PauseToggled { get; private set; }

    public InputSystem(QueueManager queue)
    {
        _queue = queue;
    }

    /// <summary>
    /// Read input state and publish events. Called once per frame from Game1.Update.
    /// Returns true if the game should exit (gamepad Back pressed).
    /// </summary>
    public bool Process(GameTime gameTime)
    {
        KeyboardExtended.Update();
        var keyboard = KeyboardExtended.GetState();

        // Gamepad quit
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            return true;

        // Publish raw keyboard state for controllers that need it
        _queue.Publish(new InputEvent(Keyboard.GetState(), gameTime));

        // Semantic events
        PauseToggled = keyboard.WasKeyPressed(Keys.Escape);
        if (PauseToggled)
            _queue.Publish(new TogglePauseEvent());

        return false;
    }
}
