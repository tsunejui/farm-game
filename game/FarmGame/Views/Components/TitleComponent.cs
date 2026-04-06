using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using FarmGame.Core;
using FarmGame.Core.Managers;

namespace FarmGame.Views.Components;

public static class TitleComponent
{
    private const int LargeFontSize = 32;

    public static Label Create(string text)
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
