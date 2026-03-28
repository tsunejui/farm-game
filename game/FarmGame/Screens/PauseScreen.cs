using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Input;
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
    private float _animTimer;

    public PauseMenuOption? SelectedAction { get; private set; }

    public void Reset()
    {
        _selectedIndex = 0;
        SelectedAction = null;
    }

    public void Update(GameTime gameTime)
    {
        _animTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        SelectedAction = null;

        var keyboard = KeyboardExtended.GetState();

        if (keyboard.WasKeyPressed(Keys.Up) || keyboard.WasKeyPressed(Keys.W))
            _selectedIndex = (_selectedIndex - 1 + _menuLabels.Length) % _menuLabels.Length;

        if (keyboard.WasKeyPressed(Keys.Down) || keyboard.WasKeyPressed(Keys.S))
            _selectedIndex = (_selectedIndex + 1) % _menuLabels.Length;

        if (keyboard.WasKeyPressed(Keys.Enter) || keyboard.WasKeyPressed(Keys.Space))
            SelectedAction = (PauseMenuOption)_selectedIndex;

        if (keyboard.WasKeyPressed(Keys.Escape))
            SelectedAction = PauseMenuOption.Resume;
    }

    public void Draw(SpriteBatch spriteBatch, SpriteFont font)
    {
        int screenW = GameConstants.ScreenWidth;
        int screenH = GameConstants.ScreenHeight;

        // Semi-transparent dark overlay
        spriteBatch.FillRectangle(
            new Rectangle(0, 0, screenW, screenH),
            new Color(0, 0, 0, 180));

        // Panel background
        int panelW = 300;
        int panelH = 200;
        int panelX = (screenW - panelW) / 2;
        int panelY = (screenH - panelH) / 2;
        spriteBatch.FillRectangle(
            new Rectangle(panelX, panelY, panelW, panelH),
            new Color(30, 40, 30));
        // Panel border
        spriteBatch.DrawRectangle(
            new Rectangle(panelX, panelY, panelW, panelH),
            new Color(80, 120, 80), 2);

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
                spriteBatch.FillRectangle(
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

}
