using Myra.Graphics2D.UI;

namespace FarmGame.Views.Components;

public static class ButtonComponent
{
    public static Button Create(string text, int width = 200, int height = 40)
    {
        var label = LabelComponent.Create(text);
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
}
