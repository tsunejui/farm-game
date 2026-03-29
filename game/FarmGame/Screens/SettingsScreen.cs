using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using MonoGame.Extended.Input;
using FarmGame.Core;

namespace FarmGame.Screens;

public class SettingsScreen : IScreen
{
    private Desktop _desktop;
    private Button[] _buttons;
    private string[] _buttonLangs;
    private int _selectedIndex;

    public Action<string> OnLanguageChanged { get; set; }
    public GameState ReturnState { get; set; } = GameState.TitleScreen;

    public void Initialize() { _selectedIndex = 0; BuildUI(); }
    public void Rebuild() { _selectedIndex = 0; BuildUI(); }
    public void OnEnter(GameState fromState) { ReturnState = fromState; Rebuild(); }

    private void BuildUI()
    {
        var root = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 16,
            Padding = new Myra.Graphics2D.Thickness(40, 30),
            Background = new SolidBrush(new Color(20, 30, 20)),
            Border = new SolidBrush(new Color(60, 100, 60)),
            BorderThickness = new Myra.Graphics2D.Thickness(2),
            Width = 400,
        };

        var title = UIHelper.CreateLabel(LocaleManager.Get("ui", "settings"), 28);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.TextColor = new Color(200, 220, 200);
        title.Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 20);
        root.Widgets.Add(title);

        var langLabel = UIHelper.CreateLabel(LocaleManager.Get("ui", "language"));
        langLabel.HorizontalAlignment = HorizontalAlignment.Center;
        langLabel.Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 8);
        root.Widgets.Add(langLabel);

        var enBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "lang_english"), 150);
        enBtn.Click += (_, _) => OnLanguageChanged?.Invoke("en");

        var zhBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "lang_chinese"), 150);
        zhBtn.Click += (_, _) => OnLanguageChanged?.Invoke("zh-TW");

        var langRow = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 12,
        };
        langRow.Widgets.Add(enBtn);
        langRow.Widgets.Add(zhBtn);
        root.Widgets.Add(langRow);

        var backBtn = UIHelper.CreateButton(LocaleManager.Get("ui", "back"));
        backBtn.Margin = new Myra.Graphics2D.Thickness(0, 20, 0, 0);
        root.Widgets.Add(backBtn);

        _buttons = new[] { enBtn, zhBtn, backBtn };
        _buttonLangs = new[] { "en", "zh-TW", null };

        _desktop = new Desktop { Root = root };
        UpdateButtonFocus();
    }

    public ScreenTransition Update(GameTime gameTime)
    {
        var kb = KeyboardExtended.GetState();
        if (kb.WasKeyPressed(Keys.Up) || kb.WasKeyPressed(Keys.W) ||
            kb.WasKeyPressed(Keys.Left) || kb.WasKeyPressed(Keys.A))
        { _selectedIndex = (_selectedIndex - 1 + _buttons.Length) % _buttons.Length; UpdateButtonFocus(); }
        if (kb.WasKeyPressed(Keys.Down) || kb.WasKeyPressed(Keys.S) ||
            kb.WasKeyPressed(Keys.Right) || kb.WasKeyPressed(Keys.D))
        { _selectedIndex = (_selectedIndex + 1) % _buttons.Length; UpdateButtonFocus(); }

        if (kb.WasKeyPressed(Keys.Enter) || kb.WasKeyPressed(Keys.Space))
        {
            var lang = _buttonLangs[_selectedIndex];
            if (lang != null)
            {
                OnLanguageChanged?.Invoke(lang);
                return ScreenTransition.None;
            }
            return ScreenTransition.To(ReturnState);
        }

        if (kb.WasKeyPressed(Keys.Escape))
            return ScreenTransition.To(ReturnState);

        return ScreenTransition.None;
    }

    public void Draw(SpriteBatch spriteBatch) { _desktop?.Render(); }

    private void UpdateButtonFocus()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            var label = (Label)_buttons[i].Content;
            if (i == _selectedIndex)
                label.TextColor = Color.White;
            else if (_buttonLangs[i] != null && _buttonLangs[i] == LocaleManager.CurrentLanguage)
                label.TextColor = new Color(34, 200, 34);
            else
                label.TextColor = new Color(120, 120, 120);
        }
    }
}
