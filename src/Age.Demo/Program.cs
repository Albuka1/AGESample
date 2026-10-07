using Age.Assets;
using Age.Audio;
using Age.Core;
using Age.Demo;
using Age.Input;
using Age.Physics;
using Age.Rendering;
using Age.UI;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddAgeCore();
services.AddAgeAssets();
services.AddAgeInput();
services.AddAgeAudio();
services.AddAgePhysics();
services.AddAgeUI();
services.AddAgeRendering();

services.AddSingleton<WindowInputService>();
services.AddSingleton<IInputService>(provider => provider.GetRequiredService<WindowInputService>());

services.AddSingleton<DemoState>();
services.AddSingleton<DemoScene>();
services.AddSingleton<DemoUi>();

services.AddSingleton<MotionSystem>();
services.AddSingleton<PlayerSystem>();
services.AddSingleton<CollisionTintSystem>();
services.AddSingleton<CameraSystem>();
services.AddSingleton<UiInteractionSystem>();
services.AddSingleton<HudSystem>();

using ServiceProvider provider = services.BuildServiceProvider();

IWindowService windowService = provider.GetRequiredService<IWindowService>();
windowService.Create(DemoConfig.WindowWidth, DemoConfig.WindowHeight, "Auae Game Engine");

IRenderer renderer = provider.GetRequiredService<IRenderer>();
renderer.Attach(windowService);

WindowInputService input = provider.GetRequiredService<WindowInputService>();

var world = new World();

DemoScene scene = provider.GetRequiredService<DemoScene>();
scene.Build(world);

DemoUi ui = provider.GetRequiredService<DemoUi>();
ui.Build(world);

var viewport = new Vector2(DemoConfig.WindowWidth, DemoConfig.WindowHeight);
TransformComponent playerTransform = world.Get<TransformComponent>(scene.Player);
Vector2 playerSize = world.Get<SpriteComponent>(scene.Player).Size;
Vector2 playerCenter = playerTransform.Position + (playerSize * 0.5f);

DemoState state = provider.GetRequiredService<DemoState>();
state.Camera = new Camera2D
{
    Position = playerCenter - (viewport * 0.5f),
    Zoom = 1f,
    ViewportSize = viewport,
};

SystemPipeline pipeline = provider.GetRequiredService<SystemPipeline>();
pipeline.Add(provider.GetRequiredService<PlayerSystem>());
pipeline.Add(provider.GetRequiredService<MotionSystem>());
pipeline.Add(provider.GetRequiredService<CollisionSystem>());
pipeline.Add(provider.GetRequiredService<CollisionTintSystem>());
pipeline.Add(provider.GetRequiredService<UIUpdateSystem>());
pipeline.Add(provider.GetRequiredService<UiInteractionSystem>());
pipeline.Add(provider.GetRequiredService<CameraSystem>());
pipeline.Add(provider.GetRequiredService<HudSystem>());

RenderSystem renderSystem = provider.GetRequiredService<RenderSystem>();
UIRenderSystem uiRenderSystem = provider.GetRequiredService<UIRenderSystem>();
RenderPipeline renderPipeline = provider.GetRequiredService<RenderPipeline>();
renderPipeline.Add(renderSystem);
renderPipeline.Add(uiRenderSystem);
SplashScreen splash = provider.GetRequiredService<SplashScreen>();

// The loop opens the input frame of every registered input service and then advances the simulation in fixed steps, so
// the demo no longer has to call BeginFrame itself and no longer moves by the time a frame happened to take.
provider.GetRequiredService<IGameLoop>().Run(
    update: step => world.Update(step, pipeline),
    render: time =>
    {
        if (splash.Draw(renderer, time, input))
        {
            return;
        }

        // The camera takes the size of the frame before the passes run, so its culling agrees with the projection of the
        // renderer, including after the window was resized.
        state.Camera.ViewportSize = renderer.ViewportSize;
        renderPipeline.Render(world, state.Camera);
    });

// The renderer owns device objects that live in the OpenGL context of the window, so it is disposed while the window is
// still open; the container then disposes the services, and every call of theirs is a no-op by then.
splash.Dispose();
renderer.Dispose();
windowService.Close();
