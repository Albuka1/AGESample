# Resources

What the game reads at run time. Every path that a document or a line of code names is relative to this folder, and the
folder is copied next to the executable by the build, which makes that copy the asset root.

A folder is added when the first file that belongs in it is added, and never before: the table below says where a file
goes, so a person who is about to write one does not have to invent a place for it.

| Folder | What is in it | Who reads it |
| --- | --- | --- |
| `Audio` | The sounds of the game, one folder per kind. | `ISoundService`, through the loader of the format. |
| `Fonts` | The fonts that a line of text is baked from, and the licence of each one. | `IFontService`. |
| `Locale` | The strings of the game, one folder per language, named by the short tag of the language (`en`, `ru`). A key that the language does not hold falls back to `en`. | `ILocaleService`. |
| `Prototypes` | The entities of the game as documents, one folder per group with a `base.yml` that holds what the group shares. | `PrototypeManager`. |
| `Textures` | The images of the game, and the sprite sheet documents beside them. | `ITextureService` and `ISpriteSheetService`. |

## Naming

- A path is written with a forward slash and is relative to this folder: `Textures/Tiles/tiles.bmp` is the file
  `Resources/Textures/Tiles/tiles.bmp`.
- A file that belongs to a group sits in the folder of that group rather than in the root: `Audio/Effects/click.wav`, not
  `Audio/click.wav`.
- A sprite sheet is a document named like the image beside it — `goblin.tga` and `goblin.yml` — so a sprite that names the
  document names the image with it.

## Nothing here is read at compile time

A build copies this folder and nothing more: a path that is wrong is a placeholder sprite and one line of the log rather
than a build that fails. `Age.Content.Lint` in the engine clone is what checks the documents of a build against the files
that are really here, and it is meant to run in CI rather than by hand.

## A new game

The three folders every game ends up with are `Textures`, `Fonts` and `Audio`. `Prototypes` and `Locale` are worth having
from the start even when they hold one file each: a game whose content is data is a game whose content is edited without a
build, and the two folders are what that begins with.
