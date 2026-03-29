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
    private ScreenTransition _pendingTransition;

    public void Initialize() { _selectedIndex = 0; BuildUI(); }
    public void Rebuild() { _selectedIndex = 0; BuildUI(); }
    public void OnEnter(GameState fromState) { Reset(); }

    private void Reset() { _selectedIndex = 0; UpdateButtonFocus(); }

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

        _pendingTransition = null;

        var resumeBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "resume"));
        resumeBtn.Click += (_, _) => _pendingTransition = ScreenTransition.To(GameState.Playing);
        panel.Widgets.Add(resumeBtn);

        var settingsBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "settings"));
        settingsBtn.Click += (_, _) => _pendingTransition = ScreenTransition.To(GameState.Settings);
        panel.Widgets.Add(settingsBtn);

        var exitBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "exit_game"));
        exitBtn.Click += (_, _) => _pendingTransition = ScreenTransition.To(GameState.TitleScreen);
        panel.Widgets.Add(exitBtn);

        _buttons = new[] { resumeBtn, settingsBtn, exitBtn };

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
            return _selectedIndex switch
            {
                0 => ScreenTransition.To(GameState.Playing),
                1 => ScreenTransition.To(GameState.Settings),
                _ => ScreenTransition.To(GameState.TitleScreen),
            };
        }

        if (kb.WasKeyPressed(Keys.Escape))
            return ScreenTransition.To(GameState.Playing);

        if (_pendingTransition != null)
        {
            var t = _pendingTransition;
            _pendingTransition = null;
            return t;
        }

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
