// =============================================================================
// Game1.cs — Main game entry class
// =============================================================================

using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;
using MonoGame.Extended.Input;
using FarmGame.Bootstrap;
using FarmGame.Core;
using FarmGame.Data;
using FarmGame.Persistence;
using FarmGame.Persistence.Models;
using FarmGame.Persistence.Repositories;
using FarmGame.Screens;

namespace FarmGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private GameState _gameState;
    private string _contentDir;

    // Screens
    private ScreenManager _screenManager;
    private PlayingScreen _playingScreen;

    // Data
    private DataRegistry _registry;
    private string _databaseError;
    private SettingRepository _settings;
    private PlayerStateSaver _stateSaver;
    private PlayerState _savedState;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // 1. Config
        _contentDir = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory);
        ConfigInitializer.Run(_contentDir);

        // 2. Database
        var dbResult = DatabaseInitializer.Run();
        if (dbResult.Success)
        {
            _settings = dbResult.Settings;
            _stateSaver = new PlayerStateSaver(dbResult.PlayerStateRepo, dbResult.PlayerUuid);
            _savedState = dbResult.SavedState;
        }
        else
        {
            _databaseError = dbResult.Error;
        }

        // 3. Locale
        LocaleInitializer.Run(_contentDir, _settings);

        _gameState = GameState.TitleScreen;
        base.Initialize();
    }

    protected override void LoadContent()
    {
        // 4. Graphics + Font + Myra
        _spriteBatch = GraphicsInitializer.Run(this, _graphics, _contentDir);

        // 5. Data Registry
        _registry = DataInitializer.Run(_contentDir);

        // 6. Screens
        var titleScreen = new TitleScreen();
        titleScreen.OnStartGame = StartGame;
        titleScreen.Initialize();
        if (!string.IsNullOrEmpty(_databaseError))
            titleScreen.SetError(_databaseError);

        var pauseScreen = new PauseScreen();
        pauseScreen.Initialize();

        var settingsScreen = new SettingsScreen();
        _screenManager = new ScreenManager(_contentDir, _settings);
        settingsScreen.OnLanguageChanged = _screenManager.ChangeLanguage;
        settingsScreen.Initialize();

        _playingScreen = new PlayingScreen(GraphicsDevice, _registry, LoadTexture);

        _screenManager.Register(GameState.TitleScreen, titleScreen);
        _screenManager.Register(GameState.Settings, settingsScreen);
        _screenManager.Register(GameState.Paused, pauseScreen);
        _screenManager.Register(GameState.Playing, _playingScreen);

        Log.Information("[Init] Screens initialized");
    }

    private void StartGame()
    {
        _playingScreen.StartGame(_savedState);
        _gameState = GameState.Playing;
    }

    private Texture2D LoadTexture(string path)
    {
        var pngPath = Path.Combine(_contentDir, path + ".png");
        if (File.Exists(pngPath))
        {
            using var stream = File.OpenRead(pngPath);
            return Texture2D.FromStream(GraphicsDevice, stream);
        }
        return Content.Load<Texture2D>(path);
    }

    protected override void OnExiting(object sender, ExitingEventArgs args)
    {
        SavePlayerState();
        base.OnExiting(sender, args);
    }

    private void SavePlayerState()
    {
        _stateSaver?.Save(_playingScreen?.Player, _playingScreen?.CurrentMap?.MapId ?? GameConstants.StartMap);
    }

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

            if (target == GameState.TitleScreen && _gameState == GameState.Paused)
                SavePlayerState();

            if (_screenManager.TryGet(target, out var screen))
                screen.OnEnter(_gameState);

            _gameState = target;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardExtended.Update();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        if (_screenManager.TryGet(_gameState, out var screen))
        {
            var transition = screen.Update(gameTime);
            if (transition != ScreenTransition.None)
                HandleTransition(transition);
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        if (_gameState == GameState.Paused)
            _playingScreen.DrawWorld(_spriteBatch);

        if (_screenManager.TryGet(_gameState, out var activeScreen))
            activeScreen.Draw(_spriteBatch);

        base.Draw(gameTime);
    }
}
