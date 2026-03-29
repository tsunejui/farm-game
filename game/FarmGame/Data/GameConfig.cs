using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FarmGame.Data;

public class GameConfig
{
    public ScreenConfig Screen { get; set; } = new();
    public TileConfig Tile { get; set; } = new();
    public PlayerConfig Player { get; set; } = new();
    public GameStartConfig Game { get; set; } = new();

    public static GameConfig Load(string yamlPath)
    {
        var yaml = File.ReadAllText(yamlPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        return deserializer.Deserialize<GameConfig>(yaml);
    }
}

public class ScreenConfig
{
    public int Width { get; set; } = 800;
    public int Height { get; set; } = 600;
}

public class TileConfig
{
    public int Size { get; set; } = 32;
}

public class PlayerConfig
{
    [YamlMember(Alias = "move_speed")]
    public float MoveSpeed { get; set; } = 4.0f;

    public string Color { get; set; } = "#FF4500";

    [YamlMember(Alias = "body_padding")]
    public int BodyPadding { get; set; } = 2;

    [YamlMember(Alias = "indicator_size")]
    public int IndicatorSize { get; set; } = 8;

    [YamlMember(Alias = "jump_height")]
    public int JumpHeight { get; set; } = 12;

    [YamlMember(Alias = "jump_duration")]
    public float JumpDuration { get; set; } = 0.4f;

    [YamlMember(Alias = "attack_duration")]
    public float AttackDuration { get; set; } = 0.3f;

    [YamlMember(Alias = "attack_range")]
    public int AttackRange { get; set; } = 16;

    [YamlMember(Alias = "attack_width")]
    public int AttackWidth { get; set; } = 20;

    [YamlMember(Alias = "attack_color")]
    public string AttackColor { get; set; } = "#FFD700";
}

public class GameStartConfig
{
    [YamlMember(Alias = "start_map")]
    public string StartMap { get; set; } = "farm_home";

    public string Title { get; set; } = "Farm Game";

    [YamlMember(Alias = "default_language")]
    public string DefaultLanguage { get; set; } = "en";
}
