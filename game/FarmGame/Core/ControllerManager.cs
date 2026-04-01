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
/// Manages all controllers: creation, registration, game loop orchestration.
///
/// Update pipeline (called from Game1.Update):
///   Phase 1: Parallel UpdateLogic — each controller on its own thread
///   Phase 2: ProcessQueues — drain all queues on main thread
///   Phase 3: SyncState — copy LogicState → RenderState on main thread
///
/// Draw pipeline (called from Game1.Draw):
///   Single-thread responsibility chain — controllers drawn in Order sequence
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

    public void ConfigureAll(
        IAssetService assets,
        DataRegistry registry,
        GameSession session,
        QueueManager queue)
    {
        _queue = queue;

        Register(new BackgroundController());

        World = new WorldController(assets, registry, session, queue);
        Register(World);

        Register(new ParticleController());

        UI = new UIController();
        UI.OnLeaveGame = () => OnLeaveGame?.Invoke();
        UI.OnSettings = () => OnSettings?.Invoke();
        Register(UI);

        Register(new NetworkSystemController());

        foreach (var c in _controllers)
        {
            queue.RegisterHandler(c);
            c.Subscribe(queue);
        }
        queue.Build();

        foreach (var c in _controllers)
            c.LoadResource(assets.GraphicsDevice, assets.ContentDir);

        Log.Information("[ControllerManager] All controllers configured");
    }

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

    // ─── Update: multi-threaded logic → main-thread sync ────

    /// <summary>
    /// Full update pipeline. Called once per frame from Game1.Update.
    ///
    /// Phase 1: Each controller's UpdateLogic runs in parallel (Parallel.ForEach).
    ///          Controllers write to their own LogicState only — no shared mutation.
    /// Phase 2: Drain all queues via MediatR on the main thread.
    /// Phase 3: Copy each controller's LogicState → RenderState on the main thread.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        // Phase 1: Parallel update — each controller on its own thread
        var active = _controllers.Where(c => c.IsActive).ToArray();
        Parallel.ForEach(active, controller => controller.UpdateLogic(gameTime));

        // Phase 2: Process queues — main thread (Commands first, then Events)
        _queue.ProcessAll();

        // Phase 3: Sync — copy LogicState → RenderState on main thread
        foreach (var c in _controllers)
            c.SyncState();
    }

    // ─── Draw: single-thread responsibility chain ───────────

    /// <summary>
    /// Draw all controllers in Order sequence on the main thread.
    /// Lower Order = drawn first (behind). Higher Order = drawn on top.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        _sortedForDraw ??= _controllers.OrderBy(c => c.Order).ToArray();

        foreach (var c in _sortedForDraw)
            c.DrawRender(spriteBatch);
    }
}
