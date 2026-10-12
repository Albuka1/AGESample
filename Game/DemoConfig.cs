using Age.Core;

namespace Age.Platformer;

/// <summary>
/// Every number the demo is tuned by, in one place, so a person who copies this template has one file to change rather than a value
/// to hunt for across the systems. Nothing here is engine API: it is what this demo is.
/// </summary>
internal static class DemoConfig
{
    /// <summary>The width of the window the demo opens with.</summary>
    public const int WindowWidth = 1280;

    /// <summary>The height of the window the demo opens with.</summary>
    public const int WindowHeight = 720;

    /// <summary>The name of the window, and the name of the folder the settings of the demo are kept in.</summary>
    public const string Title = "AGE Demo";

    /// <summary>How fast the sprite a person moves with WASD travels, in units of the world per second.</summary>
    public const float MoveSpeed = 240f;

    /// <summary>How long a sprite that the key `E` puts on screen lives, in seconds, until the setting of the console changes it.</summary>
    public const float SpawnLifetime = 3f;

    /// <summary>The size of the area the demo is authored against, which its interface is scaled from as well.</summary>
    public static readonly Vector2 Design = new(1280f, 720f);

    /// <summary>The paths of the content of the demo, as the loader names them.</summary>
    public const string TilePath = "Textures/Tiles/tiles.bmp";
    public const string FontPath = "Fonts/Cousine-Regular.ttf";
    public const string VignettePath = "Shaders/vignette.frag";
    public const string ClickPath = "Audio/Effects/click.wav";
}
