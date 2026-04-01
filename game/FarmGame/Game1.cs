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
// Note: The game world continues running even when menu panels are open.
//       Panels (pause, settings) are overlays, not full-screen state changes.
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
    //
    // Pre-game screens (title) block the world.
    // In-game panels (pause, settings) are overlays — the world keeps running.
    // =========================================================================
    protected override void Update(GameTime gameTime)
    {
        if (_input.Process(gameTime))
        {
            Exit();
            return;
        }

        // ── Pre-game screens: title, loading (world not yet started) ──
        if (_gameState == GameState.TitleScreen || _gameState == GameState.Loading)
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

        // ── In-game: world always runs, panels are overlays ──

        // Toggle pause overlay
        if (_input.PauseToggled)
        {
            if (_gameState == GameState.Paused)
            {
                _gameState = GameState.Playing;
            }
            else if (_gameState == GameState.Playing)
            {
                _gameState = GameState.Paused;
                if (_init.ScreenManager.TryGet(GameState.Paused, out var panel))
                    panel.OnEnter(GameState.Playing);
            }
        }

        // World update (always runs regardless of panel state)
        _controllerManager.ParallelUpdate(gameTime);
        _queue.ProcessAll();
        _controllerManager.SyncAll();

        // Panel update (if an overlay is open)
        if (_gameState != GameState.Playing &&
            _init.ScreenManager.TryGet(_gameState, out var overlay))
        {
            var transition = overlay.Update(gameTime);
            if (transition != ScreenTransition.None)
                HandleTransition(transition);
        }

        base.Update(gameTime);
    }

    // =========================================================================
    // Draw
    //
    // World is always drawn when in-game. Panels render on top as overlays.
    // =========================================================================
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        bool inGame = _gameState == GameState.Playing
                   || _gameState == GameState.Paused
                   || _gameState == GameState.Settings;

        // Draw world (controller chain) when in-game
        if (inGame)
            _controllerManager.DrawAll(_spriteBatch);

        // Draw active screen/panel on top
        if (_init.ScreenManager.TryGet(_gameState, out var activeScreen))
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
