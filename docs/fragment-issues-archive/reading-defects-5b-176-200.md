# FRAGMENT-ISSUES archive — section 5b, rows 176 to 200

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 176 to 200 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-180

*Moved from the register on 2026-11-17.*

**Defect.** **TWO FRAGMENTS DESCRIBE `Level.Elevation` AND `Level.ProjectElevation` THE OPPOSITE WAY ROUND FROM FIVE OTHERS - AND ONE OF THEM COMPUTES WITH ITS DESCRIPTION.** Found 2026-09-23 beside row 5b-179. `list-levels/impl/any/fragment.cs` says *"Elevation from the project's INTERNAL ORIGIN"* and *"ProjectElevation from the project's ELEVATION BASE - the number Revit puts on the level head"*; `create-level/impl/any/fragment.cs` says the same and applies `existing.ProjectElevation - existing.Elevation` as a shift before `Level.Create`. `report-level-elevations`, `measure-ceiling-height`, `create-roof`, `check-valve-accessibility`, `create-ceiling`, `create-floor` and `create-wall` rest on the other reading, which is Autodesk's (row 5b-179). **Not checked**: what Revit 2024 returns for each on a survey-based model, and what `Level.Create`'s elevation is measured from - both need a model.

**State.** **MOVED TO ISSUE [#904](https://github.com/Ajmalpshaik/Heron-AI/issues/904) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** Neither fragment is in package C4's scope. If `create-level`'s reading is the wrong one, its shift goes the wrong way on exactly the models where it matters

---

### Row 5b-185

*Moved from the register on 2026-11-17.*

**Defect.** **D-48 IS CITED AS THE RULE THAT `brain` MAY NOT IMPORT `mcp`, AND D-48 SAYS NOTHING ABOUT LAYERS.** Found 2026-09-23 while reading every citation of D-47, D-48 and D-49 for [row 5b-171](../FRAGMENT-ISSUES.md#). D-48 is *"One broken part costs one part, never the whole library"*, and the first D-48, now D-102, was about several fragments feeding one; neither states a layering rule. Yet `brain/` modules such as `heron_hr.py`, `heron_scaffold.py` and `heron_fix.py`, agent contracts under `brain/agents/`, their suites and `docs/PROPOSALS.md` give *"brain may not read mcp (D-48)"* or *"D-48's layering"* as the authority. The rule itself is real - the `ALLOWED` table in [`tools/check-structure.py`](../../tools/check-structure.py), drawn from [docs/02 §7](../02-architecture-overview.md) - so nothing is wrongly allowed; a reader who follows the citation finds a different decision. `check-structure.py` may be where it started: it says the imports it first found were gates in `tools/` reading `brain/`, *"which is what D-48 asks those gates to do"* - true of D-48, and a different claim from *brain may not import mcp*. Derive the citations: `git grep -n -E 'D-48' -- brain tools tests docs \| grep -i -E 'mcp\|layer'`.

**State.** **MOVED TO ISSUE [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired**, by the rule at the head of this section. The repair starts with a decision to cite - one that already owns the layering, or a new one written for it - and that choice is the owner's; the citations follow it.

---

### Row 5b-187

*Moved from the register on 2026-11-17.*

**Defect.** **`CONNECT_AIR_TERMINALS` ON REVIT 2024 LIFTED EVERY TERMINAL ONTO ITS DUCT'S UNDERSIDE, MADE NO TAP, AND JOINED A SUPPLY DIFFUSER TO A RETURN DUCT - THE CARD SAYS REVIT DECLINES THAT, AND THE HEADER SAYS IT CUTS A TAP.** Found 2026-09-23 on the owner's PC, running [NEEDS-CHECKING AJ1](../needs-checking/group-aj.md) on the test project `Heron-C3-test`, Revit 2024 (session 3544), the add-in built from `main` at `173a4524`. Three `M_Supply Diffuser: 600 x 600 Face 300 x 300 Connection` under 400 x 300 ducts whose underside was at 2850 mm: one at 2300 under the supply duct, one at 2748 with its neck touching the supply duct, one at 2300 under the return duct. `connect-air-terminals` with apply and a reach (`maxDistanceMm`) of 1000 returned `connected` 3, `refused` 0, and `moved` *925837: moved 550.0 mm (z +550.0), 925838: moved 101.6 mm (z +101.6), 925839: moved 550.0 mm (z +550.0)*, with *"925839 tapped into 925835"* - the return duct. The owner's spot elevations read 2300, 2748, 2300 before and 2850, 2850, 2850 after, each diffuser drawn inside its duct, and the model held no duct fitting at all afterwards. So `MechanicalUtils.ConnectAirTerminalOnDuct` put each terminal's insertion point - its face - on the duct's underside and joined it there, and did not look at the system classification. The card's purpose says *"a supply diffuser will not tap a return duct"* and the header *"ConnectAirTerminalOnDuct creates the tap fitting on the duct, positions it under the terminal's connector"*: on this release neither is what happened. **What worked, measured:** `moved` reported every lift exactly as the owner measured it by hand, and nothing was listed at 0.0 mm. **Not checked:** Revit 2020, where the other tool saw 625 mm; a round duct; another diffuser family; a routing preference other than `Radius Elbows / Taps`.

**State.** **MOVED TO ISSUE [#922](https://github.com/Ajmalpshaik/Heron-AI/issues/922) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired**, as the owner asked for this run. The tolerance question - how far a terminal may move before Heron calls it a problem - was put to him once with these numbers and is his; so is whether ceiling diffusers should be connected this way at all. The proof he accepted on 2026-09-23 records exactly this behaviour.

---

### Row 5b-197

*Moved from the register on 2026-11-17.*

**Defect.** **`HeronAtomicWrite.WriteAllText` THROWS NOW AND THEN ON WINDOWS - `File.Replace` HAS NO RETRY - AND `tests/test_kernel.py` CRASHED ON IT IN 3 RUNS OF 40.** Found 2026-09-24 on the owner's PC, when the after record for row 5b-195 ruled REVERT because `test_kernel.py` passed before and not after, with nothing under `platform/` or `tests/Heron.Kernel.TestHost/` changed. Run 40 times the way `change-evidence` runs a suite: **37 passed and 3 crashed**, every crash the same - `System.IO.IOException: Unable to remove the file to be replaced`, from `File.Replace` in `HeronAtomicWrite.WriteAllText` ([`HeronAtomicWrite.cs:83`](../../platform/Heron.Core/HeronAtomicWrite.cs)), reached by the test host's second write to one file (`Program.cs:268`) and unhandled, so the host exits `4294967295` and every check after its section 5 never runs. That is `File.Replace`'s known Windows race: another process - typically an antivirus or indexer scan of the file just written - holds the target for a moment, and the call fails rather than waiting. **It is not only a test's problem:** production writes through the same method at `revit/Heron.Bridge/BridgeIdentity.cs:170`, the discovery file a chat finds Revit by, and `platform/Heron.Core/HeronConfig.cs:138`, the settings save. **Not checked:** which process held the file, and whether a running Revit has ever met it.

**State.** **MOVED TO ISSUE [#898](https://github.com/Ajmalpshaik/Heron-AI/issues/898) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - fixed in source 2026-10-08 (project review); the measurement is owed on the owner's PC.** `HeronAtomicWrite.WriteAllText` now calls `File.Replace` through `ReplaceWithRetry`: on an `IOException` that is not a missing file it waits 25, 50, 75 and 100 ms and tries again, five attempts in all, and the last one throws as before. Compiled on 2020 to 2027 and `tests/test_kernel.py` passes here - but Linux cannot show the race. **For the owner:** run `python tests/test_kernel.py` 40 times on the PC that gave 37 of 40; 40 of 40 closes this row. Was: **OPEN - recorded, not repaired**, by the rule at the head of this section: it was found doing something else, and the repair is in the kernel every release loads, so it costs a rebuild on all eight. The usual cure is a short retry on that one `IOException` before giving up.

---

### Row 5b-200

*Moved from the register on 2026-11-17.*

**Defect.** **THE RE-RANKER'S ANNOUNCEMENT - THE SENTENCE R-77 SAYS A PERSON READS BEFORE AGREEING TO A DOWNLOAD - SAYS 500 MB TO 2 GB, AND ON LINUX PIP FETCHED 3,099 MB. AND IT SAYS *"no re-ranker installed"* WHEN THE PACKAGE IS INSTALLED AND ONLY THE WEIGHTS ARE MISSING.** Found 2026-09-24 taking `A10` in a cloud session. `brain/heron_rerank.py`'s `SIZE` is R-79's field reading, and `announcement()` prints it before any network call, which is the right order. **Measured:** `pip download sentence-transformers` on Linux x86_64, Python 3.11 - **3,099 MB in 60 files**: `torch` 555 MB, NVIDIA's GPU libraries 2,190 MB, `triton` 248 MB, everything else 106 MB - and **5,753 MB installed**. pip's default `torch` on Linux is the CUDA build, and the CPU-only index at `download.pytorch.org` is refused by this environment's proxy (403), so the smaller build could not be tried. **The command the constant's own comment names to confirm it, `pip download --no-deps sentence-transformers torch`, would have fetched about 560 MB - inside the range - and missed the 2,438 MB of GPU libraries `torch` then pulls in.** **Second, smaller, same file:** after `pip install sentence-transformers` and before `--fetch --yes`, `python brain/heron_rerank.py` printed *"Backend: absent - no re-ranker installed"*. The package was installed; the weights were not, because `_load()` asks with `local_files_only=True`, on purpose. A person told *not installed* installs again, when the step they need is `--fetch --yes`. **And its closing *"NOT MEASURED ... The after needs a machine that can reach huggingface.co - see A10"* is out of date as of this row.** **The range is typed a second time, in the header of [`requirements-optional.txt`](../../requirements-optional.txt)** - *"One of them is 500 MB to 2 GB"* - a few lines above that file's own rule, *"NO SIZES ARE WRITTEN HERE"*. **Not checked:** the Windows figure - pip's `torch` for Windows is a different wheel, and the owner's PC is where the announcement is actually read.

**State.** **MOVED TO ISSUE [#931](https://github.com/Ajmalpshaik/Heron-AI/issues/931) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired**, by the rule at the head of this section. Choosing the repair is the owner's: print a figure per platform, or print that the figure depends on the platform and the command that shows it with dependencies, and name *installed, weights not fetched* as its own state. `tests/test_rerank.py` section 7 checks only that `SIZE` appears in the announcement, so either repair leaves it green.

---
