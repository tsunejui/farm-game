using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FarmGame.Data;

public class GameConfig
{
    [YamlMember(Alias = "log_level")]
    public string LogLevel { get; set; } = "info";

    public ScreenConfig Screen { get; set; } = new();
    public TileConfig Tile { get; set; } = new();
    public PlayerConfig Player { get; set; } = new();
    public GameStartConfig Game { get; set; } = new();
    public SaveConfig Save { get; set; } = new();
    public HudConfig Hud { get; set; } = new();
    public CombatConfig Combat { get; set; } = new();

    [YamlMember(Alias = "entity_info")]
    public EntityInfoConfig EntityInfo { get; set; } = new();

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

    [YamlMember(Alias = "max_hp")]
    public int MaxHp { get; set; } = 20;

    public float Strength { get; set; } = 5f;
    public float Dexterity { get; set; } = 3f;

    [YamlMember(Alias = "weapon_atk")]
    public float WeaponAtk { get; set; } = 2f;

    [YamlMember(Alias = "buff_percent")]
    public float BuffPercent { get; set; } = 0f;

    [YamlMember(Alias = "crit_rate")]
    public float CritRate { get; set; } = 0.1f;

    [YamlMember(Alias = "crit_damage")]
    public float CritDamage { get; set; } = 1.5f;
}

public class GameStartConfig
{
    [YamlMember(Alias = "start_map")]
    public string StartMap { get; set; } = "farm_home";

    public string Title { get; set; } = "Farm Game";

    [YamlMember(Alias = "default_language")]
    public string DefaultLanguage { get; set; } = "en";
}

public class SaveConfig
{
    [YamlMember(Alias = "auto_save_interval")]
    public float AutoSaveInterval { get; set; } = 60f;
}

public class HudConfig
{
    public ToastConfig Toast { get; set; } = new();

    [YamlMember(Alias = "map_transition")]
    public MapTransitionConfig MapTransition { get; set; } = new();
}

public class ToastConfig
{
    [YamlMember(Alias = "fade_in_ms")]
    public int FadeInMs { get; set; } = 200;

    [YamlMember(Alias = "fade_out_ms")]
    public int FadeOutMs { get; set; } = 300;

    [YamlMember(Alias = "duration_ms")]
    public int DurationMs { get; set; } = 2500;

    [YamlMember(Alias = "max_toasts")]
    public int MaxToasts { get; set; } = 5;

    [YamlMember(Alias = "font_size")]
    public int FontSize { get; set; } = 16;
}

public class MapTransitionConfig
{
    [YamlMember(Alias = "fade_in_ms")]
    public int FadeInMs { get; set; } = 300;

    [YamlMember(Alias = "hold_ms")]
    public int HoldMs { get; set; } = 800;

    [YamlMember(Alias = "fade_out_ms")]
    public int FadeOutMs { get; set; } = 500;

    [YamlMember(Alias = "font_size")]
    public int FontSize { get; set; } = 32;
}

public class EntityInfoConfig
{
    [YamlMember(Alias = "proximity_tiles")]
    public int ProximityTiles { get; set; } = 2;

    [YamlMember(Alias = "font_size")]
    public int FontSize { get; set; } = 12;

    [YamlMember(Alias = "hp_font_size")]
    public int HpFontSize { get; set; } = 8;

    [YamlMember(Alias = "hp_bar_width")]
    public int HpBarWidth { get; set; } = 24;

    [YamlMember(Alias = "hp_bar_height")]
    public int HpBarHeight { get; set; } = 3;

    [YamlMember(Alias = "hp_bar_offset_y")]
    public int HpBarOffsetY { get; set; } = 2;

    [YamlMember(Alias = "name_offset_y")]
    public int NameOffsetY { get; set; } = 4;
}

public class CombatConfig
{
    [YamlMember(Alias = "damage_tick_duration_ms")]
    public int DamageTickDurationMs { get; set; } = 500;

    [YamlMember(Alias = "defense_model")]
    public string DefenseModel { get; set; } = "subtraction";

    [YamlMember(Alias = "defense_constant")]
    public float DefenseConstant { get; set; } = 100f;

    [YamlMember(Alias = "damage_variance")]
    public float DamageVariance { get; set; } = 0.05f;

    [YamlMember(Alias = "flash_opacity")]
    public float FlashOpacity { get; set; } = 0.01f;

    [YamlMember(Alias = "knockback_tiles")]
    public int KnockbackTiles { get; set; } = 1;
}
