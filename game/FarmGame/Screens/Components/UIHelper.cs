using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using FarmGame.Core;

namespace FarmGame.Screens.Components;

public static class UIHelper
{
    private const int DefaultFontSize = 20;
    private const int LargeFontSize = 32;

    public static Label CreateLabel(string text, int fontSize = DefaultFontSize)
    {
        return new Label
        {
            Text = text,
            Font = FontManager.GetFont(fontSize),
            TextColor = Color.White,
        };
    }

    public static Button CreateButton(string text, int width = 200, int height = 40)
    {
        var label = CreateLabel(text);
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.VerticalAlignment = VerticalAlignment.Center;

        return new Button
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Width = width,
            Height = height,
            Content = label,
        };
    }

    public static Label CreateTitle(string text)
    {
        return new Label
        {
            Text = text,
            Font = FontManager.GetFont(LargeFontSize),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = new Color(34, 200, 34),
        };
    }
}
