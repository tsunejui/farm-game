using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using FarmGame.Controllers;
using FarmGame.Data;
using FarmGame.Services;

namespace FarmGame.Core;

/// <summary>
/// Manages all controllers: creation, registration, parallel update, ordered draw.
/// QueueManager is injected so controllers can subscribe to specific queues.
/// </summary>
public class ControllerManager
{
    private readonly List<IController> _controllers = new();
    private IController[] _sortedForDraw;
    private QueueManager _queue;

    public WorldController World { get; private set; }
    public UIController UI { get; private set; }

    public Action OnLeaveGame { get; set; }
    public Action OnSettings { get; set; }

    // ─── Initialization ─────────────────────────────────────

    /// <summary>
    /// Create all controllers, wire callbacks, subscribe to queues, load resources.
    /// Called from InitManager.LoadContent after QueueManager is ready.
    /// </summary>
    public void ConfigureAll(
        IAssetService assets,
        DataRegistry registry,
        GameSession session,
        QueueManager queue)
    {
        _queue = queue;

        // Create controllers
        Register(new BackgroundController());

        World = new WorldController(assets, registry, session, queue);
        Register(World);

        Register(new ParticleController());

        UI = new UIController();
        UI.OnLeaveGame = () => OnLeaveGame?.Invoke();
        UI.OnSettings = () => OnSettings?.Invoke();
        Register(UI);

        Register(new NetworkSystemController());

        // Each controller subscribes to the queues it needs
        foreach (var c in _controllers)
        {
            queue.RegisterHandler(c);
            c.Subscribe(queue);
        }
        queue.Build();

        // Load resources
        foreach (var c in _controllers)
            c.LoadResource(assets.GraphicsDevice, assets.ContentDir);

        Log.Information("[ControllerManager] All controllers configured");
    }

    // ─── Registration ───────────────────────────────────────

    private void Register(IController controller)
    {
        _controllers.Add(controller);
        _sortedForDraw = null;
        Log.Information("[ControllerManager] Registered: {Name} (order={Order})",
            controller.Name, controller.Order);
    }

    public T Get<T>() where T : class, IController
    {
        return _controllers.OfType<T>().FirstOrDefault();
    }

    // ─── Game Loop ──────────────────────────────────────────

    public void ParallelUpdate(GameTime gameTime)
    {
        var active = _controllers.Where(c => c.IsActive).ToArray();
        Parallel.ForEach(active, controller => controller.UpdateLogic(gameTime));
    }

    public void SyncAll()
    {
        foreach (var c in _controllers)
            c.SyncState();
    }

    public void DrawAll(SpriteBatch spriteBatch)
    {
        _sortedForDraw ??= _controllers.OrderBy(c => c.Order).ToArray();

        foreach (var c in _sortedForDraw)
            c.DrawRender(spriteBatch);
    }
}
