// =============================================================================
// Game1.cs — Main game entry class
//
// Lifecycle:
//   Game1()      → Create 5 controllers, register to ControllerManager.
//   Initialize() → ControllerManager.Initialize().
//   LoadContent() → Graphics init, ControllerManager.Load().
//   Update()     → ControllerManager.Update().
//   Draw()       → ControllerManager.Draw().
//   OnExiting()  → ControllerManager.Shutdown().
// =============================================================================

using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Serilog;
using FarmGame.Core;
using FarmGame.Controllers;
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
    // LoadContent — Graphics init + ControllerManager.Load()
    // =========================================================================
    protected override void LoadContent()
    {
        // Graphics init (needs Game + GraphicsDeviceManager, must stay in Game1)
        _graphics.PreferredBackBufferWidth = GameConstants.ScreenWidth;
        _graphics.PreferredBackBufferHeight = GameConstants.ScreenHeight;
        _graphics.ApplyChanges();
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        FontManager.Initialize(GraphicsDevice, _contentDir);
        MyraEnvironment.Game = this;

        // WorldController needs GraphicsDevice before Load
        var assets = new AssetService(GraphicsDevice, Content, _contentDir);
        _controllerManager.World.SetGraphicsContext(GraphicsDevice, assets.LoadTexture);

        // Load all controllers (each wires its own dependencies)
        _controllerManager.Load();

        // BackgroundController exit callback (needs Game1.Exit reference, must be after Load)
        _controllerManager.Background.OnExitGame = () => Exit();

        Log.Information("[Game1] Initialization complete");
    }

    // =========================================================================
    // Update — ControllerManager.Update()
    // =========================================================================
    protected override void Update(GameTime gameTime)
    {
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
