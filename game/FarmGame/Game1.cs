// =============================================================================
// Game1.cs — Main game entry class
//
// Inherits from MonoGame's Game class and manages the entire game lifecycle,
// including initialization, content loading, the game state machine
// (TitleScreen → Playing → Paused), and per-frame update and render logic.
//
// Functions:
//   - Game1()        : Constructor. Initializes GraphicsDeviceManager and sets Content root directory.
//   - Initialize()   : Loads game config from YAML, sets window size and initial state.
//                       see: https://docs.monogame.net/api/Microsoft.Xna.Framework.Game.html#Microsoft_Xna_Framework_Game_Initialize
//   - LoadContent()  : Creates SpriteBatch, 1x1 white pixel texture, font, and loads all data via DataRegistry.
//                       see: https://docs.monogame.net/api/Microsoft.Xna.Framework.Game.html#Microsoft_Xna_Framework_Game_LoadContent
//   - StartGame()    : Builds the map, player, and camera from DataRegistry, then transitions to Playing state.
//   - Update()       : Per-frame update. Handles menu actions, player movement, and pause toggle based on game state.
//                       see: https://docs.monogame.net/api/Microsoft.Xna.Framework.Game.html#Microsoft_Xna_Framework_Game_Update_Microsoft_Xna_Framework_GameTime_
//   - Draw()         : Per-frame render. Draws title screen, game scene, or pause overlay based on game state.
//                       see: https://docs.monogame.net/api/Microsoft.Xna.Framework.Game.html#Microsoft_Xna_Framework_Game_Draw_Microsoft_Xna_Framework_GameTime_
// =============================================================================

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FarmGame.Camera;
using FarmGame.Core;
using FarmGame.Data;
using FarmGame.Entities;
using FarmGame.Persistence;
using FarmGame.Persistence.Models;
using FarmGame.Persistence.Repositories;
using FarmGame.Screens;
using FarmGame.World;

namespace FarmGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;
    private SpriteFont _font;
    private GameState _gameState;

    // Screens
    private TitleScreen _titleScreen;
    private PauseScreen _pauseScreen;
    private KeyboardState _previousKeyboard;

    // Data
    private DataRegistry _registry;
    private DatabaseBootstrapper _database;
    private string _databaseError;
    private string _playerUuid;
    private SettingRepository _settings;
    private PlayerStateRepository _playerStateRepo;

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
        // Load config from YAML before initializing graphics
        var contentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory);
        var config = GameConfig.Load(Path.Combine(contentDir, "config.yaml"));
        GameConstants.LoadFrom(config);

        _graphics.PreferredBackBufferWidth = GameConstants.ScreenWidth;
        _graphics.PreferredBackBufferHeight = GameConstants.ScreenHeight;
        _graphics.ApplyChanges();

        // Initialize database
        InitializeDatabase();

        _gameState = GameState.TitleScreen;

        base.Initialize();
    }

    private void InitializeDatabase()
    {
        var dbDir = DatabasePathResolver.GetDatabaseDirectory(GameConstants.GameTitle);
        var dirResult = DatabasePathResolver.EnsureDirectoryExists(dbDir);
        if (!dirResult.Success)
        {
            _databaseError = dirResult.ErrorMessage;
            return;
        }

        var permResult = DatabasePathResolver.CheckWritePermission(dbDir);
        if (!permResult.Success)
        {
            _databaseError = permResult.ErrorMessage;
            return;
        }

        var dbPath = DatabasePathResolver.GetDatabasePath(GameConstants.GameTitle);
        _database = new DatabaseBootstrapper(dbPath);
        var initResult = _database.Initialize();
        if (!initResult.Success)
        {
            _databaseError = initResult.ErrorMessage;
            return;
        }

        _settings = new SettingRepository(_database);
        _playerStateRepo = new PlayerStateRepository(_database);

        // Create or load player UUID
        _playerUuid = _settings.Get("player_uuid");
        if (string.IsNullOrEmpty(_playerUuid))
        {
            _playerUuid = Guid.NewGuid().ToString();
            _settings.Set("player_uuid", _playerUuid);
        }
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _font = Content.Load<SpriteFont>("DefaultFont");

        _titleScreen = new TitleScreen();
        _pauseScreen = new PauseScreen();

        // Load all data at startup
        var contentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory);
        _registry = DataRegistry.LoadAll(contentDir);
    }

    private void StartGame()
    {
        // Backup database before starting gameplay
        if (_database != null)
        {
            var dbPath = DatabasePathResolver.GetDatabasePath(GameConstants.GameTitle);
            DatabaseBackup.Backup(dbPath);
        }

        // Load saved state if exists
        PlayerState savedState = null;
        if (_playerStateRepo != null && _playerUuid != null)
        {
            var loadResult = _playerStateRepo.Load(_playerUuid);
            if (loadResult.Success)
                savedState = loadResult.Value;
        }

        var mapId = savedState?.CurrentMap ?? GameConstants.StartMap;
        var mapDef = _registry.Maps[mapId];
        _currentMap = MapBuilder.Build(mapDef, _registry, Content.Load<Texture2D>);

        var config = mapDef.Config;
        Point playerStart;
        Direction facingDirection;

        if (savedState != null)
        {
            playerStart = new Point(savedState.PositionX, savedState.PositionY);
            Enum.TryParse(savedState.FacingDirection, out facingDirection);
        }
        else
        {
            playerStart = new Point(config.PlayerStart[0], config.PlayerStart[1]);
            facingDirection = Direction.Down;
        }

        _player = new Player(playerStart, _currentMap, facingDirection);
        _camera = new Camera2D(GraphicsDevice.Viewport);
        _gameState = GameState.Playing;
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

        _playerStateRepo.Save(_playerUuid, state, GameConstants.GameTitle);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        switch (_gameState)
        {
            case GameState.TitleScreen:
                _titleScreen.Update(gameTime);
                if (_titleScreen.SelectedAction == TitleMenuOption.StartGame)
                    StartGame();
                else if (_titleScreen.SelectedAction == TitleMenuOption.ExitGame)
                    Exit();
                break;

            case GameState.Playing:
                var keyboard = Keyboard.GetState();
                if (keyboard.IsKeyDown(Keys.Escape) && _previousKeyboard.IsKeyUp(Keys.Escape))
                {
                    _pauseScreen.Reset();
                    _gameState = GameState.Paused;
                }
                else
                {
                    _player.Update(gameTime);
                    _camera.Update(_player, _currentMap);
                }
                _previousKeyboard = keyboard;
                break;

            case GameState.Paused:
                _pauseScreen.Update(gameTime);
                if (_pauseScreen.SelectedAction == PauseMenuOption.Resume)
                    _gameState = GameState.Playing;
                else if (_pauseScreen.SelectedAction == PauseMenuOption.ExitGame)
                    Exit();
                break;
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        switch (_gameState)
        {
            case GameState.TitleScreen:
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _titleScreen.Draw(_spriteBatch, _pixel, _font, _databaseError);
                _spriteBatch.End();
                break;

            case GameState.Playing:
                _spriteBatch.Begin(
                    transformMatrix: _camera.TransformMatrix,
                    samplerState: SamplerState.PointClamp);
                _currentMap.Draw(_spriteBatch, _pixel, _camera);
                _player.Draw(_spriteBatch, _pixel);
                _spriteBatch.End();
                break;

            case GameState.Paused:
                _spriteBatch.Begin(
                    transformMatrix: _camera.TransformMatrix,
                    samplerState: SamplerState.PointClamp);
                _currentMap.Draw(_spriteBatch, _pixel, _camera);
                _player.Draw(_spriteBatch, _pixel);
                _spriteBatch.End();
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _pauseScreen.Draw(_spriteBatch, _pixel, _font);
                _spriteBatch.End();
                break;
        }

        base.Draw(gameTime);
    }
}
