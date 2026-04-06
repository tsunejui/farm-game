// =============================================================================
// IView.cs — Common interface for all game views
//
// ViewTransition:
//   - None             : No state change, stay on current view.
//   - To(GameState)    : Request transition to a different game state.
//   - ExitGame()       : Request game exit.
//
// IView:
//   - Initialize()     : Build UI widgets and set initial state. Called once on startup.
//   - Rebuild()        : Recreate UI (e.g. after language change). Preserves no runtime state.
//   - OnEnter(from)    : Called when transitioning into this view.
//   - Update(gameTime) : Per-frame input handling. Returns ViewTransition to signal state changes.
//   - Draw(spriteBatch): Render the view. Myra views call Desktop.Render() internally.
// =============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FarmGame.Core;

namespace FarmGame.Views;

public class ViewTransition
{
    public static readonly ViewTransition None = new() { Target = null };

    public GameState? Target { get; init; }
    public bool Exit { get; init; }

    public static ViewTransition To(GameState state) => new() { Target = state };
    public static ViewTransition ExitGame() => new() { Exit = true };
}

public interface IView
{
    void Initialize();
    void Rebuild();
    void OnEnter(GameState fromState);
    void OnExit(GameState toState) { }
    ViewTransition Update(GameTime gameTime);
    void Draw(SpriteBatch spriteBatch);
}
