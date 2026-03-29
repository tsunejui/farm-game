// =============================================================================
// Game1.cs — Main game entry class
// =============================================================================

using System;
using System.Collections.Generic;
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
    private Dictionary<GameState, IScreen> _screens;
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
        settingsScreen.OnLanguageChanged = ChangeLanguage;
        settingsScreen.Initialize();

        _screens = new Dictionary<GameState, IScreen>
        {
            { GameState.TitleScreen, titleScreen },
            { GameState.Settings, settingsScreen },
            { GameState.Paused, pauseScreen },
        };

        _mapTransition = new MapTransitionOverlay();
        _toast = new ToastAlert();

        Log.Information("[Init] Screens initialized");

        // 6. Data Registry
        _registry = DataInitializer.Run(_contentDir);
    }

    private void StartGame()
    {
        var mapId = _savedState?.CurrentMap ?? GameConstants.StartMap;
        var mapDef = _registry.Maps[mapId];
        _currentMap = MapBuilder.Build(mapDef, _registry, LoadTexture);

        var config = mapDef.Config;
        Point playerStart;
        Direction facingDirection;

        if (_savedState != null)
        {
            playerStart = new Point(_savedState.PositionX, _savedState.PositionY);
            Enum.TryParse(_savedState.FacingDirection, out facingDirection);
        }
        else
        {
            playerStart = new Point(config.PlayerStart[0], config.PlayerStart[1]);
            facingDirection = Direction.Down;
        }

        _player = new Player(playerStart, _currentMap, facingDirection);
        _camera = new Camera2D(GraphicsDevice);

        var mapName = LocaleManager.Get("maps", mapId, mapDef.Metadata.DisplayName ?? mapId);
        _mapTransition.Start(mapName);
        _toast.Show(LocaleManager.Format("ui", "entered_map", mapName));

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

    private void ChangeLanguage(string language)
    {
        LocaleManager.Load(_contentDir, language);
        _settings?.Set("language", language);
        Log.Information("Language changed to: {Language}", language);

        foreach (var screen in _screens.Values)
            screen.Rebuild();
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

            // Pre-transition hooks
            if (target == GameState.Paused && _screens.TryGetValue(GameState.Paused, out var pauseScreen))
                ((PauseScreen)pauseScreen).Reset();

            if (target == GameState.Settings && _screens.TryGetValue(GameState.Settings, out var settingsScreen))
                settingsScreen.Rebuild();

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
        else if (_screens.TryGetValue(_gameState, out var screen))
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

        if (_screens.TryGetValue(_gameState, out var screen))
        {
            screen.Draw(_spriteBatch);
        }

        base.Draw(gameTime);
    }
}
