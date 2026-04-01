using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;


namespace FarmGame.Core;

/// <summary>
/// Interface for all game controllers in the responsibility chain.
/// Controllers manage a specific layer of the game (background, world, UI, etc.)
/// using double-buffered state for thread safety.
/// </summary>
public interface IController
{
    /// <summary>Display name for logging.</summary>
    string Name { get; }

    /// <summary>Draw order (lower = drawn first = behind).</summary>
    int Order { get; }

    /// <summary>Whether this controller's UpdateLogic should run.</summary>
    bool IsActive { get; set; }

    /// <summary>Subscribe to events via QueueManager. Called during Initialize.</summary>
    void Subscribe(QueueManager queue);

    /// <summary>Load textures, fonts, and other content resources.</summary>
    void LoadResource(GraphicsDevice graphicsDevice, string contentDir);

    /// <summary>
    /// Update game logic (may run on worker thread via Parallel.ForEach).
    /// Writes to LogicState only. Must NOT touch RenderState.
    /// </summary>
    void UpdateLogic(GameTime gameTime);

    /// <summary>
    /// Copy LogicState → RenderState. Called on main thread after parallel update.
    /// </summary>
    void SyncState();

    /// <summary>
    /// Draw using RenderState only. Called on main thread in order.
    /// </summary>
    void DrawRender(SpriteBatch spriteBatch);
}
