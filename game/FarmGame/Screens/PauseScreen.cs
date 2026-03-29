using System;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;

namespace FarmGame.Screens;

public enum PauseMenuOption
{
    Resume,
    ExitGame
}

public class PauseScreen
{
    private Desktop _desktop;

    public PauseMenuOption? SelectedAction { get; private set; }

    public void Initialize()
    {
        SelectedAction = null;

        var overlay = new Panel();

        // Center panel
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

        // Resume button
        var resumeBtn = CreateButton("Resume");
        resumeBtn.Click += (_, _) => SelectedAction = PauseMenuOption.Resume;
        panel.Widgets.Add(resumeBtn);

        // Exit Game button
        var exitBtn = CreateButton("Exit Game");
        exitBtn.Click += (_, _) => SelectedAction = PauseMenuOption.ExitGame;
        panel.Widgets.Add(exitBtn);

        overlay.Widgets.Add(panel);

        _desktop = new Desktop();
        _desktop.Root = overlay;
    }

    public void Reset()
    {
        SelectedAction = null;
    }

    public void Update(GameTime gameTime)
    {
        SelectedAction = null;
    }

    public void Draw()
    {
        _desktop?.Render();
    }

    private static Button CreateButton(string text)
    {
        var btn = new Button
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
        return btn;
    }
}
