# Session note — A NEWLINE WAS A FOLDER NAME IN THE ONE FILE NOTHING COMPILES

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — A NEWLINE WAS A FOLDER NAME IN THE ONE FILE NOTHING COMPILES

**[Row 5b-132](../FRAGMENT-ISSUES.md), FIXED.** `tools/check-products.py` read end to end — 527
lines, never opened before, and the **largest** of the seven unread CI gates.

It is the gate on `platform/heron-products.json`, which [D-93](../DECISIONS.md) makes the single
list of what Heron installs. Its own words are why it exists:

> the price of that is a file nothing compiles: a typo in it reaches a modeller's machine
> untouched by every other gate in this folder

**All three format patterns ended in `$`, and in Python `$` also matches just before a final
newline.** Measured, on manifests built by mutating a copy of the real one:

| the value | the gate |
|---|---|
| `"folder": "Heron\n"` | accepted, exit **0** |
| `"id": "heron-doc\n"` | accepted, exit **0** |
| `"addInId": "7A1F…5B41\n"` | matched the GUID pattern |

`FOLDER` carries a comment saying exactly why it exists — *"a separator here would write outside
the release folder, and `..` would write outside Addins altogether"* — so it is a **safety check
with a documented purpose and an input that walks past it**. JSON carries a newline inside a
string quite happily. All three end in `\Z` now.

**One of the three was caught downstream anyway, and it is worth recording which**: the GUID case
exited 1 regardless, because `check_assets` compares the manifest's `addInId` against the shipped
`.addin`'s and that comparison is exact. The id and the folder had nothing behind them.

### The rest of the file is a negative result, and the suite records it as one

`tests/test_check_products.py` — every other section green **before the fix and after it**: a
duplicate `addInId` (the thing D-88 says no compiler, test or gate in this repository can see); a
`partOf` naming nothing, *the typo that shows up as a missing tab rather than as an error*; a
release the repository does not build, refused with AJ Tools' L3 in the message; a release named
twice; two products sharing a folder (R-41, L5); a version disagreeing with
`Directory.Build.props`; an unknown key; a lower-case GUID; a manifest that will not parse; and
`--file` with nothing after it.

**Shown to have teeth three ways**: the anchors put back (**2 red**), the duplicate-GUID report
removed (**1 red**), and a release the repository does not build accepted (**1 red**).

### Worth knowing rather than re-deriving

The supported release list is **read** from `brain/heron_dotnet.RELEASES`, never typed, because AJ
Tools' L3 was a hardcoded version list left behind after per-version builds landed — and the
installer **silently installed nothing** on three releases while the document advertised them.

Headingness is **derived** from `partOf` and from nothing else, so it is not a field anybody can
set wrongly, and it is enforced in both directions. `folder` is deliberately **not** derived from
the assembly name, because `heron-bridge`'s assembly is `Heron.Revit.Addin.dll` and its folder is
`Heron` — the live layout on every machine Heron is installed on. `shipped_addin` walks the
**whole** of `revit/` after an earlier version looked in one named folder and passed a manifest it
should have questioned. And **PROVING exists as a third state because the checker asked for it**:
a file Revit will load is a file the one true list has to account for, and silence would have been
the dishonest option.

**Measured and not a defect**: only one `.addin` of each name exists under `revit/`, so
`shipped_addin`'s first match is unambiguous today.

**One thing recorded rather than fixed**: `--file` is the only flag and an unknown one is ignored
in silence, so `--fil` runs against the default manifest and exits 0. Stated precisely rather than
talked up — the first line it prints is `Manifest: <path>`, so it does **name** the file it read.
What is missing is the refusal, not the honesty.

**Two CI gates remain unopened**: `check-fragments-compile` (400) and `check-intrusion` (213).
