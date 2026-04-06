// =============================================================================
// ObjectManager.cs — Manages game objects, player, and camera
//
// Holds references to Player, Camera2D, and delegates to GameMap
// for entity management. Provides save/restore operations.
// =============================================================================

using FarmGame.Camera;
using FarmGame.Entities;

namespace FarmGame.World;

public class ObjectManager
{
    public Player Player { get; set; }
    public Camera2D Camera { get; set; }

    /// <summary>Update player, camera following, and map entities.</summary>
    public void Update(GameMap map, float deltaTime,
        Microsoft.Xna.Framework.GameTime gameTime, bool inputBlocked)
    {
        if (Player != null && gameTime != null)
        {
            Player.InputBlocked = inputBlocked;
            Player.Update(gameTime);
        }

        map?.Update(deltaTime);

        if (Camera != null && Player != null)
            Camera.Update(Player);
    }
}
