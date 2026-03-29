using System;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D.UI;

namespace FarmGame.Screens;

public enum TitleMenuOption
{
    StartGame,
    ExitGame
}

public class TitleScreen
{
    private Desktop _desktop;
    private Label _errorLabel;

    public TitleMenuOption? SelectedAction { get; private set; }

    public void Initialize()
    {
        SelectedAction = null;

        var root = new VerticalStackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 16,
        };

        // Title
        root.Widgets.Add(new Label
        {
            Text = "Farm Game",
            HorizontalAlignment = HorizontalAlignment.Center,
            Scale = new Vector2(3f),
            TextColor = new Color(34, 200, 34),
            Margin = new Myra.Graphics2D.Thickness(0, 0, 0, 40),
        });

        // Start Game button
        var startBtn = CreateButton("Start Game");
        startBtn.Click += (_, _) => SelectedAction = TitleMenuOption.StartGame;
        root.Widgets.Add(startBtn);

        // Exit Game button
        var exitBtn = CreateButton("Exit Game");
        exitBtn.Click += (_, _) => SelectedAction = TitleMenuOption.ExitGame;
        root.Widgets.Add(exitBtn);

        // Hint
        root.Widgets.Add(new Label
        {
            Text = "Click to select",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = new Color(80, 80, 80),
            Margin = new Myra.Graphics2D.Thickness(0, 30, 0, 0),
        });

        // Error label (hidden by default)
        _errorLabel = new Label
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = Color.Red,
        };
        root.Widgets.Add(_errorLabel);

        _desktop = new Desktop();
        _desktop.Root = root;
    }

    public void Update(GameTime gameTime)
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
