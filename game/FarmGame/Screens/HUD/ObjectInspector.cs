// =============================================================================
// ObjectInspector.cs — Mouse-driven object inspection HUD
//
// Hover: shows object name + category at bottom-right of screen
// Click: opens status panel at top-center with name + HP + close button
//        Draws gold marker under selected object
// =============================================================================

using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using FarmGame.Camera;
using FarmGame.Core;
using FarmGame.World;

namespace FarmGame.Screens.HUD;

public class ObjectInspector
{
    private WorldObject _hoveredObject;
    private WorldObject _selectedObject;
    private MouseState _prevMouse;

    // Update mouse state and detect hover/click on objects
    public void Update(GameMap map, Camera2D camera)
    {
        var mouse = Mouse.GetState();
        var worldPos = camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
        int tileX = (int)worldPos.X / GameConstants.TileSize;
        int tileY = (int)worldPos.Y / GameConstants.TileSize;

        // Hover detection
        _hoveredObject = map.GetObjectAt(tileX, tileY);

        // Click detection (left button pressed this frame)
        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            if (_hoveredObject != null)
                _selectedObject = _hoveredObject;
        }

        _prevMouse = mouse;
    }

    // Check if close button was clicked (call from Update, returns true if panel closed)
    public void HandleCloseClick()
    {
        if (_selectedObject == null) return;

        var mouse = Mouse.GetState();
        if (mouse.LeftButton != ButtonState.Pressed || _prevMouse.LeftButton != ButtonState.Released)
            return;

        // Close button hit test (top-right of panel)
        int panelW = 200;
        int panelX = GameConstants.ScreenWidth / 2 - panelW / 2;
        int panelY = 10;
        int closeX = panelX + panelW - 22;
        int closeY = panelY + 4;
        int closeSize = 18;

        if (mouse.X >= closeX && mouse.X <= closeX + closeSize &&
            mouse.Y >= closeY && mouse.Y <= closeY + closeSize)
        {
            _selectedObject = null;
        }
    }

    // Draw gold marker under selected object (in world space, inside camera transform)
    public void DrawWorldMarker(SpriteBatch spriteBatch)
    {
        if (_selectedObject == null) return;

        int ts = GameConstants.TileSize;
        int px = _selectedObject.TileX * ts;
        int py = _selectedObject.TileY * ts;
        int pw = _selectedObject.EffectiveWidth * ts;
        int ph = _selectedObject.EffectiveHeight * ts;

        // Gold underline marker below the object
        int markerH = 3;
        int markerY = py + ph + 1;
        spriteBatch.FillRectangle(new Rectangle(px, markerY, pw, markerH), Color.Gold);

        // Gold corner brackets
        int bracketLen = 6;
        var gold = Color.Gold * 0.8f;
        // Top-left
        spriteBatch.FillRectangle(new Rectangle(px - 1, py - 1, bracketLen, 1), gold);
        spriteBatch.FillRectangle(new Rectangle(px - 1, py - 1, 1, bracketLen), gold);
        // Top-right
        spriteBatch.FillRectangle(new Rectangle(px + pw - bracketLen + 1, py - 1, bracketLen, 1), gold);
        spriteBatch.FillRectangle(new Rectangle(px + pw, py - 1, 1, bracketLen), gold);
        // Bottom-left
        spriteBatch.FillRectangle(new Rectangle(px - 1, py + ph, bracketLen, 1), gold);
        spriteBatch.FillRectangle(new Rectangle(px - 1, py + ph - bracketLen + 1, 1, bracketLen), gold);
        // Bottom-right
        spriteBatch.FillRectangle(new Rectangle(px + pw - bracketLen + 1, py + ph, bracketLen, 1), gold);
        spriteBatch.FillRectangle(new Rectangle(px + pw, py + ph - bracketLen + 1, 1, bracketLen), gold);
    }

    // Draw screen-space HUD elements (hover tooltip + status panel)
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

        int padding = 8;
        int lineSpacing = 4;
        var nameSize = font.MeasureString(name);
        var catSize = font.MeasureString(category);
        int boxW = (int)Math.Max(nameSize.X, catSize.X) + padding * 2;
        int boxH = (int)(nameSize.Y + catSize.Y) + lineSpacing + padding * 2;

        int screenW = GameConstants.ScreenWidth;
        int screenH = GameConstants.ScreenHeight;
        int boxX = screenW - boxW - 12;
        int boxY = screenH - boxH - 12;

        // Background
        spriteBatch.FillRectangle(new Rectangle(boxX, boxY, boxW, boxH), Color.Black * 0.7f);
        // Border
        spriteBatch.DrawRectangle(new Rectangle(boxX, boxY, boxW, boxH), Color.Gray * 0.5f);

        // Name (white)
        font.DrawText(spriteBatch, name,
            new Vector2(boxX + padding, boxY + padding), Color.White);

        // Category (gray)
        var catFont = FontManager.GetFont(12);
        catFont?.DrawText(spriteBatch, category,
            new Vector2(boxX + padding, boxY + padding + nameSize.Y + lineSpacing),
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

        int panelW = 200;
        int panelH = 70;
        int panelX = GameConstants.ScreenWidth / 2 - panelW / 2;
        int panelY = 10;
        int padding = 10;

        // Panel background
        spriteBatch.FillRectangle(new Rectangle(panelX, panelY, panelW, panelH),
            new Color(20, 30, 20) * 0.9f);
        spriteBatch.DrawRectangle(new Rectangle(panelX, panelY, panelW, panelH),
            Color.Gold * 0.6f);

        // Name
        titleFont.DrawText(spriteBatch, name,
            new Vector2(panelX + padding, panelY + padding), Color.White);

        // HP bar
        int barW = panelW - padding * 2;
        int barH = 6;
        int barY = panelY + padding + 24;
        spriteBatch.FillRectangle(new Rectangle(panelX + padding, barY, barW, barH),
            Color.Black * 0.5f);

        float hpRatio = Math.Clamp(
            (float)_selectedObject.State.CurrentHp / _selectedObject.State.MaxHp, 0f, 1f);
        int fillW = (int)(barW * hpRatio);
        Color barColor = hpRatio > 0.5f
            ? Color.Lerp(Color.Yellow, Color.LimeGreen, (hpRatio - 0.5f) * 2f)
            : Color.Lerp(Color.Red, Color.Yellow, hpRatio * 2f);
        if (fillW > 0)
            spriteBatch.FillRectangle(new Rectangle(panelX + padding, barY, fillW, barH), barColor);

        // HP text
        hpFont.DrawText(spriteBatch, hpText,
            new Vector2(panelX + padding, barY + barH + 3), Color.White * 0.9f);

        // Close button [X] at top-right
        int closeSize = 18;
        int closeX = panelX + panelW - closeSize - 4;
        int closeY = panelY + 4;
        spriteBatch.FillRectangle(new Rectangle(closeX, closeY, closeSize, closeSize),
            Color.DarkRed * 0.8f);
        var closeFont = FontManager.GetFont(12);
        closeFont?.DrawText(spriteBatch, "X",
            new Vector2(closeX + 4, closeY + 1), Color.White);
    }

    public WorldObject SelectedObject => _selectedObject;
}
