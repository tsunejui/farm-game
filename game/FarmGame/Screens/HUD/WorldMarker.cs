// =============================================================================
// WorldMarker.cs — Gold bracket marker drawn around selected object
//
// Rendered in world space (inside camera transform).
// =============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using FarmGame.Core;
using FarmGame.World;

namespace FarmGame.Screens.HUD;

public class WorldMarker
{
    public void Draw(SpriteBatch spriteBatch, WorldObject selected)
    {
        if (selected == null) return;

        int ts = GameConstants.TileSize;
        int px = selected.TileX * ts;
        int py = selected.TileY * ts;
        int pw = selected.EffectiveWidth * ts;
        int ph = selected.EffectiveHeight * ts;

        // Gold underline
        spriteBatch.FillRectangle(new Rectangle(px, py + ph + 1, pw, 3), Color.Gold);

        // Gold corner brackets
        int bl = 6;
        var gold = Color.Gold * 0.8f;
        spriteBatch.FillRectangle(new Rectangle(px - 1, py - 1, bl, 1), gold);
        spriteBatch.FillRectangle(new Rectangle(px - 1, py - 1, 1, bl), gold);
        spriteBatch.FillRectangle(new Rectangle(px + pw - bl + 1, py - 1, bl, 1), gold);
        spriteBatch.FillRectangle(new Rectangle(px + pw, py - 1, 1, bl), gold);
        spriteBatch.FillRectangle(new Rectangle(px - 1, py + ph, bl, 1), gold);
        spriteBatch.FillRectangle(new Rectangle(px - 1, py + ph - bl + 1, 1, bl), gold);
        spriteBatch.FillRectangle(new Rectangle(px + pw - bl + 1, py + ph, bl, 1), gold);
        spriteBatch.FillRectangle(new Rectangle(px + pw, py + ph - bl + 1, 1, bl), gold);
    }
}
