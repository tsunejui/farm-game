using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using MonoGame.Extended.Input;
using FarmGame.Core;

namespace FarmGame.Screens;

public class PauseScreen : IScreen
{
    private Desktop _desktop;
    private Button[] _buttons;
    private int _selectedIndex;

    public void Initialize() { _selectedIndex = 0; BuildUI(); }
    public void Rebuild() { _selectedIndex = 0; BuildUI(); }

    public void Reset() { _selectedIndex = 0; UpdateButtonFocus(); }

    private void BuildUI()
    {
        var panel = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 12,
            Padding = new Myra.Graphics2D.Thickness(30, 20),
            Background = new SolidBrush(new Color(30, 40, 30)),
            Border = new SolidBrush(new Color(80, 120, 80)),
            BorderThickness = new Myra.Graphics2D.Thickness(2),
            Width = 300,
        };

        var title = UIHelper.CreateLabel(LocaleManager.Get("ui", "paused"), 28);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.TextColor = new Color(200, 220, 200);
        title.Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 20);
        panel.Widgets.Add(title);

        var resumeBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "resume"));
        panel.Widgets.Add(resumeBtn);

        var exitBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "exit_game"));
        panel.Widgets.Add(exitBtn);

        _buttons = new[] { resumeBtn, exitBtn };

        _desktop = new Desktop { Root = new Panel { Widgets = { panel } } };
        UpdateButtonFocus();
    }

    public ScreenTransition Update(GameTime gameTime)
    {
        var kb = KeyboardExtended.GetState();
        if (kb.WasKeyPressed(Keys.Up) || kb.WasKeyPressed(Keys.W))
        { _selectedIndex = (_selectedIndex - 1 + _buttons.Length) % _buttons.Length; UpdateButtonFocus(); }
        if (kb.WasKeyPressed(Keys.Down) || kb.WasKeyPressed(Keys.S))
        { _selectedIndex = (_selectedIndex + 1) % _buttons.Length; UpdateButtonFocus(); }

        if (kb.WasKeyPressed(Keys.Enter) || kb.WasKeyPressed(Keys.Space))
        {
            return _selectedIndex == 0
                ? ScreenTransition.To(GameState.Playing)
                : ScreenTransition.ExitGame();
        }

        if (kb.WasKeyPressed(Keys.Escape))
            return ScreenTransition.To(GameState.Playing);

        return ScreenTransition.None;
    }

    public void Draw(SpriteBatch spriteBatch) { _desktop?.Render(); }

    private void UpdateButtonFocus()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            var label = (Label)_buttons[i].Content;
            label.TextColor = i == _selectedIndex ? Color.White : new Color(120, 120, 120);
        }
    }
}
