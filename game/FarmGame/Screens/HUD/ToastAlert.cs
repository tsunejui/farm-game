using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using MonoGame.Extended;
using FarmGame.Core;

namespace FarmGame.Screens.HUD;

// =============================================================================
// ToastAlert.cs — Event notification toast at bottom-left of gameplay screen
//
// Uses FontStashSharp for Unicode (Chinese) text rendering.
// =============================================================================
public class ToastAlert
{
    private const int FadeInMs = 200;
    private const int FadeOutMs = 300;
    private const int DefaultDurationMs = 2500;
    private const int MaxToasts = 5;
    private const int MarginLeft = 12;
    private const int MarginBottom = 12;
    private const int Padding = 6;
    private const int Spacing = 4;
    private const int FontSize = 16;

    private readonly List<Toast> _toasts = new();

    public void Show(string message, int durationMs = DefaultDurationMs)
    {
        _toasts.Add(new Toast
        {
            Message = message,
            TotalMs = FadeInMs + durationMs + FadeOutMs,
            DurationMs = durationMs,
            ElapsedMs = 0,
        });

        while (_toasts.Count > MaxToasts)
            _toasts.RemoveAt(0);
    }

    public void Update(float deltaTime)
    {
        int deltaMs = (int)(deltaTime * 1000);

        for (int i = _toasts.Count - 1; i >= 0; i--)
        {
            _toasts[i].ElapsedMs += deltaMs;
            if (_toasts[i].ElapsedMs >= _toasts[i].TotalMs)
                _toasts.RemoveAt(i);
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (_toasts.Count == 0) return;

        var font = FontManager.GetFont(FontSize);
        if (font == null) return;

        int screenH = GameConstants.ScreenHeight;
        int y = screenH - MarginBottom;

        for (int i = _toasts.Count - 1; i >= 0; i--)
        {
            var toast = _toasts[i];
            float alpha = CalculateAlpha(toast);
            if (alpha <= 0f) continue;

            var textSize = font.MeasureString(toast.Message);
            int boxW = (int)textSize.X + Padding * 2;
            int boxH = (int)textSize.Y + Padding * 2;

            y -= boxH + Spacing;

            var bgRect = new Rectangle(MarginLeft, y, boxW, boxH);
            spriteBatch.FillRectangle(bgRect, Color.Black * (alpha * 0.7f));

            font.DrawText(spriteBatch, toast.Message,
                new Vector2(MarginLeft + Padding, y + Padding),
                Color.White * alpha);
        }
    }

    private static float CalculateAlpha(Toast toast)
    {
        if (toast.ElapsedMs < FadeInMs)
            return (float)toast.ElapsedMs / FadeInMs;

        int fadeOutStart = FadeInMs + toast.DurationMs;
        if (toast.ElapsedMs < fadeOutStart)
            return 1f;

        float progress = (float)(toast.ElapsedMs - fadeOutStart) / FadeOutMs;
        return 1f - MathHelper.Clamp(progress, 0f, 1f);
    }

    private class Toast
    {
        public string Message;
        public int TotalMs;
        public int DurationMs;
        public int ElapsedMs;
    }
}
