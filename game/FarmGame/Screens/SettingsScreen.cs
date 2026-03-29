using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using MonoGame.Extended.Input;
using FarmGame.Core;

namespace FarmGame.Screens;

public enum SettingsAction
{
    Back,
    LanguageChanged
}

public class SettingsScreen
{
    private Desktop _desktop;
    private Button[] _buttons;
    private int _selectedIndex;

    // Map button index to language code (null = not a language button)
    private string[] _buttonLangs;

    public SettingsAction? SelectedAction { get; private set; }
    public string SelectedLanguage { get; private set; }

    public void Initialize()
    {
        SelectedAction = null;
        _selectedIndex = 0;
        BuildUI();
    }

    public void Rebuild()
    {
        _selectedIndex = 0;
        BuildUI();
    }

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

        // Title
        root.Widgets.Add(new Label
        {
            Text = LocaleManager.Get("ui", "settings"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Scale = new Vector2(2.5f),
            TextColor = new Color(200, 220, 200),
            Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 20),
        });

        // Language label
        root.Widgets.Add(new Label
        {
            Text = LocaleManager.Get("ui", "language"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = Color.White,
            Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 8),
        });

        // Language buttons
        var enBtn = CreateLangButton(LocaleManager.Get("ui", "lang_english"), "en");
        var zhBtn = CreateLangButton(LocaleManager.Get("ui", "lang_chinese"), "zh-TW");

        var langRow = new HorizontalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Spacing = 12,
        };
        langRow.Widgets.Add(enBtn);
        langRow.Widgets.Add(zhBtn);
        root.Widgets.Add(langRow);

        // Back button
        var backBtn = CreateButton(LocaleManager.Get("ui", "back"));
        backBtn.Click += (_, _) => SelectedAction = SettingsAction.Back;
        backBtn.Margin = new Myra.Graphics2D.Thickness(0, 20, 0, 0);
        root.Widgets.Add(backBtn);

        // Navigable buttons: English, Chinese, Back
        _buttons = new[] { enBtn, zhBtn, backBtn };
        _buttonLangs = new[] { "en", "zh-TW", null };

        _desktop = new Desktop();
        _desktop.Root = root;

        UpdateButtonFocus();
        UpdateLangHighlight();
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = KeyboardExtended.GetState();

        if (keyboard.WasKeyPressed(Keys.Up) || keyboard.WasKeyPressed(Keys.W) ||
            keyboard.WasKeyPressed(Keys.Left) || keyboard.WasKeyPressed(Keys.A))
        {
            _selectedIndex = (_selectedIndex - 1 + _buttons.Length) % _buttons.Length;
            UpdateButtonFocus();
        }

        if (keyboard.WasKeyPressed(Keys.Down) || keyboard.WasKeyPressed(Keys.S) ||
            keyboard.WasKeyPressed(Keys.Right) || keyboard.WasKeyPressed(Keys.D))
        {
            _selectedIndex = (_selectedIndex + 1) % _buttons.Length;
            UpdateButtonFocus();
        }

        if (keyboard.WasKeyPressed(Keys.Enter) || keyboard.WasKeyPressed(Keys.Space))
        {
            var lang = _buttonLangs[_selectedIndex];
            if (lang != null)
            {
                SelectedLanguage = lang;
                SelectedAction = SettingsAction.LanguageChanged;
            }
            else
            {
                SelectedAction = SettingsAction.Back;
            }
        }

        if (keyboard.WasKeyPressed(Keys.Escape))
        {
            SelectedAction = SettingsAction.Back;
        }
    }

    public void ConsumeAction()
    {
        SelectedAction = null;
    }

    public void Draw()
    {
        _desktop?.Render();
    }

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

    private void UpdateLangHighlight()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            if (_buttonLangs[i] == null) continue;
            var label = (Label)_buttons[i].Content;
            if (_buttonLangs[i] == LocaleManager.CurrentLanguage && i != _selectedIndex)
                label.TextColor = new Color(34, 200, 34);
        }
    }

    private Button CreateLangButton(string text, string lang)
    {
        var btn = CreateButton(text);
        btn.Width = 150;
        btn.Click += (_, _) =>
        {
            SelectedLanguage = lang;
            SelectedAction = SettingsAction.LanguageChanged;
        };
        return btn;
    }

    private static Button CreateButton(string text)
    {
        return new Button
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = 200,
            Height = 40,
            Content = new Label
            {
                Text = text,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            },
        };
    }
}
