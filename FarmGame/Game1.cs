using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FarmGame.Camera;
using FarmGame.Core;
using FarmGame.Entities;
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

    // Gameplay
    private TileMap _tileMap;
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
        _graphics.PreferredBackBufferWidth = GameConstants.ScreenWidth;
        _graphics.PreferredBackBufferHeight = GameConstants.ScreenHeight;
        _graphics.ApplyChanges();

        _gameState = GameState.TitleScreen;

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _font = Content.Load<SpriteFont>("DefaultFont");

        _titleScreen = new TitleScreen();
        _pauseScreen = new PauseScreen();
    }

    private void StartGame()
    {
        var contentDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory);
        var (map, playerStart) = MapLoader.Load(
            Path.Combine(contentDir, "Maps", "farm.yaml"));
        _tileMap = map;
        _player = new Player(playerStart, _tileMap);
        _camera = new Camera2D(GraphicsDevice.Viewport);
        _gameState = GameState.Playing;
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
                    _camera.Update(_player, _tileMap);
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
                _titleScreen.Draw(_spriteBatch, _pixel, _font);
                _spriteBatch.End();
                break;

            case GameState.Playing:
                _spriteBatch.Begin(
                    transformMatrix: _camera.TransformMatrix,
                    samplerState: SamplerState.PointClamp);
                _tileMap.Draw(_spriteBatch, _pixel, _camera);
                _player.Draw(_spriteBatch, _pixel);
                _spriteBatch.End();
                break;

            case GameState.Paused:
                _spriteBatch.Begin(
                    transformMatrix: _camera.TransformMatrix,
                    samplerState: SamplerState.PointClamp);
                _tileMap.Draw(_spriteBatch, _pixel, _camera);
                _player.Draw(_spriteBatch, _pixel);
                _spriteBatch.End();
                // Overlay pause menu (no transform, screen-space)
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                _pauseScreen.Draw(_spriteBatch, _pixel, _font);
                _spriteBatch.End();
                break;
        }

        base.Draw(gameTime);
    }
}
