// =============================================================================
// Game1.cs — Main game entry class (Controller-based architecture)
//
// Architecture: Double-buffered state + Responsibility chain rendering +
//               Event-driven communication via MediatR + Parallel update.
//
// Lifecycle:
//   Game1()      → Initialize MediatR QueueManager, ControllerManager, graphics.
//   Initialize() → Register all controllers, subscribe to event queues.
//   LoadContent() → Load resources via each controller's LoadResource method.
//   Update()     → Input → Parallel UpdateLogic → Event Processing → SyncState.
//   Draw()       → Responsibility chain: controllers drawn in Order sequence.
// =============================================================================

using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using Serilog;
using FarmGame.Core;
using FarmGame.Queues;
using FarmGame.Queues.Events;
using FarmGame.Bootstrap;
using FarmGame.Controllers;
using FarmGame.Data;
using FarmGame.Screens;

namespace FarmGame;

public class Game1 : Game
{
    // ─── Core Systems ───────────────────────────────────────
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private string _contentDir;

    // ─── Architecture ───────────────────────────────────────
    private QueueManager _queue;
    private ControllerManager _controllerManager;

    // ─── Legacy Systems (screen-based, kept for title/pause/settings) ──
    private GameState _gameState;
    private InitManager _init;
    private readonly Dictionary<string, Texture2D> _textureCache = new();

    // ─── Controllers ────────────────────────────────────────
    private WorldController _worldController;
    private UIController _uiController;

    // =========================================================================
    // Game1() — Constructor
    //
    // Initializes QueueManager (MediatR event bus), ControllerManager
    // (responsibility chain), and GraphicsDeviceManager.
    // =========================================================================
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _queue = new QueueManager();
        _controllerManager = new ControllerManager();
    }

    // =========================================================================
    // Initialize() — Register controllers and subscribe to event queues
    //
    // Each controller is instantiated and registered with ControllerManager.
    // Controllers subscribe to specific events via QueueManager.
    // =========================================================================
    protected override void Initialize()
    {
        _contentDir = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory);

        // Core initialization (config, database, locale)
        _init = new InitManager();
        _init.InitializeCore(_contentDir);

        _gameState = GameState.TitleScreen;
        base.Initialize(); // triggers LoadContent()
    }

    // =========================================================================
    // LoadContent() — Load resources via controllers and legacy screens
    //
    // Initializes SpriteBatch, loads DataRegistry, creates controllers,
    // wires up legacy screen system for title/pause/settings.
    // =========================================================================
    protected override void LoadContent()
    {
        // Legacy content loading (screens, fonts, data)
        _init.LoadContent(this, _graphics, _contentDir, StartGame, LoadTexture);
        _spriteBatch = _init.SpriteBatch;

        // Create controllers with dependencies
        var registry = DataInitializer.GetCachedRegistry();

        // BackgroundController (Order: 0)
        var bgController = new BackgroundController();
        _controllerManager.Register(bgController);

        // WorldController (Order: 100)
        _worldController = new WorldController(
            GraphicsDevice, registry ?? new DataRegistry(),
            LoadTexture, _init.Session, _contentDir, _queue);
        _controllerManager.Register(_worldController);

        // ParticleController (Order: 200)
        var particleController = new ParticleController();
        _controllerManager.Register(particleController);

        // UIController (Order: 300)
        _uiController = new UIController();
        _controllerManager.Register(_uiController);

        // NetworkSystemController (Order: 900)
        var networkController = new NetworkSystemController();
        _controllerManager.Register(networkController);

        // Subscribe all controllers to events
        _controllerManager.SubscribeAll(_queue);

        // Load controller resources
        _controllerManager.LoadAllResources(GraphicsDevice, _contentDir);

        Log.Information("[Game1] All controllers registered and resources loaded");
    }

    // =========================================================================
    // StartGame() — Transition from title screen to gameplay
    // =========================================================================
    private void StartGame()
    {
        var savedState = _init.Session?.LoadPlayer();
        _worldController.StartGame(savedState);
        _gameState = GameState.Playing;
    }

    // =========================================================================
    // LoadTexture() — Texture loader with cache
    // =========================================================================
    private Texture2D LoadTexture(string path)
    {
        if (_textureCache.TryGetValue(path, out var cached))
            return cached;

        Texture2D texture;
        var pngPath = Path.Combine(_contentDir, path + ".png");

        if (File.Exists(pngPath))
        {
            using var stream = File.OpenRead(pngPath);
            texture = Texture2D.FromStream(GraphicsDevice, stream);
        }
        else
        {
            texture = Content.Load<Texture2D>(path);
        }

        _textureCache[path] = texture;
        return texture;
    }

    private void UnloadTextures()
    {
        foreach (var tex in _textureCache.Values)
            tex.Dispose();
        _textureCache.Clear();
    }

    // =========================================================================
    // OnExiting() — Persist state and release resources
    // =========================================================================
    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        _worldController?.SaveState();
        UnloadTextures();
        _queue?.Dispose();
        base.OnExiting(sender, args);
    }

    // =========================================================================
    // Update(gameTime) — Input → Parallel Update → Event Processing → Sync
    //
    // 1. Input Handle: capture keyboard and publish InputEvent.
    // 2. Parallel Update: Parallel.ForEach on all active controllers.
    // 3. Event Processing: drain pending events from QueueManager.
    // 4. Sync Point: copy LogicState → RenderState on main thread.
    // =========================================================================
    protected override void Update(GameTime gameTime)
    {
        KeyboardExtended.Update();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        // ── Legacy screen handling (title, settings, pause) ──
        if (_gameState != GameState.Playing)
        {
            if (_init.ScreenManager.TryGet(_gameState, out var screen))
            {
                var transition = screen.Update(gameTime);
                if (transition != ScreenTransition.None)
                    HandleTransition(transition);
            }
            base.Update(gameTime);
            return;
        }

        // ── Controller-based update (Playing state) ──

        // 1. Input Handle: publish keyboard state as event
        var keyboard = KeyboardExtended.GetState();
        _queue.Publish(new InputEvent(Keyboard.GetState(), gameTime));

        // Check pause
        if (keyboard.WasKeyPressed(Keys.Escape))
        {
            _worldController.SaveState();
            _gameState = GameState.Paused;
            if (_init.ScreenManager.TryGet(GameState.Paused, out var pauseScreen))
                pauseScreen.OnEnter(GameState.Playing);
            base.Update(gameTime);
            return;
        }

        // 2. Parallel Update: all controllers run UpdateLogic concurrently
        _controllerManager.ParallelUpdate(gameTime);

        // 3. Event Processing: process accumulated events on main thread
        _queue.ProcessPendingEvents();

        // 4. Sync Point: LogicState → RenderState (main thread)
        _controllerManager.SyncAll();

        base.Update(gameTime);
    }

    // =========================================================================
    // Draw(gameTime) — Responsibility chain rendering
    //
    // Clear screen, then draw controllers in Order sequence.
    // Legacy screens (pause overlay) drawn on top when applicable.
    // =========================================================================
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (_gameState == GameState.Playing)
        {
            // Responsibility chain: controllers drawn in Order sequence
            _controllerManager.DrawAll(_spriteBatch);
        }

        // Legacy screens (title, pause overlay, settings)
        if (_gameState == GameState.Paused)
        {
            // Draw frozen world behind pause overlay
            _controllerManager.DrawAll(_spriteBatch);
        }

        if (_init.ScreenManager.TryGet(_gameState, out var activeScreen))
            activeScreen.Draw(_spriteBatch);

        base.Draw(gameTime);
    }

    // =========================================================================
    // HandleTransition() — Process legacy screen transitions
    // =========================================================================
    private void HandleTransition(ScreenTransition transition)
    {
        if (transition.Exit)
        {
            Exit();
            return;
        }

        if (transition.Target.HasValue)
        {
            var target = transition.Target.Value;

            if (_init.ScreenManager.TryGet(_gameState, out var currentScreen))
                currentScreen.OnExit(target);

            if (target == GameState.Playing && _gameState == GameState.Paused)
            {
                // Resuming from pause — no need to rebuild
            }
            else if (_init.ScreenManager.TryGet(target, out var nextScreen))
                nextScreen.OnEnter(_gameState);

            _gameState = target;
        }
    }
}
