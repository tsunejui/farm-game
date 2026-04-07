// =============================================================================
// GameMenuPanel.cs — In-game menu overlay (ESC key)
//
// Drawn on top of the running game world. Options:
//   - Resume: close this panel
//   - Settings: open settings screen
//   - Leave Game: save and return to title
// =============================================================================

using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input;
using FarmGame.Core;
using FarmGame.Core.Managers;

namespace FarmGame.Views.Panels;

public class GameMenuPanel
{
    private static readonly string[] MenuItems = { "Resume", "Settings", "Leave Game" };
    private int _selectedIndex;
    private bool _isOpen;
    private ButtonState _prevMouseLeft = ButtonState.Released;

    // Cached item rectangles for mouse hit testing (set during Draw)
    private readonly Rectangle[] _itemRects = new Rectangle[MenuItems.Length];

    public bool IsOpen => _isOpen;

    /// <summary>Fired when "Leave Game" is selected.</summary>
    public Action OnLeaveGame { get; set; }

    /// <summary>Fired when "Settings" is selected.</summary>
    public Action OnSettings { get; set; }

    public void Toggle()
    {
        _isOpen = !_isOpen;
        if (_isOpen)
            _selectedIndex = 0;
    }

    public void Open()
    {
        _isOpen = true;
        _selectedIndex = 0;
    }

    public void Close() => _isOpen = false;

    public void Update()
    {
        if (!_isOpen) return;

        var keyboard = KeyboardExtended.GetState();

        if (keyboard.WasKeyPressed(Keys.Escape))
        {
            Close();
            return;
        }

        // Keyboard navigation
        if (keyboard.WasKeyPressed(Keys.Up) || keyboard.WasKeyPressed(Keys.W))
            _selectedIndex = (_selectedIndex - 1 + MenuItems.Length) % MenuItems.Length;
        if (keyboard.WasKeyPressed(Keys.Down) || keyboard.WasKeyPressed(Keys.S))
            _selectedIndex = (_selectedIndex + 1) % MenuItems.Length;

        // Keyboard confirm
        if (keyboard.WasKeyPressed(Keys.Enter) || keyboard.WasKeyPressed(Keys.Z))
        {
            ExecuteSelected();
            return;
        }

        // Mouse hover and click (only trigger on press, not hold)
        var mouse = Mouse.GetState();
        var mousePos = new Point(mouse.X, mouse.Y);
        bool mouseJustPressed = mouse.LeftButton == ButtonState.Pressed
            && _prevMouseLeft == ButtonState.Released;
        _prevMouseLeft = mouse.LeftButton;

        for (int i = 0; i < _itemRects.Length; i++)
        {
            if (_itemRects[i].Contains(mousePos))
            {
                _selectedIndex = i;

                if (mouseJustPressed)
                {
                    ExecuteSelected();
                    return;
                }
            }
        }
    }

    private void ExecuteSelected()
    {
        switch (_selectedIndex)
        {
            case 0: Close(); break;               // Resume
            case 1: OnSettings?.Invoke(); break;   // Settings
            case 2: OnLeaveGame?.Invoke(); break;  // Leave Game
        }
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        if (!_isOpen) return;

        int screenW = GameConstants.ScreenWidth;
        int screenH = GameConstants.ScreenHeight;

        // Dim overlay
        spriteBatch.FillRectangle(new Rectangle(0, 0, screenW, screenH), Color.Black * 0.5f);

        // Panel
        int panelW = 240;
        int panelH = 40 + MenuItems.Length * 40;
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2;

        spriteBatch.FillRectangle(new Rectangle(panelX, panelY, panelW, panelH),
            new Color(30, 40, 30));
        spriteBatch.DrawRectangle(new Rectangle(panelX, panelY, panelW, panelH),
            new Color(80, 120, 80), 2);

        // Title
        var titleFont = FontManager.GetFont(24);
        if (titleFont != null)
        {
            string title = LocaleManager.Get("ui", "menu", "Menu");
            var titleSize = titleFont.MeasureString(title);
            titleFont.DrawText(spriteBatch, title,
                new Vector2(panelX + (panelW - titleSize.X) / 2, panelY + 8),
                new Color(34, 200, 34));
        }

        // Menu items
        var font = FontManager.GetFont(20);
        if (font == null) return;

        for (int i = 0; i < MenuItems.Length; i++)
        {
            string label = MenuItems[i] switch
            {
                "Resume" => LocaleManager.Get("ui", "resume", "Resume"),
                "Settings" => LocaleManager.Get("ui", "settings", "Settings"),
                "Leave Game" => LocaleManager.Get("ui", "leave_game", "Leave Game"),
                _ => MenuItems[i],
            };

            int itemY = panelY + 40 + i * 40;
            bool selected = i == _selectedIndex;
            Color color = selected ? Color.White : Color.Gray;

            // Cache item rect for mouse hit testing
            _itemRects[i] = new Rectangle(panelX + 4, itemY, panelW - 8, 36);

            if (selected)
            {
                spriteBatch.FillRectangle(_itemRects[i], new Color(50, 70, 50));
            }

            var textSize = font.MeasureString(label);
            font.DrawText(spriteBatch, label,
                new Vector2(panelX + (panelW - textSize.X) / 2, itemY + 8),
                color);
        }
    }
}
