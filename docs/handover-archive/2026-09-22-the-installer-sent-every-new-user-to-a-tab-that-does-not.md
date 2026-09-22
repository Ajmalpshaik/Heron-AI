# Session note — THE INSTALLER SENT EVERY NEW USER TO A TAB THAT DOES NOT EXIST

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — THE INSTALLER SENT EVERY NEW USER TO A TAB THAT DOES NOT EXIST

**[Row 5b-138](../FRAGMENT-ISSUES.md), FIXED.** `tools/setup.ps1` read end to end — 209 lines, and **the
last tool in `tools/` that no suite names at all.** Every tool in `tools/` is now held by at least one
suite.

It is the first thing anybody runs, and the last thing it prints is what to do next. Line 183 said:

```
2. Ribbon -> Heron AI -> Heron
```

**[D-85](../DECISIONS.md) renamed that tab on 2026-09-20** — tab `Heron AI` → `Heron`, panel `Bridge` →
`AI Bridge` — and its own consequences section says what a miss costs:

> *"Eight printed strings moved with it … **A tab renamed without them is a tool that tells the user to
> press a button that no longer exists.**"*

It listed *"the deploy script"*. **`setup.ps1` was the ninth and it was missed.**

#### Measured five ways, all agreeing

| Where | What it says |
|---|---|
| `HeronApplication.cs:53-54` | `TabName = "Heron"`, `PanelName = "AI Bridge"` |
| `platform/heron-products.json` | `"tab": "Heron"` — and D-93 makes that the one true list |
| `tests/test_ribbon_tab_sharing.py:136` | asserts the same |
| **`Z1` in `NEEDS-CHECKING.md`** | the struck-through observation **on a real Revit** |
| **`tools/deploy-addin.ps1:721`** | **the script `setup.ps1` itself calls** — prints it correctly |

**Nine files print that ribbon path. Eight were right; this was the one that was not.**

#### Traced and not raised

[`docs/07-installation-and-update.md:326`](../07-installation-and-update.md) also says tab **Heron AI**,
and **it is right to** — it is a recorded journal entry with a note beside it saying the labels changed
on 2026-09-20, that a proof is not edited after the fact
([Golden Rule 4](../14-golden-rules.md)), and what a journal written today would say instead.

#### The suite cannot run the script, and says so

There is no PowerShell on this container and the script builds C# for a Revit that is not here.
`tests/test_setup_script.py` asks only the half answerable **without Windows**: does what the installer
*says* agree with what the repository *does*? **The ribbon path is derived from the add-in's own
constants and never typed in the test**, so the next rename fails here until the script follows; the
supported release list is read from `brain/heron_dotnet.RELEASES` for the same reason.

**2 red** against the file as found. Teeth five ways: the line exactly as found (2), the add-in
renaming its tab with the script left behind (1), a typed path stopping existing (1), a list of years
back in the code (1), and a year shown to a user leaving the supported list (1).

**Two of the suite's own checks were wrong first and were rewritten rather than relaxed** — a negative
lookahead that backtracked past the space and failed on a correct line, and a year count that counted
years in comments.

**Recorded, not fixed:** nothing checks that the nine printers agree **with each other**. That belongs
with `test_ribbon_tab_sharing.py`, which owns the tab question.

#### Where the sweep stands in `tools/`

**All 50 tools are now named by at least one suite.** **22 have still never been read**, largest first:
`generate-jobs` (1157), `prove-skill` (950), `prove-agent` (934), `check-skill-routing` (770),
`batch-prove` (735), `generate-contract-reference` (718), `generate-skill-catalog` (660),
`check-revit-gate` (651), `prove-tracking` (633), `heron-backup` (454), `check-risk-crossings` (440),
`measure-brain` (435), `new-agent` (410), `generate-agent-map` (400), `measure-routes` (365),
`generate-fragment-catalog` (359), `generate-api-docs` (338), `check-reachable` (334),
`check-declared-questions` (277), `owner-queue` (231), `check-api-surface` (223), `api-changes` (208).
