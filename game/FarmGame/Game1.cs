// =============================================================================
// Game1.cs — Main game entry class (Controller-based architecture)
//
// Lifecycle:
//   Game1()      → Initialize QueueManager, ControllerManager, GraphicsDevice.
//   Initialize() → Core bootstrap (config, database, locale).
//   LoadContent() → Create AssetService, configure controllers, load screens.
//   Update()     → InputSystem.Process → Parallel UpdateLogic → Events → Sync.
//   Draw()       → Responsibility chain: controllers drawn in Order sequence.
// =============================================================================

using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using FarmGame.Core;
using FarmGame.Queues;
using FarmGame.Bootstrap;
using FarmGame.Screens;
using FarmGame.Services;

namespace FarmGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private string _contentDir;

    private QueueManager _queue;
    private ControllerManager _controllerManager;
    private IAssetService _assets;
    private InputSystem _input;

    // Legacy screen system (title, pause, settings)
    private GameState _gameState;
    private InitManager _init;

    // =========================================================================
    // Constructor — GraphicsDevice + QueueManager + ControllerManager
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
    // Initialize — Core bootstrap (config, database, locale)
    // =========================================================================
    protected override void Initialize()
    {
        _contentDir = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory);

        _init = new InitManager();
        _init.InitializeCore(_contentDir);

        _gameState = GameState.TitleScreen;
        base.Initialize();
    }

    // =========================================================================
    // LoadContent — Create services, configure controllers, load screens
    // =========================================================================
    protected override void LoadContent()
    {
        // Create AssetService (replaces raw LoadTexture in Game1)
        _assets = new AssetService(GraphicsDevice, Content, _contentDir);

        // Legacy screen loading (title, pause, settings)
        _init.LoadContent(this, _graphics, _contentDir, StartGame, _assets.LoadTexture);
        _spriteBatch = _init.SpriteBatch;

        // Configure all controllers in one call
        var registry = DataInitializer.GetCachedRegistry() ?? new Data.DataRegistry();
        _controllerManager.ConfigureAll(_assets, registry, _init.Session, _queue);

        // Create input system
        _input = new InputSystem(_queue);

        Log.Information("[Game1] Initialization complete");
    }

    private void StartGame()
    {
        var savedState = _init.Session?.LoadPlayer();
        _controllerManager.World.StartGame(savedState);
        _gameState = GameState.Playing;
    }

    // =========================================================================
    // OnExiting — Persist state and release resources
    // =========================================================================
    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        _controllerManager.World?.SaveState();
        _assets?.UnloadAll();
        _queue?.Dispose();
        base.OnExiting(sender, args);
    }

    // =========================================================================
    // Update — InputSystem → Parallel Update → Event Processing → SyncState
    // =========================================================================
    protected override void Update(GameTime gameTime)
    {
        // InputSystem reads keyboard/gamepad and publishes semantic events
        if (_input.Process(gameTime))
        {
            Exit();
            return;
        }

        // Legacy screen handling (title, settings, pause)
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

        // Handle pause toggle from InputSystem
        if (_input.PauseToggled)
        {
            _controllerManager.World?.SaveState();
            _gameState = GameState.Paused;
            if (_init.ScreenManager.TryGet(GameState.Paused, out var pausePanel))
                pausePanel.OnEnter(GameState.Playing);
            base.Update(gameTime);
            return;
        }

        // Parallel Update → Event Processing → Sync
        _controllerManager.ParallelUpdate(gameTime);
        _queue.ProcessAll();
        _controllerManager.SyncAll();

        base.Update(gameTime);
    }

    // =========================================================================
    // Draw — Responsibility chain rendering
    // =========================================================================
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (_gameState == GameState.Playing || _gameState == GameState.Paused)
            _controllerManager.DrawAll(_spriteBatch);

        if (_init.ScreenManager.TryGet(_gameState, out var activeScreen))
            activeScreen.Draw(_spriteBatch);

        base.Draw(gameTime);
    }

    // =========================================================================
    // HandleTransition — Process legacy screen transitions
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
                // Resuming from pause
            }
            else if (_init.ScreenManager.TryGet(target, out var nextScreen))
                nextScreen.OnEnter(_gameState);

            _gameState = target;
        }
    }
}
