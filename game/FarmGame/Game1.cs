// =============================================================================
// Game1.cs — Main game entry class
// =============================================================================

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Serilog;
using MonoGame.Extended.Input;
using FarmGame.Bootstrap;
using FarmGame.Camera;
using FarmGame.Core;
using FarmGame.Data;
using FarmGame.Entities;
using FarmGame.Persistence;
using FarmGame.Persistence.Models;
using FarmGame.Persistence.Repositories;
using FarmGame.Screens;
using FarmGame.Screens.HUD;
using FarmGame.World;

namespace FarmGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private GameState _gameState;
    private string _contentDir;

    // Screens
    private ScreenManager _screenManager;
    private MapTransitionOverlay _mapTransition;
    private ToastAlert _toast;

    // Data
    private DataRegistry _registry;
    private DatabaseBootstrapper _database;
    private string _databaseError;
    private string _playerUuid;
    private SettingRepository _settings;
    private PlayerStateRepository _playerStateRepo;
    private PlayerState _savedState;

    // Gameplay
    private GameMap _currentMap;
    private Player _player;
    private Camera2D _camera;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        // 1. Config
        _contentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory);
        ConfigInitializer.Run(_contentDir);

        // 2. Database
        var dbResult = DatabaseInitializer.Run();
        if (dbResult.Success)
        {
            _database = dbResult.Database;
            _settings = dbResult.Settings;
            _playerStateRepo = dbResult.PlayerStateRepo;
            _playerUuid = dbResult.PlayerUuid;
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

        // 5. Screens
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
        _screenManager.Register(GameState.TitleScreen, titleScreen);
        _screenManager.Register(GameState.Settings, settingsScreen);
        _screenManager.Register(GameState.Paused, pauseScreen);

        _mapTransition = new MapTransitionOverlay();
        _toast = new ToastAlert();

        Log.Information("[Init] Screens initialized");

        // 6. Data Registry
        _registry = DataInitializer.Run(_contentDir);
    }

    private void StartGame()
    {
        var result = GameplayInitializer.Run(_savedState, _registry, LoadTexture, GraphicsDevice);
        _currentMap = result.Map;
        _player = result.Player;
        _camera = result.Camera;

        _mapTransition.Start(result.MapName);
        _toast.Show(LocaleManager.Format("ui", "entered_map", result.MapName));

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
        if (_database == null || _playerStateRepo == null || _player == null)
            return;

        var state = new PlayerState
        {
            Uuid = _playerUuid,
            PositionX = _player.GridPosition.X,
            PositionY = _player.GridPosition.Y,
            FacingDirection = _player.FacingDirection.ToString(),
            CurrentMap = GameConstants.StartMap,
        };

        var result = _playerStateRepo.Save(_playerUuid, state, GameConstants.GameTitle);
        if (result.Success)
            Log.Information("Player state saved: pos=({X},{Y}), dir={Dir}",
                state.PositionX, state.PositionY, state.FacingDirection);
        else
            Log.Error("Failed to save player state: {Error}", result.ErrorMessage);
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

        if (_gameState == GameState.Playing)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _mapTransition.Update(dt);
            _toast.Update(dt);
            var keyboard = KeyboardExtended.GetState();
            if (keyboard.WasKeyPressed(Keys.Escape))
            {
                HandleTransition(ScreenTransition.To(GameState.Paused));
            }
            else
            {
                _player.Update(gameTime);
                _camera.Update(_player, _currentMap);
            }
        }
        else if (_screenManager.TryGet(_gameState, out var screen))
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

        if (_gameState == GameState.Playing || _gameState == GameState.Paused)
        {
            _spriteBatch.Begin(
                transformMatrix: _camera.TransformMatrix,
                samplerState: SamplerState.PointClamp);
            _currentMap.Draw(_spriteBatch, _camera);
            _player.Draw(_spriteBatch);
            _spriteBatch.End();
        }

        if (_gameState == GameState.Playing)
        {
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _toast.Draw(_spriteBatch);
            if (_mapTransition.IsActive)
                _mapTransition.Draw(_spriteBatch);
            _spriteBatch.End();
        }

        if (_screenManager.TryGet(_gameState, out var activeScreen))
        {
            activeScreen.Draw(_spriteBatch);
        }

        base.Draw(gameTime);
    }
}
