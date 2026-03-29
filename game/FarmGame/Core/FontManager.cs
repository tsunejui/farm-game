using System.IO;
using Microsoft.Xna.Framework.Graphics;
using AssetManagementBase;
using FontStashSharp;
using Myra;
using Serilog;

namespace FarmGame.Core;

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

            // Set Myra's default asset manager to load from Fonts directory
            // so the stylesheet can find our Chinese-capable font
            var fontsDir = Path.Combine(contentDir, "Fonts");
            MyraEnvironment.DefaultAssetManager =
                AssetManager.CreateFileAssetManager(fontsDir);

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
}
