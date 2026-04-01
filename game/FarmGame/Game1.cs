// =============================================================================
// Game1.cs — Main game entry class
//
// Lifecycle:
//   Game1()      → Create InitManager, QueueManager, ControllerManager.
//   Initialize() → Fluent bootstrap: Config → Database → Locale → Controllers.
//   LoadContent() → Assets, screens, controller configuration.
//   Update()     → InputSystem → ControllerManager.Update (parallel threads).
//   Draw()       → ControllerManager.Draw (single-thread responsibility chain).
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
    // Constructor
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
    // Initialize
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
    // LoadContent
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
    // Update — Input → ControllerManager.Update (parallel threads + sync)
    // =========================================================================
    protected override void Update(GameTime gameTime)
    {
        if (_input.Process(gameTime))
        {
            Exit();
            return;
        }

        // Pre-game screens (title, loading)
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

        // Sync input blocking — menu open = all game input suppressed
        bool menuOpen = _controllerManager.UI?.IsMenuOpen ?? false;
        _input.InputBlocked = menuOpen;
        if (_controllerManager.World != null)
            _controllerManager.World.InputBlocked = menuOpen;

        // Controllers update: parallel threads → queue drain → state sync
        _controllerManager.Update(gameTime);

        base.Update(gameTime);
    }

    // =========================================================================
    // Draw — ControllerManager.Draw (single-thread responsibility chain)
    // =========================================================================
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (_gameState == GameState.Playing)
            _controllerManager.Draw(_spriteBatch);

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
