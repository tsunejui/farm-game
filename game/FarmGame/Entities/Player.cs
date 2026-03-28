using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using FarmGame.Core;
using FarmGame.Entities.Actions;
using FarmGame.Entities.Actions.Player;
using FarmGame.World;

namespace FarmGame.Entities;

// =============================================================================
// Player.cs — Player entity coordinator
//
// Orchestrates all player actions and renders the player body.
// Each action (movement, jump, attack) is a separate IPlayerAction
// that handles its own update logic and visual effects.
//
// Functions:
//   - Player()                : Constructor. Creates all actions and registers them in the update loop.
//   - Update()                : Per-frame update. Delegates to each action's Update via the IPlayerAction interface.
//   - Draw()                  : Per-frame render. Draws action effects first, then body and direction indicator on top.
//   - DrawBody()              : Renders the player body as a colored rectangle inset from the tile edge by body_padding.
//   - DrawDirectionIndicator(): Renders a small white square on the edge of the body indicating the facing direction.
// =============================================================================
public class Player
{
    private readonly MovementAction _movement;
    private readonly JumpAction _jump;
    private readonly AttackAction _attack;
    private readonly IPlayerAction[] _actions;

    public Vector2 PixelPosition => _movement.PixelPosition;

    public Player(Point startPosition, GameMap tileMap)
    {
        _movement = new MovementAction(startPosition, tileMap);
        _jump = new JumpAction();
        _attack = new AttackAction();
        _actions = new IPlayerAction[] { _movement, _jump, _attack };
    }

    public void Update(GameTime gameTime)
    {
        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        var keyboard = Keyboard.GetState();

        foreach (var action in _actions)
            action.Update(deltaTime, keyboard);
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel)
    {
        var context = new ActionDrawContext
        {
            SpriteBatch = spriteBatch,
            Pixel = pixel,
            PixelPosition = _movement.PixelPosition,
            FacingDirection = _movement.FacingDirection,
            YOffset = _jump.Offset,
        };

        // Action effects drawn before body (e.g. shadow under player)
        foreach (var action in _actions)
            action.Draw(context);

        // Body and direction indicator on top
        DrawBody(spriteBatch, pixel);
        DrawDirectionIndicator(spriteBatch, pixel);
    }

    private void DrawBody(SpriteBatch spriteBatch, Texture2D pixel)
    {
        int pad = GameConstants.PlayerBodyPadding;
        int bodyW = GameConstants.TileSize - pad * 2;
        int bodyH = GameConstants.TileSize - pad * 2;
        int baseX = (int)_movement.PixelPosition.X + pad;
        int baseY = (int)_movement.PixelPosition.Y + pad;

        var bodyRect = new Rectangle(baseX, baseY + (int)_jump.Offset, bodyW, bodyH);
        spriteBatch.Draw(pixel, bodyRect, GameConstants.PlayerColor);
    }

    private void DrawDirectionIndicator(SpriteBatch spriteBatch, Texture2D pixel)
    {
        int sz = GameConstants.PlayerIndicatorSize;
        int pad = GameConstants.PlayerBodyPadding;
        int half = sz / 2;
        int yOffset = (int)_jump.Offset;
        int cx = (int)_movement.PixelPosition.X + GameConstants.TileSize / 2 - half;
        int cy = (int)_movement.PixelPosition.Y + GameConstants.TileSize / 2 - half + yOffset;

        Rectangle rect = _movement.FacingDirection switch
        {
            Direction.Up => new Rectangle(cx, (int)_movement.PixelPosition.Y + pad + yOffset, sz, sz),
            Direction.Down => new Rectangle(cx, (int)_movement.PixelPosition.Y + GameConstants.TileSize - pad - sz + yOffset, sz, sz),
            Direction.Left => new Rectangle((int)_movement.PixelPosition.X + pad, cy, sz, sz),
            Direction.Right => new Rectangle((int)_movement.PixelPosition.X + GameConstants.TileSize - pad - sz, cy, sz, sz),
            _ => new Rectangle(cx, cy, sz, sz),
        };

        spriteBatch.Draw(pixel, rect, Color.White);
    }
}
