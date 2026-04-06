// =============================================================================
// Game1.cs — Main game entry class
//
// Lifecycle:
//   Game1()      → Create 5 controllers, register to ControllerManager.
//   Initialize() → ControllerManager.Initialize() (each controller creates managers).
//   LoadContent() → Graphics init, ControllerManager.Load(), wire screens, build queue.
//   Update()     → ControllerManager.Update() (parallel + queue drain + sync).
//   Draw()       → ControllerManager.Draw() (sequential by Order).
//   OnExiting()  → ControllerManager.Shutdown().
// =============================================================================

using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
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

    private readonly ControllerManager _controllerManager;

    // =========================================================================
    // Constructor — Create and register 5 controllers
    // =========================================================================
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _controllerManager = new ControllerManager();
        _controllerManager.Register(new SystemController());
        _controllerManager.Register(new InputController());
        _controllerManager.Register(new BackgroundController());
        _controllerManager.Register(new WorldController());
        _controllerManager.Register(new NetworkController());
    }

    // =========================================================================
    // Initialize — ControllerManager.Initialize()
    // =========================================================================
    protected override void Initialize()
    {
        _contentDir = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory);

        // SystemController needs content dir before Initialize
        _controllerManager.System.ContentDir = _contentDir;

        // Each controller creates its sub-managers
        _controllerManager.Initialize();

        base.Initialize();
    }

    // =========================================================================
    // LoadContent — Graphics init, ControllerManager.Load(), wire screens
    // =========================================================================
    protected override void LoadContent()
    {
        // Graphics init (needs Game + GraphicsDeviceManager, stays in Game1)
        _graphics.PreferredBackBufferWidth = GameConstants.ScreenWidth;
        _graphics.PreferredBackBufferHeight = GameConstants.ScreenHeight;
        _graphics.ApplyChanges();
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        FontManager.Initialize(GraphicsDevice, _contentDir);
        MyraEnvironment.Game = this;

        // Create asset service
        var assets = new AssetService(GraphicsDevice, Content, _contentDir);

        // Load effect definitions
        World.Effects.EffectRegistry.LoadDefinitions(_contentDir, assets.LoadTexture);

        // Load controllers (each loads its resources)
        _controllerManager.Load(_controllerManager.System.Config);

        // Wire InputController to QueueManager
        _controllerManager.Input.SetQueueManager(_controllerManager.System.Queue);

        // Wire WorldController dependencies
        var registry = _controllerManager.System.Config.ToDataRegistry();
        _controllerManager.World.Configure(
            assets, registry,
            _controllerManager.System.Session,
            _controllerManager.System.Queue,
            _controllerManager.Background.Screen);

        _controllerManager.World.OnLeaveGame = () =>
            _controllerManager.Background.TransitionTo(GameState.TitleScreen);
        _controllerManager.World.OnSettings = () =>
            _controllerManager.Background.TransitionTo(GameState.Settings);

        // BackgroundController exit callback
        _controllerManager.Background.OnExitGame = () => Exit();
        _controllerManager.Background.OnStartGame = () =>
        {
            var savedState = _controllerManager.System.Session?.LoadPlayer();
            _controllerManager.World.StartGame(savedState);
            _controllerManager.Background.TransitionTo(GameState.Playing);
        };

        // Create and register screens
        var titleScreen = new TitleScreen();
        titleScreen.OnStartGame = () => _controllerManager.Background.OnStartGame?.Invoke();
        titleScreen.HasSavedState = _controllerManager.System.Session?.HasSavedState ?? false;
        titleScreen.Initialize();

        var settingsScreen = new SettingsScreen();
        settingsScreen.HasSavedState = () => _controllerManager.System.Session?.HasSavedState ?? false;
        settingsScreen.OnLanguageChanged = (lang) =>
        {
            _controllerManager.System.Session?.ChangeLanguage(lang, _contentDir);
        };
        settingsScreen.OnDeleteCharacter = () =>
        {
            _controllerManager.System.Session?.DeleteAndReset();
            titleScreen.HasSavedState = false;
            _controllerManager.Background.TransitionTo(GameState.TitleScreen);
        };
        settingsScreen.Initialize();

        var loadingScreen = new LoadingScreen();
        loadingScreen.Initialize();

        _controllerManager.Background.RegisterScreen(GameState.TitleScreen, titleScreen);
        _controllerManager.Background.RegisterScreen(GameState.Settings, settingsScreen);
        _controllerManager.Background.RegisterScreen(GameState.Loading, loadingScreen);

        // Register MediatR handlers and build queue
        var queue = _controllerManager.System.Queue;
        queue.RegisterHandler(_controllerManager.World);
        queue.RegisterHandler(_controllerManager.Network);
        queue.Build();

        // Start on title screen
        _controllerManager.Background.InitializeScreen(GameState.TitleScreen);

        Log.Information("[Game1] Initialization complete");
    }

    // =========================================================================
    // Update — ControllerManager.Update()
    // =========================================================================
    protected override void Update(GameTime gameTime)
    {
        // Check exit request from InputController
        if (_controllerManager.Input.ExitRequested)
        {
            Exit();
            return;
        }

        // Sync input blocking (menu open → block game input)
        bool menuOpen = _controllerManager.World?.IsMenuOpen ?? false;
        _controllerManager.Input.InputBlocked = menuOpen;
        if (_controllerManager.World != null)
            _controllerManager.World.InputBlocked = menuOpen;

        _controllerManager.Update(gameTime);

        base.Update(gameTime);
    }

    // =========================================================================
    // Draw — ControllerManager.Draw()
    // =========================================================================
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _controllerManager.Draw(_spriteBatch);
        base.Draw(gameTime);
    }

    // =========================================================================
    // OnExiting — ControllerManager.Shutdown()
    // =========================================================================
    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        _controllerManager.Shutdown();
        base.OnExiting(sender, args);
    }
}
