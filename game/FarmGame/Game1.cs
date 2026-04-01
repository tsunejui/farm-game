// =============================================================================
// Game1.cs — Main game entry class
//
// Lifecycle:
//   Game1()      → Create InitManager, QueueManager, ControllerManager.
//   Initialize() → Fluent bootstrap chain: Config → Database → Locale → Controllers.
//   LoadContent() → Load assets, screens, configure controllers.
//   Update()     → InputSystem → Parallel Update → ProcessQueues → Sync.
//   Draw()       → Responsibility chain rendering.
// =============================================================================

using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using FarmGame.Core;
using FarmGame.Screens;
using FarmGame.Services;

namespace FarmGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private string _contentDir;

    private readonly InitManager _init;
    private readonly QueueManager _queue;
    private readonly ControllerManager _controllerManager;
    private IAssetService _assets;
    private InputSystem _input;

    private GameState _gameState;

    // =========================================================================
    // Constructor — Create all managers
    // =========================================================================
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _init = new InitManager();
        _queue = new QueueManager();
        _controllerManager = new ControllerManager();
    }

    // =========================================================================
    // Initialize — Fluent bootstrap chain
    // =========================================================================
    protected override void Initialize()
    {
        _contentDir = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory);

        _init
            .WithConfig(_contentDir)
            .WithDatabase()
            .WithLocale()
            .WithControllers(_controllerManager)
            .Bootstrap();

        // Wire controller callbacks (screen transitions triggered from in-game menu)
        _controllerManager.OnLeaveGame = () =>
        {
            _controllerManager.World?.SaveState();
            _gameState = GameState.TitleScreen;
            if (_init.ScreenManager.TryGet(GameState.TitleScreen, out var title))
                title.OnEnter(GameState.Playing);
        };
        _controllerManager.OnSettings = () =>
        {
            _gameState = GameState.Settings;
            if (_init.ScreenManager.TryGet(GameState.Settings, out var settings))
                settings.OnEnter(GameState.Playing);
        };

        _gameState = GameState.TitleScreen;
        base.Initialize();
    }

    // =========================================================================
    // LoadContent — Assets, screens, controllers
    // =========================================================================
    protected override void LoadContent()
    {
        _assets = new AssetService(GraphicsDevice, Content, _contentDir);
        _input = new InputSystem(_queue);

        _init.LoadContent(this, _graphics, _contentDir, StartGame, _assets, _queue);
        _spriteBatch = _init.SpriteBatch;

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

        // Pre-game screens (title, loading) — world not started
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

        // In-game — world always runs
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
