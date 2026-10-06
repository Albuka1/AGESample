using Age.Core;
using Age.Input;
using Age.Rendering;

namespace Age.Demo;

internal sealed class CameraSystem : ISystem
{
    private readonly IInputService _input;
    private readonly DemoState _state;
    private readonly DemoScene _scene;

    public CameraSystem(IInputService input, DemoState state, DemoScene scene)
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
        float delta = (float)time.Delta;
        Camera2D camera = _state.Camera;

        Vector2 pan = Vector2.Zero;

        if (_input.IsKeyDown(Key.Left))
        {
            pan.X -= 1f;
        }

        if (_input.IsKeyDown(Key.Right))
        {
            pan.X += 1f;
        }

        if (_input.IsKeyDown(Key.Up))
        {
            pan.Y -= 1f;
        }

        if (_input.IsKeyDown(Key.Down))
        {
            pan.Y += 1f;
        }

        if (pan != Vector2.Zero)
        {
            _state.FollowPlayer = false;
            camera.Position += pan * (DemoConfig.CameraPanSpeed * delta);
        }

        if (_input.IsKeyDown(Key.Q))
        {
            camera.Zoom *= MathF.Exp(DemoConfig.CameraZoomSpeed * delta);
        }

        if (_input.IsKeyDown(Key.E))
        {
            camera.Zoom *= MathF.Exp(-DemoConfig.CameraZoomSpeed * delta);
        }

        camera.Zoom = Math.Clamp(camera.Zoom, DemoConfig.MinZoom, DemoConfig.MaxZoom);

        if (_input.IsKeyPressed(Key.F))
        {
            _state.FollowPlayer = !_state.FollowPlayer;
        }

        if (_state.FollowPlayer && world.IsAlive(_scene.Player))
        {
            TransformComponent transform = world.Get<TransformComponent>(_scene.Player);
            Vector2 size = world.Get<SpriteComponent>(_scene.Player).Size * transform.Scale;
            Vector2 center = transform.Position + (size * 0.5f);
            var half = new Vector2(camera.ViewportSize.X * 0.5f, camera.ViewportSize.Y * 0.5f);
            Vector2 target = (center * (1f / camera.Zoom)) - half;

            camera.Position += (target - camera.Position) * Math.Clamp(delta * DemoConfig.FollowLerp, 0f, 1f);
        }

        _state.Camera = camera;
    }
}
