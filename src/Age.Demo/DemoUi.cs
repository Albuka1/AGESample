using Age.Core;
using Age.UI;

namespace Age.Demo;

internal sealed class DemoUi
{
    private const float PanelX = 12f;
    private const float PanelY = 12f;
    private const float PanelWidth = 520f;
    private const float PanelHeight = 100f;
    private const float LineHeight = 14f;
    private const float ButtonWidth = 110f;
    private const float ButtonHeight = 26f;
    private const float ButtonTop = 68f;

    internal Entity Panel { get; private set; }

    internal Entity TitleLabel { get; private set; }

    internal Entity StatusLabel { get; private set; }

    internal Entity HelpLabel { get; private set; }

    internal Entity PauseButton { get; private set; }

    internal Entity ResetButton { get; private set; }

    internal Entity ZoomInButton { get; private set; }

    internal Entity ZoomOutButton { get; private set; }

    internal void Build(World world)
    {
        ArgumentNullException.ThrowIfNull(world);

        Panel = CreatePanel(world);
        TitleLabel = CreateLabel(world, 24f, 20f, 1, DemoConfig.Title, DemoConfig.TextAccent);
        StatusLabel = CreateLabel(world, 24f, 20f + LineHeight, 1, string.Empty, DemoConfig.TextPrimary);
        HelpLabel = CreateLabel(world, 24f, 20f + (LineHeight * 2f), 1, DemoConfig.Help, DemoConfig.TextMuted);

        PauseButton = CreateButton(world, 24f, "PAUSE", 2);
        ResetButton = CreateButton(world, 142f, "RESET", 3);
        ZoomInButton = CreateButton(world, 260f, "ZOOM IN", 4);
        ZoomOutButton = CreateButton(world, 378f, "ZOOM OUT", 5);
    }

    private static Entity CreatePanel(World world)
    {
        Entity entity = world.CreateEntity();
        world.Set(entity, new RectTransformComponent
        {
            Position = new Vector2(PanelX, PanelY),
            Size = new Vector2(PanelWidth, PanelHeight),
            ZOrder = 0,
            Visible = true,
        });
        world.Set(entity, new ButtonComponent { BaseColor = DemoConfig.PanelColor, Interactable = false });

        return entity;
    }

    private static Entity CreateLabel(World world, float x, float y, int zOrder, string text, Color color)
    {
        Entity entity = world.CreateEntity();
        world.Set(entity, new RectTransformComponent
        {
            Position = new Vector2(x, y),
            Size = new Vector2(PanelWidth - 24f, BitmapFontMetrics.GlyphHeight),
            ZOrder = zOrder,
            Visible = true,
        });
        world.Set(entity, new TextLabelComponent { Text = text, Color = color });

        return entity;
    }

    private static Entity CreateButton(World world, float x, string caption, int zOrder)
    {
        Entity entity = world.CreateEntity();
        world.Set(entity, new RectTransformComponent
        {
            Position = new Vector2(x, ButtonTop),
            Size = new Vector2(ButtonWidth, ButtonHeight),
            ZOrder = zOrder,
            Visible = true,
        });
        world.Set(entity, new ButtonComponent { BaseColor = DemoConfig.ButtonIdle, Interactable = true });

        float captionWidth = caption.Length * BitmapFontMetrics.GlyphWidth;
        Entity captionEntity = world.CreateEntity();
        world.Set(captionEntity, new RectTransformComponent
        {
            Position = new Vector2(
                x + ((ButtonWidth - captionWidth) * 0.5f),
                ButtonTop + ((ButtonHeight - BitmapFontMetrics.GlyphHeight) * 0.5f)),
            Size = new Vector2(captionWidth, BitmapFontMetrics.GlyphHeight),
            ZOrder = zOrder + 10,
            Visible = true,
        });
        world.Set(captionEntity, new TextLabelComponent { Text = caption, Color = DemoConfig.TextPrimary });

        return entity;
    }
}
