// =============================================================================
// ControllerManager.cs — Manages all game controllers
//
// Orchestrates the lifecycle of 5 controllers:
//   Register → Initialize → Load → [Update → Draw] → Shutdown
//
// All controllers are always active. ScreenManager (owned by BackgroundController)
// controls which screens render, not which controllers are active.
// =============================================================================

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using FarmGame.Controllers;

namespace FarmGame.Core;

public class ControllerManager
{
    private readonly List<IController> _controllers = new();

    // ─── Typed Access ───────────────────────────────────────

    public SystemController System { get; private set; }
    public InputController Input { get; private set; }
    public BackgroundController Background { get; private set; }
    public WorldController World { get; private set; }
    public NetworkController Network { get; private set; }

    // ─── Registration ───────────────────────────────────────

    /// <summary>Register a controller. Sets typed property if recognized type.</summary>
    public void Register(IController controller)
    {
        _controllers.Add(controller);

        switch (controller)
        {
            case SystemController s: System = s; break;
            case InputController i: Input = i; break;
            case BackgroundController b: Background = b; break;
            case WorldController w: World = w; break;
            case NetworkController n: Network = n; break;
        }

        Log.Information("[ControllerManager] Registered: {Name} (order={Order})",
            controller.Name, controller.Order);
    }

    // ─── Lifecycle ──────────────────────────────────────────

    /// <summary>Call Initialize() on each controller in Order.</summary>
    public void Initialize()
    {
        foreach (var c in _controllers.OrderBy(c => c.Order))
            c.Initialize();

        Log.Information("[ControllerManager] Initialized {Count} controllers", _controllers.Count);
    }

    /// <summary>Call Load() on each controller in Order, passing this ControllerManager.</summary>
    public void Load()
    {
        foreach (var c in _controllers.OrderBy(c => c.Order))
            c.Load(this);

        Log.Information("[ControllerManager] Loaded {Count} controllers", _controllers.Count);
    }

    /// <summary>Call Shutdown() on each controller in reverse Order.</summary>
    public void Shutdown()
    {
        foreach (var c in _controllers.OrderByDescending(c => c.Order))
            c.Shutdown();

        Log.Information("[ControllerManager] Shutdown {Count} controllers", _controllers.Count);
    }

    // ─── Update: parallel UpdateLogic → queue drain → SyncState ──

    /// <summary>
    /// Phase 1: Parallel UpdateLogic on all controllers.
    /// Phase 2: Drain QueueManager (if SystemController is available).
    /// Phase 3: SyncState on main thread.
    /// </summary>
    public void Update(GameTime gameTime)
    {
        // Phase 1: Parallel update
        Parallel.ForEach(_controllers, c => c.UpdateLogic(gameTime));

        // Phase 2: Queue drain
        System?.Queue?.ProcessAll();

        // Phase 3: Sync state
        foreach (var c in _controllers)
            c.SyncState();
    }

    // ─── Draw: sequential by Order ──────────────────────────

    public void Draw(SpriteBatch spriteBatch)
    {
        foreach (var c in _controllers.OrderBy(c => c.Order))
            c.DrawRender(spriteBatch);
    }
}
