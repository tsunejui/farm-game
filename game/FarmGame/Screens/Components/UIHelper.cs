using Myra.Graphics2D.UI;

namespace FarmGame.Screens.Components;

/// <summary>
/// Convenience facade — delegates to individual component classes.
/// Prefer using LabelComponent, ButtonComponent, TitleComponent directly.
/// </summary>
public static class UIHelper
{
    public static Label CreateLabel(string text, int fontSize = 20)
        => LabelComponent.Create(text, fontSize);

    public static Button CreateButton(string text, int width = 200, int height = 40)
        => ButtonComponent.Create(text, width, height);

    public static Label CreateTitle(string text)
        => TitleComponent.Create(text);
}
