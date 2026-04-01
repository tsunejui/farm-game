// =============================================================================
// Game1.cs — Main game entry class (Controller-based architecture)
//
// Lifecycle:
//   Game1()      → Initialize QueueManager, ControllerManager, GraphicsDevice.
//   Initialize() → Core bootstrap (config, database, locale).
//   LoadContent() → Create AssetService, configure controllers, load screens.
//   Update()     → InputSystem.Process → Parallel UpdateLogic → Events → Sync.
//   Draw()       → Responsibility chain: controllers drawn in Order sequence.
//
// Once in Playing state, the world never stops. Menu panels are managed
// by UIController as in-game overlays, not as separate game states.
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

    private GameState _gameState;
    private InitManager _init;

    // =========================================================================
    // Constructor
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
    // Initialize
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
    // LoadContent
    // =========================================================================
    protected override void LoadContent()
    {
        _assets = new AssetService(GraphicsDevice, Content, _contentDir);

        _init.LoadContent(this, _graphics, _contentDir, StartGame, _assets.LoadTexture);
        _spriteBatch = _init.SpriteBatch;

        var registry = DataInitializer.GetCachedRegistry() ?? new Data.DataRegistry();
        _controllerManager.ConfigureAll(_assets, registry, _init.Session, _queue);

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
    // OnExiting
    // =========================================================================
    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        _controllerManager.World?.SaveState();
        _assets?.UnloadAll();
        _queue?.Dispose();
        base.OnExiting(sender, args);
    }

    // =========================================================================
    // Update
    // =========================================================================
    protected override void Update(GameTime gameTime)
    {
        if (_input.Process(gameTime))
        {
            Exit();
            return;
        }

        // Pre-game screens (title, loading) — world not started yet
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

        // In-game — world always runs, no pause state
        _controllerManager.ParallelUpdate(gameTime);
        _queue.ProcessAll();
        _controllerManager.SyncAll();

        base.Update(gameTime);
    }

    // =========================================================================
    // Draw
    // =========================================================================
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (_gameState == GameState.Playing)
            _controllerManager.DrawAll(_spriteBatch);

        // Pre-game screens (title, settings, loading)
        if (_gameState != GameState.Playing &&
            _init.ScreenManager.TryGet(_gameState, out var activeScreen))
            activeScreen.Draw(_spriteBatch);

        base.Draw(gameTime);
    }

    // =========================================================================
    // HandleTransition
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

            if (_init.ScreenManager.TryGet(target, out var nextScreen))
                nextScreen.OnEnter(_gameState);

            _gameState = target;
        }
    }
}
