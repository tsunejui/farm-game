using FarmGame.Core;

namespace FarmGame.World;

public static class MapGenerator
{
    public static TileMap GenerateDefault()
    {
        var map = new TileMap(GameConstants.MapWidth, GameConstants.MapHeight);

        // Fill with grass
        for (int x = 0; x < GameConstants.MapWidth; x++)
            for (int y = 0; y < GameConstants.MapHeight; y++)
                map.SetTile(x, y, TileType.Grass);

        // Horizontal path strip across the middle
        for (int x = 0; x < GameConstants.MapWidth; x++)
        {
            map.SetTile(x, 14, TileType.Path);
            map.SetTile(x, 15, TileType.Path);
        }

        // Vertical path from top to bottom
        for (int y = 0; y < GameConstants.MapHeight; y++)
        {
            map.SetTile(20, y, TileType.Path);
            map.SetTile(21, y, TileType.Path);
        }

        // Farm plot (dirt area)
        for (int x = 8; x < 14; x++)
            for (int y = 8; y < 12; y++)
                map.SetTile(x, y, TileType.Dirt);

        // Water pond
        for (int x = 30; x < 35; x++)
            for (int y = 5; y < 9; y++)
                map.SetTile(x, y, TileType.Water);

        // Small lake in bottom-left
        for (int x = 2; x < 6; x++)
            for (int y = 22; y < 26; y++)
                map.SetTile(x, y, TileType.Water);

        return map;
    }
}
