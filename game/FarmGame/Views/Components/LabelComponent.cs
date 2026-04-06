using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using FarmGame.Core;
using FarmGame.Core.Managers;

namespace FarmGame.Views.Components;

public static class LabelComponent
{
    private const int DefaultFontSize = 20;

    public static Label Create(string text, int fontSize = DefaultFontSize)
    {
        return new Label
        {
            Text = text,
            Font = FontManager.GetFont(fontSize),
            TextColor = Color.White,
        };
    }
}
