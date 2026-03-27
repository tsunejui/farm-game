using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FarmGame.Core;

namespace FarmGame.Screens;

public enum PauseMenuOption
{
    Resume,
    ExitGame
}

public class PauseScreen
{
    private readonly string[] _menuLabels = { "Resume", "Exit Game" };
    private int _selectedIndex;
    private KeyboardState _previousKeyboard;
    private float _animTimer;

    public PauseMenuOption? SelectedAction { get; private set; }

    public void Reset()
    {
        _selectedIndex = 0;
        SelectedAction = null;
        _previousKeyboard = Keyboard.GetState();
    }

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
            SelectedAction = (PauseMenuOption)_selectedIndex;

        if (IsKeyPressed(keyboard, Keys.Escape))
            SelectedAction = PauseMenuOption.Resume;

        _previousKeyboard = keyboard;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        int screenW = GameConstants.ScreenWidth;
        int screenH = GameConstants.ScreenHeight;

        // Semi-transparent dark overlay
        spriteBatch.Draw(pixel,
            new Rectangle(0, 0, screenW, screenH),
            new Color(0, 0, 0, 180));

        // Panel background
        int panelW = 300;
        int panelH = 200;
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2;
        spriteBatch.Draw(pixel,
            new Rectangle(panelX, panelY, panelW, panelH),
            new Color(30, 40, 30));
        // Panel border
        DrawBorder(spriteBatch, pixel, panelX, panelY, panelW, panelH, 2, new Color(80, 120, 80));

        // Title
        string title = "Paused";
        var titleSize = font.MeasureString(title);
        float titleScale = 2f;
        spriteBatch.DrawString(font, title,
            new Vector2((screenW - titleSize.X * titleScale) / 2f, panelY + 20f),
            new Color(200, 220, 200),
            0f, Vector2.Zero, titleScale, SpriteEffects.None, 0f);

        // Menu options
        float menuStartY = panelY + 80f;
        float menuSpacing = 45f;

        for (int i = 0; i < _menuLabels.Length; i++)
        {
            bool isSelected = i == _selectedIndex;
            string label = _menuLabels[i];
            float scale = isSelected ? 1.5f : 1.2f;
            var labelSize = font.MeasureString(label);
            float x = (screenW - labelSize.X * scale) / 2f;
            float y = menuStartY + i * menuSpacing;

            Color color = isSelected ? Color.White : new Color(120, 120, 120);

            if (isSelected)
            {
                int barWidth = (int)(labelSize.X * scale) + 30;
                int barHeight = (int)(labelSize.Y * scale) + 8;
                spriteBatch.Draw(pixel,
                    new Rectangle((screenW - barWidth) / 2, (int)y - 4, barWidth, barHeight),
                    new Color(34, 139, 34, 80));

                if (_animTimer % 1f < 0.7f)
                {
                    spriteBatch.DrawString(font, ">",
                        new Vector2(x - 25f, y), Color.White,
                        0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                }
            }

            spriteBatch.DrawString(font, label,
                new Vector2(x + 2f, y + 2f), new Color(0, 0, 0, 100),
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            spriteBatch.DrawString(font, label,
                new Vector2(x, y), color,
                0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }

    private static void DrawBorder(SpriteBatch spriteBatch, Texture2D pixel,
        int x, int y, int w, int h, int thickness, Color color)
    {
        spriteBatch.Draw(pixel, new Rectangle(x, y, w, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(x, y + h - thickness, w, thickness), color);
        spriteBatch.Draw(pixel, new Rectangle(x, y, thickness, h), color);
        spriteBatch.Draw(pixel, new Rectangle(x + w - thickness, y, thickness, h), color);
    }

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && _previousKeyboard.IsKeyUp(key);
    }
}
