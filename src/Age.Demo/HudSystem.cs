using Age.Core;
using Age.Physics;
using Age.Rendering;
using Age.UI;

namespace Age.Demo;

internal sealed class HudSystem : ISystem
{
    private readonly DemoState _state;
    private readonly DemoUi _ui;

    public HudSystem(DemoState state, DemoUi ui)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(ui);

        _state = state;
        _ui = ui;
    }

    public void Update(World world, in GameTime time)
    {
        float frameRate = time.Delta > 0d ? (float)(1d / time.Delta) : 0f;
        _state.SmoothedFrameRate = _state.SmoothedFrameRate <= 0f
            ? frameRate
            : _state.SmoothedFrameRate + ((frameRate - _state.SmoothedFrameRate) * 0.1f);

        int sprites = world.Enumerate<SpriteComponent>().Count();
        int collisions = world.Enumerate<CollisionComponent>().Count();
        _state.CollisionCount = collisions;

        string mode = _state.IsPaused ? "PAUSED" : "RUNNING";
        string follow = _state.FollowPlayer ? "FOLLOW" : "FREE";

        string status = FormattableString.Invariant(
            $"FPS {_state.SmoothedFrameRate:0}  SPRITES {sprites}  COLLISIONS {collisions}  ZOOM {_state.Camera.Zoom:0.00}  {mode}  {follow}");

        SetText(world, _ui.StatusLabel, status);
    }

    private static void SetText(World world, Entity entity, string text)
    {
        if (!world.IsAlive(entity))
        {
            return;
        }

        TextLabelComponent label = world.Get<TextLabelComponent>(entity);
        if (label.Text == text)
        {
            return;
        }

        label.Text = text;
        world.Set(entity, label);
    }
}
