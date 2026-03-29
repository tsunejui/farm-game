// =============================================================================
// Game1.cs — Main game entry class
//
// This is the MonoGame application root. It owns the game loop lifecycle:
//   Initialize → LoadContent → [Update → Draw per frame] → OnExiting
//
// Responsibilities:
//   - Bootstrap the engine via InitManager (config, database, screens)
//   - Drive the screen-based state machine (TitleScreen / Playing / Paused / etc.)
//   - Manage texture loading with an in-memory cache
//   - Persist player state on exit and on screen transitions
//
// The class deliberately avoids knowing concrete screen logic. Screen transitions
// are handled through the IScreen / ScreenTransition abstraction; the only
// exception is PlayingScreen, which is held directly for StartGame / SaveState
// calls (exposed via InitManager.PlayingScreen to avoid unsafe downcasts).
//
// Functions:
//   - Game1()                : Constructor. Creates GraphicsDeviceManager and
//                              sets the Content root directory.
//   - Initialize()           : MonoGame lifecycle. Resolves content path, runs
//                              core bootstrap (config, DB, locale, ScreenManager),
//                              and sets initial state to TitleScreen.
//   - LoadContent()          : MonoGame lifecycle. Delegates to InitManager to
//                              create SpriteBatch, load DataRegistry, build all
//                              screens, and wire callbacks.
//   - StartGame()            : Callback from TitleScreen. Loads saved state (or
//                              null for new game), hands it to PlayingScreen,
//                              and switches state to Playing.
//   - LoadTexture(path)      : Texture loader with Dictionary cache. Tries raw
//                              .png first, falls back to Content Pipeline XNB.
//                              Passed as Func<string,Texture2D> to MapBuilder.
//   - UnloadTextures()       : Disposes all cached FromStream textures. Called
//                              on exit; should also be called on map switch.
//   - OnExiting(sender,args) : MonoGame lifecycle. Persists player state to
//                              SQLite and releases cached GPU resources.
//   - SavePlayerState()      : Null-safe delegate to PlayingScreen.SaveState().
//   - HandleTransition(t)    : Processes ScreenTransition from Update. Calls
//                              OnExit on the current screen, OnEnter on the
//                              target screen, then commits the state change.
//   - Update(gameTime)       : Per-frame logic. Refreshes input, checks gamepad
//                              quit, delegates to active screen, and processes
//                              any returned ScreenTransition. No rendering here.
//   - Draw(gameTime)         : Per-frame render. Clears to black, draws frozen
//                              world behind pause overlay if Paused, then draws
//                              active screen. No state mutations here.
// =============================================================================

using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended.Input;
using FarmGame.Core;
using FarmGame.Screens;

namespace FarmGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private string _contentDir;          // Absolute path to the Content directory
    private GameState _gameState;        // Current screen state (drives Update/Draw routing)
    private InitManager _init;           // Holds all initialization results (screens, session, etc.)

    // Texture cache — avoids reloading the same asset from disk multiple times.
    // Textures loaded via FromStream are NOT managed by ContentManager, so we
    // must track and Dispose them ourselves in UnloadTextures().
    private readonly Dictionary<string, Texture2D> _textureCache = new();

    // =========================================================================
    // Game1() — Constructor
    //
    // Creates the GraphicsDeviceManager (required before Initialize) and sets
    // the content root directory for the MonoGame content pipeline.
    // =========================================================================
    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    // =========================================================================
    // Initialize() — Called once before the first frame
    //
    // Resolves the absolute content directory path, then runs the core
    // initialization sequence (config loading, database setup, locale loading,
    // screen manager creation). Sets the initial game state to TitleScreen.
    // =========================================================================
    protected override void Initialize()
    {
        // Resolve absolute path: {exe directory}/Content
        _contentDir = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory);

        _init = new InitManager();
        _init.InitializeCore(_contentDir);

        // Game always starts on the title screen
        _gameState = GameState.TitleScreen;

        base.Initialize(); // triggers LoadContent()
    }

    // =========================================================================
    // LoadContent() — Called once after Initialize, before the first Update
    //
    // Delegates to InitManager to set up the SpriteBatch, load the DataRegistry
    // (YAML terrain/item/map definitions), create all screen instances, and wire
    // up callbacks (start game, language change, character deletion).
    // =========================================================================
    protected override void LoadContent()
    {
        _init.LoadContent(this, _graphics, _contentDir, StartGame, LoadTexture);
    }

    // =========================================================================
    // StartGame() — Callback invoked by TitleScreen when the player presses Start
    //
    // Loads the saved player state (or null for a new game) and hands it to
    // PlayingScreen, which builds the map, player entity, and camera.
    // Then transitions the state machine to Playing.
    // =========================================================================
    private void StartGame()
    {
        _init.PlayingScreen.StartGame(_init.Session?.LoadPlayer());
        _gameState = GameState.Playing;
    }

    // =========================================================================
    // LoadTexture(path) — Loads a Texture2D with caching
    //
    // Passed as a Func<string, Texture2D> callback to MapBuilder so that the
    // world layer doesn't depend on Game/ContentManager directly.
    //
    // Lookup order:
    //   1. Return from cache if already loaded.
    //   2. Try loading a raw .png file from the Content directory.
    //   3. Fall back to the MonoGame Content Pipeline (XNB).
    //
    // Cached textures must be released via UnloadTextures() since FromStream
    // textures are not tracked by ContentManager.
    // =========================================================================
    private Texture2D LoadTexture(string path)
    {
        // Cache hit — return immediately without disk I/O
        if (_textureCache.TryGetValue(path, out var cached))
            return cached;

        Texture2D texture;
        var pngPath = Path.Combine(_contentDir, path + ".png");

        if (File.Exists(pngPath))
        {
            // Raw PNG path — load directly from file stream
            using var stream = File.OpenRead(pngPath);
            texture = Texture2D.FromStream(GraphicsDevice, stream);
        }
        else
        {
            // Content Pipeline fallback (loads pre-compiled XNB asset)
            texture = Content.Load<Texture2D>(path);
        }

        _textureCache[path] = texture;
        return texture;
    }

    // =========================================================================
    // UnloadTextures() — Disposes all cached textures and clears the cache
    //
    // Called during OnExiting to release GPU resources. Should also be called
    // before a map switch if the old map's textures are no longer needed.
    // =========================================================================
    private void UnloadTextures()
    {
        foreach (var tex in _textureCache.Values)
            tex.Dispose();
        _textureCache.Clear();
    }

    // =========================================================================
    // OnExiting() — Called when the game window is closing
    //
    // Ensures the player's state is persisted to the SQLite database and all
    // manually loaded textures are properly disposed before shutdown.
    // =========================================================================
    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        SavePlayerState();   // Persist player position/direction/map to DB
        UnloadTextures();    // Release GPU resources not managed by ContentManager
        base.OnExiting(sender, args);
    }

    // =========================================================================
    // SavePlayerState() — Delegates save to PlayingScreen → GameSession
    //
    // Null-safe: does nothing if PlayingScreen was never initialized (e.g. the
    // player quit from the title screen without ever starting a game).
    // =========================================================================
    private void SavePlayerState()
    {
        _init.PlayingScreen?.SaveState();
    }

    // =========================================================================
    // HandleTransition(transition) — Processes a screen transition request
    //
    // Called when a screen's Update() returns a non-None ScreenTransition.
    //
    // Flow:
    //   1. If Exit is requested, shut down the game.
    //   2. Otherwise, notify the current screen via OnExit (allows PlayingScreen
    //      to auto-save), notify the target screen via OnEnter, then update
    //      the state machine.
    // =========================================================================
    private void HandleTransition(ScreenTransition transition)
    {
        // Exit request (e.g. "Close Game" on TitleScreen)
        if (transition.Exit)
        {
            Exit();
            return;
        }

        if (transition.Target.HasValue)
        {
            var target = transition.Target.Value;

            // Notify current screen it is being exited (e.g. PlayingScreen saves state)
            if (_init.ScreenManager.TryGet(_gameState, out var currentScreen))
                currentScreen.OnExit(target);

            // Notify target screen it is being entered (e.g. PauseScreen resets selection)
            if (_init.ScreenManager.TryGet(target, out var nextScreen))
                nextScreen.OnEnter(_gameState);

            // Commit state change
            _gameState = target;
        }
    }

    // =========================================================================
    // Update(gameTime) — Per-frame logic tick (60 fps by default)
    //
    // Responsibilities:
    //   1. Refresh input state (MonoGame.Extended keyboard tracker).
    //   2. Check for gamepad Back button (universal quit shortcut).
    //   3. Delegate to the active screen's Update, which returns a
    //      ScreenTransition indicating whether to stay or change state.
    //   4. If a transition is requested, process it via HandleTransition.
    //
    // No rendering occurs here — that is strictly in Draw().
    // =========================================================================
    protected override void Update(GameTime gameTime)
    {
        // Refresh extended keyboard state (tracks WasKeyPressed / IsKeyDown)
        KeyboardExtended.Update();

        // Universal quit via gamepad Back button
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        // Delegate to active screen and handle any requested transition
        if (_init.ScreenManager.TryGet(_gameState, out var screen))
        {
            var transition = screen.Update(gameTime);
            if (transition != ScreenTransition.None)
                HandleTransition(transition);
        }

        base.Update(gameTime);
    }

    // =========================================================================
    // Draw(gameTime) — Per-frame render pass
    //
    // Rendering order:
    //   1. Clear the back buffer to black.
    //   2. If Paused, draw the gameplay world as a frozen background so the
    //      pause menu overlays the game scene (via IWorldRenderer.DrawWorld).
    //   3. Draw the active screen (title menu, pause overlay, playing HUD, etc.)
    //
    // All state mutations happen in Update — Draw is purely presentational.
    // =========================================================================
    protected override void Draw(GameTime gameTime)
    {
        // Clear to black — serves as the default background for all screens
        GraphicsDevice.Clear(Color.Black);

        // When paused, render the gameplay world behind the pause overlay
        if (_gameState == GameState.Paused &&
            _init.ScreenManager.TryGet(GameState.Playing, out var playingScreen))
            (playingScreen as IWorldRenderer)?.DrawWorld(_init.SpriteBatch);

        // Render the active screen (menus, HUD, or gameplay)
        if (_init.ScreenManager.TryGet(_gameState, out var activeScreen))
            activeScreen.Draw(_init.SpriteBatch);

        base.Draw(gameTime);
    }
}
