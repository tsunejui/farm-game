// =============================================================================
// StatusPanel.cs — Top-center object status panel
//
// Shows selected object's name, HP bar, HP text, effect icons, close button.
// Manages close button hover/click state and effect icon hover detection.
// =============================================================================

using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using FarmGame.Core;
using FarmGame.World;
using FarmGame.World.Effects;

namespace FarmGame.Screens.HUD;

public class StatusPanel
{
    public const int PanelW = 220;
    public const int PanelY = 10;
    public const int Padding = 10;
    public const int CloseSize = 18;
    public const int IconSize = 20;
    public const int IconSpacing = 2;

    private bool _closeHovered;

    public bool CloseHovered => _closeHovered;

    // Returns the hovered effect description (or null)
    public string UpdateInteraction(MouseState mouse, bool clicked, WorldObject selected)
    {
        _closeHovered = false;
        string hoveredEffectDesc = null;

        if (selected == null) return null;

        int panelX = GameConstants.ScreenWidth / 2 - PanelW / 2;
        int closeX = panelX + PanelW - CloseSize - 4;
        int closeY = PanelY + 4;

        // Close button hover
        if (mouse.X >= closeX && mouse.X <= closeX + CloseSize &&
            mouse.Y >= closeY && mouse.Y <= closeY + CloseSize)
            _closeHovered = true;

        // Effect icon hover
        int effectCount = selected.Effects.Count;
        if (effectCount > 0)
        {
            int iconsPerRow = (PanelW - Padding * 2) / (IconSize + IconSpacing);
            if (iconsPerRow < 1) iconsPerRow = 1;
            int barY = PanelY + Padding + 24;
            int iconStartY = barY + 6 + 22;

            for (int i = 0; i < effectCount; i++)
            {
                int row = i / iconsPerRow;
                int col = i % iconsPerRow;
                int ix = panelX + Padding + col * (IconSize + IconSpacing);
                int iy = iconStartY + row * (IconSize + IconSpacing);

                if (mouse.X >= ix && mouse.X <= ix + IconSize &&
                    mouse.Y >= iy && mouse.Y <= iy + IconSize)
                {
                    var def = EffectRegistry.GetDefinition(selected.Effects[i].EffectId);
                    hoveredEffectDesc = def?.Description ?? selected.Effects[i].EffectId;
                    break;
                }
            }
        }

        return hoveredEffectDesc;
    }

    public void Draw(SpriteBatch spriteBatch, WorldObject selected)
    {
        if (selected == null) return;

        var titleFont = FontManager.GetFont(18);
        var hpFont = FontManager.GetFont(14);
        if (titleFont == null || hpFont == null) return;

        bool isDead = !selected.State.IsAlive;
        string name = InspectorHelper.GetLocalizedName(selected);
        if (isDead)
            name += " " + LocaleManager.Get("ui", "dead_suffix", "(Dead)");

        string hpText = $"HP: {selected.State.CurrentHp} / {selected.State.MaxHp}";

        int effectCount = selected.Effects.Count;
        int iconsPerRow = (PanelW - Padding * 2) / (IconSize + IconSpacing);
        if (iconsPerRow < 1) iconsPerRow = 1;
        int iconRows = effectCount > 0 ? (effectCount + iconsPerRow - 1) / iconsPerRow : 0;
        int effectAreaH = iconRows > 0 ? iconRows * (IconSize + IconSpacing) + 6 : 0;

        int panelH = 70 + effectAreaH;
        int panelX = GameConstants.ScreenWidth / 2 - PanelW / 2;

        // Panel background
        spriteBatch.FillRectangle(new Rectangle(panelX, PanelY, PanelW, panelH),
            new Color(20, 30, 20) * 0.9f);
        spriteBatch.DrawRectangle(new Rectangle(panelX, PanelY, PanelW, panelH),
            Color.Gold * 0.6f);

        // Name
        Color nameColor = isDead ? Color.Gray : Color.White;
        titleFont.DrawText(spriteBatch, name,
            new Vector2(panelX + Padding, PanelY + Padding), nameColor);

        // HP bar
        int barW = PanelW - Padding * 2;
        int barH = 6;
        int barY = PanelY + Padding + 24;
        spriteBatch.FillRectangle(new Rectangle(panelX + Padding, barY, barW, barH),
            Color.Black * 0.5f);

        float hpRatio = Math.Clamp(
            (float)selected.State.CurrentHp / selected.State.MaxHp, 0f, 1f);
        int fillW = (int)(barW * hpRatio);
        Color barColor = hpRatio > 0.5f
            ? Color.Lerp(Color.Yellow, Color.LimeGreen, (hpRatio - 0.5f) * 2f)
            : Color.Lerp(Color.Red, Color.Yellow, hpRatio * 2f);
        if (fillW > 0)
            spriteBatch.FillRectangle(new Rectangle(panelX + Padding, barY, fillW, barH), barColor);

        // HP text
        hpFont.DrawText(spriteBatch, hpText,
            new Vector2(panelX + Padding, barY + barH + 3), Color.White * 0.9f);

        // Effect icons
        DrawEffectIcons(spriteBatch, selected, panelX, barY + barH + 22, iconsPerRow);

        // Close button
        DrawCloseButton(spriteBatch, panelX);
    }

    private static void DrawEffectIcons(SpriteBatch spriteBatch, WorldObject selected,
        int panelX, int iconStartY, int iconsPerRow)
    {
        int effectCount = selected.Effects.Count;
        if (effectCount == 0) return;

        for (int i = 0; i < effectCount; i++)
        {
            var ae = selected.Effects[i];
            int row = i / iconsPerRow;
            int col = i % iconsPerRow;
            int ix = panelX + Padding + col * (IconSize + IconSpacing);
            int iy = iconStartY + row * (IconSize + IconSpacing);

            spriteBatch.FillRectangle(new Rectangle(ix, iy, IconSize, IconSize),
                new Color(40, 50, 40) * 0.9f);
            spriteBatch.DrawRectangle(new Rectangle(ix, iy, IconSize, IconSize),
                Color.Gray * 0.4f);

            var def = EffectRegistry.GetDefinition(ae.EffectId);
            if (def?.Texture != null)
            {
                spriteBatch.Draw(def.Texture,
                    new Rectangle(ix + 2, iy + 2, IconSize - 4, IconSize - 4),
                    Color.White);
            }
            else
            {
                var iconFont = FontManager.GetFont(10);
                iconFont?.DrawText(spriteBatch, ae.EffectId[..1].ToUpper(),
                    new Vector2(ix + 4, iy + 3), Color.White * 0.7f);
            }
        }
    }

    private void DrawCloseButton(SpriteBatch spriteBatch, int panelX)
    {
        int closeX = panelX + PanelW - CloseSize - 4;
        int closeY = PanelY + 4;

        bool closePressed = _closeHovered &&
            Mouse.GetState().LeftButton == ButtonState.Pressed;
        Color closeBg = closePressed ? Color.Red
            : _closeHovered ? new Color(180, 40, 40)
            : Color.DarkRed * 0.8f;
        Color closeFg = _closeHovered ? Color.White : Color.White * 0.8f;

        spriteBatch.FillRectangle(new Rectangle(closeX, closeY, CloseSize, CloseSize), closeBg);

        var closeFont = FontManager.GetFont(12);
        if (closeFont != null)
        {
            var xSize = closeFont.MeasureString("X");
            float xTextX = closeX + (CloseSize - xSize.X) / 2f;
            float xTextY = closeY + (CloseSize - xSize.Y) / 2f;
            closeFont.DrawText(spriteBatch, "X", new Vector2(xTextX, xTextY), closeFg);
        }
    }
}
