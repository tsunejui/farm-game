using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using FarmGame.Core;

namespace FarmGame.Services;

/// <summary>
/// Centralized input handling. Reads raw keyboard/gamepad state each frame
/// and translates it into semantic events published to QueueManager.
/// </summary>
public class InputSystem
{
    private readonly QueueManager _queue;

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

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            return true;

        // Publish raw keyboard state for controllers
        _queue.Publish(new InputEvent(Keyboard.GetState(), gameTime));

        // ESC → TogglePauseEvent (for future in-game menu panel via UIController)
        if (keyboard.WasKeyPressed(Keys.Escape))
            _queue.Publish(new TogglePauseEvent());

        return false;
    }
}
