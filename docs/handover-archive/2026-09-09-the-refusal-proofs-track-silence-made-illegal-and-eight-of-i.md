# HANDOVER — 2026-09-09 (the REFUSAL-PROOFS track): silence made illegal, and eight of it proved

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**To carry this on, say:** *"Read the REFUSAL-PROOFS track entry in HANDOVER.md."*

§3h.1 of [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) asked for one thing: **a fragment handed something
it cannot use must REFUSE and say why, never report `0`.** Twelve fragments were changed (PR #50),
eight have been run against a model with both legs (PR #67), and §3j holds the numbers.

### What is DONE

| | |
|---|---|
| **12 fragments refuse** | `color-by-parameter`, `measure-run-quantities`, `edit-text-values`, the seven schedule ones, `add-revision-cloud`, `set-view-section-box` |
| **8 proved, both legs** | signed by Ajmal PS and promoted — the library is **167 proven, 193 to go** |
| **9 new `provides`** | seven `refused`, two `refusalReasons` — §3h.1 was wrong that every fragment already had one |
| **Model unharmed** | 9,628 elements before and after, unsaved-changes marker cleared |

**The seven schedule fragments now resolve a `ScheduleSheetInstance`**, so §3e's door is open for all of
them, not just the two READ ones. `add-schedule-fields` answered `handed 3, schedulesSeen 3` and handed
back 256 real field names.

### What is NOT done, in the order it is worth doing

**1. THE EIGHT ARE DONE — signed and promoted.** Ajmal PS accepted all eight on 2026-09-09, so each
carries a `proof:` block with his name, the model and session, both cases verbatim and a fingerprint;
`heron-status` was then moved to `PROVEN` as the separate act `accept` reserves for a person. **The
library went 159 → 167 proven, 201 → 193 to go.**

Each proof records the gap *no second route was run* — D-30's *"where one exists"* clause, undecided
rather than failed. Anyone re-reading these should know that is what the signature stood over.

The drafts are gone: `accept` consumes them as it records, so each fact has one home rather than two.
The commands, for the next batch:

```bash
python brain/heron_validate.py review <fragment>
python brain/heron_validate.py accept <fragment> --by "Ajmal PS"
# accept does NOT promote - it says so on every run. Afterwards:
sed -i 's/^heron-status: DRAFT$/heron-status: PROVEN/' brain/fragments/<fragment>/fragment.yaml
```

**2. `edit-text-values` HAS NEVER RUN — not one leg.** `Text Notes` is not selectable on the sheet
`Notes, Symbols & Schedules`, so the seven notes §3c found live in some other view and **nothing in the
library lists which**. §3d's `LIST_*` gap is the only thing between it and a proof; the refusal path is
written and compiles. **Its draft says `NOT ESTABLISHED` for both legs and must not be accepted.**

**3. Two were skipped by decision, not failure.** `export-schedule-to-csv` is `risk: PUBLISH` and the
client refuses to send above Modify (§5 defect 9). `add-revision-cloud` wants `revisionId` as an
`ElementId`, which D-54 does not resolve — the same `ElementId` wall the caller-inputs track names as
its next blocker.

**4. `set-view-section-box`'s proof is stale and still says `PROVEN`.** It leaves a `refusalReasons`
output it did not have when fingerprint `49c749930e4bed33` was taken. Nothing it recorded is
contradicted, and §3f's stronger negative — a 3D view whose selection has nothing to enclose — is now
worth running, because it finally reports something different from the weak one.

### THE MARGIN GUARD HAS COME BACK ONCE. IT MUST NOT COME BACK AGAIN.

The owner had the inside-out-margin guard removed from `set-view-section-box` before #50 merged. The
CALLER-INPUTS track dropped it correctly in #55 and wrote *"it must not be re-added"* in this file —
and then **a later merge (#61) put it back anyway**, carrying a real `enclosed = 0;` fix inside it.
Removed again here.

**The lesson is not "be careful".** It is that a commit dropped from one branch survives on every other
branch that already had it, and git will not flag it: the rewritten copies have different patch-ids, so
`--cherry-pick` reports nothing and a merge reads it as new work. **Check by content, not by history** —
`git diff main...HEAD | grep` for the thing that was supposed to be gone.

**AND THE SAME MERGE LEFT MAIN NOT COMPILING.** `set-schedule-filters` ended up declaring `refused`
**twice** — both branches added the same entry and the merge kept both — so
`tools/check-fragments-compile.py` failed on all eight releases with
`CS0128: __provides_refused is already defined`. Found by running the gate while closing this session,
not by anything that watches. Fixed here, and the whole library was swept: **that was the only one.**

> A merge can produce a fragment that no longer compiles without either side being wrong. **Run the
> compile gate after a merge, not only after an edit.** `tools/check-*.py` all passed while this was
> broken — none of them reads a contract for duplicate names.

### Three things about ARRANGING a proof, each learned by getting it wrong

**1. `open-view` cannot be a setup step.** Setup runs every step down `run_fragment_read`, and
`open-view` is `risk: EXECUTE` — step 0 is refused and **every leg of every job fails**, which reads as
a broken arrangement. Adding it made a batch of seven *worse* than leaving it out. Run it as its own
`validate` first; give the negative a view name that does not exist and the positive's view stays up.

**2. Both legs must select in ONE view, or the arrangement depends on what is on screen.** Selection is
view-scoped. A positive on a sheet and a negative in `FloorPlan: M1` works only until an earlier job
leaves the wrong view open — then six jobs fail at setup with no hint that the *view* is what moved.
Find the contrast inside one view: `Schedule Graphics` against `Title Blocks` on the same sheet.

**3. One Revit, one session — and it never says so.** §3i already records a stale lease reporting
`Could not identify the active model`. Two more shapes of the same collision, both met here:

- **`ping` succeeds while every real request times out.** The bridge answers on its own thread; the
  Revit API thread is what is busy. `doctor` showing `ping ok` does **not** mean Revit is available.
- **The lease countdown goes UP.** Each refused attempt counts as activity and renews the very lease it
  is waiting on. Stop touching it and wait, or stop the other session.

**And the MCP tools and the command line hold DIFFERENT leases.** One `revit_use_this_model` from a
chat takes the lease under that chat's id; the CLI pins its own and is refused; `release` cannot hand
back a lease it does not own; and the MCP server's id is a fresh uuid per process, so it cannot be
matched. **Pick one and stay on it for the whole run.**

### Two facts about the machinery worth not rediscovering

- **`write.enabled` is a file, and it is `false` at rest.** `%APPDATA%\Heron\config\heron.config`. It
  takes effect immediately — nothing to restart — and every MODIFY proof needs it `true`. Set it back
  afterwards; Heron's own health check says so.
- **Proof drafts are gitignored on purpose** (`.gitignore:153-154`). They cannot be committed and should
  not be. `accept` writes the `proof:` block into the fragment and deletes the draft, and it leaves
  `heron-status` alone — promoting is a separate, deliberate act.

### Where the work is

| | |
|---|---|
| PR #50 | the twelve refusals |
| PR #67 | §3j — the eight proofs, and the three arrangement findings |
| PR #69 | the close-out: the duplicate `refused` that stopped main compiling, and the margin guard removed a second time |
| `tools/jobs/reprove-refusals.yaml` | the schedule arrangement, on the sheet |
| `tools/jobs/measure-only.yaml` | the duct arrangement, in `FloorPlan: M1` — two files because of finding 2 |

---
