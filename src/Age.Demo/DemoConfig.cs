using Age.Core;

namespace Age.Demo;

internal static class DemoConfig
{
    internal const int WindowWidth = 1280;
    internal const int WindowHeight = 720;

    internal const float PlayAreaWidth = 1920f;
    internal const float PlayAreaHeight = 1080f;

    internal const int BoxCount = 28;
    internal const int BoxSeed = 20260206;
    internal const float BoxMinSize = 22f;
    internal const float BoxMaxSize = 58f;
    internal const float BoxMinSpeed = 70f;
    internal const float BoxMaxSpeed = 215f;

    internal const float PlayerSize = 46f;
    internal const float PlayerSpeed = 430f;

    internal const float CameraPanSpeed = 640f;
    internal const float CameraZoomSpeed = 1.7f;
    internal const float MinZoom = 0.35f;
    internal const float MaxZoom = 2.4f;
    internal const float FollowLerp = 6f;

    internal const float CollisionTint = 1.9f;

    internal const string Title = "AUAE GAME ENGINE";
    internal const string Help = "WASD MOVE  ARROWS PAN  Q/E ZOOM  F FOLLOW";

    internal static readonly Color[] Palette =
    [
        new(239, 71, 111),
        new(255, 209, 102),
        new(6, 214, 160),
        new(17, 138, 178),
        new(155, 93, 229),
        new(241, 143, 1),
        new(46, 196, 182),
        new(231, 111, 81),
    ];

    internal static readonly Color PanelColor = new(10, 14, 26) { A = 205 };
    internal static readonly Color ButtonIdle = new(38, 48, 78);
    internal static readonly Color ButtonHover = new(58, 74, 116);
    internal static readonly Color ButtonPressed = new(90, 140, 210);
    internal static readonly Color TextPrimary = new(236, 240, 248);
    internal static readonly Color TextAccent = new(118, 214, 255);
    internal static readonly Color TextMuted = new(146, 156, 180);
    internal static readonly Color PlayerColor = new(255, 190, 60);
}
