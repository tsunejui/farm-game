// =============================================================================
// InputController.cs — Keyboard, mouse, gamepad input handling
//
// Order: 20 (runs early to publish input events before other controllers)
// Reads raw input state each frame and publishes semantic events
// to QueueManager. Subscribers read events by queue ID.
// =============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using FarmGame.Core;

namespace FarmGame.Controllers;

public class InputLogicState
{
    public bool InputBlocked { get; set; }
    public bool ExitRequested { get; set; }
}

public class InputRenderState { }

public class InputController : BaseController<InputLogicState, InputRenderState>
{
    public override string Name => "Input";
    public override int Order => 20;

    private QueueManager _queue;

    /// <summary>
    /// When true, game input (InputEvent) is suppressed.
    /// Menu panels still receive keyboard input via their own Update().
    /// </summary>
    public bool InputBlocked
    {
        get => LogicState.InputBlocked;
        set => LogicState.InputBlocked = value;
    }

    /// <summary>True when the player pressed Back button to exit.</summary>
    public bool ExitRequested => LogicState.ExitRequested;

    public override void Load(ControllerManager controllers)
    {
        _queue = controllers.System.Queue;
    }

    public override void UpdateLogic(GameTime gameTime)
    {
        if (_queue == null) return;

        KeyboardExtended.Update();
        var keyboard = KeyboardExtended.GetState();

        // Back button = exit
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
        {
            LogicState.ExitRequested = true;
            return;
        }

        // ESC always works (toggle menu)
        if (keyboard.WasKeyPressed(Keys.Escape))
            _queue.Publish(new TogglePauseEvent());

        // Game input only when not blocked by menu/panel
        if (!LogicState.InputBlocked)
            _queue.Publish(new InputEvent(Keyboard.GetState(), gameTime));
    }

    public override void Shutdown() { }

    protected override void CopyState(InputLogicState logic, InputRenderState render) { }
}
