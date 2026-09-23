# Needs checking — Group AF

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## 2026-09-22 — ONE DOWNLOAD, BOTH DOORS, AND AN UPDATE CHECK THAT ONLY CHECKS

**The owner answered four questions with one shape** and asked for it built:

> *"If someone downloads it, they need to have everything… just click the installer and it installs.
> If he has internet, this will check only if there is an update or not — no need to download again.
> Even if he doesn't have internet he can still use the tools… it will ask where you need to keep."*

`Q-PE-5`, `Q-PE-12`, `Q-PE-13`, `Q-PE-14` all answered. `R-51` to `R-56` written. `R-30` corrected —
it had claimed *"the files are already in the repo"*, and they never were.

### What was actually run here, and what it said

| | |
|---|---|
| **PASS** | The builder packs **`heron-project.zip`** beside the plugin — brain, skills, MCP, docs. Opened and counted rather than trusted: **1673 entries**, and **zero** from `tests/`, `revit/`, `tools/`, `.git/` or `__pycache__` |
| **PASS** | Route 2 runs. Three folders, three different correct refusals or acceptances, on a Linux box with no Revit |
| **PASS** | A **tampered zip in a handover folder is refused** — the suite builds the folder, hashes it, then changes the zip underneath |
| **PASS** | An **unreadable `checksums.txt` fails closed**, installing nothing rather than assuming there is nothing to check |
| **PASS** | The update check: `0.10.0` is newer than `0.9.0`, `1.2` equals `1.2.0`, `0.1.0-rc1` is **cannot tell** rather than `0.1.0`, and **ahead is never reported as up to date** |
| **PASS** | **Seventeen more breaks seen to fail** — 5 on the packing, 5 on the folder, 7 on the update check |
| **PASS** | Ten gates · every installer suite |
| **NEEDS REAL REVIT** | `AF1` to `AF7` below |

### What the checksum CANNOT do, and it is written into the code

It catches **damage**, not a determined tamperer. Anyone who can rewrite a zip in that folder can
rewrite `checksums.txt` beside it, and both will then agree. **What closes that is a signature over the
release — Stage 8, which is not built.** Until it is, route 2 is exactly as safe as the person who
handed over the folder.

### Owed on the owner's PC — Revit 2020, 2024 and 2027

**Close Revit before each one.** Captures into `docs/proof/` with the row id in the filename.

| id | what to do | what proves it |
|---|---|---|
| **AF1** | Take the `dist` folder the builder makes to a PC. Run `heron-install --from <that folder>` | It installs, prints one line per product, ends `0 failed`, exit **0**. Start Revit — **the Heron tab is there**. **No internet was used** |
| **AF2** | Unplug the network. Run `AF1` again | **Identical result.** `R-53`: after the download nothing needs the internet |
| **AF3** | Point `--from` at the folder ABOVE the real one | Refused, exit **1**, and it says *"point at that one instead"* rather than a path error |
| **AF4** | Open `checksums.txt` in that folder, change one character of one line, run `--from` again | **Refused**, exit **1**, *"did not arrive whole"*. **Nothing is installed and nothing is left half-copied** |
| **AF5** | Unzip `heron-project.zip` somewhere and open that folder in Claude Code | The MCP server starts from `.mcp.json`, and the AI can answer from `brain/`. **`Q-PE-13` proved, or not** |
| **AF6** | With a release published and a NEWER version in it than the folder holds, run `--from <folder>` | It installs from the folder, then says **`x -> y`** afterwards and that **nothing was updated**. It must **not** download the plugin to work that out |
| **AF7** | Same as `AF6` with no internet, and again with `--no-check` | Both install normally. The no-internet one says it could not check; `--no-check` says nothing at all. **Neither changes the exit code** |

**`AF1` gates the rest.** **`AF4` is the one to read carefully** — if a changed `checksums.txt` still
installs, that is a FAIL and the whole verification story is wrong.

**`AF6` needs a published release**, which does not exist yet — `AC1`. Until then it is NOT RUN, not a
pass.

**Still NOT BUILT: the installer does not yet ASK where to keep the folder (`R-52`).** `heron-install`
installs the plugin and reports; unpacking `heron-project.zip` and choosing its home is the window's
job and Stage 9's. Recorded rather than left to be discovered.
