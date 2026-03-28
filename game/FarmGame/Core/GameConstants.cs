using Microsoft.Xna.Framework;
using FarmGame.Data;

namespace FarmGame.Core;

public static class GameConstants
{
    public static int TileSize { get; private set; } = 32;
    public static float PlayerMoveSpeed { get; private set; } = 4.0f;
    public static int ScreenWidth { get; private set; } = 800;
    public static int ScreenHeight { get; private set; } = 600;
    public static string StartMap { get; private set; } = "farm_home";
    public static string GameTitle { get; private set; } = "Farm Game";
    public static Color PlayerColor { get; private set; } = Color.OrangeRed;
    public static int PlayerBodyPadding { get; private set; } = 2;
    public static int PlayerIndicatorSize { get; private set; } = 8;

    public static void LoadFrom(GameConfig config)
    {
        ScreenWidth = config.Screen.Width;
        ScreenHeight = config.Screen.Height;
        TileSize = config.Tile.Size;
        PlayerMoveSpeed = config.Player.MoveSpeed;
        StartMap = config.Game.StartMap;
        GameTitle = config.Game.Title;
        PlayerBodyPadding = config.Player.BodyPadding;
        PlayerIndicatorSize = config.Player.IndicatorSize;
        PlayerColor = ColorHelper.FromHex(config.Player.Color);
    }
}
