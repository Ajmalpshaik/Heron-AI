# Project review and fix plan - 2026-10-08

**Status: ACTIVE.** Written 2026-10-08 at the owner's request: check the whole project, plan the
fixes, then fix what can be fixed without Revit, continuously.

**Closure condition:** deleted when every row in the waves below is either fixed (its register row
says so, with the commit) or has moved to the register that owns it. It is a schedule, not a
register - the permanent home of every item is the row id it names in
[FRAGMENT-ISSUES](../../FRAGMENT-ISSUES.md).

---

## 1. What was checked, and what it said

| check | result, 2026-10-08, branch `claude/heron-project-review-aik7wy` at `7bd6234` |
|---|---|
| The eleven CI gates (`heron-ship` §1) | **all eleven PASS**, after `git fetch --unshallow` for `check-decision-titles` |
| `git diff --check` | PASS |
| `tools/check-gaps.py` | **NOT RUN to the end** - stopped at the container's background time limit while running the suites. Run it on its own; read its buckets, not its exit code |
| `tools/open-defects.py` | read by id; every open row of sections 5 and 5b was triaged against the code on disk |
| The register itself | **one defect found by running its own tool** - see wave 1 |

Every open row whose state did not already name a wait (*built and NOT RUN*, *NEEDS REAL REVIT*,
*for the owner*) was read against the code as it stands, in four batches, and put in exactly one
bucket. **Derive the open list again before using any of this** - `python tools/open-defects.py` -
because rows move every day.

## 2. The buckets

| bucket | meaning | where the work goes |
|---|---|---|
| **FIX HERE** | Python, docs, cards, or C# outside a `PROVEN` fragment's `impl/`; no owner decision; no Revit needed to know the right answer | waves 1-6 below |
| **STALE** | the code already does what the row asks | close the row with the evidence |
| **NEEDS REVIT** | only a run on a real model can show the right fix, or the fix is inside a `PROVEN` fragment's `impl/` (it would stale the proof) | the owner's PC - [NEEDS-CHECKING](../../NEEDS-CHECKING.md) and `python tools/owner-queue.py` |
| **NEEDS THE OWNER** | the row asks a policy or design question | the owner, one question at a time, in plain words (how D-81 to D-84 were answered) |

## 3. The waves - FIX HERE, in order

Each fix: read the row, reproduce it, write the check and **see it fail first**
([`heron-ship` §2a](../../../.claude/skills/heron-ship/SKILL.md)), fix, run the gates. One row's fix
never widens into the next.

### Wave 1 - the register tooling (found by this review)

| row | fix |
|---|---|
| new 5b row | `tools/split-register.py` re-banded rows 201-370 and wrote every link to a file beside them one folder too high (`../section-5b-rows-051-075.md`). `tools/register-text.py` now reads a link to a file beside it as a link into the folder, and the split writes it back as the file beside it. The real register is re-banded with the fixed tool, and every link elsewhere that named a moved row is re-pointed to its band |
| 5b-255, 5b-262, 5b-363 | the same defect in `tools/needs-checking-register.py` and `tools/split-needs-checking.py`, which rewrote groups it was not moving. Fixed the same way, and a group the run is not moving is now left exactly as it was |

### Wave 2 - close the stale rows, with evidence

133, 151, 178, 5b-217 (a duplicate of 5b-298).

### Wave 3 - small Python, tool and suite fixes

5b-196 (`change-evidence.py` decodes without `encoding=`), 5b-186 (`test_heron_session.py`
`commonpath` across drives), 5b-153 (rows 106 and 150 hidden from the open list), 5b-108
(`heron_scope.Store.db` lets an `ATTACH` past the guard), 5b-158 (the central-skip sentence ignores
`skipReasons`), 5b-199 (`check-gaps` files A15 under Revit), 5b-229 (`check-routing` rebuilds only on
a changed id set), 5b-242 (`batch-prove --dry-run` lets a job without `negative-setup-set` through),
5b-278 (`test_workforce` check 5 reads the live register), 5b-160 (the build matrix baseline),
5b-198 (`check-change` misses a changed contract), 5b-228 (a value called IGNORED that a setup step
takes), 5b-150 (`check-api-surface` with no `dotnet`).

### Wave 4 - words on cards and pages (no proof goes stale: the fingerprint is `impl/` only)

5b-162 (docs/38 on what a cloud session shows), 5b-232 (a comment naming a capability nobody
provides), 5b-358 (`array-family-forms` card), 5b-193 (`select-in-region` says *in* for *touches*),
177 part 1 (`create-family-extrusion` purpose), the stale typed counts in group I (from row 143), the
unreachable `UNREAD_KEYS["optional"]` text (from row 101), and the job files still in feet (from row
5b-204).

### Wave 5 - C# outside a PROVEN `impl/`, compiled on 2020-2027 before it is pushed

5b-292 (a type named `Family : Type`), 5b-197 (`HeronAtomicWrite` with no retry), 5b-159 (the
dispatcher drops `warnings`), 5b-134 (`check-fragments-compile` repeats the props), 128
(`place-structural-family` accepts a column type as a beam), 5b-338 (the space probe at head height),
5b-203 (four DRAFT readers with no duplicate-name check), row 10's `SafeRollBack` sentence, row 120's
`create-electrical-circuit` `refused` type. **Every one of these still needs its run in Revit** - a
compile is not a proof.

### Wave 6 - larger, one change each, and only after waves 1-5

75 (a linked element bound as a host id), 5b-233 (a shared store rebuilt from a branch), 5b-274 (the
Companion guesses an outcome), 5b-339 (a head on a re-entrant wall passes - a published check, so it
needs its own before/after measure), 146 (four questions declared nowhere), 5b-181 (version symbols
never reach a fragment - changes the compile path every fragment runs through), 5b-289 (a new
boundary-lines fragment), 5b-321 and 5b-337 (need `model2vec` installed to measure routing).

## 4. Not fixable here - and why

**NEEDS REVIT** (the triage's evidence is in the work log of this session; the register rows say the
rest): 10, 18, 19, 21, 32, 36, 41, 45, 143, 175, 179, 180, 5b-154, 5b-180, 5b-188, 5b-192, 5b-194,
5b-202, 5b-211, 5b-218, 5b-222, 5b-260, 5b-291, 5b-294, 5b-295, 5b-296, 5b-297, 5b-300, 5b-301,
5b-302, 5b-303, 5b-304, 5b-314, 5b-323, 5b-336, 5b-342, 5b-350, 5b-351, 5b-352, 5b-361, 5b-367, and
every row whose state already says *built and NOT RUN*.

**Two found in passing, worth the owner's eye first:** row 18/45 - Revit's own API names
`RBS_CURVE_HOR_OFFSET_PARAM` and `RBS_CURVE_VERT_OFFSET_PARAM` *Horizontal* and *Vertical
Justification*: they are the justification settings, not offsets, which fits "only 0 takes". And
5b-350 is HIGH - a silent model change with its cause not established.

**NEEDS THE OWNER:** 73, 99, 101, 103, 108, 116, 120, 129, 131, 132, 135, 137, 140, 141, 142, 144,
148, 158, 174, 176, 5b-83, 5b-95, 5b-103, 5b-105, 5b-107, 5b-110, 5b-113, 5b-125, 5b-131, 5b-136,
5b-145, 5b-161, 5b-175, 5b-182, 5b-185, 5b-187, 5b-200, 5b-205, 5b-212, 5b-213, 5b-214, 5b-215,
5b-219, 5b-221, 5b-224, 5b-225, 5b-226, 5b-227, 5b-230, 5b-235, 5b-237, 5b-240, 5b-246, 5b-271,
5b-285, 5b-288, 5b-290, 5b-320, 5b-331, 5b-346, 5b-348, 5b-356, 5b-364.

## 5. Progress

**The register is the record** - each row's own state says FIXED, or OPEN with what is still owed. This
is the schedule's view of it, on branch `claude/heron-project-review-aik7wy` (PR #449).

| wave | done | still owed |
|---|---|---|
| 1 | 5b-380 (was numbered 5b-371 until a parallel PR took it), 5b-255, 5b-262, 5b-363 | **5b-381** - the splitter still does not re-point links that NAME a moved row; done by hand twice |
| 2 | 133, 151, 178, 5b-217 closed | - |
| 3 | 5b-196, 5b-186, 5b-153, 5b-108, 5b-158, 5b-199, 5b-229, 5b-242, 5b-160, 5b-198, 5b-228, 5b-150 | 5b-278 is half done - one check isolated, the rest is the planner owner's call |
| 4 | 5b-162, 5b-232, 5b-193; card words for 5b-358 and 177; job files for 5b-204; row 101's dead entry | group I's counts are dated records - left as they are, legally |
| 5 | 5b-292, 5b-134 | **fixed in source, the run owed:** 5b-197 (40 kernel runs on the owner's PC), 128 and 5b-338 (a model), row 10's move path. **Not started:** 5b-159 (a number reader in the bridge), 5b-203, row 120's `refused` |
| 6 | 5b-339 | 75, 5b-233, 5b-274, 146, 5b-181, 5b-289, 5b-321, 5b-337 |

**Parallel work collides on the register.** Every other open PR appends its rows to
`section-5b-rows-176-200.md`, which this branch re-banded; each merge of main here moved those rows to
their band by hand and renumbered nothing of theirs. Once this merges, new rows go at the end of
`section-5b-rows-376-400.md`.

This note is deleted once waves 1-6 are all either fixed or moved.
