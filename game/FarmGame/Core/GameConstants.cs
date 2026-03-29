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
    public static int PlayerJumpHeight { get; private set; } = 12;
    public static float PlayerJumpDuration { get; private set; } = 0.4f;
    public static float PlayerAttackDuration { get; private set; } = 0.3f;
    public static int PlayerAttackRange { get; private set; } = 16;
    public static int PlayerAttackWidth { get; private set; } = 20;
    public static Color PlayerAttackColor { get; private set; } = Color.Gold;
    public static int PlayerMaxHp { get; private set; } = 20;
    public static string DefaultLanguage { get; private set; } = "en";
    public static float AutoSaveInterval { get; private set; } = 60f;

    // HUD — Toast
    public static int ToastFadeInMs { get; private set; } = 200;
    public static int ToastFadeOutMs { get; private set; } = 300;
    public static int ToastDurationMs { get; private set; } = 2500;
    public static int ToastMaxCount { get; private set; } = 5;
    public static int ToastFontSize { get; private set; } = 16;

    // Entity info display
    public static int EntityInfoProximityTiles { get; private set; } = 2;
    public static int EntityInfoFontSize { get; private set; } = 12;
    public static int EntityInfoHpFontSize { get; private set; } = 8;
    public static int EntityInfoHpBarWidth { get; private set; } = 24;
    public static int EntityInfoHpBarHeight { get; private set; } = 3;
    public static int EntityInfoHpBarOffsetY { get; private set; } = 2;
    public static int EntityInfoNameOffsetY { get; private set; } = 4;

    // Combat
    public static int DamageTickDurationMs { get; private set; } = 500;
    public static int DefaultMinDamage { get; private set; } = 1;
    public static int DefaultMaxDamage { get; private set; } = 3;
    public static float DamageFlashOpacity { get; private set; } = 0.01f;

    // HUD — Map Transition
    public static int MapTransitionFadeInMs { get; private set; } = 300;
    public static int MapTransitionHoldMs { get; private set; } = 800;
    public static int MapTransitionFadeOutMs { get; private set; } = 500;
    public static int MapTransitionFontSize { get; private set; } = 32;

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
        PlayerJumpHeight = config.Player.JumpHeight;
        PlayerJumpDuration = config.Player.JumpDuration;
        PlayerAttackDuration = config.Player.AttackDuration;
        PlayerAttackRange = config.Player.AttackRange;
        PlayerAttackWidth = config.Player.AttackWidth;
        PlayerAttackColor = ColorHelper.FromHex(config.Player.AttackColor);
        PlayerMaxHp = config.Player.MaxHp;
        DefaultLanguage = config.Game.DefaultLanguage;
        AutoSaveInterval = config.Save.AutoSaveInterval;

        ToastFadeInMs = config.Hud.Toast.FadeInMs;
        ToastFadeOutMs = config.Hud.Toast.FadeOutMs;
        ToastDurationMs = config.Hud.Toast.DurationMs;
        ToastMaxCount = config.Hud.Toast.MaxToasts;
        ToastFontSize = config.Hud.Toast.FontSize;

        MapTransitionFadeInMs = config.Hud.MapTransition.FadeInMs;
        MapTransitionHoldMs = config.Hud.MapTransition.HoldMs;
        MapTransitionFadeOutMs = config.Hud.MapTransition.FadeOutMs;
        MapTransitionFontSize = config.Hud.MapTransition.FontSize;

        EntityInfoProximityTiles = config.EntityInfo.ProximityTiles;
        EntityInfoFontSize = config.EntityInfo.FontSize;
        EntityInfoHpFontSize = config.EntityInfo.HpFontSize;
        EntityInfoHpBarWidth = config.EntityInfo.HpBarWidth;
        EntityInfoHpBarHeight = config.EntityInfo.HpBarHeight;
        EntityInfoHpBarOffsetY = config.EntityInfo.HpBarOffsetY;
        EntityInfoNameOffsetY = config.EntityInfo.NameOffsetY;

        DamageTickDurationMs = config.Combat.DamageTickDurationMs;
        DefaultMinDamage = config.Combat.DefaultMinDamage;
        DefaultMaxDamage = config.Combat.DefaultMaxDamage;
        DamageFlashOpacity = config.Combat.FlashOpacity;
    }
}
