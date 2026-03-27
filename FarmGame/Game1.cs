using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FarmGame.Camera;
using FarmGame.Core;
using FarmGame.Entities;
using FarmGame.World;

namespace FarmGame;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;
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

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _tileMap = MapGenerator.GenerateDefault();
        _player = new Player(new Point(10, 10), _tileMap);
        _camera = new Camera2D(GraphicsDevice.Viewport);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        _player.Update(gameTime);
        _camera.Update(_player, _tileMap);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(
            transformMatrix: _camera.TransformMatrix,
            samplerState: SamplerState.PointClamp);

        _tileMap.Draw(_spriteBatch, _pixel, _camera);
        _player.Draw(_spriteBatch, _pixel);

        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
