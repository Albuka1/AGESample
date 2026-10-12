using Age.Core;
using Age.Physics;
using Age.Rendering;

namespace Age.Platformer;

/// <summary>
/// A page of the developer window that belongs to the demo rather than to the engine, and one that may be closed by its cross: what
/// it reports is the world of a run, which a person looks at and then closes again, unlike the console which stays.
/// </summary>
/// <remarks>
/// A page is read from the world on every frame rather than kept up to date, so it is always what the game holds right now - which is
/// the whole of what a developer looks at it for. The two arguments are the way the page reaches a world that a loaded scene may
/// replace: it asks for the world that is there rather than holding the one that was.
/// </remarks>
internal sealed class WorldTab(Func<World> world, FixedTimestep timestep) : IDevWindowTab
{
    private static readonly Color TextColour = new(225, 225, 225);
    private static readonly Color HintColour = new(150, 150, 150);

    private readonly float _line = BitmapFontMetrics.GlyphHeight + 4f;

    /// <inheritdoc />
    public string Title => "world";

    /// <inheritdoc />
    public bool Closable => true;

    /// <inheritdoc />
    /// <remarks>What this page reports is read on every frame, so it reads no pointer of its own.</remarks>
    public void Update(in GameTime frame, Rect body, in WindowPointer pointer)
    {
    }

    /// <inheritdoc />
    public void Render(IRenderer renderer, Rect body)
    {
        ArgumentNullException.ThrowIfNull(renderer);

        World current = world();

        string[] lines =
        [
            $"entities {current.Enumerate().Count()}",
            $"sprites {current.Enumerate<SpriteComponent>().Count()}",
            $"colliders {current.Enumerate<ColliderComponent>().Count()}",
            $"tick {timestep.Tick} at {timestep.TimeScale:0.##}x {(timestep.Paused ? "paused" : "running")}",
        ];

        for (var index = 0; index < lines.Length; index++)
        {
            renderer.DrawText(lines[index], new Vector2(body.X, body.Y + (index * _line)), index == 3 ? HintColour : TextColour);
        }
    }
}

