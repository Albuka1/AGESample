using Age.Core;

namespace Age.Demo;

internal sealed class DemoState
{
    public Camera2D Camera = new() { Zoom = 1f };

    public bool IsPaused;

    public bool FollowPlayer = true;

    public float SmoothedFrameRate;

    public int CollisionCount;
}
