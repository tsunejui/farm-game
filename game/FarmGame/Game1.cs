// =============================================================================
// Game1.cs — Main game entry class
//
// Lifecycle:
//   Game1()      → Create InitManager, QueueManager, ControllerManager.
//   Initialize() → Fluent bootstrap, wire callbacks.
//   LoadContent() → Create assets, controllers, register scenes in ScreenManager.
//   Update()     → InputSystem → ControllerManager.Update (parallel).
//   Draw()       → ControllerManager.Draw (single-thread responsibility chain).
//
// ScreenManager decides which controllers are active per scene.
// No more manual if/else on GameState in Update/Draw.
// =============================================================================

using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Serilog;
using FarmGame.Core;
using FarmGame.Controllers;
using FarmGame.Screens;
using FarmGame.Screens.HUD;
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
    private ScreenManager _screenManager;
    private IAssetService _assets;
    private InputSystem _input;

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

        // Wire controller callbacks for scene transitions
        _controllerManager.OnLeaveGame = () => _screenManager.TransitionTo(GameState.TitleScreen);
        _controllerManager.OnSettings = () => _screenManager.TransitionTo(GameState.Settings);

        base.Initialize();
    }

    // =========================================================================
    // LoadContent
    // =========================================================================
    protected override void LoadContent()
    {
        _assets = new AssetService(GraphicsDevice, Content, _contentDir);
        _input = new InputSystem(_queue);

        // Create screen-based controllers
        var titleScreen = new TitleScreen();
        titleScreen.OnStartGame = StartGame;
        titleScreen.HasSavedState = _init.Session?.HasSavedState ?? false;
        titleScreen.Initialize();

        var settingsScreen = new SettingsScreen();
        settingsScreen.HasSavedState = () => _init.Session?.HasSavedState ?? false;
        settingsScreen.OnLanguageChanged = (lang) =>
        {
            _init.Session?.ChangeLanguage(lang, _contentDir);
        };
        settingsScreen.OnDeleteCharacter = () =>
        {
            _controllerManager.Loading?.Configure(() =>
            {
                _init.Session?.DeleteAndReset();
                titleScreen.HasSavedState = false;
            }, GameState.TitleScreen);
            _screenManager.TransitionTo(GameState.Loading);
        };
        settingsScreen.Initialize();

        var loadingScreen = new LoadingScreen();
        loadingScreen.Initialize();

        var titleCtrl = new TitleController(titleScreen);
        var settingsCtrl = new SettingsController(settingsScreen);
        var loadingCtrl = new LoadingController(loadingScreen);

        // Wire transition callbacks
        void HandleTransition(ScreenTransition t)
        {
            if (t.Exit) { Exit(); return; }
            if (t.Target.HasValue) _screenManager.TransitionTo(t.Target.Value);
        }
        titleCtrl.OnTransition = HandleTransition;
        settingsCtrl.OnTransition = HandleTransition;
        loadingCtrl.OnTransition = HandleTransition;

        // Load data & effects
        _spriteBatch = Bootstrap.GraphicsInitializer.Run(this, _graphics, _contentDir);
        Bootstrap.DataInitializer.Run(_contentDir);
        World.Effects.EffectRegistry.LoadDefinitions(_contentDir, _assets.LoadTexture);

        // Configure all controllers
        var registry = Bootstrap.DataInitializer.GetCachedRegistry() ?? new Data.DataRegistry();
        _controllerManager.ConfigureAll(_assets, registry, _init.Session, _queue,
            titleCtrl, settingsCtrl, loadingCtrl);

        // ScreenManager: register scenes (which controllers per GameState)
        _screenManager = new ScreenManager(_controllerManager);

        _screenManager.RegisterScene(GameState.TitleScreen, () =>
            new IController[] { _controllerManager.Title });

        _screenManager.RegisterScene(GameState.Settings, () =>
            new IController[] { _controllerManager.Settings });

        _screenManager.RegisterScene(GameState.Loading, () =>
            new IController[] { _controllerManager.Loading });

        _screenManager.RegisterScene(GameState.Playing, () =>
            new IController[]
            {
                _controllerManager.Get<BackgroundController>(),
                _controllerManager.World,
                _controllerManager.Get<ParticleController>(),
                _controllerManager.UI,
                _controllerManager.Get<NetworkSystemController>(),
            });

        // Start on title screen
        _screenManager.Initialize(GameState.TitleScreen);

        Log.Information("[Game1] Initialization complete");
    }

    private void StartGame()
    {
        var savedState = _init.Session?.LoadPlayer();
        _controllerManager.World.StartGame(savedState);
        _screenManager.TransitionTo(GameState.Playing);
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
    // Update — InputSystem → ControllerManager.Update (parallel)
    // =========================================================================
    protected override void Update(GameTime gameTime)
    {
        if (_input.Process(gameTime))
        {
            Exit();
            return;
        }

        // Sync input blocking
        bool menuOpen = _controllerManager.UI?.IsMenuOpen ?? false;
        _input.InputBlocked = menuOpen;
        if (_controllerManager.World != null)
            _controllerManager.World.InputBlocked = menuOpen;

        // All active controllers update (parallel) → queue drain → sync
        _controllerManager.Update(gameTime);

        base.Update(gameTime);
    }

    // =========================================================================
    // Draw — ControllerManager.Draw (single-thread responsibility chain)
    // =========================================================================
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _controllerManager.Draw(_spriteBatch);
        base.Draw(gameTime);
    }
}
