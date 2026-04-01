// =============================================================================
// DialogueOverlay.cs — Centered dialogue panel for interactable objects
//
// Shows a panel in the center of the screen with text lines and a close
// button. Closed by clicking the close button, pressing Escape, or Z.
// While open, blocks player movement input.
// =============================================================================

using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using FarmGame.Core;

namespace FarmGame.Screens.Panels;

public class DialogueOverlay
{
    private List<string> _lines;
    private string _title;
    private bool _closeHovered;
    private MouseState _prevMouse;
    private int _openCooldown; // skip N frames after opening to prevent instant Z-close

    public bool IsOpen { get; private set; }

    public void Open(string title, List<string> lines)
    {
        _title = title;
        _lines = lines;
        IsOpen = true;
        _openCooldown = 2; // skip 2 frames before accepting close input
    }

    public void Close()
    {
        IsOpen = false;
        _lines = null;
        _title = null;
    }

    // Returns true if the dialogue consumed input this frame (blocks game input)
    public bool Update()
    {
        if (!IsOpen) return false;

        var mouse = Mouse.GetState();
        var keyboard = KeyboardExtended.GetState();
        bool clicked = mouse.LeftButton == ButtonState.Pressed &&
                       _prevMouse.LeftButton == ButtonState.Released;

        // Cooldown after opening — prevents the same Z press from closing immediately
        if (_openCooldown > 0)
        {
            _openCooldown--;
            _prevMouse = mouse;
            return true;
        }

        // Close on Escape or Z
        if (keyboard.WasKeyPressed(Keys.Escape) || keyboard.WasKeyPressed(Keys.Z))
        {
            Close();
            _prevMouse = mouse;
            return true;
        }

        // Close button hit test
        var layout = CalcLayout();
        int closeX = layout.PanelX + layout.PanelW - 26;
        int closeY = layout.PanelY + 6;
        int closeSize = 20;

        _closeHovered = mouse.X >= closeX && mouse.X <= closeX + closeSize &&
                        mouse.Y >= closeY && mouse.Y <= closeY + closeSize;

        if (clicked && _closeHovered)
        {
            Close();
            _prevMouse = mouse;
            return true;
        }

        // Click "close" text line at bottom
        if (clicked)
        {
            int closeLabelY = layout.PanelY + layout.PanelH - 30;
            if (mouse.Y >= closeLabelY && mouse.Y <= closeLabelY + 22 &&
                mouse.X >= layout.PanelX && mouse.X <= layout.PanelX + layout.PanelW)
            {
                Close();
                _prevMouse = mouse;
                return true;
            }
        }

        _prevMouse = mouse;
        return true; // block game input while open
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!IsOpen || _lines == null) return;

        var titleFont = FontManager.GetFont(20);
        var lineFont = FontManager.GetFont(16);
        var closeFont = FontManager.GetFont(14);
        if (titleFont == null || lineFont == null) return;

        var layout = CalcLayout();
        int px = layout.PanelX;
        int py = layout.PanelY;
        int pw = layout.PanelW;
        int ph = layout.PanelH;

        // Dark background overlay
        spriteBatch.FillRectangle(
            new Rectangle(0, 0, GameConstants.ScreenWidth, GameConstants.ScreenHeight),
            Color.Black * 0.4f);

        // Panel
        spriteBatch.FillRectangle(new Rectangle(px, py, pw, ph),
            new Color(25, 35, 25) * 0.95f);
        spriteBatch.DrawRectangle(new Rectangle(px, py, pw, ph),
            new Color(120, 160, 120) * 0.7f);

        int pad = 16;
        int y = py + pad;

        // Title
        if (!string.IsNullOrEmpty(_title))
        {
            titleFont.DrawText(spriteBatch, _title,
                new Vector2(px + pad, y), Color.Gold);
            y += 28;
        }

        // Separator
        spriteBatch.FillRectangle(new Rectangle(px + pad, y, pw - pad * 2, 1),
            Color.Gray * 0.4f);
        y += 10;

        // Lines
        foreach (var line in _lines)
        {
            lineFont.DrawText(spriteBatch, line,
                new Vector2(px + pad, y), Color.White * 0.9f);
            y += 22;
        }

        // "Close" label at bottom
        y = py + ph - 30;
        spriteBatch.FillRectangle(new Rectangle(px + pad, y - 2, pw - pad * 2, 1),
            Color.Gray * 0.3f);

        var closeLabelFont = FontManager.GetFont(14);
        if (closeLabelFont != null)
        {
            string closeLabel = LocaleManager.Get("ui", "close_dialogue", "Close");
            var labelSize = closeLabelFont.MeasureString(closeLabel);
            float labelX = px + pw / 2f - labelSize.X / 2f;
            closeLabelFont.DrawText(spriteBatch, closeLabel,
                new Vector2(labelX, y + 4), Color.LightGray * 0.7f);
        }

        // Close button [X]
        int cbSize = 20;
        int cbX = px + pw - cbSize - 6;
        int cbY = py + 6;

        bool pressed = _closeHovered && Mouse.GetState().LeftButton == ButtonState.Pressed;
        Color cbBg = pressed ? Color.Red : _closeHovered ? new Color(180, 40, 40) : Color.DarkRed * 0.8f;
        spriteBatch.FillRectangle(new Rectangle(cbX, cbY, cbSize, cbSize), cbBg);

        var xFont = FontManager.GetFont(12);
        if (xFont != null)
        {
            var xSize = xFont.MeasureString("X");
            xFont.DrawText(spriteBatch, "X",
                new Vector2(cbX + (cbSize - xSize.X) / 2f, cbY + (cbSize - xSize.Y) / 2f),
                Color.White);
        }
    }

    private DialogueLayout CalcLayout()
    {
        int lineCount = _lines?.Count ?? 0;
        int panelW = 320;
        int panelH = 80 + lineCount * 22 + 40; // title + lines + close label
        int panelX = GameConstants.ScreenWidth / 2 - panelW / 2;
        int panelY = GameConstants.ScreenHeight / 2 - panelH / 2;

        return new DialogueLayout { PanelX = panelX, PanelY = panelY, PanelW = panelW, PanelH = panelH };
    }

    private struct DialogueLayout
    {
        public int PanelX, PanelY, PanelW, PanelH;
    }
}
