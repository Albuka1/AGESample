using Age.Core;
using Age.Input;
using Age.Physics;
using Age.Rendering;

namespace Age.Platformer;

/// <summary>
/// The small pieces of behaviour the demo is built from, kept out of <c>Program.cs</c> so that the file reads as the list of what the
/// game does rather than as the body of every one of its functions.
/// </summary>
internal static class DemoActions
{
    /// <summary>Moves an entity with WASD by the distance the step covers, which is what the demo hands a person to play with.</summary>
    /// <param name="world">The world the entity lives in.</param>
    /// <param name="entity">The entity the keyboard moves.</param>
    /// <param name="input">The input of the frame.</param>
    /// <param name="time">The time the step covers.</param>
    public static void Move(World world, Entity entity, IInputService input, in GameTime time)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(input);

        if (!world.IsAlive(entity) || !world.Has<TransformComponent>(entity))
        {
            return;
        }

        float step = (float)(time.Delta * DemoConfig.MoveSpeed);
        Vector2 offset = Vector2.Zero;

        if (input.IsKeyDown(Key.W))
        {
            offset.Y -= step;
        }

        if (input.IsKeyDown(Key.S))
        {
            offset.Y += step;
        }

        if (input.IsKeyDown(Key.A))
        {
            offset.X -= step;
        }

        if (input.IsKeyDown(Key.D))
        {
            offset.X += step;
        }

        if (offset != Vector2.Zero)
        {
            world.GetRef<TransformComponent>(entity).Position += offset;
        }
    }

    /// <summary>
    /// Tints a sprite while its collider touches another one, so that the contacts the collision system found are what a person sees.
    /// </summary>
    /// <param name="world">The world the two entities live in.</param>
    /// <param name="first">The first entity of the pair.</param>
    /// <param name="second">The second entity of the pair.</param>
    public static void TintByCollision(World world, Entity first, Entity second)
    {
        ArgumentNullException.ThrowIfNull(world);

        Tint(world, first, Color.White, new Color(65, 105, 225));
        Tint(world, second, Color.Green, Color.Red);

        static void Tint(World world, Entity entity, Color calm, Color touching)
        {
            if (!world.IsAlive(entity) || !world.Has<SpriteComponent>(entity))
            {
                return;
            }

            ref SpriteComponent sprite = ref world.GetRef<SpriteComponent>(entity);
            sprite.Color = world.Has<CollisionComponent>(entity) ? touching : calm;
        }
    }

    /// <summary>Puts the calm colour back on a sprite that is not touching anything any more.</summary>
    /// <param name="world">The world the entity lives in.</param>
    /// <param name="entity">The entity to put the calm colour back on.</param>
    /// <param name="calm">The colour the entity is drawn in while nothing touches it.</param>
    /// <remarks>
    /// A contact is announced as long as it exists, and a pair that came apart raises nothing: without this the sprite of a pair that
    /// came apart would keep the colour it was tinted while they touched.
    /// </remarks>
    public static void RestoreCalmColour(World world, Entity entity, Color calm)
    {
        ArgumentNullException.ThrowIfNull(world);

        if (!world.IsAlive(entity) || !world.Has<SpriteComponent>(entity) || world.Has<CollisionComponent>(entity))
        {
            return;
        }

        world.GetRef<SpriteComponent>(entity).Color = calm;
    }

    /// <summary>Puts a short-lived sprite on screen, at the place a spawn puts it, so a person sees that a prototype was built.</summary>
    /// <param name="world">The world to put the sprite in.</param>
    /// <param name="position">Where the sprite is spawned.</param>
    /// <param name="lifetime">How long the sprite lives, in seconds.</param>
    /// <returns>The entity that was spawned.</returns>
    /// <remarks>
    /// The lifetime is a timer of the engine rather than a counter of this game: the timer system advances every timer of a world, and
    /// the subscription of the demo destroys the entity of the one that ran out. The slot is handed out again for the next entity, so
    /// the demo keeps no book of its own. The collider is `ColliderComponent.Of`, which answers the layer it belongs to: a collider that
    /// a document writes is built the same way, so a pair of a spawn of this demo and an entity of a prototype is a pair the collision
    /// system reads.
    /// </remarks>
    public static Entity SpawnFading(World world, Vector2 position, float lifetime)
    {
        ArgumentNullException.ThrowIfNull(world);

        Entity entity = world.CreateEntity();

        world.Set(entity, new TransformComponent { Position = position, Scale = new Vector2(1f, 1f) });
        world.Set(entity, new SpriteComponent { Size = new Vector2(32f, 32f), Color = new Color(135, 206, 235), ZOrder = 2 });
        world.Set(entity, ColliderComponent.Of(new Vector2(32f, 32f), CollisionLayer.Default));
        world.Set(entity, TimerComponent.For(lifetime));

        return entity;
    }

    /// <summary>Puts a sprite whose image the build does not ship on screen.</summary>
    /// <param name="world">The world to put the sprite in.</param>
    /// <param name="position">Where the sprite is spawned.</param>
    /// <returns>The entity that was spawned.</returns>
    /// <remarks>
    /// Content that names an image a build does not ship is exactly the mistake the placeholder exists for: the sprite is drawn as a
    /// bright ERROR checkerboard, one line of the log names the file, and the game keeps running rather than taking the frame down.
    /// </remarks>
    public static Entity SpawnMissing(World world, Vector2 position)
    {
        ArgumentNullException.ThrowIfNull(world);

        Entity entity = world.CreateEntity();

        world.Set(entity, new TransformComponent { Position = position, Scale = new Vector2(1f, 1f) });
        world.Set(entity, new SpriteComponent { TexturePath = "Textures/Nowhere/gone.png", Size = new Vector2(96f, 96f), Color = Color.White, ZOrder = 3 });
        world.Set(entity, TimerComponent.For(6f));

        return entity;
    }

    /// <summary>The first sprite of a world, which is the entity the keyboard moves and the one a loaded scene brings back.</summary>
    /// <param name="world">The world to look in.</param>
    /// <returns>The first entity that carries a sprite, or the default entity when there is none.</returns>
    public static Entity FirstSprite(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        return world.Enumerate<SpriteComponent>().FirstOrDefault();
    }

    /// <summary>Reads a scene from a file into a world of its own, which replaces the world a game built.</summary>
    /// <param name="scenes">The serializer of the engine.</param>
    /// <param name="path">The file to read.</param>
    /// <returns>The world the file describes.</returns>
    /// <remarks>Loading replaces the scene: a fresh world keeps the file and the screen in step rather than piling entities on top.</remarks>
    public static World LoadScene(ISceneSerializer scenes, string path)
    {
        ArgumentNullException.ThrowIfNull(scenes);

        var loaded = new World();
        scenes.Load(loaded, File.ReadAllText(path));
        return loaded;
    }

    /// <summary>Wraps a line into the width it is drawn in, one line per run of words that fits.</summary>
    /// <param name="fonts">The fonts of the frame, which measure a line.</param>
    /// <param name="font">The font the line is measured in.</param>
    /// <param name="text">The line to wrap.</param>
    /// <param name="width">The width the line is wrapped into.</param>
    /// <returns>The lines the text was split into.</returns>
    /// <remarks>
    /// A line of a developer is longer than a small window is wide, and a draw of it would run off the edge of the frame and over
    /// whatever else the game puts there. A word wider than the window on its own is left on a line of its own rather than cut, because
    /// there is nowhere to break it.
    /// </remarks>
    public static IEnumerable<string> Wrap(IFontService fonts, FontHandle font, string text, float width)
    {
        ArgumentNullException.ThrowIfNull(fonts);

        string line = string.Empty;

        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = line.Length == 0 ? word : $"{line} {word}";

            if (line.Length > 0 && fonts.Measure(font, candidate).X > width)
            {
                yield return line;
                line = word;
                continue;
            }

            line = candidate;
        }

        if (line.Length > 0)
        {
            yield return line;
        }
    }
}
