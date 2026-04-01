using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using FarmGame.Controllers;
using FarmGame.Data;
using FarmGame.Queues;
using FarmGame.Services;

namespace FarmGame.Core;

/// <summary>
/// Manages all controllers: creation, registration, parallel update, ordered draw.
/// </summary>
public class ControllerManager
{
    private readonly List<IController> _controllers = new();
    private IController[] _sortedForDraw;

    // Exposed for Game1 to reference specific controllers
    public WorldController World { get; private set; }
    public UIController UI { get; private set; }

    /// <summary>
    /// Create and register all controllers with their dependencies.
    /// Encapsulates the full controller wiring — Game1 just calls this once.
    /// </summary>
    /// <summary>Callback when player selects "Leave Game" from in-game menu.</summary>
    public Action OnLeaveGame { get; set; }

    /// <summary>Callback when player selects "Settings" from in-game menu.</summary>
    public Action OnSettings { get; set; }

    public void ConfigureAll(
        IAssetService assets,
        DataRegistry registry,
        GameSession session,
        QueueManager queue)
    {
        Register(new BackgroundController());

        World = new WorldController(assets, registry, session, queue);
        Register(World);

        Register(new ParticleController());

        UI = new UIController();
        UI.OnLeaveGame = () => OnLeaveGame?.Invoke();
        UI.OnSettings = () => OnSettings?.Invoke();
        Register(UI);

        Register(new NetworkSystemController());

        // Wire MediatR handlers and build the event bus
        SubscribeAll(queue);

        // Load resources for all controllers
        LoadAllResources(assets.GraphicsDevice, assets.ContentDir);

        Log.Information("[ControllerManager] All controllers configured");
    }

    public void Register(IController controller)
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

    private void SubscribeAll(QueueManager queue)
    {
        foreach (var c in _controllers)
        {
            queue.RegisterHandler(c);
            c.Subscribe(queue);
        }
        queue.Build();
    }

    private void LoadAllResources(GraphicsDevice graphicsDevice, string contentDir)
    {
        foreach (var c in _controllers)
            c.LoadResource(graphicsDevice, contentDir);
    }

    /// <summary>Parallel update: all active controllers run UpdateLogic concurrently.</summary>
    public void ParallelUpdate(GameTime gameTime)
    {
        var active = _controllers.Where(c => c.IsActive).ToArray();
        Parallel.ForEach(active, controller => controller.UpdateLogic(gameTime));
    }

    /// <summary>Sync point: copy LogicState → RenderState on main thread.</summary>
    public void SyncAll()
    {
        foreach (var c in _controllers)
            c.SyncState();
    }

    /// <summary>Responsibility chain draw: render controllers in Order sequence.</summary>
    public void DrawAll(SpriteBatch spriteBatch)
    {
        _sortedForDraw ??= _controllers.OrderBy(c => c.Order).ToArray();

        foreach (var c in _sortedForDraw)
            c.DrawRender(spriteBatch);
    }
}
