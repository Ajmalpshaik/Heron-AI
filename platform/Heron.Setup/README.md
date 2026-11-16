# platform/Heron.Setup — the Setup a modeller downloads

**One file, `HeronSetup-<version>.exe`, for this Windows user, with no administrator rights**
([D-167](../../docs/decisions/D-167.md)). Double-clicked, it puts Heron in `%LOCALAPPDATA%\Programs\Heron AI`
and opens **Heron's own branded installer window**, which installs the add-in for every Revit year on the
PC, sets up Heron's Python side and adds Heron to Claude Code - two ticks, both on. Heron appears in
**Settings > Apps > Installed apps** with **Uninstall**.

| | |
|---|---|
| [`HeronSetup.iss`](HeronSetup.iss) | The Inno Setup script. **Decides nothing** about what goes where |
| [`tools/build-setup.py`](../../tools/build-setup.py) | Stages the folder and runs Inno Setup. The only way the Setup is built |
| [`make-images.ps1`](make-images.ps1) and `images/` | The Setup's two pictures, made from the brand kit's own files: the logo on Mist with the orange line, and the app icon in the copying page's corner. Run it again only when a brand file changes |

## Its face is Heron's own window

**Since 2026-11-16, the owner's *"Yes, do that"* (D-167, amended):** the Setup has **no pages of its
own**. It shows one short copying page while it puts the folder in place, then opens

```text
"<Heron folder>\HeronInstaller.exe" --from "<Heron folder>\payload"
```

and closes. That window is the branded one ([D-149](../../docs/decisions/D-149.md),
[docs/brand](../../docs/brand/README.md)) - the Heron logo over the orange line, Mist and Ink, the orange
Install button - and it asks everything: the Revit years found, then two ticks, both on:

- **Set up Heron's AI side too** - unticked is `heron-install --no-python`;
- **Add Heron to Claude Code** - offered only when the Setup opened the window, done last, and only after
  an install that failed nowhere. Claude must be closed; if it is open the window says so and changes
  nothing.

The Start-menu shortcut *Heron AI > Heron Installer* opens the same window the same way. The copying page
keeps the brand too: Mist, Ink text, the heron in the corner and the orange line under the header, the
colours written in the script as `$BBGGRR` beside their brand name and hex.

## It is a door, not an engine

Everything an install DOES is the engine's, the same one the window and `heron-install` use
([`platform/README.md`](../README.md)): which products and which Revit years, the wait while Revit is
open, the checksum of every payload, the second-copy check and the put-back of `tools/deploy-addin.ps1`,
Heron's Python side. The window's `--from` is heron-install's own - the same refusal by name, the same
checked zips - and nothing is downloaded.

**Run silent** (`/SILENT`, `/VERYSILENT`, for a managed PC) nobody can press Install, so the Setup runs
`heron-install --from "<Heron folder>\payload" --no-check`, then `heron-install --add-to-claude`, and keeps
what they said as `setup-report.txt` in the folder. **NOT RUN on the real Setup** - row OA19.

Its uninstaller runs `heron-install --uninstall --list`, shows that sentence naming every product and
release, and on **Yes** runs `heron-install --uninstall`, before Windows removes the folder.

**Two checks look like rules and are only doors:** the uninstaller refuses while Revit is open, and both
the Setup and the uninstaller refuse while a Heron Installer is open from the folder - its exe is in the
folder they replace or remove. *Close Revit first* is the window's own line, and the engine waits by
itself and never closes Revit.

**The window's Remove Heron** takes Heron out of Revit and only that. Installed by the Setup, the folder
and the Claude Code entry come out through Installed apps, and the window says so after a removal.

## What it never does

- **Write `heron.config`.** Changes, Admin, Publish and Ask to Keep stay as they are - off on a new PC.
- **Touch the user's data** - `%APPDATA%\Heron`, `%USERPROFILE%\.heron` - on install or uninstall.
- **Close Revit or Claude**, or ask for administrator rights.
- **Leave a file the new version dropped.** An update first deletes Heron's own folders in the
  program folder (`[InstallDelete]`), then copies them whole - D-94, install replaces. A fragment
  somebody made on this PC INSIDE Heron's program folder goes with them, exactly as it would on
  unzipping a new release; nothing of the user's own data is kept there.
- **Install Python** ([Q-39](../../docs/open-questions/answered.md)). Without it, the report names the
  one `winget` line.
- **Call itself signed when it is not.** With no sign command, `build-setup.py` names the file
  `HeronSetup-<version>-unsigned.exe`. Signing is NOT RUN until a certificate exists - D-167 item 6.

## Building it

```bash
python tools/build-setup.py --dist dist
```

Needs Windows and Inno Setup 6 (`ISCC.exe`) to **build** - never to run. Exit 3 is NOT RUN (not Windows,
or no Inno Setup), never a pass. Without `--dist` it first runs `tools/build-release-assets.py`.

## What has run

The runs are [NEEDS-CHECKING Group OA](../../docs/needs-checking/group-oa.md): the owner's PC, Revit 2020,
2024, 2026 and 2027; 2021, 2022, 2023 and 2025 NOT RUN - no such Revit there. **OA1 to OA10 ran on
2026-11-16 on the Setup's own pages**, before its face became Heron's window; the window's route is rows
OA12 onward, owed on his PC.
