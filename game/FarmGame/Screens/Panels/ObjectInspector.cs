// =============================================================================
// ObjectInspector.cs — Coordinator for mouse-driven object inspection HUD
//
// Delegates rendering to:
//   - InfoPanel:    bottom-right instant info (hover tooltip)
//   - StatusPanel:  top-center object status (click inspect)
//   - WorldMarker:  gold bracket marker in world space
// =============================================================================

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FarmGame.Camera;
using FarmGame.Core;
using FarmGame.World;

using FarmGame.Screens.HUD;

namespace FarmGame.Screens.Panels;

public class ObjectInspector
{
    private readonly InfoPanel _infoPanel = new();
    private readonly StatusPanel _statusPanel = new();
    private readonly WorldMarker _worldMarker = new();

    private WorldObject _hoveredObject;
    private WorldObject _selectedObject;
    private WorldObject _playerObject;
    private MouseState _prevMouse;
    private string _hoveredEffectDesc;

    public WorldObject SelectedObject => _selectedObject;

    public void SetPlayerObject(WorldObject playerObj) { _playerObject = playerObj; }

    public void Update(GameMap map, Camera2D camera)
    {
        var mouse = Mouse.GetState();
        bool clicked = mouse.LeftButton == ButtonState.Pressed &&
                       _prevMouse.LeftButton == ButtonState.Released;

        _hoveredEffectDesc = null;

        // Status panel interaction (close button + effect hover)
        _hoveredEffectDesc = _statusPanel.UpdateInteraction(mouse, clicked, _selectedObject);

        // Close button click
        if (clicked && _statusPanel.CloseHovered && _selectedObject != null)
        {
            _selectedObject = null;
            _prevMouse = mouse;
            return;
        }

        // World-space hover/click
        var worldPos = camera.ScreenToWorld(new Vector2(mouse.X, mouse.Y));
        int tileX = (int)worldPos.X / GameConstants.TileSize;
        int tileY = (int)worldPos.Y / GameConstants.TileSize;

        _hoveredObject = map.GetObjectAt(tileX, tileY);
        if (_hoveredObject == null && _playerObject != null &&
            tileX == _playerObject.TileX && tileY == _playerObject.TileY)
            _hoveredObject = _playerObject;

        if (clicked && _hoveredObject != null)
            _selectedObject = _hoveredObject;

        _prevMouse = mouse;
    }

    // Draw gold marker in world space (call inside camera transform)
    public void DrawWorldMarker(SpriteBatch spriteBatch)
    {
        _worldMarker.Draw(spriteBatch, _selectedObject);
    }

    // Draw screen-space HUD elements
    public void DrawHUD(SpriteBatch spriteBatch)
    {
        _infoPanel.Draw(spriteBatch, _hoveredObject, _hoveredEffectDesc);
        _statusPanel.Draw(spriteBatch, _selectedObject);
    }
}
