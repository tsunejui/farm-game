using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using FarmGame.Core;

namespace FarmGame.Services;

/// <summary>
/// Centralized input handling. Reads raw keyboard/gamepad state each frame
/// and translates it into semantic events published to QueueManager.
///
/// When InputBlocked is true (e.g. menu open), game input events are
/// suppressed — only ESC toggle is published.
/// </summary>
public class InputSystem
{
    private readonly QueueManager _queue;

    /// <summary>
    /// When true, game input (InputEvent) is suppressed.
    /// Menu panels still receive keyboard input via their own Update().
    /// </summary>
    public bool InputBlocked { get; set; }

    public InputSystem(QueueManager queue)
    {
        _queue = queue;
    }

    public bool Process(GameTime gameTime)
    {
        KeyboardExtended.Update();
        var keyboard = KeyboardExtended.GetState();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            return true;

        // ESC always works (toggle menu)
        if (keyboard.WasKeyPressed(Keys.Escape))
            _queue.Publish(new TogglePauseEvent());

        // Game input only when not blocked by menu/panel
        if (!InputBlocked)
            _queue.Publish(new InputEvent(Keyboard.GetState(), gameTime));

        return false;
    }
}
