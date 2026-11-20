# Needs checking — Group AE

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## 2026-09-22 — THE DOOR EXISTS NOW: `heron-install`

**Tracked as GitHub issue [#972](https://github.com/Ajmalpshaik/Heron-AI/issues/972) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Q-PE-16 is answered.** The owner chose a command beside the window, having been shown the four
options: *"if I type the 'heron install' from GitHub like that, it will install from there also. That
is the second type of installation."*

[`platform/Heron.Installer.Cli/`](../../platform/Heron.Installer.Cli/) builds **`heron-install`**, and it
reaches the **same** `InstallSource` → `InstallerScreen` → `InstallPlan` → `InstallEngine` the window
reaches. Not an MCP tool, not the window driven headless, and **not `deploy-addin.ps1`**.

### What was actually run here, and what it said

| | |
|---|---|
| **PASS** | The command **ran** — `--help`, a typo'd flag, a bare flag, seven addresses through the gate, and `--from`. It is `net8.0` with no window, so unlike the installer window it runs on the machine it is developed on |
| **PASS** | Every hostile address refused **before any PowerShell ran**, on a Linux box with no Revit on it at all |
| **PASS** | The argument reader, run for real against a fake Revit in `tests/Heron.Installer.TestHost` |
| **PASS** | **Fifteen breaks seen to fail** — eight on the reader, seven on the door |
| **PASS** | Ten gates · **14 projects on all 8 releases, 0 warnings** · every installer suite |
| **NEEDS REAL REVIT** | `AE1` to `AE6` below. **It has never installed anything into a Revit** |
| **NOT STARTED** | Route 2. `--from` refuses and **says why**, naming `Q-PE-12`, rather than failing like a bug |

### A defect it found in code already merged

Asked for `https://github.com/Ajmalpshaik/Heron-AI` — **the owner's own repository**, just not a
release — the gate refused it saying *"it is not Heron's own repository. Heron will not install
software from somebody else's."*

**It is his own.** One check was answering two questions — *whose is it* and *what is it* — and needed
three path segments to answer either, so a bare repository address with only two fell into the wrong
half. **The refusal was right and the reason was false**, which is worse than a blunt no: it sends
somebody to check an address that was never the problem.

**Why the suite had passed:** it asked that the address be refused, and that the refusal point at the
releases page. The wrong sentence does both — *every* refusal ends with that page. Nothing asked
**which** refusal arrived. Split into two checks, and the suite now asks.

**Found by running the command, not by reading the code.**

### Owed on the owner's PC — Revit 2020, 2024 and 2027

**Close Revit before each one.** Screenshots or terminal captures into `docs/proof/` with the row id in
the filename.

| id | what to do | what proves it |
|---|---|---|
| **AE1** | Open a terminal in the repository. Run `heron-install --list` | It lists the products and the Revit releases found on the PC, says where it would install, and **changes nothing**. Exit code **0** |
| **AE2** | With Revit **open**, run `heron-install --releases 2024` | It names the open Revit and waits, printing a line rather than sitting silent. Close Revit; it carries on by itself. If it gives up, exit code is **3** and it says nothing was changed |
| **AE3** | With Revit closed, run `heron-install --releases 2024` | It installs, prints one line per product, ends `0 failed`, exit code **0**. Start Revit 2024 — **the Heron tab is there** |
| **AE4** | Run `heron-install --source https://github.com/Ajmalpshaik/Heron-AI` | **Refused**, exit **1**, and the refusal says the address points at the **repository rather than a release** — NOT that it is somebody else's. This is the defect above, on a real machine |
| **AE5** | Run `heron-install --source https://github.com/someone-else/Heron-AI/releases` | **Refused**, exit **1**, and it says it will not install from somebody else's repository. **Nothing is downloaded and no Revit is touched** |
| **AE6** | Run `heron-install --products nonsense` | **Refused** by name, exit **1**, and it lists what there actually is |

**`AE1` gates the rest** — if the listing is wrong, what `AE3` installs is wrong too.

**`AE4` is the one to read carefully.** If it still says *"somebody else's"*, the fix did not reach the
machine and the row **FAILS** — say so rather than reading past it.

**`AE3` needs something to install.** On a checkout with builds in it, it uses those. On a machine with
none, it needs a published release, which does not exist yet — `AC1` to `AC5`. Run `AE3` on the
checkout, not on a bare machine, until those pass.

---

## 2026-09-22 — STAGE 6's SECURITY GATE IS BUILT, and the door it guards is not

Stage 6 item 3 says *"read `Q-PE-10` before building this one — whether route 1 may act on any
repository, or only Heron's own signed release, is unresolved and it is a security question."*
**`Q-PE-10` was answered on 2026-09-21**: Heron's own signed release, never an arbitrary repository.

So the gate was built first, because it is the only part of the stage that is a security rule.

### What `InstallSource` refuses, and why each one is a real trick

| refused | why it matters |
|---|---|
| `https://github.com/someone-else/repo/releases/latest` | somebody else's code, running inside Revit with live models open |
| `https://evil-github.com/...` | **ends with** nothing useful — but a `Contains` check passes it |
| `https://github.com.evil.example/...` | **ends with** `evil.example`; a sloppy `EndsWith` on the wrong side passes it |
| `https://github.com/owner-a-evil/...` | **starts with** the real owner — a `StartsWith` check passes it |
| `https://github.com@evil.example/...` | reads as GitHub to a person, resolves to `evil.example` |
| `https://evil@github.com/...` | the host really **is** GitHub — only the user-info guard catches this |
| `http://` and `ftp://` | what arrives is whatever the network sent, and so is the checksum |
| `https://github.com:8443/...` | GitHub's releases never name a port |
| `https://github.com/owner-a/repo-a` | the **repository**, not a release — source code is whatever a branch says today |
| `install this repo` | not an address at all |

**Every refusal names what would be accepted.** A refusal that only says no leaves somebody guessing,
and the guess they make is usually to try harder rather than to try the right thing.

**It opens no socket.** `R-50` — *the AI never reads a repository's contents to decide what to
install* — is true by construction here rather than by discipline: `Judge` takes a string and the
product list, and could not fetch anything if it wanted to.

**Who Heron is comes from the manifest**, the `source` block added for Stage 5. A repository renamed is
a line in a file, not a rebuild.

### What was actually run, and what it said

| | |
|---|---|
| **PASS** | Four accepted shapes, **eighteen refused**, each refusal naming Heron's own release and never the word "error" |
| **PASS** | A local folder and a network share are told apart from a hostile address, and point at the other door |
| **PASS** | A manifest that does not know its own source refuses **everything** rather than guessing a repository name |
| **PASS** | Ten gates, 13 projects on 8 releases, every installer suite |
| **NOT STARTED** | **The door.** Routes 1 and 2 are the AI installing, and there is nothing for the AI to call — [Q-PE-16](../work-notes/plans/plugin-extension/03-open-questions.md) |
| **NEEDS REAL REVIT** | Nothing yet. There is no row to give, because there is no route to run |

**Seen to fail — one break per guard, each letting exactly one attack through:**
`EndsWith` host → `evil-github.com` · `Contains` host → `github.com.evil.example` · `StartsWith`
owner → `owner-a-evil` · `http` allowed → `http://` and `ftp://` · user-info guard removed →
`https://evil@github.com/...` · `releases` not required → the repository itself.

### TWO THINGS THE BREAKS FOUND, and both were mine

**A guard nobody could tell was gone.** Deleting the user-info check broke **nothing**: every hostile
address tried also had a hostile host, which the host check caught. That made it unfalsifiable — code
that looks like a safeguard and is provably nothing. The case only it catches, where the host really
**is** `github.com`, was added; then it went red. **A security guard that cannot be shown to fire is
not a guard, it is a comment.**

**And a refusal that was true and useless.** A Windows path is a **valid absolute URI** —
`Uri.TryCreate` turns `D:\Heron-AI` into a `file:` address — so somebody pointing at their own clone was
told *"Heron will only fetch over https, and that address is file"*. Found by the check that asks for
the other door to be named, not by reading.

### The door, and the option to avoid

[Q-PE-16](../work-notes/plans/plugin-extension/03-open-questions.md) holds four ways to give the AI
something to call. **The one to avoid is driving `deploy-addin.ps1` directly** — it works today, it
looks like progress, and it skips `InstallPlan` and `InstallEngine` entirely. **Stage 6's own opening
line calls that failure:** *"if this stage ends up re-implementing any install rule, it has failed,
however well it works."*

**Route 2 needs `Q-PE-12` as well as `Q-PE-16`.** Route 1 needs only the door.

---
