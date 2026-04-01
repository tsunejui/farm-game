using Microsoft.Xna.Framework.Graphics;

namespace FarmGame.Services;

/// <summary>
/// Abstraction for loading and managing game assets (textures, etc.).
/// Controllers and services use this instead of touching file paths directly.
/// </summary>
public interface IAssetService
{
    /// <summary>Load a texture by logical path (no extension). Cached.</summary>
    Texture2D LoadTexture(string path);

    /// <summary>Release all cached textures.</summary>
    void UnloadAll();

    /// <summary>Absolute path to the Content directory (for raw file access).</summary>
    string ContentDir { get; }

    /// <summary>The GraphicsDevice for texture creation.</summary>
    GraphicsDevice GraphicsDevice { get; }
}
