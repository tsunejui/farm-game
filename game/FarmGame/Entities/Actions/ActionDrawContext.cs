using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FarmGame.Entities.Actions;

public readonly struct ActionDrawContext
{
    public SpriteBatch SpriteBatch { get; init; }
    public Texture2D Pixel { get; init; }
    public Vector2 PixelPosition { get; init; }
    public Direction FacingDirection { get; init; }
    public float YOffset { get; init; }
}
