using MonoGame.Extended.Input;

namespace FarmGame.Entities.Actions;

public interface IPlayerAction
{
    bool IsActive { get; }
    void Update(float deltaTime, KeyboardStateExtended keyboard);
    void Reset();
    void Draw(ActionDrawContext context);
}
