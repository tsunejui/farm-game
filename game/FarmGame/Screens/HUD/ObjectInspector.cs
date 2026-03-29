// =============================================================================
// ObjectInspector.cs — Mouse-driven object inspection HUD
//
// Hover object: info panel at bottom-right (name, category, death status)
// Hover effect icon: info panel shows effect description
// Click object: status panel at top-center (name, HP, effects)
// =============================================================================

using System;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using FarmGame.Camera;
using FarmGame.Core;
using FarmGame.World;
using FarmGame.World.Effects;

namespace FarmGame.Screens.HUD;

public class ObjectInspector
{
    private WorldObject _hoveredObject;
    private WorldObject _selectedObject;
    private WorldObject _playerObject;
    private MouseState _prevMouse;
    private string _hoveredEffectDesc;  // set when hovering an effect icon
    private bool _closeHovered;

    private const int PanelW = 220;
    private const int PanelY = 10;
    private const int Padding = 10;
    private const int CloseSize = 18;
    private const int IconSize = 20;
    private const int IconSpacing = 2;

    public WorldObject SelectedObject => _selectedObject;

    public void SetPlayerObject(WorldObject playerObj) { _playerObject = playerObj; }

    public void Update(GameMap map, Camera2D camera)
    {
        var mouse = Mouse.GetState();
        bool clicked = mouse.LeftButton == ButtonState.Pressed &&
                       _prevMouse.LeftButton == ButtonState.Released;

        _hoveredEffectDesc = null;
        _closeHovered = false;

        int panelX = GameConstants.ScreenWidth / 2 - PanelW / 2;
        int closeX = panelX + PanelW - CloseSize - 4;
        int closeY = PanelY + 4;

        // Close button hover + click
        if (_selectedObject != null)
        {
            if (mouse.X >= closeX && mouse.X <= closeX + CloseSize &&
                mouse.Y >= closeY && mouse.Y <= closeY + CloseSize)
            {
                _closeHovered = true;
                if (clicked)
                {
                    _selectedObject = null;
                    _prevMouse = mouse;
                    return;
                }
            }

            // Effect icon hover detection
            DetectEffectHover(mouse, panelX);
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

    private void DetectEffectHover(MouseState mouse, int panelX)
    {
        int effectCount = _selectedObject.Effects.Count;
        if (effectCount == 0) return;

        int iconsPerRow = (PanelW - Padding * 2) / (IconSize + IconSpacing);
        if (iconsPerRow < 1) iconsPerRow = 1;
        int barY = PanelY + Padding + 24;
        int iconStartY = barY + 6 + 22;

        for (int i = 0; i < effectCount; i++)
        {
            int row = i / iconsPerRow;
            int col = i % iconsPerRow;
            int ix = panelX + Padding + col * (IconSize + IconSpacing);
            int iy = iconStartY + row * (IconSize + IconSpacing);

            if (mouse.X >= ix && mouse.X <= ix + IconSize &&
                mouse.Y >= iy && mouse.Y <= iy + IconSize)
            {
                var def = EffectRegistry.GetDefinition(_selectedObject.Effects[i].EffectId);
                if (def != null)
                    _hoveredEffectDesc = def.Description;
                else
                    _hoveredEffectDesc = _selectedObject.Effects[i].EffectId;
                break;
            }
        }
    }

    public void DrawWorldMarker(SpriteBatch spriteBatch)
    {
        if (_selectedObject == null) return;

        int ts = GameConstants.TileSize;
        int px = _selectedObject.TileX * ts;
        int py = _selectedObject.TileY * ts;
        int pw = _selectedObject.EffectiveWidth * ts;
        int ph = _selectedObject.EffectiveHeight * ts;

        spriteBatch.FillRectangle(new Rectangle(px, py + ph + 1, pw, 3), Color.Gold);

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

    public void DrawHUD(SpriteBatch spriteBatch)
    {
        DrawInfoPanel(spriteBatch);
        DrawStatusPanel(spriteBatch);
    }

    // Bottom-right info panel: object hover OR effect hover description
    private void DrawInfoPanel(SpriteBatch spriteBatch)
    {
        // Priority: effect description > object hover
        if (_hoveredEffectDesc != null)
        {
            DrawInfoBox(spriteBatch,
                LocaleManager.Get("ui", "effect_label", "Effect"),
                _hoveredEffectDesc);
            return;
        }

        if (_hoveredObject == null) return;

        bool isDead = !_hoveredObject.State.IsAlive;
        string name = GetLocalizedName(_hoveredObject);
        if (isDead)
            name += " " + LocaleManager.Get("ui", "dead_suffix", "(Dead)");

        string category = isDead
            ? LocaleManager.Get("ui", "corpse", "Corpse")
            : GetLocalizedCategory(_hoveredObject);

        DrawInfoBox(spriteBatch, name, category);
    }

    private void DrawInfoBox(SpriteBatch spriteBatch, string line1, string line2)
    {
        var font = FontManager.GetFont(14);
        if (font == null) return;

        int pad = 8;
        int lineSpacing = 4;
        var s1 = font.MeasureString(line1);
        var catFont = FontManager.GetFont(12);
        var s2 = catFont?.MeasureString(line2) ?? Vector2.Zero;
        int boxW = (int)Math.Max(s1.X, s2.X) + pad * 2;
        int boxH = (int)(s1.Y + s2.Y) + lineSpacing + pad * 2;

        int boxX = GameConstants.ScreenWidth - boxW - 12;
        int boxY = GameConstants.ScreenHeight - boxH - 12;

        spriteBatch.FillRectangle(new Rectangle(boxX, boxY, boxW, boxH), Color.Black * 0.7f);
        spriteBatch.DrawRectangle(new Rectangle(boxX, boxY, boxW, boxH), Color.Gray * 0.5f);

        font.DrawText(spriteBatch, line1,
            new Vector2(boxX + pad, boxY + pad), Color.White);
        catFont?.DrawText(spriteBatch, line2,
            new Vector2(boxX + pad, boxY + pad + s1.Y + lineSpacing),
            Color.LightGray * 0.8f);
    }

    private void DrawStatusPanel(SpriteBatch spriteBatch)
    {
        if (_selectedObject == null) return;

        var titleFont = FontManager.GetFont(18);
        var hpFont = FontManager.GetFont(14);
        if (titleFont == null || hpFont == null) return;

        bool isDead = !_selectedObject.State.IsAlive;
        string name = GetLocalizedName(_selectedObject);
        if (isDead)
            name += " " + LocaleManager.Get("ui", "dead_suffix", "(Dead)");

        string hpText = $"HP: {_selectedObject.State.CurrentHp} / {_selectedObject.State.MaxHp}";

        int effectCount = _selectedObject.Effects.Count;
        int iconsPerRow = (PanelW - Padding * 2) / (IconSize + IconSpacing);
        if (iconsPerRow < 1) iconsPerRow = 1;
        int iconRows = effectCount > 0 ? (effectCount + iconsPerRow - 1) / iconsPerRow : 0;
        int effectAreaH = iconRows > 0 ? iconRows * (IconSize + IconSpacing) + 6 : 0;

        int panelH = 70 + effectAreaH;
        int panelX = GameConstants.ScreenWidth / 2 - PanelW / 2;

        // Panel background
        spriteBatch.FillRectangle(new Rectangle(panelX, PanelY, PanelW, panelH),
            new Color(20, 30, 20) * 0.9f);
        spriteBatch.DrawRectangle(new Rectangle(panelX, PanelY, PanelW, panelH),
            Color.Gold * 0.6f);

        // Name
        Color nameColor = isDead ? Color.Gray : Color.White;
        titleFont.DrawText(spriteBatch, name,
            new Vector2(panelX + Padding, PanelY + Padding), nameColor);

        // HP bar
        int barW = PanelW - Padding * 2;
        int barH = 6;
        int barY = PanelY + Padding + 24;
        spriteBatch.FillRectangle(new Rectangle(panelX + Padding, barY, barW, barH),
            Color.Black * 0.5f);

        float hpRatio = Math.Clamp(
            (float)_selectedObject.State.CurrentHp / _selectedObject.State.MaxHp, 0f, 1f);
        int fillW = (int)(barW * hpRatio);
        Color barColor = hpRatio > 0.5f
            ? Color.Lerp(Color.Yellow, Color.LimeGreen, (hpRatio - 0.5f) * 2f)
            : Color.Lerp(Color.Red, Color.Yellow, hpRatio * 2f);
        if (fillW > 0)
            spriteBatch.FillRectangle(new Rectangle(panelX + Padding, barY, fillW, barH), barColor);

        // HP text
        hpFont.DrawText(spriteBatch, hpText,
            new Vector2(panelX + Padding, barY + barH + 3), Color.White * 0.9f);

        // Effect icons
        if (effectCount > 0)
        {
            int iconStartY = barY + barH + 22;

            for (int i = 0; i < effectCount; i++)
            {
                var ae = _selectedObject.Effects[i];
                int row = i / iconsPerRow;
                int col = i % iconsPerRow;
                int ix = panelX + Padding + col * (IconSize + IconSpacing);
                int iy = iconStartY + row * (IconSize + IconSpacing);

                // Icon background
                spriteBatch.FillRectangle(new Rectangle(ix, iy, IconSize, IconSize),
                    new Color(40, 50, 40) * 0.9f);
                spriteBatch.DrawRectangle(new Rectangle(ix, iy, IconSize, IconSize),
                    Color.Gray * 0.4f);

                // Effect icon texture
                var def = EffectRegistry.GetDefinition(ae.EffectId);
                if (def?.Texture != null)
                {
                    spriteBatch.Draw(def.Texture,
                        new Rectangle(ix + 2, iy + 2, IconSize - 4, IconSize - 4),
                        Color.White);
                }
                else
                {
                    var iconFont = FontManager.GetFont(10);
                    iconFont?.DrawText(spriteBatch, ae.EffectId[..1].ToUpper(),
                        new Vector2(ix + 4, iy + 3), Color.White * 0.7f);
                }
            }
        }

        // Close button [X] — hover/click color feedback
        int closeX = panelX + PanelW - CloseSize - 4;
        int closeY2 = PanelY + 4;

        bool closePressed = _closeHovered &&
            Mouse.GetState().LeftButton == ButtonState.Pressed;
        Color closeBg = closePressed ? Color.Red
            : _closeHovered ? new Color(180, 40, 40)
            : Color.DarkRed * 0.8f;
        Color closeFg = _closeHovered ? Color.White : Color.White * 0.8f;

        spriteBatch.FillRectangle(new Rectangle(closeX, closeY2, CloseSize, CloseSize), closeBg);

        var closeFont = FontManager.GetFont(12);
        if (closeFont != null)
        {
            var xSize = closeFont.MeasureString("X");
            float xTextX = closeX + (CloseSize - xSize.X) / 2f;
            float xTextY = closeY2 + (CloseSize - xSize.Y) / 2f;
            closeFont.DrawText(spriteBatch, "X", new Vector2(xTextX, xTextY), closeFg);
        }
    }

    private static string GetLocalizedName(WorldObject obj)
    {
        return LocaleManager.Get("items", obj.ItemId,
            obj.Definition.Metadata.DisplayName);
    }

    private static string GetLocalizedCategory(WorldObject obj)
    {
        string catKey = obj.Category.ToString().ToLowerInvariant();
        return LocaleManager.Get("ui", "category_" + catKey, obj.Category.ToString());
    }
}
