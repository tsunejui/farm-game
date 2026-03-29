using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.UI;
using MonoGame.Extended.Input;
using FarmGame.Core;

namespace FarmGame.Screens;

public enum TitleMenuOption
{
    StartGame,
    Settings,
    ExitGame
}

public class TitleScreen
{
    private Desktop _desktop;
    private Label _errorLabel;
    private Button[] _buttons;
    private int _selectedIndex;

    public TitleMenuOption? SelectedAction { get; private set; }

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
        };

        // Title
        root.Widgets.Add(new Label
        {
            Text = LocaleManager.Get("ui", "game_title"),
            HorizontalAlignment = HorizontalAlignment.Center,
            Scale = new Vector2(3f),
            TextColor = new Color(34, 200, 34),
            Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 40),
        });

        // Buttons
        var startBtn = CreateButton(LocaleManager.Get("ui", "start_game"));
        startBtn.Click += (_, _) => SelectedAction = TitleMenuOption.StartGame;
        root.Widgets.Add(startBtn);

        var settingsBtn = CreateButton(LocaleManager.Get("ui", "settings"));
        settingsBtn.Click += (_, _) => SelectedAction = TitleMenuOption.Settings;
        root.Widgets.Add(settingsBtn);

        var exitBtn = CreateButton(LocaleManager.Get("ui", "exit_game"));
        exitBtn.Click += (_, _) => SelectedAction = TitleMenuOption.ExitGame;
        root.Widgets.Add(exitBtn);

        _buttons = new[] { startBtn, settingsBtn, exitBtn };

        // Hint
        root.Widgets.Add(new Label
        {
            Text = LocaleManager.Get("ui", "hint_menu"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = new Color(80, 80, 80),
            Margin = new Myra.Graphics2D.Thickness(0, 30, 0, 0),
        });

        // Error label
        _errorLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = Color.Red,
        };
        root.Widgets.Add(_errorLabel);

        _desktop = new Desktop();
        _desktop.Root = root;

        UpdateButtonFocus();
    }

    public void Update(GameTime gameTime)
    {
        var keyboard = KeyboardExtended.GetState();

        if (keyboard.WasKeyPressed(Keys.Up) || keyboard.WasKeyPressed(Keys.W))
        {
            _selectedIndex = (_selectedIndex - 1 + _buttons.Length) % _buttons.Length;
            UpdateButtonFocus();
        }

        if (keyboard.WasKeyPressed(Keys.Down) || keyboard.WasKeyPressed(Keys.S))
        {
            _selectedIndex = (_selectedIndex + 1) % _buttons.Length;
            UpdateButtonFocus();
        }

        if (keyboard.WasKeyPressed(Keys.Enter) || keyboard.WasKeyPressed(Keys.Space))
        {
            SelectedAction = (TitleMenuOption)_selectedIndex;
        }
    }

    public void ConsumeAction()
    {
        SelectedAction = null;
    }

    public void SetError(string errorMessage)
    {
        if (_errorLabel != null)
            _errorLabel.Text = errorMessage ?? "";
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
            label.TextColor = i == _selectedIndex ? Color.White : new Color(120, 120, 120);
        }
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
