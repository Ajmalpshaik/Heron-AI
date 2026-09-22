# Session note — Sitting of 2026-09-16, third - what reading somebody else's code found in ours

> **Archived session note** from 2026-09-21. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---


# Sitting of 2026-09-16, third - what reading somebody else's code found in ours

**Asked to look at an unrelated open-source WPF app** (a 2D drawing tool with a Roslyn scripting host
and an MCP bridge - the same three problems Heron has) **and say what was worth taking.**

**Almost nothing was worth taking, and that is the result rather than a disappointment.** Its geometry
library is 2D and Revit's is 3D and authoritative; its pipe-with-an-ACL transport is what
`BridgeServer` already does; its compile cache is what `RevitFragment` already does, keyed the same
way. **What it was worth was as a mirror.** Three things wrong with Heron came out of reading it, and
none of them would have been found by reading Heron.

## The one that could have killed Revit

`RunScript`'s comment says *"a fragment that throws is a finding, not a crash."* True of every
exception but one. **`StackOverflowException` cannot be caught** - the runtime fails fast, no handler
runs, and the process it ends is Revit with the user's unsaved model in it.

**Zero of the 372 fragments recurse today**, so this was a door standing open rather than a fire. But
a fragment's `.cs` is live - read off disk and sent on every call - so the door is one edit wide.

### The idea was taken. The code was NOT, and that is the whole lesson

The usual form of this rewriter guards named members and **deliberately skips lambdas**, for a stated
and perfectly good reason: recursion flowing only through anonymous functions is not a real shape in
ordinary C#.

**Measured here: 0 of 372 fragments declare a method or a local function.** A fragment is a script -
top-level statements and `Func<>` lambdas. So the received rule, applied unexamined, **would have
shipped a guard that compiles, passes review, and protects nothing at all.** Guarding block-bodied
lambdas instead covers **91**.

That is [the standing rule](../../HERON_CONSTITUTION.md) earning its keep in the most literal way
available: *study the thinking, never copy the code.* Copying would have produced a green gate over an
open hole - which is the failure this whole repository is organised against.

**What is still unguarded, deliberately:** an expression-bodied lambda. Rewriting one can turn `Func`
into `Expression` and silently pick a different overload. **A guard that occasionally misses is worth
more than one that changes what working code means.**

### It is proved over the library, and that proves less than it sounds

`tests/Heron.StackGuard.TestHost` runs the rewriter over **every fragment in the library**: line
counts unchanged (so error line numbers still point at `fragment.cs`), no new diagnostic introduced,
and every lambda-carrying fragment guarded. On 2026-09-16 that was **372 fragments and 836 checks**;
on 2026-09-21 it is **395 fragments, 882 checks and 91 carrying a guard**, which is the host counting
rather than this page claiming. It links `HeronStackGuard.cs` **by source** rather than
referencing the add-in, because that file touches no Autodesk type - the same mocked-boundary trick
`Heron.Bridge.TestHost` uses, and it keeps **one** copy of the rewriter. Two halves written to check
each other is how [row 96](../FRAGMENT-ISSUES.md) happened.

**AND FOR FIVE DAYS NOTHING RAN IT.** The host was added to `heron_dotnet.PROJECTS` so that it
COMPILES on all eight releases - this page calls that "the 'a gate nobody runs' shape caught before
it could set" - and the half it caught was the compiling half. No suite built it and no suite
executed it, so 882 checks sat outside every count of *every test passes*, and the paragraph above
went on describing a library of 372. `tests/test_stack_guard.py` is the missing runner, written in
the same shape as `test_kernel.py`: it works out the newest framework an installed SDK can build and
an installed runtime can run, builds the host with `HeronTfm` overridden to a plain `net10.0`
(`RevitVersion=2024` maps to `net48`, which needs Mono), runs it, and **exits 3 where there is no
.NET - which is not a pass**. See [row 170](../FRAGMENT-ISSUES.md).

**None of that says the catch works.** No test here can tell you what the CLR does when the stack
actually runs out. That is **[J9](../NEEDS-CHECKING.md)**, and it needs Revit.

## The one that quietly loses your settings

`HeronConfig.Save` and `BridgeIdentity` both wrote a sibling `.tmp` and then

```csharp
if (File.Exists(target)) File.Delete(target);
File.Move(tmp, target);
```

which reads as careful and is **worse than a plain overwrite**. A plain write leaves a truncated file;
this leaves **no file**, through a window made wider by the extra syscall inside it.

**In `HeronConfig` it does not corrupt the settings, it erases them, with no message.** `Load` answers
a missing file with `Defaults` and swallows `IOException` without a word, and the next `Save` writes
those defaults back. Every step is individually reasonable. The file whose own header reads *"Owned by
you. Never overwritten by an update"* is the one that disappears.

Fixed with `HeronAtomicWrite`: `File.Replace` when the target exists, a plain rename when it does not.
**`File.Replace` and not `File.Move(tmp, target, overwrite: true)`** - the overwrite argument arrived
in .NET Core 3.0 and `Heron.Core` is built for net472 and net48 as well.

## The one that is recorded and NOT fixed

Five fragments narrow through Revit's own spatial index. **Four do not** -
`check-minimum-clearance`, `check-vertical-clearance`, `check-insulation-clearance` and
`find-nearest-elements` nest two loops with no pruning. [Row 106](../FRAGMENT-ISSUES.md).

**No claim is made that it is slow, because nothing has timed it.** `check-minimum-clearance` already
reports `pairsChecked`, so one run answers it. And the fix is not a swap - a spatial filter finds what
*intersects*, clearance is about what does *not touch yet*, so the outline has to be grown first. It
also edits fragment files, so **four fingerprints change and four proofs need re-signing.** That bill
is why it is a row rather than a commit.

## What this cost, and what it did not

| | |
|---|---|
| Proofs gone stale | **none.** Both fixes are add-in C#; the fingerprint hashes the fragment file on disk, and no fragment file was touched |
| Gates | `check-compile` (5 projects x 8 releases), `check-docs`, `check-metadata`, `check-structure`, `check-package` - all **exit 0** |
| New project | `tests/Heron.StackGuard.TestHost`, **added to `heron_dotnet.PROJECTS`** - it compiled nowhere until it was, which is the "a gate nobody runs" shape caught before it could set |
| Still owed | **[J9](../NEEDS-CHECKING.md)**, and it needs the add-in deployed |

**Worked in a separate worktree throughout.** The main tree was on another session's
`rename-phase-fragment` with two commits on it, and moving somebody else's checkout to save creating
a folder is not a trade worth making.


## J9 ran the same evening, and it PASSED

**Revit 2024, session 33812, started 20:13:52 against an add-in deployed 19:54:19.** That comparison
was made FIRST and it is the only reason the result means anything: a Revit started before the deploy
would have loaded the old add-in, died, and the guard would have been blamed for it.

A fragment recursing with no floor came back as an ordinary finding:

```
zz-j9-stack-guard-probe  [fragment_threw]
    'zz-j9-stack-guard-probe' threw while running: Insufficient stack to
    continue executing the program safely.
```

**Revit lived** - same PID, `ping` 1 ms, and a real read of 3,565 elements afterwards. The probe
fragment was deleted and the library is back to 372.

**The model was NOT blank.** `test projject.rvt`, 3,565 elements, open throughout - not the
arrangement the row asked for. It makes the result stronger and it is said out loud because a FAIL
would have taken that session with it.

**Still uncovered, deliberately:** an expression-bodied recursive lambda. Running one is expected to
end Revit, so it was not run unasked.

## Row 106 measured, and the measurement CHANGED the row

`check-minimum-clearance` was recorded as comparing every element against every element. Timed on the
same model:

| | |
|---|---|
| `pairsChecked` reported | **9,506** |
| the same figure derived independently | **9,506**, to the unit - the counter is honest |
| placed elements handed in | **3,565** |
| elements the loop actually paired | **98** - the ones with a bounding box |
| naive pairs it skipped | **12,699,719** of 12,709,225 |

**THE LOOP ALREADY PRUNES, and the row did not know that.** The n-squared is over GEOMETRY, not over
everything handed in. The concern is real and narrower than it was written.

**And this model cannot settle it.** Its largest physical category is **28 Walls**; the biggest
categories are settings - 180 Electrical Load Classification Parameter Elements, 125 Space Type
Settings, 91 Legend Components. **No ducts and no pipes at all** (the 17 `Pipe Segments` are segment
definitions). The 9,506-pair run took **1.52 s** and the 756-pair run **2.21 s** - the bigger one was
FASTER, so both are process start-up and one bridge round trip with the loop invisible inside them.

**So the row stays OPEN with what would close it: a real MEP model**, where nearly every element has
geometry and that pruning stops helping. `Snowdon Towers Sample HVAC.rvt` ships with Revit 2024 and is
already named in this library's own proofs. **Opening it was offered and declined on the day**, so the
measurement is still owed. **A fast number from a model that cannot show the effect would have been
worse than none** - it would have been quoted later to dismiss a real question.

## Three things that cost a round trip each, all of them already written down

- **A reply truncates a list to three.** The first sizing probe answered `12 item(s) [a, b, c, ...]`.
  **When the CONTENTS are the answer, return a string.**
- **`check-minimum-clearance` cannot be arranged from a selection** - *"one selection cannot say which
  is which"*, because `targets` binds from the CHAIN. **`python tools/generate-jobs.py` says exactly
  this about exactly this fragment**, and reading it first would have saved the attempt.
- **The lease refuses a second chat, and retrying renews it.** Two refusals arrived before the other
  session was stopped. The countdown moving 4 -> 3 minutes is the signal it is expiring rather than
  being renewed; the fast route is the Heron ribbon button off and on.

## Where the day ended

| | | derive it |
|---|---|---|
| Fragments | **310 `PROVEN` / 62 `DRAFT`**, 372 total | `grep -h "^heron-status:" brain/fragments/*/fragment.yaml \| sort \| uniq -c` |
| Waiting on the owner | **140** | `python tools/owner-queue.py` |
| Heron's own open defects | **30** | `python tools/open-defects.py` |
| Deployed | **2020, 2024, 2027** - each verified to carry its own framework and both fixes | |

**Two parked CI branches still need one command in an interactive terminal:**
`gh auth refresh -h github.com -s workflow`.
