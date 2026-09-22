# Session note — 2026-09-14/15 — the machinery was the backlog

> **Archived session note** from 2026-09-19. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---


# 2026-09-14/15 — the machinery was the backlog

> **Appended, not edited above.** This is [suggestion 5](2026-09-16-what-is-new-since-the-last-handover.md#what-i-would-do-next--suggestions-not-decisions)
> being taken: a dated section per sitting. **Where a sentence above this line disagrees with one below
> it, the one below is later.** Nothing above has been deleted.
>
> Its working note was deleted on 2026-09-19, its durable half having landed first.
> Durable detail: [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) rows **89–94** and
> [`DECISIONS.md`](../DECISIONS.md) **D-72**.

## Where the library stands

**Derive it. Do not read it here.**

```bash
grep -h "^heron-status:" brain/fragments/*/fragment.yaml | sort | uniq -c
```

At close: **298 `PROVEN` / 62 `DRAFT`**, 360 total — up from 291. Also derivable: **131 `READ` proved,
153 `MODIFY` proved.**

Two things the count alone will not tell you:

- **Nothing is left signed-but-unpromoted.** `python tools/check-signatures.py` exits 0 on that half.
  Worth knowing anyway: `accept` writes the proof and **deliberately does not promote** — promotion is
  a separate hand edit, and that gap once left thirteen of his signatures sitting at DRAFT.
- **One signature is STALE** — `set-wall-constraints`, signed 2026-09-13 with its code changed
  underneath afterwards. That is D-30 working, not a fault. **Re-proving it is the owner's call.**

## What was proved, and the thing worth taking from it

Seven fragments. **Not one was blocked by anything in a model.** Every one was blocked by the
machinery, and each block was a different shape:

| | what was actually in the way |
|---|---|
| `set-sheet-title-block` | a counter that counted the CALL, not the change ([row 89](../FRAGMENT-ISSUES.md)) |
| `create-line` | `IList<IList<XYZ>>` could not be typed at all |
| `filter-elements-by-id` | `selected` reached `OneElement` and never reached the id-list branch |
| `override-graphics-in-view`, `set-category-graphics`, `set-link-graphics` | `OverrideGraphicSettings` could not be typed |
| `apply-view-filter` | a yes/no result counted as zero, so **no arrangement could ever have passed** ([row 93](../FRAGMENT-ISSUES.md)) |

**The 5-of-5 batch is the only one this project has had**, and the reason is the point: those five were
arranged *after* the blockers came out. The four rounds before it returned 1, 2, 1 and 2 from 23 jobs,
and every failure was a blocker already written down. **Read `python tools/generate-jobs.py` before
building a batch** — it names the blocker per fragment.

## Two findings that change how to work

**A fragment's `.cs` is LIVE. The add-in's is not.** `heron_bridge_client.py` reads
`brain/fragments/<name>/impl/any/fragment.cs` off disk and sends it on **every call**, so a fragment
can be fixed, re-run and re-proved **with Revit open**. Only `revit/Heron.Revit.Addin/*.cs` costs a
close-build-deploy-reopen. Two defects were found, fixed and re-proved on 2026-09-14 without closing
Revit once. **Do not ask the owner to close Revit for a fragment fix** — batch the add-in changes and
spend one close on all of them.

**The reply truncates, and it hid a bug for four days.** `RevitFragment.Describe` renders a list item
short, so a finding that opens with context arrives with its last sentence cut off. **When a refusal is
unexplained, put the API's own message FIRST and run it again.**

## Superseded above this line

- **"What is still refused, most-wanted first: `ElementId`, `Element` as a specific instance,
  `OverrideGraphicSettings`"** — four types were built on 2026-09-14 ([D-72](../DECISIONS.md)):
  `IList<IList<XYZ>>` (a PIPE between pairs), `OverrideGraphicSettings`, `ForgeTypeId`,
  `ParameterValue`, plus `selected` for a **list** of element ids. **One type is still refused and it
  is not waiting on a rule: `IList<Reference>`, a FACE.** It is picked with a mouse and no text names
  one.
- **`switch-active-project` is not a fragment defect and never will be proved.** With two projects
  genuinely open, four routes were tried and all four answered with Revit's own sentence — *"Changing
  the active view is not applicable to inactive documents."* [Row 94](../FRAGMENT-ISSUES.md). It sits in
  the 62 and can never move; **whether it stays counted is the owner's call.**

## Waiting on the owner

| | what | why only he can |
|---|---|---|
| **2** | select ONE element in Revit, for each of **13** fragments that need *that particular one* | a Revit selection is a person's act |
| **3** | **`--allow-publish`, yes or no** — 3 fragments are `risk: ADMIN`. `add-project-parameter` also writes a shared-parameter file to disk **which does not roll back** | proving a PUBLISH/ADMIN fragment is a decision |
| **4** | `gh auth refresh -h github.com -s workflow` in an interactive terminal | the token has no `workflow` scope; see below |
| **5** | decide whether `switch-active-project` stays in the DRAFT count | it changes a number he tracks |

`set-global-parameter` is **no longer code-blocked** — `ParameterValue` is typeable. It needs a global
to exist and both open models hold **zero**; making one is `create-global-parameter`, which is `ADMIN`.

## GitHub, and the one thing that cannot be pushed from here

`main` is current and CI is green. Branches on origin: `main`, the Dependabot branch, and whatever
another session has open — **check, do not assume.**

**`.github/workflows/` cannot be pushed from this machine.** The `gh` token is `gist, read:org, repo`
with **no `workflow` scope**, and a push carrying a workflow file **rejects the whole push**, not just
that file. Two things are parked on it:

- a local branch **`ci/run-check-signatures`** — one commit, seven lines, adding `check-signatures` to
  the Gates workflow. **CI does not run that gate today.**
- **PR #122** (Dependabot, `actions/setup-dotnet` 5→6) — clean and mergeable, refused on the same scope.

The `dependencies` label Dependabot asks for **has been created**, so that complaint is gone from
future bumps.

## Two tests are red on `main` and they are NOT from this work

```
tests/test_builder.py        KeyError: 'brain/heron_duct_sizing_reviewer.py'
tests/test_instructions.py   a duplicate id is refused by name
```

Both files were last touched by **PR #141**, merged by another session on 2026-09-14. Neither touches
anything this sitting changed. **CI is green because neither is in the workflow's suite** — which is
itself worth knowing. Written down here rather than left to be blamed on the nearest commit.

## One number that lied for weeks

`tools/owner-queue.py` printed *"163 DRAFT fragments need a model"* **two lines above its own
instruction to derive the number**. The real figure was 63. It counts them itself now. **A tool that
tells you to check a number it has just got wrong teaches the reader to trust neither.**
