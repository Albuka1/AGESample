using Age.Core;
using Age.Rendering;

namespace Age.Demo;

internal sealed class MotionSystem : ISystem
{
    private readonly DemoState _state;

    public MotionSystem(DemoState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
    }

    public void Update(World world, in GameTime time)
    {
        if (_state.IsPaused)
        {
            return;
        }

        float delta = (float)time.Delta;

        foreach (Entity entity in world.Enumerate<VelocityComponent>())
        {
            if (!world.Has<TransformComponent>(entity) || !world.Has<SpriteComponent>(entity))
            {
                continue;
            }

            VelocityComponent velocity = world.Get<VelocityComponent>(entity);
            TransformComponent transform = world.Get<TransformComponent>(entity);
            Vector2 size = world.Get<SpriteComponent>(entity).Size * transform.Scale;

            Vector2 position = transform.Position + (velocity.Value * delta);
            Vector2 limit = new(DemoConfig.PlayAreaWidth - size.X, DemoConfig.PlayAreaHeight - size.Y);

            if (position.X < 0f)
            {
                position.X = 0f;
                velocity.Value.X = -velocity.Value.X;
            }
            else if (position.X > limit.X)
            {
                position.X = limit.X;
                velocity.Value.X = -velocity.Value.X;
            }

            if (position.Y < 0f)
            {
                position.Y = 0f;
                velocity.Value.Y = -velocity.Value.Y;
            }
            else if (position.Y > limit.Y)
            {
                position.Y = limit.Y;
                velocity.Value.Y = -velocity.Value.Y;
            }

            transform.Position = position;

            world.Set(entity, transform);
            world.Set(entity, velocity);
        }
    }
}
