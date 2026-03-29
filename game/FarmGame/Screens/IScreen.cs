// =============================================================================
// IScreen.cs — Common interface for all game screens
//
// ScreenTransition:
//   - None             : No state change, stay on current screen.
//   - To(GameState)    : Request transition to a different game state.
//   - ExitGame()       : Request game exit.
//
// IScreen:
//   - Initialize()     : Build UI widgets and set initial state. Called once on startup.
//   - Rebuild()        : Recreate UI (e.g. after language change). Preserves no runtime state.
//   - OnEnter(from)    : Called when transitioning into this screen. Handle per-screen enter logic.
//   - Update(gameTime) : Per-frame input handling. Returns ScreenTransition to signal state changes.
//   - Draw(spriteBatch): Render the screen. Myra screens call Desktop.Render() internally.
// =============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarmGame.Core;

namespace FarmGame.Screens;

public class ScreenTransition
{
    public static readonly ScreenTransition None = new() { Target = null };

    public GameState? Target { get; init; }
    public bool Exit { get; init; }

    public static ScreenTransition To(GameState state) => new() { Target = state };
    public static ScreenTransition ExitGame() => new() { Exit = true };
}

public interface IScreen
{
    void Initialize();
    void Rebuild();
    void OnEnter(GameState fromState);
    ScreenTransition Update(GameTime gameTime);
    void Draw(SpriteBatch spriteBatch);
}
