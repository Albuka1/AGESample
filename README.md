# AGESample

An interactive demo for the [Auae Game Engine (AGE)](https://github.com/Albuka1/AGE),
pinned to the engine release **v0.2.0**.
The engine is consumed as a **git submodule** and built from source through
project references, so the demo always runs against the exact engine revision
pinned in this repository.

## What the demo shows

| Feature | Where |
| ------- | ----- |
| Entity-component system | `DemoScene` builds entities from `Transform`, `Sprite`, `Collider` and `Velocity` components |
| Custom components | `VelocityComponent` and `BaseColorComponent` are declared by the demo, not by the engine |
| Physics | The engine `CollisionSystem` detects overlaps; `CollisionTintSystem` brightens whatever is touching |
| Rendering | Solid-color sprites, a screen-space UI layer, and the built-in 8x8 bitmap font for text |
| Camera | Two-dimensional camera with panning, zooming and smooth follow |
| Input | `WindowInputService` implements the engine `IInputService` over Silk.NET input and replaces the engine default through dependency injection |
| UI | Retained buttons and labels with hover and press feedback driven by the engine `UIUpdateSystem` |
| Game loop | `SilkGameLoop` owns the frame loop, the input frame and the fixed steps, and it draws once per frame after them |
| Branding | The engine splash shows the AGE logo while the first frames run |

## Controls

| Input | Action |
| ----- | ------ |
| `W` `A` `S` `D` | Move the player square |
| Arrow keys | Pan the camera, which turns follow mode off |
| `Q` / `E` | Zoom out / in |
| `F` | Toggle follow-the-player |
| `Space` | Pause or resume the simulation |
| `R` | Reset the scene |
| Mouse | Hover and click the UI buttons |

## Getting started

```bash
git clone --recurse-submodules https://github.com/Albuka1/AGESample.git
cd AGESample
dotnet build AGESample.slnx -c Release
dotnet run --project src/Age.Demo -c Release
```

Already cloned without the submodule?

```bash
git submodule update --init --recursive
```

## Layout

```
AGESample.slnx
Directory.Build.props      target framework, nullable and warning settings
Directory.Packages.props   central package versions for the demo
engine/                    AGE as a git submodule, pinned to a revision
src/Age.Demo/              the demo application
```

The engine keeps its own `Directory.Build.props` and `Directory.Packages.props`
inside the submodule, so the demo never changes how the engine builds.

## Notes and limitations

- The engine gained a lot since `v0.1.0`, and this demo does not use all of it yet. It
  still draws solid-color quads because it ships no image, and its text still uses the
  built-in bitmap font because it ships no font file. The engine can do both now:
  `ITextureService` loads and caches PNG, JPEG, BMP, TGA and GIF, `IFontService` bakes a
  TrueType font into a glyph atlas, and `Age.Sample` in the engine repository shows
  both. The engine also ships a `SilkInputService` now, which the demo replaces with its
  own `WindowInputService` on purpose, to show that the container decides.
- The engine `UIRenderSystem` draws `ButtonComponent.BaseColor` as-is, so the
  demo applies hover and press colours itself inside `UiInteractionSystem`.
- A GPU with OpenGL 3.3 core profile support is required.

## License

MIT. See [LICENSE](LICENSE).
