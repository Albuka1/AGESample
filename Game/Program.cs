using System.Globalization;
using System.Text.Json;
using Age.Assets;
using Age.Audio;
using Age.Content;
using Age.Content.Locale;
using Age.Content.Prototypes;
using Age.Core;
using Age.Input;
using Age.Physics;
using Age.Platformer;
using Age.Rendering;
using Age.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// What this demo was written for is what decides whether it runs at all: the version of the engine that the game declares in
// `game.version.json` beside its executable is compared with the one that is running it, and a game of another contract is refused
// here, before a window is opened or a service is built. Before 1.0 the minor number carries a change a game has to follow, so 0.4
// and 0.5 are not the same contract; a game of the same contract that names another build runs with a line, which is the usual case
// of a build that moved forward.
GameVersion? declared = VersionCheck.ReadExecutable();

if (declared is not null)
{
    VersionMatch match = VersionCheck.Compare(EngineVersion.Value, declared.Engine);

    if (match == VersionMatch.Different)
    {
        Console.Error.WriteLine($"This demo was written for AGE {declared.Engine} and the engine that is running it is {EngineVersion.Value}.");
        Console.Error.WriteLine("The two do not name the same contract, so this demo is not started: read the CHANGELOG for what changed between them.");
        return 1;
    }

    if (match == VersionMatch.Compatible)
    {
        Console.Error.WriteLine($"This demo was written for AGE {declared.Engine} and the engine that is running it is {EngineVersion.Value}: a different build of the same contract.");
    }
}

var services = new ServiceCollection();

// What the engine and this demo report goes into the console of the developer overlay, which is what makes a file that failed to load
// or a device that refused something visible while the game runs. Logging is registered before `AddAgeCore`, because that call adds a
// logger that reports nothing only while nothing else is registered.
services.AddLogging(builder =>
{
    builder.SetMinimumLevel(LogLevel.Information);
    builder.Services.AddSingleton<ILoggerProvider>(provider => new ConsoleLoggerProvider(provider.GetRequiredService<IConsoleService>()));
});

// The services of the engine, one call per assembly. This demo uses all of them, because being a demonstration of what the engine can
// do is the whole of what it is for: a game of your own registers what it uses and leaves the rest out.
services.AddAgeCore();
services.AddAgeContent();
services.AddAgeAssets();
services.AddAgeInput();
services.AddAgeAudio();
services.AddAgeOpenALAudio();
services.AddAgePhysics();
services.AddAgeUI();
services.AddAgeRendering();
services.AddAgeSilkInput();

// This demo says what it is called, so its settings and its saves live in a folder of their own under the roaming application data of
// the account. `AddAgeCore` registers a name taken from the entry assembly and the last registration is the one that answers.
services.AddSingleton<IUserDataService>(_ => new UserDataService(DemoConfig.Title));

using ServiceProvider provider = services.BuildServiceProvider();

var world = new World();


// The systems of the engine that own the time of the game. A timer counts down and announces its end; a tween computes a value and
// announces it; the animation of a sprite sheet plays on the same fixed steps, so a goblin spawned from content walks because its
// prototype names a state of its sheet rather than because this demo counts frames.
SystemPipeline pipeline = provider.GetRequiredService<SystemPipeline>();
pipeline.Add(provider.GetRequiredService<CollisionSystem>());
pipeline.Add(provider.GetRequiredService<TimerSystem>());
pipeline.Add(provider.GetRequiredService<TweenSystem>());
pipeline.Add(provider.GetRequiredService<SpriteAnimationSystem>());

// The interface is a frame system rather than a step system: the pointer is a state of the frame and keeps working while the
// simulation is paused, which is the whole point of a pause menu. The layout runs first, because it is what turns the anchors of an
// element into the rectangle that the pointer test and the pass of the UI both read.
pipeline.AddFrame(provider.GetRequiredService<UILayoutSystem>());
pipeline.AddFrame(provider.GetRequiredService<UIUpdateSystem>());

IWindowService windowService = provider.GetRequiredService<IWindowService>();
windowService.Create(DemoConfig.WindowWidth, DemoConfig.WindowHeight, DemoConfig.Title);

IRenderer renderer = provider.GetRequiredService<IRenderer>();
renderer.Attach(windowService);

// A game that loads behind the logo says what it has to do and lets the frame show the logo and the bar under it while the steps run,
// one per frame: the window is on screen from the first step rather than after the last one. The handles the steps produce are
// declared here, because the callbacks of the frame read them and every one of them is assigned before the loop that reads it runs.
var loading = new List<Action>();
int loadingStep = 0;

IAssetLoader assets = provider.GetRequiredService<IAssetLoader>();
PrototypeManager prototypes = provider.GetRequiredService<PrototypeManager>();
SpawnService spawner = provider.GetRequiredService<SpawnService>();
IMaterialService materials = provider.GetRequiredService<IMaterialService>();
ITextureService textures = provider.GetRequiredService<ITextureService>();
IFontService fonts = provider.GetRequiredService<IFontService>();
IShaderService shaders = provider.GetRequiredService<IShaderService>();
ISoundService sounds = provider.GetRequiredService<ISoundService>();
IInputService input = provider.GetRequiredService<IInputService>();
IGameLoop gameLoop = provider.GetRequiredService<IGameLoop>();
SplashScreen splash = provider.GetRequiredService<SplashScreen>();
FixedTimestep timestep = provider.GetRequiredService<FixedTimestep>();
ISceneSerializer scenes = provider.GetRequiredService<ISceneSerializer>();
RenderPipeline renderPipeline = provider.GetRequiredService<RenderPipeline>();
IConsoleService console = provider.GetRequiredService<IConsoleService>();
CVarService cvars = provider.GetRequiredService<CVarService>();
ILocaleService locale = provider.GetRequiredService<ILocaleService>();
IUserDataService userData = provider.GetRequiredService<IUserDataService>();
DevOverlay overlay = provider.GetRequiredService<DevOverlay>();
DevConsoleOverlay consoleOverlay = provider.GetRequiredService<DevConsoleOverlay>();
IDevWindowService devWindow = provider.GetRequiredService<IDevWindowService>();

FontHandle font = default;
ShaderHandle vignette = default;
SoundHandle click = default;

Entity hero = default;
Entity companion = default;
Entity lastSpawned = default;

// The camera of the frame that was drawn last, which is the one a person is looking through now. The command `point` of the console
// asks it where a position of the screen is in the world, which is what a click asks before it decides what stands where.
Camera2D drawnThrough = new() { Position = Vector2.Zero, Zoom = 1f, ViewportSize = DemoConfig.Design };
bool fitWorld = false;

VignettePass vignettePass = null!;


// A bus belongs to a world, so every world this demo makes needs its subscriptions again: the collision system announces each pair it
// found, the interface announces a press on a button, and a timer announces that it ran out. All of them save this demo from looking
// for the same thing every step, and all of them run at the boundary of the step, which is where the world hands them over.
void SubscribeEvents(World subscribed)
{
    subscribed.Events.Subscribe<CollisionEvent>((_, collision) => DemoActions.TintByCollision(subscribed, collision.First, collision.Second));

    // A click of a button is heard through the UI channel of the mixer, which is what the audio of 0.5.0 added: a sound is no
    // longer just a number and a loop flag, it is a value that says how it is heard, and a channel is one of the six values of it.
    subscribed.Events.Subscribe<ButtonPressedEvent>((_, _) =>
        sounds.Play(click, AudioPlaybackSettings.Default(AudioChannel.Ui)));

    subscribed.Events.Subscribe<TimerElapsedEvent>((_, timer) => subscribed.RequestDestroy(timer.Entity));

    // The entity of an event is the one the event carries, because the world raises these without naming one to the handler. A tween
    // computes a value and announces it; where that value belongs is the business of the game, which is what keeps the tween free of
    // a delegate that a scene could neither save nor describe without reflection.
    subscribed.Events.Subscribe<TweenUpdatedEvent>((_, tween) =>
    {
        if (subscribed.IsAlive(tween.Entity) && subscribed.Has<TransformComponent>(tween.Entity))
        {
            subscribed.GetRef<TransformComponent>(tween.Entity).Rotation = tween.Value;
        }
    });
}

// The pages of the developer window that gather the scene: the tree of the interface, the images the run resolved, and the prototypes
// with what was built from them. Each is a tab of its own, so a person reads one thing at a time rather than a wall of numbers. The
// world page belongs to this demo rather than to the engine and carries a cross, because it reports the world of a run rather than
// what the engine holds: the console stays, this page comes and goes.
devWindow.Add(new WorldTab(() => world, timestep));
devWindow.Add(new UITreeTab(() => world));
devWindow.Add(new TexturesTab(textures, () => world));
devWindow.Add(new PrototypesTab(() => prototypes.Ids, () => world));

// Where the choices of a person are kept: a file under the roaming application data of the account, in the folder of this demo, so a
// setting that was written while the game ran is what the next run starts with and the folder the game was installed into is never
// written to. The language is one of those settings, which is what makes a choice survive a restart.
string settingsPath = userData.PathIn(UserDataFolder.Data, "settings.json");
string scenePath = userData.PathIn(UserDataFolder.Saves, "scene.json");

// The settings file is meant to be read and changed by a person, so it is written with the indentation of a document rather than as
// one line, and the names of the settings are kept as they were registered.
var settingsJson = new JsonSerializerOptions { WriteIndented = true };

// Writes every setting of this demo to the settings file, which is what `cvars set` does after a setting takes a value: the file holds
// one pair per setting, so what a person chose is read back the next time rather than asked again.
void SaveSettings()
{
    string json = JsonSerializer.Serialize(
        cvars.Values.ToDictionary(cvar => cvar.Name, cvar => cvar.Text),
        settingsJson);

    File.WriteAllText(settingsPath, json);
}

// Reads the settings file, when there is one, and writes what it holds over the settings of this demo. A file that is not there or
// does not read leaves every setting as it was registered, which is what a first run is.
void LoadSettings()
{
    if (!File.Exists(settingsPath))
    {
        return;
    }

    Dictionary<string, string> written;

    try
    {
        written = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(settingsPath), settingsJson) ?? [];
    }
    catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
    {
        // A file that is malformed, or that cannot be read because it is held by another program or the game has no right to it, is not
        // a reason to stop the game: the settings start as they were registered and the reason is a line of the console.
        console.WriteWarning($"the settings file {settingsPath} does not read, so the settings start as they were registered");
        return;
    }

    // The language is applied after the file: a file that holds a language this demo does not ship leaves the one the system chose.
    cvars.Apply(written);
    locale.Language = cvars.Get<string>("locale");
}


// The content of a game is read from a folder beside the executable, and every path a document or a line of this demo names is
// relative to it: `Textures/Tiles/tiles.bmp` is the file `Resources/Textures/Tiles/tiles.bmp` of the repository, and a build copies
// the folder next to the executable, which is what makes that copy the asset root.
loading.Add(() => assets.Initialize(Path.Combine(AppContext.BaseDirectory, "Resources")));

// The settings of a person are read once the loader knows where the game keeps its files: the language of the settings file wins over
// the one the system is set to, which is what makes a choice survive a restart.
loading.Add(LoadSettings);

// The content of a game is data: a document under Resources/Prototypes declares a prototype by its identifier, names the components it
// carries with the values they start with, and inherits the rest from a parent. The manager reads every file first and then resolves
// them, so a mistake in a document is a message here rather than a surprise in a fight. A document whose type is `entity` is read as
// an entity, which is what a spawn makes out of it.
loading.Add(() =>
{
    prototypes.Register(EntityPrototype.Kind, EntityPrototype.Read);
    prototypes.Register(MaterialPrototype.Kind, MaterialPrototype.Read);
    console.Write($"Loaded {prototypes.Load(assets, "Prototypes")} prototypes.");
});

// A material is the stage of a shader and the values its uniforms start with, read from the content: a layer of a sprite names the
// material rather than the path of a stage, so a sprite that pulses is a line of a document rather than a line of code, and the same
// material draws every sprite that names it.
loading.Add(() =>
{
    foreach (MaterialPrototype material in prototypes.Enumerate<MaterialPrototype>())
    {
        materials.Register(material.Id, material);
    }

    materials.Build();
});

// The font the HUD of this demo is drawn in, the vignette over the frame, and the sound of a click: one step each, so the bar under the
// logo moves three times while the three files are read.
loading.Add(() => font = fonts.Load(DemoConfig.FontPath, 24f, ' ', '\u04FF'));

// A shader of the content: the fragment stage under Resources/Shaders is read through the asset loader, compiled once, and drawn with
// the vertex stage of the engine, which places the quad. This one is drawn over the frame at the end of it, which is what a pass of a
// game adds of its own.
loading.Add(() => vignette = shaders.Load(DemoConfig.VignettePath));
loading.Add(() => click = sounds.Load(DemoConfig.ClickPath));

// The entities of this demo. The two that are moved by hand are built here so that the demo has something on screen even if the
// content of the game is missing; the goblin and the beacon are built from the documents under Resources/Prototypes, which is what
// shows that a prototype is the way a game declares an entity rather than a way it saves one.
loading.Add(() =>
{
    hero = world.CreateEntity();
    world.Set(hero, new TransformComponent { Position = new Vector2(400f, 300f), Scale = new Vector2(1f, 1f) });
    world.Set(hero, new SpriteComponent { TexturePath = DemoConfig.TilePath, Size = new Vector2(64f, 64f), Color = Color.White, ZOrder = 0 });
    world.Set(hero, new ColliderComponent { Size = new Vector2(64f, 64f) });

    companion = world.CreateEntity();
    world.Set(companion, new TransformComponent { Position = new Vector2(520f, 380f), Scale = new Vector2(1f, 1f) });
    world.Set(companion, new SpriteComponent { TexturePath = DemoConfig.TilePath, Size = new Vector2(48f, 48f), Color = Color.Green, ZOrder = 1 });
    world.Set(companion, new ColliderComponent { Size = new Vector2(48f, 48f) });

    // A tween turns the companion on the spot, which is what a value of an entity that nothing else reads a step for looks like: the
    // tween computes it and announces it, and the subscription above writes it where Transform holds it.
    world.Set(companion, new TweenComponent { From = 0f, To = MathF.Tau, Duration = 6f, Looping = true });

    // What a spawn of the content makes: a walking goblin whose frames come from the sheet its prototype names, and a beacon of two
    // layers, the second of which is drawn by the pulse material of the content rather than by a path in the code.
    if (prototypes.Has<EntityPrototype>("Goblin"))
    {
        spawner.Spawn(world, "Goblin", new Vector2(300f, 480f));
    }

    if (prototypes.Has<EntityPrototype>("Beacon"))
    {
        spawner.Spawn(world, "Beacon", new Vector2(760f, 260f));
    }
});

// The passes of the drawing, in the order they draw: the world, then its text, then the interface, and then the vignette of this demo,
// which reads the frame the three of them left behind.
loading.Add(() =>
{
    renderPipeline.Add(provider.GetRequiredService<RenderSystem>());

    // The text of a world is a pass of its own, between the world and the interface: a line stands where the entity that carries it
    // stands, so it belongs to the world, and it is drawn over the sprites and under the UI.
    renderPipeline.Add(provider.GetRequiredService<TextRenderSystem>());
    renderPipeline.Add(provider.GetRequiredService<UIRenderSystem>());

    // The vignette of this demo is a pass like the ones of the engine, which is what the pipeline is for: it runs after the world and
    // the interface and before the overlay, so the console and the numbers of the developer stay above it.
    vignettePass = new VignettePass(renderer, vignette);
    renderPipeline.Add(vignettePass);

    // The developer console takes the whole input while it is open, and the numbers of a frame are read while the game runs, so the two
    // are passes of their own. The console opens with the grave accent, Tab completes or walks the suggestions, and F3 shows and hides
    // the numbers. This is what a person presses first, and it is why the demo has a console at all.
    consoleOverlay.OpenKey = Key.GraveAccent;
    renderPipeline.Add(consoleOverlay);
    renderPipeline.Add(overlay);
});

// A scene on disk replaces the world this demo built, which is why this is the last step: the content is loaded by then, so a document
// that names a prototype of the game resolves, and the game starts with the scene rather than with the entities above.
loading.Add(() =>
{
    if (File.Exists(scenePath))
    {
        world = DemoActions.LoadScene(scenes, scenePath);
        hero = DemoActions.FirstSprite(world);
        console.Write($"Loaded the scene from {scenePath}.");
    }

    // A bus belongs to a world rather than to the game, so the subscriptions are made for the world that is there once every step ran:
    // a scene that replaced it above has a bus of its own, and every handler has to be added to that one.
    SubscribeEvents(world);

    // One sprite is already on its way when the game starts, so the HUD has something to report and the slot logic runs before the
    // first key press. It belongs to the world that is there now, which is the scene when there is one.
    lastSpawned = DemoActions.SpawnFading(world, new Vector2(640f, 560f), cvars.Get<float>("spawnLifetime"));
});


// The settings of this demo become commands of the console: `cvars set spawnLifetime 0.5` changes what the line above reads from then
// on, and what a person chooses is written to a file of their account, so it is what the game starts with next time.
cvars.Register("spawnLifetime", DemoConfig.SpawnLifetime, "How long a sprite that the key `E` puts on screen lives, in seconds.");

// The language the strings are read in is a setting rather than a way the game was built: a person changes it while the game runs, and
// everything that asks the locale service for a key answers in the new language from then on. It starts as the one the system is set
// to, when the demo ships that language, and as the base language otherwise, and a choice written to the settings file wins over both.
cvars.Register("locale", locale.Language, "The language the strings of the game are read in, such as en or ru.");

// The levels of the command after the name: `cvars` lists the settings (registered by the service itself), `cvars get <name>` answers
// one, and `cvars set <name> <value>` writes it and saves it. The words are levels of a tree rather than parts of one name, so each of
// them is completed and suggested on its own, and nothing is typed until the level that needs it is reached.
console.Register("cvars get", "Answers what a setting holds. Usage: cvars get spawnLifetime.", arguments =>
{
    if (arguments.Count == 0)
    {
        console.WriteError("usage: cvars get <name>");
        return;
    }

    console.Write($"{arguments[0]} = {cvars.GetText(arguments[0])}");
});

console.Register("cvars set", "Writes a setting and saves it. Usage: cvars set spawnLifetime 4, cvars set locale ru.", arguments =>
{
    if (arguments.Count < 2)
    {
        console.WriteError("usage: cvars set <name> <value>");
        return;
    }

    if (!cvars.SetFromText(arguments[0], arguments[1]))
    {
        console.WriteError($"there is no setting called {arguments[0]}");
        return;
    }

    SaveSettings();
    console.Write($"{arguments[0]} = {cvars.GetText(arguments[0])}, saved to {settingsPath}");
});

// A spawn of the content, so a person sees a prototype built into the world rather than reads about it. The command reports the slot,
// which is the identifier of the entity and its generation, so a person watches a slot be handed out again.
console.Register("goblin", "Puts a Goblin of the content in the world, at 320 by 240.", _ =>
{
    Entity spawned = spawner.Spawn(world, "Goblin", new Vector2(320f, 240f));
    console.Write($"goblin spawned as entity {spawned.Id} generation {spawned.Generation}");
});

console.Register("beacon", "Puts a Beacon of the content in the world, at 700 by 360, the second layer of which the pulse material draws.", _ =>
    spawner.Spawn(world, "Beacon", new Vector2(700f, 360f)));


// The shader of this demo is a setting of the sample rather than of the engine: this turns it off and on while the game runs, which is
// what shows that the quads collected before a shader changed are drawn with the state they were collected under.
console.Register("vignette", "Turns the vignette of this demo on and off, which is a pass of the content drawn over the frame.", _ =>
{
    vignettePass.Enabled = !vignettePass.Enabled;
    console.Write($"vignette {(vignettePass.Enabled ? "on" : "off")}");
});

// The scaler of the interface is content of this demo rather than a decision of the engine, and the layout system resolves it on every
// frame: `ui` reports the scale the canvas ended up with and where the anchored elements were put, and `ui 0`, `ui 1` and `ui 0.5`
// change the axis the scale follows, which is what a person tuning a layout watches.
console.Register("ui", "Reports the scaler of the interface and the rectangle of every anchored element, and sets which axis the scale follows when it is given a value.", arguments =>
{
    Entity found = world.Enumerate<CanvasComponent>().FirstOrDefault();

    if (!world.IsAlive(found) || !world.Has<CanvasComponent>(found))
    {
        console.Write("this world holds no canvas, so the interface is measured against the window itself");
        return;
    }

    ref CanvasComponent canvas = ref world.GetRef<CanvasComponent>(found);

    if (arguments.Count > 0 && float.TryParse(arguments[0], CultureInfo.InvariantCulture, out float match))
    {
        canvas.Match = match;
    }

    console.Write($"canvas {canvas.Resolution.X:0}x{canvas.Resolution.Y:0} design units at scale {canvas.Scale:0.###}, match {canvas.Match:0.##}, window {renderer.ViewportSize.X:0}x{renderer.ViewportSize.Y:0}");

    foreach (Entity element in world.Enumerate<RectTransformComponent>())
    {
        RectTransformComponent rect = world.Get<RectTransformComponent>(element);

        if (rect.Anchored)
        {
            console.Write($"  anchored from {rect.AnchorMin.X:0.#},{rect.AnchorMin.Y:0.#} to {rect.AnchorMax.X:0.#},{rect.AnchorMax.Y:0.#} sits at {rect.Position.X:0},{rect.Position.Y:0} with a size of {rect.Size.X:0}x{rect.Size.Y:0}");
        }
    }
});

// Where the pointer is, in the three spaces a click has to tell apart: the window, the interface and the world. This is what a person
// runs before writing a pick of their own, because it answers whether the camera is where they think it is.
console.Register("point", "Reports where the pointer is on the screen and in the world, and where that world position is drawn.", _ =>
{
    Vector2 screen = input.MousePosition;
    Vector2 inWorld = drawnThrough.ScreenToWorld(screen);
    Vector2 back = drawnThrough.WorldToScreen(inWorld);

    console.Write($"pointer {screen.X:0}x{screen.Y:0} on screen is {inWorld.X:0}x{inWorld.Y:0} in the world, and that is drawn back at {back.X:0}x{back.Y:0}");
});

// The two ways of building the camera: one unit per pixel of the window, which shows more of the world in a larger window, and the area
// this demo is authored against, which looks the same at every resolution. `fit` switches between them and reports what the window shows.
console.Register("fit", "Reports how the camera of the world is built and switches it to the other of the two ways.", _ =>
{
    fitWorld = !fitWorld;

    Camera2D fitted = Camera2D.Fit(DemoConfig.Design, renderer.ViewportSize, CameraFit.Cover);
    Vector2 view = fitWorld ? fitted.VisibleWorld.Size : renderer.ViewportSize;

    console.Write($"world {(fitWorld ? "fitted" : "one unit per pixel")}: window {renderer.ViewportSize.X:0}x{renderer.ViewportSize.Y:0} shows {view.X:0}x{view.Y:0} units of the world");
});


// A dialog of the content rather than a string of the code: the key `ui-entities` is read in the language the settings chose, and the
// count is passed as a value, which is what makes the two languages one document each rather than a branch of the code.
console.Register("strings", "Reports what the content of the demo says in the language that was chosen.", _ =>
{
    console.Write($"language {locale.Language}, base {locale.BaseLanguage}");
    console.Write($"items: {locale.Get("ui-entities", ("count", 1))} / {locale.Get("ui-entities", ("count", 4))}");

    if (prototypes.TryGet("Goblin", out EntityPrototype? goblin) && goblin is not null)
    {
        console.Write($"goblin: {locale.NameOf(goblin)} / {locale.Describe(goblin)}");
    }
});

// A sprite whose image a build does not ship, which is the mistake the placeholder exists for: the sprite is drawn as a bright ERROR
// checkerboard and one line of the console names the file, and the game keeps running rather than taking the frame down with it.
console.Register("missing", "Puts a sprite whose image is not there on screen, which is what the placeholder of an image is for.", _ =>
    DemoActions.SpawnMissing(world, new Vector2(640f, 200f)));

// The way out of the game is a command rather than a key, so a key does not have to be kept free for leaving and a person who types
// `quit` is not surprised by it: the loop stops once the frame that runs the command is drawn, which is what the shutdown waits for.
console.Register("quit", "Closes the game.", _ => gameLoop.Stop());

// The developer window stands beside the game as a window of the operating system rather than a panel over the frame, so it is opened by
// a command rather than a key: what it holds is a place to look at the engine, and a key of a game is better spent on the game.
console.Register("devwindow", "Shows or hides the developer window beside the game.", _ =>
{
    devWindow.Toggle();
    console.Write($"developer window {(devWindow.IsOpen ? "open" : "closed")}");
});


// The simulation runs in fixed steps, so movement, collision and the interface advance by the same amount on every frame at any frame
// rate. The loading, the splash, the keys and the drawing run once per frame, and the simulation stays paused until the last loading
// step is done, so nothing moves behind the logo. `Space` pauses the clock, which stops the steps without stopping the frames, and
// holding `Ctrl` slows the simulation down with `FixedTimestep.TimeScale`.
bool started = false;

// The lines the HUD reports, which are drawn at the bottom of the frame in the font of this demo. The height of a line and the margin
// from the edges are constants rather than magic numbers, because the loop that stacks the lines and the line that is cut both read them.
const float HudLineHeight = 30f;
const float HudMargin = 12f;

gameLoop.Run(
    update: step =>
    {
        if (!started)
        {
            return;
        }

        // The keys that move the sprite belong to the game while the game is the window a person is typing into, and to the console while
        // a line is being typed into it: the console is a text field and takes the whole keyboard, which is what a line typed into it
        // means. The developer window stands beside the game and is a window of the operating system of its own, so while it has the
        // focus the keys are going there and the sprite stands still; the moment it gives the focus back the sprite moves again, which is
        // what makes the two windows live side by side rather than one taking the other's input away.
        if (!consoleOverlay.IsVisible && !devWindow.Focused)
        {
            DemoActions.Move(world, hero, input, step);
        }

        world.Update(step, pipeline);

        // A contact is announced by `CollisionEvent`, but a pair that came apart raises nothing: the collision component of the sprite is
        // gone and the event was about the contact, so the calm colour goes back on here, every step.
        DemoActions.RestoreCalmColour(world, hero, Color.White);
        DemoActions.RestoreCalmColour(world, companion, Color.Green);
    },
    render: time =>
    {
        // The steps of the loading of this demo run one per frame while the logo is on screen, and the bar under it shows how many of
        // them are left: the window is drawn from the first frame rather than standing empty until everything is ready. A step that reads
        // a file or compiles a program takes the frame it runs in, which is what the logo is there for.
        if (loadingStep < loading.Count)
        {
            loading[loadingStep]();
            loadingStep++;
            splash.Progress = loadingStep / (float)loading.Count;

            if (loadingStep == loading.Count)
            {
                // The last step is done, so the game starts now instead of waiting out the rest of the duration of the logo.
                splash.End();
            }
        }

        // The logo is drawn on every frame, including the frames of the loading, which is what makes the bar under it move: the result is
        // kept rather than tested in the condition, because a call that is short-circuited away is a frame of the logo that was never
        // drawn. The game starts once every step ran and the logo is done with the frame.
        bool drawn = splash.Draw(renderer, time, input);

        if (loadingStep < loading.Count || drawn)
        {
            return;
        }

        started = true;

        // The splash is over, so the clock runs: this is the one place the simulation is let go, and from here the tick of the HUD counts
        // the steps of this game. `Space` and the console own the clock from now on, so nothing resets it on a frame.
        timestep.Paused = false;

        // The frame systems run on the time of this frame, whether or not the simulation advanced, so the interface and the overlays keep
        // working while the clock is paused.
        world.UpdateFrame(time, pipeline);

        // The console reads the keys of this frame before the passes draw, and the numbers of a frame are read beside it. The console
        // takes the whole input while it is visible, so the game below reads the keys only when it is not. While the developer window has
        // the focus the keys are going to that window rather than to the game, so the console of the game reads nothing: a line typed into
        // the window beside the game is not a line typed into the game, which is what keeps the two consoles apart.
        if (!devWindow.Focused)
        {
            consoleOverlay.Update(time);
        }

        // The numbers of a frame stand below the console while it is open, which is what keeps the two from drawing over one another: the
        // console reports the height of its panel and the overlay is offset by it.
        overlay.TopMargin = consoleOverlay.PanelHeight;
        overlay.Update(time);

        // The developer window stands beside the game rather than over it, so it is opened by the `devwindow` command of the console and
        // pumped every frame: pumping it draws its page, which is what keeps it in step with the game without a loop of its own.
        devWindow.Pump(time);

        if (!consoleOverlay.IsVisible && !devWindow.Focused)
        {
            if (input.IsKeyPressed(Key.Space))
            {
                timestep.Paused = !timestep.Paused;
            }

            // Held rather than pressed: the factor is a live state of the clock, so holding the key slows the world down and letting go
            // brings it back to normal speed.
            timestep.TimeScale = input.IsKeyDown(Key.ControlLeft) ? 0.25d : 1d;

            if (input.IsKeyPressed(Key.E))
            {
                lastSpawned = DemoActions.SpawnFading(world, input.MousePosition, cvars.Get<float>("spawnLifetime"));
                console.Write($"spawned entity {lastSpawned.Id} generation {lastSpawned.Generation} at the pointer");
            }

            if (input.IsKeyPressed(Key.F))
            {
                File.WriteAllText(scenePath, scenes.Save(world));
                console.Write($"Saved the scene to {scenePath}.");
            }

            if (input.IsKeyPressed(Key.R) && File.Exists(scenePath))
            {
                world = DemoActions.LoadScene(scenes, scenePath);
                hero = DemoActions.FirstSprite(world);
                SubscribeEvents(world);
                console.Write("Loaded the scene again.");
            }
        }

        // The camera of the world: one unit per pixel by default, so a larger window shows more of it, and the area this demo is authored
        // against when `fit` turned it that way, so a window of any shape shows the same view cropped rather than stretched. The camera is
        // kept rather than rebuilt, because the command that reports the pointer reads the one that drew the last frame.
        drawnThrough = fitWorld
            ? Camera2D.Fit(DemoConfig.Design, renderer.ViewportSize, CameraFit.Cover)
            : new Camera2D { Position = Vector2.Zero, Zoom = 1f, ViewportSize = renderer.ViewportSize };

        renderPipeline.Render(world, drawnThrough);

        // The HUD of this demo is drawn over everything, in the font of the content and with the service of the engine rather than a
        // bitmap font: a line that a game draws itself is what a font handle is for. A line longer than the window is wrapped first, so
        // the words of a developer do not run off the edge of the frame.
        renderer.BeginFrame(false);

        float wrapWidth = renderer.ViewportSize.X - (HudMargin * 2f);

        List<(string Text, Color Colour)> lines =
        [
            .. DemoActions.Wrap(fonts, font, $"window {renderer.ViewportSize.X:0}x{renderer.ViewportSize.Y:0}  font, sheets and shaders of the content  prototypes {prototypes.Count}", wrapWidth)
                .Select(line => (line, new Color(220, 220, 220))),
            .. DemoActions.Wrap(fonts, font, $"tick {timestep.Tick}  {(timestep.Paused ? "paused, press Space" : "running")}  holding Ctrl is 0.25x  WASD moves, E spawns, F saves, R loads, ` opens the console", wrapWidth)
                .Select(line => (line, new Color(150, 200, 160))),
        ];

        for (var index = 0; index < lines.Count; index++)
        {
            float y = renderer.ViewportSize.Y - ((lines.Count - index) * HudLineHeight) - HudMargin;
            fonts.Draw(font, lines[index].Text, new Vector2(HudMargin, y), lines[index].Colour);
        }

        renderer.EndFrame();
    });

// The developer window of the operating system is closed before the rest of the shutdown, because it owns a context of its own: what it
// holds goes with it while the window of the game is still open.
devWindow.Dispose();

// The device objects live in the OpenGL context of the window, so the game releases them while the window is still open. One call runs
// every step of the shutdown in the order the engine registered: the splash and what it uploaded, the atlases and the textures of the
// frame, the samples of the sound device, the renderer, and the window itself last.
provider.GetRequiredService<GameShutdown>().Run();

// The game ran, so the process exits well: the only other exit is the one above, where a demo of another contract of the engine is refused
// before anything of it is built.
return 0;
