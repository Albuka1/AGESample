using Age.Core;
using Age.Input;
using Age.Rendering;
using Silk.NET.Input;
using Key = Age.Input.Key;
using MouseButton = Age.Input.MouseButton;
using SilkKey = Silk.NET.Input.Key;
using SilkMouseButton = Silk.NET.Input.MouseButton;

namespace Age.Demo;

internal sealed class WindowInputService : IInputService
{
    private static readonly (SilkKey Silk, Key Engine)[] KeyMap =
    [
        (SilkKey.W, Key.W),
        (SilkKey.A, Key.A),
        (SilkKey.S, Key.S),
        (SilkKey.D, Key.D),
        (SilkKey.Q, Key.Q),
        (SilkKey.E, Key.E),
        (SilkKey.R, Key.R),
        (SilkKey.F, Key.F),
        (SilkKey.Up, Key.Up),
        (SilkKey.Down, Key.Down),
        (SilkKey.Left, Key.Left),
        (SilkKey.Right, Key.Right),
        (SilkKey.Space, Key.Space),
        (SilkKey.ShiftLeft, Key.Shift),
        (SilkKey.ShiftRight, Key.Shift),
        (SilkKey.ControlLeft, Key.Ctrl),
        (SilkKey.ControlRight, Key.Ctrl),
        (SilkKey.AltLeft, Key.Alt),
        (SilkKey.AltRight, Key.Alt),
        (SilkKey.Enter, Key.Enter),
        (SilkKey.Escape, Key.Escape),
        (SilkKey.Tab, Key.Tab),
        (SilkKey.Number0, Key.Digit0),
        (SilkKey.Number1, Key.Digit1),
        (SilkKey.Number2, Key.Digit2),
        (SilkKey.Number3, Key.Digit3),
        (SilkKey.Number4, Key.Digit4),
        (SilkKey.Number5, Key.Digit5),
        (SilkKey.Number6, Key.Digit6),
        (SilkKey.Number7, Key.Digit7),
        (SilkKey.Number8, Key.Digit8),
        (SilkKey.Number9, Key.Digit9),
    ];

    private readonly IKeyboard _keyboard;
    private readonly IMouse _mouse;
    private readonly HashSet<Key> _down = [];
    private readonly HashSet<Key> _previous = [];

    private Vector2 _mousePosition;
    private bool _mouseDown;
    private bool _mousePressed;

    public WindowInputService(IWindowService windowService)
    {
        ArgumentNullException.ThrowIfNull(windowService);

        IInputContext context = windowService.Window.CreateInput();

        _keyboard = context.Keyboards.Count > 0
            ? context.Keyboards[0]
            : throw new InvalidOperationException("No keyboard is available for the demo window.");

        _mouse = context.Mice.Count > 0
            ? context.Mice[0]
            : throw new InvalidOperationException("No mouse is available for the demo window.");
    }

    public Vector2 MousePosition => _mousePosition;

    public bool IsKeyDown(Key key) => _down.Contains(key);

    public bool IsKeyPressed(Key key) => _down.Contains(key) && !_previous.Contains(key);

    public bool IsMouseButtonDown(MouseButton button) => button == MouseButton.Left && _mouseDown;

    public bool IsMouseButtonPressed(MouseButton button) => button == MouseButton.Left && _mousePressed;

    internal void BeginFrame()
    {
        _previous.Clear();

        foreach (Key key in _down)
        {
            _previous.Add(key);
        }

        _down.Clear();

        foreach ((SilkKey silk, Key engine) in KeyMap)
        {
            if (_keyboard.IsKeyPressed(silk))
            {
                _down.Add(engine);
            }
        }

        bool mouseDown = _mouse.IsButtonPressed(SilkMouseButton.Left);
        _mousePressed = mouseDown && !_mouseDown;
        _mouseDown = mouseDown;
        _mousePosition = new Vector2(_mouse.Position.X, _mouse.Position.Y);
    }
}
