using Age.Core;
using Age.Input;
using Age.Rendering;

namespace Age.Demo;

internal sealed class PlayerSystem : ISystem
{
    private readonly IInputService _input;
    private readonly DemoState _state;
    private readonly DemoScene _scene;

    public PlayerSystem(IInputService input, DemoState state, DemoScene scene)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(scene);

        _input = input;
        _state = state;
        _scene = scene;
    }

    public void Update(World world, in GameTime time)
    {
        if (_state.IsPaused || !world.IsAlive(_scene.Player))
        {
            return;
        }

        Vector2 direction = Vector2.Zero;

        if (_input.IsKeyDown(Key.W))
        {
            direction.Y -= 1f;
        }

        if (_input.IsKeyDown(Key.S))
        {
            direction.Y += 1f;
        }

        if (_input.IsKeyDown(Key.A))
        {
            direction.X -= 1f;
        }

        if (_input.IsKeyDown(Key.D))
        {
            direction.X += 1f;
        }

        if (direction == Vector2.Zero)
        {
            return;
        }

        float delta = (float)time.Delta;
        TransformComponent transform = world.Get<TransformComponent>(_scene.Player);
        Vector2 size = world.Get<SpriteComponent>(_scene.Player).Size * transform.Scale;

        Vector2 position = transform.Position + (direction * (DemoConfig.PlayerSpeed * delta));
        position.X = Math.Clamp(position.X, 0f, DemoConfig.PlayAreaWidth - size.X);
        position.Y = Math.Clamp(position.Y, 0f, DemoConfig.PlayAreaHeight - size.Y);

        transform.Position = position;
        world.Set(_scene.Player, transform);
    }
}
