# AGE Demo

A demonstration of what the [Auae Game Engine (AGE)](https://github.com/Albuka1/AGE) can do, and the starting point this
repository exists to be: clone it, rename the game, and the engine, the content, the console, the tests and the CI are already
wired together.

The engine is a **git submodule** in `AGE/`, pinned to **v0.4.0**, and it is built from source through project references, so the
demo always runs against the exact revision this repository names rather than against a package that moved.

## Getting started

```bash
git clone --recurse-submodules https://github.com/Albuka1/AGESample.git
cd AGESample
dotnet run --project Game -c Release
```

Already cloned without the submodule?

```bash
git submodule update --init --recursive
```

A GPU with OpenGL 3.3 core profile support is required, and the .NET SDK 10.0.100 or later.

## Controls

| Input | Action |
| ----- | ------ |
| `` ` `` | Opens the developer console, which is the first thing worth pressing |
| `W` `A` `S` `D` | Moves the white sprite |
| `Space` | Pauses or resumes the clock |
| `Ctrl` (held) | Slows the simulation to a quarter speed, which is what shows the fixed steps |
| `E` | Spawns a short-lived sprite at the pointer |
| `F` / `R` | Saves the scene to the account folder, and loads it back |
| Mouse | The spawned sprites follow the pointer, and the interface takes a click |

Inside the console, `help` lists everything. The commands this demo registers are:

| Command | What it does |
| ------- | ------------ |
| `goblin` / `beacon` | Spawns a prototype of the content, which is what shows that a prototype is data |
| `cvars` / `cvars get` / `cvars set` | Reads and writes the settings of the demo, which are saved to the account folder |
| `strings` | Reports what the content says in the chosen language, which is what the locale service is for |
| `vignette` | Turns the post-process of the content off and on |
| `ui` | Reports the scaler of the interface and every anchored element |
| `point` | Reports where the pointer is, in the window, in the interface and in the world |
| `fit` | Switches the camera between one unit per pixel and the design area |
| `missing` | Puts a sprite whose image is not there on screen, which is what the placeholder is for |
| `devwindow` | Opens the developer window beside the game |
| `quit` | Closes the game |


## Layout

```
Age.Platformer.slnx        the game and the engine projects, and the tests
Directory.Build.props      target framework, the version of the game and the output layout
Directory.Packages.props   the versions of every package the game itself uses
global.json                the SDK, and the runner that `dotnet test` uses
AGE/                       the engine as a git submodule, pinned to a revision
Game/                      the game: the code and Resources/
Game.Tests/                the tests of the game
```

The engine keeps its own `Directory.Build.props` and `Directory.Packages.props` inside the submodule, so the game never changes
how the engine builds, and a project of the game refers to the folders at the root of `AGE/`.

Above `Age.Platformer.csproj` and `Age.Platformer.Tests.csproj`, the files and folders are not part of the game at all: they
are the parts of this template. `Directory.Build.props` holds `Version`, `game.version.json` holds the same number, and the
names appear once in the solution, the two projects and the CI workflow. Renaming the game is changing those, not the code.

## What the demo shows

`Program.cs` is written in the order a game is started, and every step of the engine is commented with what it is for. It is
meant to be read from the top.

| Feature | Where |
| ------- | ----- |
| Version gate | `VersionCheck` refuses a game of another contract of the engine before a window is opened |
| Logging into the console | `ConsoleLoggerProvider` sends every line of the engine and the game to the developer console |
| Fixed steps | `SystemPipeline` runs the simulation in fixed steps and the interface once per frame, so a pause still draws |
| Content as data | `PrototypeManager` reads `Resources/Prototypes`, and `SpawnService` builds an entity out of a document |
| Materials and shaders | `MaterialPrototype` and `ShaderService` load the pulse stage and the vignette from `Resources/Shaders` |
| Sprite sheets | `SpriteAnimationSystem` walks a goblin because its prototype names a state of its sheet |
| Timers and tweens | `TimerSystem` announces the end of a spawned sprite, and `TweenSystem` turns the companion |
| Collision | `CollisionSystem` announces the pairs it found, and the demo tints them |
| Fonts | `IFontService` draws the HUD in the TrueType font of the content rather than in a bitmap font |
| Sound | `ISoundService` plays the click of the content on a press of a button |
| Localisation | `ILocaleService` answers in `en` or `ru` from the documents of the content |
| Settings | `CVarService` reads and writes the settings of a person to the account folder |
| Interface | `UILayoutSystem` and `UIUpdateSystem` drive the retained elements |
| The developer console | `DevConsoleOverlay` takes the whole keyboard while it is open |
| The developer window | `IDevWindowService` stands beside the game with a page of the engine and a page of the demo |
| Passes of a game | `VignettePass` is added to the same pipeline as the passes of the engine and reads the frame |
| Scene saving | `ISceneSerializer` writes the world to the account folder and reads it back |
| Shutdown | `GameShutdown` releases the device objects in the order the engine registered, in one call |

## Tests

The tests of the game are the tests of its own logic, and they run without a window: the behaviour is handed the state of the
world and the input of a frame, and what it writes is what is asserted.

```bash
dotnet test --project Game.Tests/Age.Platformer.Tests.csproj -c Release
```

A key is pressed through `InputStateTracker`, which is the backend-independent half of the input of the engine, so behaviour
that reads the keyboard is tested by reporting a key rather than by opening a window.

## Content

`Game/Resources/` is what the game reads at run time, and the build copies it beside the executable. Its own
[README](Game/Resources/README.md) says where each kind of file goes. Nothing in it is read at compile time: a path that is
wrong is a placeholder and a line of the console rather than a build that fails, which is exactly what the `missing` command of
the demo puts on screen.

## License

MIT. See [LICENSE](LICENSE). The engine keeps its own licence inside `AGE/`, and the font of the content keeps the one beside it.

