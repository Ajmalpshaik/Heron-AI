# platform/Heron.Setup — the Setup a modeller downloads

**One file, `HeronSetup-<version>.exe`, for this Windows user, with no administrator rights**
([D-165](../../docs/decisions/D-165.md)). Double-clicked, it puts Heron in `%LOCALAPPDATA%\Programs\Heron AI`,
installs the add-in for every Revit year on the PC, sets up Heron's Python side, offers to add Heron to
Claude Code, and appears in **Settings > Apps > Installed apps** with **Uninstall**.

| | |
|---|---|
| [`HeronSetup.iss`](HeronSetup.iss) | The Inno Setup script. **Decides nothing** about what goes where |
| [`tools/build-setup.py`](../../tools/build-setup.py) | Stages the folder and runs Inno Setup. The only way the Setup is built |

## It is a door, not an engine

Everything an install DOES is the engine's, the same one the window and `heron-install` use
([`platform/README.md`](../README.md)): which products and which Revit years, the wait while Revit is
open, the checksum of every payload, the second-copy check and the put-back of `tools/deploy-addin.ps1`,
Heron's Python side. The Setup copies the folder, runs

```text
heron-install --from "<Heron folder>\payload" --no-check      plus --no-python when that tick is off
heron-install --add-to-claude            when its tick is on - both ticks start ON
```

and shows what they said, word for word. Its uninstaller runs `heron-install --uninstall --list`, shows
that sentence naming every product and release, and on **Yes** runs `heron-install --uninstall`, before
Windows removes the folder.

**Two checks look like rules and are only doors:** *close Revit first* is asked before Install is
pressed, so a modeller does not watch a window that waits (the engine still waits by itself, and never
closes Revit); and the uninstaller refuses while Revit is open, for the same reason.

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
  `HeronSetup-<version>-unsigned.exe`. Signing is NOT RUN until a certificate exists - D-165 item 6.

## Building it

```bash
python tools/build-setup.py --dist dist
```

Needs Windows and Inno Setup 6 (`ISCC.exe`) to **build** - never to run. Exit 3 is NOT RUN (not Windows,
or no Inno Setup), never a pass. Without `--dist` it first runs `tools/build-release-assets.py`.

## What has run

**Nothing yet, on 2026-11-16.** The runs are [NEEDS-CHECKING Group OA](../../docs/needs-checking/group-oa.md):
the owner's PC, Revit 2020, 2024, 2026 and 2027; 2021, 2022, 2023 and 2025 NOT RUN - no such Revit there.
