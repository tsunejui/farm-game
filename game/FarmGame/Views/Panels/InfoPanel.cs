// =============================================================================
// InfoPanel.cs — Bottom-right instant info panel
//
// Shows context-sensitive info:
//   - Object hover: name + category (or "Corpse" if dead)
//   - Effect icon hover: effect name + description
// =============================================================================

using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using FarmGame.Core;
using FarmGame.Core.Managers;
using FarmGame.World;
using FarmGame.Views.HUD;

namespace FarmGame.Views.Panels;

public class InfoPanel
{
    public void Draw(SpriteBatch spriteBatch, WorldObject hoveredObject, string hoveredEffectDesc)
    {
        if (hoveredEffectDesc != null)
        {
            DrawBox(spriteBatch,
                LocaleManager.Get("ui", "effect_label", "Effect"),
                hoveredEffectDesc);
            return;
        }

        if (hoveredObject == null) return;

        bool isDead = !hoveredObject.State.IsAlive;
        string name = InspectorHelper.GetLocalizedName(hoveredObject);
        if (isDead)
            name += " " + LocaleManager.Get("ui", "dead_suffix", "(Dead)");

        string category = isDead
            ? LocaleManager.Get("ui", "corpse", "Corpse")
            : InspectorHelper.GetLocalizedCategory(hoveredObject);

        DrawBox(spriteBatch, name, category);
    }

    private static void DrawBox(SpriteBatch spriteBatch, string line1, string line2)
    {
        var font = FontManager.GetFont(22);
        if (font == null) return;

        int pad = 12;
        int lineSpacing = 6;
        var s1 = font.MeasureString(line1);
        var catFont = FontManager.GetFont(18);
        var s2 = catFont?.MeasureString(line2) ?? Vector2.Zero;
        int boxW = (int)Math.Max(s1.X, s2.X) + pad * 2;
        int boxH = (int)(s1.Y + s2.Y) + lineSpacing + pad * 2;

        int boxX = GameConstants.ScreenWidth - boxW - 12;
        int boxY = GameConstants.ScreenHeight - boxH - 12;

        spriteBatch.FillRectangle(new Rectangle(boxX, boxY, boxW, boxH), Color.Black * 0.7f);
        spriteBatch.DrawRectangle(new Rectangle(boxX, boxY, boxW, boxH), Color.Gray * 0.5f);

        font.DrawText(spriteBatch, line1,
            new Vector2(boxX + pad, boxY + pad), Color.White);
        catFont?.DrawText(spriteBatch, line2,
            new Vector2(boxX + pad, boxY + pad + s1.Y + lineSpacing),
            Color.LightGray * 0.8f);
    }
}
