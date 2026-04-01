using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using FarmGame.Queues;

namespace FarmGame.Core;

/// <summary>
/// Base class for all controllers implementing double-buffered state.
/// TLogic is the mutable state written during UpdateLogic.
/// TRender is the read-only snapshot used during DrawRender.
/// </summary>
public abstract class BaseController<TLogic, TRender> : IController
    where TLogic : class, new()
    where TRender : class, new()
{
    public abstract string Name { get; }
    public abstract int Order { get; }
    public bool IsActive { get; set; } = true;

    /// <summary>Mutable state — written only during UpdateLogic (worker thread).</summary>
    protected TLogic LogicState { get; } = new();

    /// <summary>Read-only snapshot — read only during DrawRender (main thread).</summary>
    protected TRender RenderState { get; private set; } = new();

    private readonly object _syncLock = new();

    public virtual void Subscribe(QueueManager queue) { }
    public virtual void LoadResource(GraphicsDevice graphicsDevice, string contentDir) { }
    public virtual void UpdateLogic(GameTime gameTime) { }
    public virtual void DrawRender(SpriteBatch spriteBatch) { }

    /// <summary>
    /// Copy LogicState → RenderState on main thread.
    /// Override CopyState to define the field-by-field copy.
    /// </summary>
    public void SyncState()
    {
        lock (_syncLock)
        {
            var snapshot = new TRender();
            CopyState(LogicState, snapshot);
            RenderState = snapshot;
        }
    }

    /// <summary>
    /// Define how to copy fields from logic to render state.
    /// Must be implemented by each controller.
    /// </summary>
    protected abstract void CopyState(TLogic logic, TRender render);
}
