# Building and testing Samosa Kitchen

The source for the dish, selector, counter and ticket views is in [`src/`](../src/). All the food meshes live in [`Assets/samosa.smk`](../Assets/samosa.smk), an embedded resource. The build references your own PlateUp! installation; this repository does not include game or KitchenLib assemblies.

## Build the player DLL

The reference configuration is Windows, PowerShell 7, a .NET SDK with Roslyn C# 9 support, PlateUp! 1.5.3 and KitchenLib 0.10.2. Run these commands from the repository root:

```powershell
# Replace this example with the directory that contains PlateUp.exe.
$game = 'C:\Games\SteamLibrary\steamapps\common\PlateUp\PlateUp'
.\build.ps1 -Game $game
```

The output is `build/SamosaKitchen.dll`. The script first looks for KitchenLib under the game's local `Mods` directory, then in the same Steam library's Workshop cache. If yours is elsewhere, pass `-KitchenLib 'C:\path\to\KitchenLib-Workshop.dll'`. You can also set `PLATEUP_GAME_DIR` instead of passing `-Game`.

To try your build, close PlateUp! and copy **only the player DLL** to `Mods/SamosaKitchen/content/` beside the game executable. Create those directories if needed. To package a build:

```powershell
.\tools\package.ps1
```

This writes `dist/SamosaKitchen-<version>.zip` with the DLL, a [text-only guide](QUICKSTART.md) and a [PNG counter guide](images/masala-counter.png). Use `-Game $game` to additionally check that the packaged DLL matches your installed copy.

## Tests

The Python tests check that all 15 filling masks are registered and that the meshes have valid geometry and four distinct symbols. Python 3.10+ and its standard library are enough:

```sh
python -m unittest discover -s tests -p "test_*.py"
```

[`tests/SamosaProbe.cs`](../tests/SamosaProbe.cs) is a separate, optional in-engine integration probe for native orders, recipe transitions, the counter and plate handling. Build it with `./build.ps1 -Game $game -QAOnly` for a **development** session. It isn't needed to play, and isn't included in any player download.

## Rebuild or edit the artwork

```sh
python tools/make_variants.py
python tools/make_art.py --obj Art/obj
```

The first command regenerates the explicit flavor registrations; the second writes the mesh bank and editable OBJ/MTL files. The [Blender-importable OBJ files](../Art/obj/) are exports: change [`make_art.py`](../tools/make_art.py) to make a persistent change, or provide your own exporter for the bank. Editing an OBJ alone does not change the DLL.

To regenerate the illustrations, install [NumPy](https://numpy.org/) and [Pillow](https://python-pillow.org/) in your development environment, then run:

```sh
python tools/make_legend.py docs/images/masala-counter.png
python tools/make_readme_art.py
```

Neither library nor the rendering fonts are needed to play.
