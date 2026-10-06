using Age.Core;
using Age.Physics;
using Age.Rendering;

namespace Age.Demo;

internal sealed class CollisionTintSystem : ISystem
{
    public void Update(World world, in GameTime time)
    {
        foreach (Entity entity in world.Enumerate<SpriteComponent>())
        {
            if (!world.Has<BaseColorComponent>(entity))
            {
                continue;
            }

            Color baseColor = world.Get<BaseColorComponent>(entity).Value;
            ref SpriteComponent sprite = ref world.GetRef<SpriteComponent>(entity);
            sprite.Color = world.Has<CollisionComponent>(entity)
                ? baseColor * DemoConfig.CollisionTint
                : baseColor;
        }
    }
}
