using Microsoft.Xna.Framework.Input;

namespace FarmGame.Entities.Actions;

public interface IPlayerAction
{
    bool IsActive { get; }
    void Update(float deltaTime, KeyboardState keyboard);
    void Reset();
    void Draw(ActionDrawContext context);
}
