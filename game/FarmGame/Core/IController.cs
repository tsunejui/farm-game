using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FarmGame.Core;

/// <summary>
/// Interface for all game controllers.
/// Each controller owns sub-managers and follows the lifecycle:
///   Initialize() → Load(config) → [UpdateLogic → SyncState → DrawRender] → Shutdown()
/// </summary>
public interface IController
{
    /// <summary>Display name for logging.</summary>
    string Name { get; }

    /// <summary>Draw order (lower = drawn first = behind).</summary>
    int Order { get; }

    /// <summary>Whether this controller should update and draw.</summary>
    bool IsActive { get; set; }

    /// <summary>Create sub-managers and internal state. No external dependencies needed.</summary>
    void Initialize();

    /// <summary>Load resources, wire dependencies using config from SystemController.</summary>
    void Load(ConfigManager config);

    /// <summary>Graceful shutdown: close connections, dispose managers, flush queues.</summary>
    void Shutdown();

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
