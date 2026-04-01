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
/// Manages all controllers. Controllers are registered once during init
/// and persist for the game's lifetime. ScreenManager activates/deactivates
/// subsets based on the current scene.
///
/// Update: active controllers run in parallel (Parallel.ForEach) → queue drain → sync.
/// Draw: active controllers drawn in Order sequence (single-thread responsibility chain).
/// </summary>
public class ControllerManager
{
    private readonly Dictionary<string, IController> _allControllers = new();
    private readonly HashSet<string> _activeSet = new();
    private IController[] _activeForUpdate;
    private IController[] _activeForDraw;

    private QueueManager _queue;

    // Named access to key controllers
    public WorldController World { get; private set; }
    public UIController UI { get; private set; }
    public TitleController Title { get; private set; }
    public SettingsController Settings { get; private set; }
    public LoadingController Loading { get; private set; }

    public Action OnLeaveGame { get; set; }
    public Action OnSettings { get; set; }

    // ─── Initialization ─────────────────────────────────────

    /// <summary>
    /// Create and register all controllers. Called once during LoadContent.
    /// All controllers exist for the entire game lifetime.
    /// ScreenManager decides which are active per scene.
    /// </summary>
    public void ConfigureAll(
        IAssetService assets,
        DataRegistry registry,
        GameSession session,
        QueueManager queue,
        TitleController titleController,
        SettingsController settingsController,
        LoadingController loadingController)
    {
        _queue = queue;

        // Scene controllers (title, settings, loading)
        Title = titleController;
        Register(Title);

        Settings = settingsController;
        Register(Settings);

        Loading = loadingController;
        Register(Loading);

        // Playing scene controllers
        Register(new BackgroundController());

        World = new WorldController(assets, registry, session, queue);
        Register(World);

        Register(new ParticleController());

        UI = new UIController();
        UI.OnLeaveGame = () => OnLeaveGame?.Invoke();
        UI.OnSettings = () => OnSettings?.Invoke();
        Register(UI);

        Register(new NetworkSystemController());

        // Register all as MediatR handlers and build queue
        foreach (var c in _allControllers.Values)
        {
            queue.RegisterHandler(c);
            c.Subscribe(queue);
        }
        queue.Build();

        // Load resources for all
        foreach (var c in _allControllers.Values)
            c.LoadResource(assets.GraphicsDevice, assets.ContentDir);

        Log.Information("[ControllerManager] {Count} controllers configured", _allControllers.Count);
    }

    private void Register(IController controller)
    {
        _allControllers[controller.Name] = controller;
        Log.Information("[ControllerManager] Registered: {Name} (order={Order})", controller.Name, controller.Order);
    }

    public T Get<T>() where T : class, IController
    {
        return _allControllers.Values.OfType<T>().FirstOrDefault();
    }

    // ─── Activation (called by ScreenManager) ───────────────

    /// <summary>Deactivate all controllers.</summary>
    public void DeactivateAll()
    {
        _activeSet.Clear();
        InvalidateCache();
    }

    /// <summary>Activate a controller by instance (must already be registered).</summary>
    public void Activate(IController controller)
    {
        _activeSet.Add(controller.Name);
        InvalidateCache();
    }

    /// <summary>Activate a controller by name.</summary>
    public void Activate(string name)
    {
        if (_allControllers.ContainsKey(name))
        {
            _activeSet.Add(name);
            InvalidateCache();
        }
    }

    private void InvalidateCache()
    {
        _activeForUpdate = null;
        _activeForDraw = null;
    }

    private IController[] GetActive()
    {
        return _allControllers
            .Where(kv => _activeSet.Contains(kv.Key))
            .Select(kv => kv.Value)
            .ToArray();
    }

    // ─── Update: multi-threaded → main-thread sync ──────────

    /// <summary>
    /// Phase 1: Parallel update — each active controller on its own thread.
    /// Phase 2: Drain queues on main thread.
    /// Phase 3: Sync LogicState → RenderState on main thread.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        _activeForUpdate ??= GetActive();

        // Phase 1: Parallel
        Parallel.ForEach(_activeForUpdate, c => c.UpdateLogic(gameTime));

        // Phase 2: Queue drain
        _queue?.ProcessAll();

        // Phase 3: Sync
        foreach (var c in _activeForUpdate)
            c.SyncState();
    }

    // ─── Draw: single-thread responsibility chain ───────────

    /// <summary>
    /// Draw active controllers in Order sequence on the main thread.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch)
    {
        _activeForDraw ??= GetActive().OrderBy(c => c.Order).ToArray();

        foreach (var c in _activeForDraw)
            c.DrawRender(spriteBatch);
    }
}
