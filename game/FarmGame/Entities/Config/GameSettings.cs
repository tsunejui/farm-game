using FarmGame.Data;

namespace FarmGame.Entities.Config;

/// <summary>
/// Wraps the main game config (config.yaml).
/// Contains screen, tile, player, combat, HUD, save settings.
/// </summary>
public class GameSettings
{
    public const string ConfigId = "game";

    public GameConfig Data { get; set; }

    public GameSettings(GameConfig data)
    {
        Data = data;
    }
}
