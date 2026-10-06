using Age.Core;
using Age.Physics;
using Age.Rendering;

namespace Age.Demo;

internal sealed class DemoScene
{
    private readonly List<Entity> _simulated = [];
    private readonly Random _random = new(DemoConfig.BoxSeed);

    internal Entity Player { get; private set; }

    internal void Build(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        var playerSize = new Vector2(DemoConfig.PlayerSize, DemoConfig.PlayerSize);
        Vector2 playerPosition = new(
            (DemoConfig.PlayAreaWidth - playerSize.X) * 0.5f,
            (DemoConfig.PlayAreaHeight - playerSize.Y) * 0.5f);

        Player = CreatePlayer(world, playerPosition, playerSize);

        for (int index = 0; index < DemoConfig.BoxCount; index++)
        {
            CreateBox(world);
        }
    }

    internal void Reset(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        foreach (Entity entity in _simulated)
        {
            world.DestroyEntity(entity);
        }

        _simulated.Clear();
        Build(world);
    }

    private Entity CreatePlayer(World world, Vector2 position, Vector2 size)
    {
        Entity entity = Track(world.CreateEntity());

        world.Set(entity, new TransformComponent { Position = position, Scale = new Vector2(1f, 1f) });
        world.Set(entity, new SpriteComponent { Size = size, Color = DemoConfig.PlayerColor, ZOrder = 100 });
        world.Set(entity, new ColliderComponent { Size = size });
        world.Set(entity, new BaseColorComponent { Value = DemoConfig.PlayerColor });

        return entity;
    }

    private void CreateBox(World world)
    {
        Entity entity = Track(world.CreateEntity());

        float size = NextFloat(DemoConfig.BoxMinSize, DemoConfig.BoxMaxSize);
        var box = new Vector2(size, size);
        Vector2 position = new(
            NextFloat(0f, DemoConfig.PlayAreaWidth - size),
            NextFloat(0f, DemoConfig.PlayAreaHeight - size));

        float angle = NextFloat(0f, MathF.Tau);
        float speed = NextFloat(DemoConfig.BoxMinSpeed, DemoConfig.BoxMaxSpeed);
        var velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);
        Color color = DemoConfig.Palette[_random.Next(DemoConfig.Palette.Length)];

        world.Set(entity, new TransformComponent { Position = position, Scale = new Vector2(1f, 1f) });
        world.Set(entity, new SpriteComponent { Size = box, Color = color });
        world.Set(entity, new ColliderComponent { Size = box });
        world.Set(entity, new VelocityComponent { Value = velocity });
        world.Set(entity, new BaseColorComponent { Value = color });
    }

    private Entity Track(Entity entity)
    {
        _simulated.Add(entity);
        return entity;
    }

    private float NextFloat(float minimum, float maximum) =>
        minimum + ((float)_random.NextDouble() * (maximum - minimum));
}
