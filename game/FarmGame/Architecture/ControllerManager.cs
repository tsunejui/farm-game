using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;

namespace FarmGame.Architecture;

/// <summary>
/// Manages all controllers: registration, parallel update, ordered draw.
/// </summary>
public class ControllerManager
{
    private readonly List<IController> _controllers = new();
    private IController[] _sortedForDraw;

    public void Register(IController controller)
    {
        _controllers.Add(controller);
        _sortedForDraw = null; // invalidate cache
        Log.Information("[ControllerManager] Registered: {Name} (order={Order})",
            controller.Name, controller.Order);
    }

    public T Get<T>() where T : class, IController
    {
        return _controllers.OfType<T>().FirstOrDefault();
    }

    /// <summary>
    /// Register all controllers as MediatR handlers and build the QueueManager.
    /// </summary>
    public void SubscribeAll(QueueManager queue)
    {
        foreach (var c in _controllers)
        {
            queue.RegisterHandler(c);
            c.Subscribe(queue);
        }
        queue.Build();
    }

    /// <summary>Load resources for all controllers.</summary>
    public void LoadAllResources(GraphicsDevice graphicsDevice, string contentDir)
    {
        foreach (var c in _controllers)
            c.LoadResource(graphicsDevice, contentDir);
    }

    /// <summary>
    /// Parallel update: runs all active controllers' UpdateLogic concurrently.
    /// </summary>
    public void ParallelUpdate(GameTime gameTime)
    {
        var active = _controllers.Where(c => c.IsActive).ToArray();
        Parallel.ForEach(active, controller =>
        {
            controller.UpdateLogic(gameTime);
        });
    }

    /// <summary>
    /// Sync point: copy LogicState → RenderState on main thread.
    /// </summary>
    public void SyncAll()
    {
        foreach (var c in _controllers)
            c.SyncState();
    }

    /// <summary>
    /// Responsibility chain draw: render controllers in Order sequence.
    /// </summary>
    public void DrawAll(SpriteBatch spriteBatch)
    {
        _sortedForDraw ??= _controllers.OrderBy(c => c.Order).ToArray();

        foreach (var c in _sortedForDraw)
            c.DrawRender(spriteBatch);
    }
}
