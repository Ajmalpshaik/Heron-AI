# Proving session handover — 2026-09-14 and 15

> **Type:** Operational work note — the handover for one proving session. **Not specification.**
> Where a sentence here disagrees with the [Constitution](../../../HERON_CONSTITUTION.md), the
> [Golden Rules](../../14-golden-rules.md) or [DECISIONS.md](../../DECISIONS.md), **those win.**
>
> **Status at close: 298 `PROVEN` / 62 `DRAFT`**, up from 291. Seven signed by Ajmal PS and all seven
> promoted.
>
> **This note is scaffolding.** Everything durable is already in
> [`FRAGMENT-ISSUES.md`](../../FRAGMENT-ISSUES.md) rows 89–94, in [D-72](../../DECISIONS.md), and in
> the job files under [`tools/jobs/`](../../../tools/jobs/). **Delete this once the next session has
> read it.**

---

## 1. Where the library stands

**Derive it, do not read it here.**

```bash
grep -h "^heron-status:" brain/fragments/*/fragment.yaml | sort | uniq -c
```

At close: **298 `PROVEN`, 62 `DRAFT`**, 360 total. Also derivable: **131 `READ` proved, 153 `MODIFY`
proved.**

Nothing is left signed-but-unpromoted: `check-signatures` exits 0 on that half. One signature is
**STALE** — `set-wall-constraints`, signed 2026-09-13 with the code
changed under it afterwards. Re-proving it is the owner's call; it is not a fault.

## 2. What was proved, and what it cost to get there

Seven fragments, and **not one of them was blocked by anything in a model**. Every single one was
blocked by the machinery, and each block was a different shape:

| Fragment | What was actually in the way |
|---|---|
| `set-sheet-title-block` | a counter that counted the CALL, not the change ([row 89](../../FRAGMENT-ISSUES.md)) |
| `create-line` | `IList<IList<XYZ>>` could not be typed at all ([D-72](../../DECISIONS.md)) |
| `filter-elements-by-id` | `selected` reached `OneElement` and never reached the id-list branch |
| `override-graphics-in-view` | `OverrideGraphicSettings` could not be typed |
| `set-category-graphics` | the same, plus a view template on the view first tried |
| `set-link-graphics` | the same |
| `apply-view-filter` | a yes/no result was counted as zero, so no arrangement could pass ([row 93](../../FRAGMENT-ISSUES.md)) |

**The batch that returned 5 of 5 is the only one this project has had.** The reason is worth keeping:
those five were arranged after the blockers were removed, not before. Four earlier rounds returned 1,
2, 1 and 2 from 23 jobs because every failure was a blocker already written down.

## 3. The two findings that outrank the rest

**A fragment's `.cs` is live; the add-in's is not.** `heron_bridge_client.py` reads
`brain/fragments/<name>/impl/any/fragment.cs` off disk and sends it on **every call**, so a fragment
can be fixed, re-run and re-proved with Revit still open. Only `revit/Heron.Revit.Addin/*.cs` needs a
close-build-deploy-reopen. Two defects were found AND fixed AND re-proved on 2026-09-14 without
closing Revit once.

**The reply truncates, and it hid a bug for four days.** `RevitFragment.Describe` renders a list item
short, so a finding that opens with context arrives with its last sentence cut off. When a refusal is
unexplained, **put the API's own message FIRST and run it again.** That is how *"Changing the active
view is not applicable to inactive documents"* finally surfaced.

## 4. `switch-active-project` cannot be done, and that is now measured four ways

With **two projects genuinely open** — `test projject` in front, `Snowdon-scratch_ajmal.al` behind —
every route answered with Revit's own sentence:

1. `RequestViewChange` on the host's `UIDocument` — refused
2. `RequestViewChange` on a `UIDocument` built for the target — refused
3. the `ActiveView` **setter** on that target `UIDocument` — refused
4. naming a view **already open** in the target — refused

`ShowElements` was deliberately **not** tried: it can raise a modal dialog inside the bridge's API
context and hang the session until somebody clicks it.

**It is one of the 62 and it can never move.** Whether it stays in that count is the owner's call.
[Row 94](../../FRAGMENT-ISSUES.md).

## 5. What is genuinely blocked, and by what

Derive the live list with `python tools/generate-jobs.py`. At close:

| Blocker | Roughly | Whose call |
|---|---|---|
| needs ONE element **selected in Revit** | 13 fragments | the owner, one click each |
| two needs both filled by `elements` — one selection cannot be both | 5 | a contract change |
| `risk: ADMIN` — above what `HeronPermissions` runs in Phase 0/1 | 3 | **the owner: `--allow-publish`** |
| `IList<Reference>` — a FACE, which no text names | 1 (`place-family-on-face`) | needs Revit's own picking |
| `linkedElementIds` — nothing in the chain provides it | 1 (`copy-from-link`) | a contract change |
| content the models do not hold | several | a different model |
| the API declines the operation | 1 (`switch-active-project`) | nobody |

`set-global-parameter` is **no longer code-blocked** — `ParameterValue` is typeable now. It needs a
global to exist, and both open projects hold **zero**. Making one is `create-global-parameter`, which
is `ADMIN`.

## 6. Two tests are red on `main`, and they are not from this work

```
tests/test_builder.py        KeyError: 'brain/heron_duct_sizing_reviewer.py'
tests/test_instructions.py   a duplicate id is refused by name
```

Both files were last touched by **PR #141** (*The seams first, then the departments they made cheap*),
which another session merged on 2026-09-14. Neither touches anything this session changed. Recorded
here rather than left for somebody to trip over and blame on the nearest commit. **CI is green** —
those two are not in the workflow's suite.

## 7. Do not repeat these

- **Do not ask the owner to close Revit for a fragment fix.** See §3. Batch the add-in changes and
  spend ONE close on all of them.
- **Do not build a batch out of fragments blocked by the machinery.** Run `generate-jobs.py` first; it
  names the blocker per fragment.
- **Do not trust a stated count.** `owner-queue.py` said *"163 DRAFT fragments"* two lines above its
  own instruction to derive the number; the real figure that day was 63. It derives it now.
- **`.github/workflows/` still cannot be pushed** from this machine — the `gh` token has no `workflow`
  scope. `gh auth refresh -h github.com -s workflow` in an interactive terminal fixes it, and only the
  owner can run it. A branch `ci/run-check-signatures` is waiting locally with the one commit.
