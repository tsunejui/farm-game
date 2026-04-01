using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace FarmGame.Services;

/// <summary>
/// Loads textures with caching. Tries raw PNG first, falls back to XNB pipeline.
/// </summary>
public class AssetService : IAssetService
{
    private readonly Dictionary<string, Texture2D> _cache = new();
    private readonly ContentManager _contentManager;

    public GraphicsDevice GraphicsDevice { get; }
    public string ContentDir { get; }

    public AssetService(GraphicsDevice graphicsDevice, ContentManager contentManager, string contentDir)
    {
        GraphicsDevice = graphicsDevice;
        _contentManager = contentManager;
        ContentDir = contentDir;
    }

    public Texture2D LoadTexture(string path)
    {
        if (_cache.TryGetValue(path, out var cached))
            return cached;

        Texture2D texture;
        var pngPath = Path.Combine(ContentDir, path + ".png");

        if (File.Exists(pngPath))
        {
            using var stream = File.OpenRead(pngPath);
            texture = Texture2D.FromStream(GraphicsDevice, stream);
        }
        else
        {
            texture = _contentManager.Load<Texture2D>(path);
        }

        _cache[path] = texture;
        return texture;
    }

    public void UnloadAll()
    {
        foreach (var tex in _cache.Values)
            tex.Dispose();
        _cache.Clear();
    }
}
