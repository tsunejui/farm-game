// =============================================================================
// ObjectInspector.cs — Mouse-driven object inspection HUD
//
// Hover: shows object name + category at bottom-right of screen
// Click: opens status panel at top-center with name + HP + close button
//        Draws gold marker under selected object
//        Renders effect icons below HP bar
// =============================================================================

using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using FarmGame.Camera;
using FarmGame.Core;
using FarmGame.World;
using FarmGame.World.Effects;

namespace FarmGame.Screens.HUD;

public class ObjectInspector
{
    private WorldObject _hoveredObject;
    private WorldObject _selectedObject;
    private WorldObject _playerObject;
    private MouseState _prevMouse;

    // Status panel layout constants
    private const int PanelW = 220;
    private const int PanelY = 10;
    private const int Padding = 10;
    private const int CloseSize = 18;
    private const int IconSize = 20;
    private const int IconSpacing = 2;

    public WorldObject SelectedObject => _selectedObject;

    // Set the player as a clickable/hoverable object
    public void SetPlayerObject(WorldObject playerObj)
    {
        _playerObject = playerObj;
    }

    public void Update(GameMap map, Camera2D camera)
    {
        var mouse = Mouse.GetState();
        bool clicked = mouse.LeftButton == ButtonState.Pressed &&
                       _prevMouse.LeftButton == ButtonState.Released;

        // Check close button FIRST (before world click)
        if (clicked && _selectedObject != null)
        {
            int panelX = GameConstants.ScreenWidth / 2 - PanelW / 2;
            int closeX = panelX + PanelW - CloseSize - 4;
            int closeY = PanelY + 4;

            if (mouse.X >= closeX && mouse.X <= closeX + CloseSize &&
                mouse.Y >= closeY && mouse.Y <= closeY + CloseSize)
            {
                _selectedObject = null;
                _prevMouse = mouse;
                return;
            }
        }

        // World-space hover/click
        var worldPos = camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
        int tileX = (int)worldPos.X / GameConstants.TileSize;
        int tileY = (int)worldPos.Y / GameConstants.TileSize;

        // Hover: check map objects, then player
        _hoveredObject = map.GetObjectAt(tileX, tileY);
        if (_hoveredObject == null && _playerObject != null &&
            tileX == _playerObject.TileX && tileY == _playerObject.TileY)
            _hoveredObject = _playerObject;

        // Click: select hovered object
        if (clicked && _hoveredObject != null)
            _selectedObject = _hoveredObject;

        _prevMouse = mouse;
    }

    public void DrawWorldMarker(SpriteBatch spriteBatch)
    {
        if (_selectedObject == null) return;

        int ts = GameConstants.TileSize;
        int px = _selectedObject.TileX * ts;
        int py = _selectedObject.TileY * ts;
        int pw = _selectedObject.EffectiveWidth * ts;
        int ph = _selectedObject.EffectiveHeight * ts;

        // Gold underline
        spriteBatch.FillRectangle(new Rectangle(px, py + ph + 1, pw, 3), Color.Gold);

        // Gold corner brackets
        int bl = 6;
        var gold = Color.Gold * 0.8f;
        spriteBatch.FillRectangle(new Rectangle(px - 1, py - 1, bl, 1), gold);
        spriteBatch.FillRectangle(new Rectangle(px - 1, py - 1, 1, bl), gold);
        spriteBatch.FillRectangle(new Rectangle(px + pw - bl + 1, py - 1, bl, 1), gold);
        spriteBatch.FillRectangle(new Rectangle(px + pw, py - 1, 1, bl), gold);
        spriteBatch.FillRectangle(new Rectangle(px - 1, py + ph, bl, 1), gold);
        spriteBatch.FillRectangle(new Rectangle(px - 1, py + ph - bl + 1, 1, bl), gold);
        spriteBatch.FillRectangle(new Rectangle(px + pw - bl + 1, py + ph, bl, 1), gold);
        spriteBatch.FillRectangle(new Rectangle(px + pw, py + ph - bl + 1, 1, bl), gold);
    }

    public void DrawHUD(SpriteBatch spriteBatch)
    {
        DrawHoverTooltip(spriteBatch);
        DrawStatusPanel(spriteBatch);
    }

    private void DrawHoverTooltip(SpriteBatch spriteBatch)
    {
        if (_hoveredObject == null) return;

        var font = FontManager.GetFont(14);
        if (font == null) return;

        string name = LocaleManager.Get("items", _hoveredObject.ItemId,
            _hoveredObject.Definition.Metadata.DisplayName);
        string category = _hoveredObject.Category.ToString();

        int pad = 8;
        int lineSpacing = 4;
        var nameSize = font.MeasureString(name);
        var catSize = font.MeasureString(category);
        int boxW = (int)Math.Max(nameSize.X, catSize.X) + pad * 2;
        int boxH = (int)(nameSize.Y + catSize.Y) + lineSpacing + pad * 2;

        int boxX = GameConstants.ScreenWidth - boxW - 12;
        int boxY = GameConstants.ScreenHeight - boxH - 12;

        spriteBatch.FillRectangle(new Rectangle(boxX, boxY, boxW, boxH), Color.Black * 0.7f);
        spriteBatch.DrawRectangle(new Rectangle(boxX, boxY, boxW, boxH), Color.Gray * 0.5f);

        font.DrawText(spriteBatch, name,
            new Vector2(boxX + pad, boxY + pad), Color.White);

        var catFont = FontManager.GetFont(12);
        catFont?.DrawText(spriteBatch, category,
            new Vector2(boxX + pad, boxY + pad + nameSize.Y + lineSpacing),
            Color.LightGray * 0.8f);
    }

    private void DrawStatusPanel(SpriteBatch spriteBatch)
    {
        if (_selectedObject == null) return;

        var titleFont = FontManager.GetFont(18);
        var hpFont = FontManager.GetFont(14);
        if (titleFont == null || hpFont == null) return;

        string name = LocaleManager.Get("items", _selectedObject.ItemId,
            _selectedObject.Definition.Metadata.DisplayName);
        string hpText = $"HP: {_selectedObject.State.CurrentHp} / {_selectedObject.State.MaxHp}";

        // Calculate panel height based on effect icon rows
        int effectCount = _selectedObject.Effects.Count;
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
        titleFont.DrawText(spriteBatch, name,
            new Vector2(panelX + Padding, PanelY + Padding), Color.White);

        // HP bar
        int barW = PanelW - Padding * 2;
        int barH = 6;
        int barY = PanelY + Padding + 24;
        spriteBatch.FillRectangle(new Rectangle(panelX + Padding, barY, barW, barH),
            Color.Black * 0.5f);

        float hpRatio = Math.Clamp(
            (float)_selectedObject.State.CurrentHp / _selectedObject.State.MaxHp, 0f, 1f);
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
        if (effectCount > 0)
        {
            int iconStartY = barY + barH + 22;
            var iconFont = FontManager.GetFont(8);

            for (int i = 0; i < effectCount; i++)
            {
                var ae = _selectedObject.Effects[i];
                int row = i / iconsPerRow;
                int col = i % iconsPerRow;
                int ix = panelX + Padding + col * (IconSize + IconSpacing);
                int iy = iconStartY + row * (IconSize + IconSpacing);

                // Icon background
                spriteBatch.FillRectangle(new Rectangle(ix, iy, IconSize, IconSize),
                    new Color(40, 50, 40) * 0.9f);
                spriteBatch.DrawRectangle(new Rectangle(ix, iy, IconSize, IconSize),
                    Color.Gray * 0.4f);

                // Effect icon image (if loaded via EffectDefinition)
                var def = EffectRegistry.GetDefinition(ae.EffectId);
                if (def != null && def.Texture != null)
                {
                    spriteBatch.Draw(def.Texture,
                        new Rectangle(ix + 2, iy + 2, IconSize - 4, IconSize - 4),
                        Color.White);
                }
                else
                {
                    // Fallback: first letter of effect ID
                    iconFont?.DrawText(spriteBatch, ae.EffectId[..1].ToUpper(),
                        new Vector2(ix + 5, iy + 4), Color.White * 0.7f);
                }
            }
        }

        // Close button [X] at top-right — centered text
        int closeX = panelX + PanelW - CloseSize - 4;
        int closeY = PanelY + 4;
        spriteBatch.FillRectangle(new Rectangle(closeX, closeY, CloseSize, CloseSize),
            Color.DarkRed * 0.8f);

        var closeFont = FontManager.GetFont(12);
        if (closeFont != null)
        {
            var xSize = closeFont.MeasureString("X");
            float xTextX = closeX + (CloseSize - xSize.X) / 2f;
            float xTextY = closeY + (CloseSize - xSize.Y) / 2f;
            closeFont.DrawText(spriteBatch, "X", new Vector2(xTextX, xTextY), Color.White);
        }
    }
}
