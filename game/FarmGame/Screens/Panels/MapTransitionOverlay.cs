using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using MonoGame.Extended;
using FarmGame.Core;

namespace FarmGame.Screens.Panels;

// =============================================================================
// MapTransitionOverlay.cs — Map loading transition effect
//
// Shows the map name with a fade-in/hold/fade-out animation over gameplay.
// Uses FontStashSharp for Unicode (Chinese) text rendering.
// =============================================================================
public class MapTransitionOverlay
{
    private string _mapName;
    private int _elapsedMs;

    private int TotalMs => GameConstants.MapTransitionFadeInMs
        + GameConstants.MapTransitionHoldMs
        + GameConstants.MapTransitionFadeOutMs;

    public bool IsActive { get; private set; }

    public void Start(string mapName)
    {
        _mapName = mapName;
        _elapsedMs = 0;
        IsActive = true;
    }

    public void Update(float deltaTime)
    {
        if (!IsActive) return;

        _elapsedMs += (int)(deltaTime * 1000);
        if (_elapsedMs >= TotalMs)
        {
            IsActive = false;
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsActive) return;

        var font = FontManager.GetFont(GameConstants.MapTransitionFontSize);
        if (font == null) return;

        float alpha = CalculateAlpha();
        if (alpha <= 0f) return;

        int screenW = GameConstants.ScreenWidth;
        int screenH = GameConstants.ScreenHeight;

        // Dark overlay
        spriteBatch.FillRectangle(
            new Rectangle(0, 0, screenW, screenH),
            Color.Black * (alpha * 0.6f));

        // Map name
        var textSize = font.MeasureString(_mapName);
        var pos = new Vector2(
            (screenW - textSize.X) / 2f,
            screenH * 0.4f - textSize.Y / 2f);

        // Shadow
        font.DrawText(spriteBatch, _mapName,
            pos + new Vector2(2f, 2f),
            Color.Black * alpha);

        // Text
        font.DrawText(spriteBatch, _mapName,
            pos,
            Color.White * alpha);
    }

    private float CalculateAlpha()
    {
        int fadeIn = GameConstants.MapTransitionFadeInMs;
        int hold = GameConstants.MapTransitionHoldMs;
        int fadeOut = GameConstants.MapTransitionFadeOutMs;

        if (_elapsedMs < fadeIn)
            return (float)_elapsedMs / fadeIn;
        else if (_elapsedMs < fadeIn + hold)
            return 1f;
        else
        {
            float fadeOutProgress = (float)(_elapsedMs - fadeIn - hold) / fadeOut;
            return 1f - MathHelper.Clamp(fadeOutProgress, 0f, 1f);
        }
    }
}
