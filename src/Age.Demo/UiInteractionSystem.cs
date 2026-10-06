using Age.Core;
using Age.Input;
using Age.UI;

namespace Age.Demo;

internal sealed class UiInteractionSystem : ISystem
{
    private readonly IInputService _input;
    private readonly DemoState _state;
    private readonly DemoScene _scene;
    private readonly DemoUi _ui;

    public UiInteractionSystem(IInputService input, DemoState state, DemoScene scene, DemoUi ui)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(ui);

        _input = input;
        _state = state;
        _scene = scene;
        _ui = ui;
    }

    public void Update(World world, in GameTime time)
    {
        if (_input.IsKeyPressed(Key.Space))
        {
            _state.IsPaused = !_state.IsPaused;
        }

        if (_input.IsKeyPressed(Key.R))
        {
            _scene.Reset(world);
        }

        bool clicked = _input.IsMouseButtonPressed(MouseButton.Left);
        Entity activated = default;
        bool hasActivated = false;

        foreach (Entity entity in world.Enumerate<ButtonComponent>())
        {
            ButtonComponent button = world.Get<ButtonComponent>(entity);
            if (!button.Interactable)
            {
                continue;
            }

            button.BaseColor = button.IsPressed
                ? DemoConfig.ButtonPressed
                : button.IsHovered
                    ? DemoConfig.ButtonHover
                    : DemoConfig.ButtonIdle;

            world.Set(entity, button);

            if (clicked && button.IsHovered && !hasActivated)
            {
                activated = entity;
                hasActivated = true;
            }
        }

        if (hasActivated)
        {
            Activate(world, activated);
        }
    }

    private void Activate(World world, Entity entity)
    {
        if (entity.Id == _ui.PauseButton.Id)
        {
            _state.IsPaused = !_state.IsPaused;
        }
        else if (entity.Id == _ui.ResetButton.Id)
        {
            _scene.Reset(world);
        }
        else if (entity.Id == _ui.ZoomInButton.Id)
        {
            Zoom(1.25f);
        }
        else if (entity.Id == _ui.ZoomOutButton.Id)
        {
            Zoom(0.8f);
        }
    }

    private void Zoom(float factor)
    {
        Camera2D camera = _state.Camera;
        camera.Zoom = Math.Clamp(camera.Zoom * factor, DemoConfig.MinZoom, DemoConfig.MaxZoom);
        _state.Camera = camera;
    }
}
