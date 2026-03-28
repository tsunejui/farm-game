using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FarmGame.Core;

namespace FarmGame.Screens;

public enum TitleMenuOption
{
    StartGame,
    ExitGame
}

public class TitleScreen
{
    private readonly string[] _menuLabels = { "Start Game", "Exit Game" };
    private int _selectedIndex;
    private KeyboardState _previousKeyboard;
    private float _animTimer;

    public TitleMenuOption? SelectedAction { get; private set; }

    public void Update(GameTime gameTime)
    {
        _animTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        SelectedAction = null;

        var keyboard = Keyboard.GetState();

        if (IsKeyPressed(keyboard, Keys.Up) || IsKeyPressed(keyboard, Keys.W))
            _selectedIndex = (_selectedIndex - 1 + _menuLabels.Length) % _menuLabels.Length;

        if (IsKeyPressed(keyboard, Keys.Down) || IsKeyPressed(keyboard, Keys.S))
            _selectedIndex = (_selectedIndex + 1) % _menuLabels.Length;

        if (IsKeyPressed(keyboard, Keys.Enter) || IsKeyPressed(keyboard, Keys.Space))
            SelectedAction = (TitleMenuOption)_selectedIndex;

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, string errorMessage = null)
    {
        int screenW = GameConstants.ScreenWidth;
        int screenH = GameConstants.ScreenHeight;

        // Dark background
        spriteBatch.Draw(pixel,
            new Rectangle(0, 0, screenW, screenH),
            new Color(20, 30, 20));

        // Title text
        string title = "Farm Game";
        var titleSize = font.MeasureString(title);
        float titleScale = 3f;
        var titlePos = new Vector2(
            (screenW - titleSize.X * titleScale) / 2f,
            screenH * 0.2f);
        spriteBatch.DrawString(font, title, titlePos + Vector2.One * 3f, new Color(0, 0, 0, 120), 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);
        spriteBatch.DrawString(font, title, titlePos, new Color(34, 200, 34), 0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

        // Menu options
        float menuStartY = screenH * 0.55f;
        float menuSpacing = 50f;

        for (int i = 0; i < _menuLabels.Length; i++)
        {
            bool isSelected = i == _selectedIndex;
            string label = _menuLabels[i];
            float scale = isSelected ? 1.8f : 1.5f;
            var labelSize = font.MeasureString(label);
            float x = (screenW - labelSize.X * scale) / 2f;
            float y = menuStartY + i * menuSpacing;

            Color color = isSelected ? Color.White : new Color(120, 120, 120);

            // Selected highlight bar
            if (isSelected)
            {
                int barWidth = (int)(labelSize.X * scale) + 40;
                int barHeight = (int)(labelSize.Y * scale) + 10;
                spriteBatch.Draw(pixel,
                    new Rectangle((screenW - barWidth) / 2, (int)y - 5, barWidth, barHeight),
                    new Color(34, 139, 34, 80));

                // Blinking arrow indicator
                if (_animTimer % 1f < 0.7f)
                {
                    string arrow = ">";
                    spriteBatch.DrawString(font, arrow,
                        new Vector2(x - 30f, y), Color.White,
                        0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }

            // Shadow
            spriteBatch.DrawString(font, label,
                new Vector2(x + 2f, y + 2f), new Color(0, 0, 0, 100),
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            // Text
            spriteBatch.DrawString(font, label,
                new Vector2(x, y), color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        // Error message above hint
        if (!string.IsNullOrEmpty(errorMessage))
        {
            var errorSize = font.MeasureString(errorMessage);
            spriteBatch.DrawString(font, errorMessage,
                new Vector2((screenW - errorSize.X) / 2f, screenH - 70f),
                Color.Red);
        }

        // Hint at bottom
        string hint = "Use W/S or Arrow Keys to select, Enter to confirm";
        var hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint,
            new Vector2((screenW - hintSize.X) / 2f, screenH - 40f),
            new Color(80, 80, 80));
    }

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }
}
