using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using MonoGame.Extended.Input;

namespace FarmGame.Screens;

public enum PauseMenuOption
{
    Resume,
    ExitGame
}

public class PauseScreen
{
    private Desktop _desktop;
    private Button[] _buttons;
    private int _selectedIndex;

    public PauseMenuOption? SelectedAction { get; private set; }

    public void Initialize()
    {
        SelectedAction = null;
        _selectedIndex = 0;

        var overlay = new Panel();

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

        // Title
        panel.Widgets.Add(new Label
        {
            Text = "Paused",
            HorizontalAlignment = HorizontalAlignment.Center,
            Scale = new Vector2(2f),
            TextColor = new Color(200, 220, 200),
            Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 20),
        });

        // Buttons
        var resumeBtn = CreateButton("Resume");
        resumeBtn.Click += (_, _) => SelectedAction = PauseMenuOption.Resume;
        panel.Widgets.Add(resumeBtn);

        var exitBtn = CreateButton("Exit Game");
        exitBtn.Click += (_, _) => SelectedAction = PauseMenuOption.ExitGame;
        panel.Widgets.Add(exitBtn);

        _buttons = new[] { resumeBtn, exitBtn };

        overlay.Widgets.Add(panel);

        _desktop = new Desktop();
        _desktop.Root = overlay;

        UpdateButtonFocus();
    }

    public void Reset()
    {
        SelectedAction = null;
        _selectedIndex = 0;
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
            SelectedAction = (PauseMenuOption)_selectedIndex;
        }

        if (keyboard.WasKeyPressed(Keys.Escape))
        {
            SelectedAction = PauseMenuOption.Resume;
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
