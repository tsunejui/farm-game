// =============================================================================
// Game1.cs — Main game entry class
// =============================================================================

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
    private string _contentDir;
    private GameState _gameState;
    private InitManager _init;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _contentDir = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, Content.RootDirectory);
        _init = new InitManager();
        _init.InitializeCore(_contentDir);
        _gameState = GameState.TitleScreen;
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _init.LoadContent(this, _graphics, _contentDir, StartGame, LoadTexture);
    }

    private void StartGame()
    {
        if (_init.ScreenManager.TryGet(GameState.Playing, out var screen))
        {
            ((PlayingScreen)screen).StartGame(_init.Session?.LoadPlayer());
            _gameState = GameState.Playing;
        }
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
        if (_init.ScreenManager.TryGet(GameState.Playing, out var screen))
            ((PlayingScreen)screen).SaveState();
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

            if (_init.ScreenManager.TryGet(target, out var screen))
                screen.OnEnter(_gameState);

            _gameState = target;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardExtended.Update();

        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            Exit();

        if (_init.ScreenManager.TryGet(_gameState, out var screen))
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

        if (_gameState == GameState.Paused &&
            _init.ScreenManager.TryGet(GameState.Playing, out var playingScreen))
            (playingScreen as IWorldRenderer)?.DrawWorld(_init.SpriteBatch);

        if (_init.ScreenManager.TryGet(_gameState, out var activeScreen))
            activeScreen.Draw(_init.SpriteBatch);

        base.Draw(gameTime);
    }
}
