using System.IO;
using Microsoft.Xna.Framework.Graphics;
using FontStashSharp;
using Serilog;

namespace FarmGame.Core.Managers;

public static class FontManager
{
    private static FontSystem _fontSystem;

    public static void Initialize(GraphicsDevice graphicsDevice, string contentDir)
    {
        var fontPath = Path.Combine(contentDir, "Fonts", "NotoSansCJKtc-Regular.otf");
        if (File.Exists(fontPath))
        {
            var fontData = File.ReadAllBytes(fontPath);
            _fontSystem = new FontSystem();
            _fontSystem.AddFont(fontData);
            Log.Information("Font loaded: {Path}", fontPath);
        }
        else
        {
            _fontSystem = new FontSystem();
            Log.Warning("Font not found: {Path}", fontPath);
        }
    }

    public static SpriteFontBase GetFont(int size)
    {
        return _fontSystem.GetFont(size);
    }

    public static FontSystem GetFontSystem()
    {
        return _fontSystem;
    }
}
