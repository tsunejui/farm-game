using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra;
using Serilog;
using FarmGame.Core;

namespace FarmGame.Bootstrap;

public static class GraphicsInitializer
{
    public static SpriteBatch Run(Game game, GraphicsDeviceManager graphics, string contentDir)
    {
        graphics.PreferredBackBufferWidth = GameConstants.ScreenWidth;
        graphics.PreferredBackBufferHeight = GameConstants.ScreenHeight;
        graphics.ApplyChanges();

        var spriteBatch = new SpriteBatch(game.GraphicsDevice);

        FontManager.Initialize(game.GraphicsDevice, contentDir);
        MyraEnvironment.Game = game;

        Log.Information("[Init] Graphics initialized: {Width}x{Height}",
            GameConstants.ScreenWidth, GameConstants.ScreenHeight);

        return spriteBatch;
    }
}
