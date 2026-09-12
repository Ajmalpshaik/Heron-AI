# 2026-09-06 — Revit now SHOWS what Heron is doing to it

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**The owner's words:** *"there is no visual identification showing if the cloud or the AI is talking to
Revit. I cannot understand what is happening visually from the Revit side."*

He was right, and the gap was structural. Revit showed exactly one thing about Heron — the ribbon
button's connected picture — and that says a **pipe is open**. Nothing about whether anything is
happening right now, nothing about what, nothing about how it ended. **A refusal was invisible from
Revit**: the chat got a sentence and the screen showed nothing at all.

`HERON-REVIT-UI-022` had been reserved for this since the registry was written, deferred with the note
*"waits for Step 6, when something is finally slow enough to need it."* Step 6 shipped and the executor
arrived, so it is built now — [`HeronActivityBanner.cs`](../../revit/Heron.Revit.Addin/HeronActivityBanner.cs),
raised and lowered by [`RevitDispatcher`](../../revit/Heron.Revit.Addin/RevitDispatcher.cs), decided in
[D-50](../DECISIONS.md).

**Three things decide the whole design, and the first is not obvious:**

1. **The banner goes up BEFORE the work is handed to Revit.** Revit draws on the thread it works on, so
   the moment a job starts nothing can be painted. Any design that shows a banner *because a job turned
   out to be slow* can only decide that on the very thread the job already took. There is no
   delayed-appearance option — it is raise-it-first or nothing.
2. **It comes down when Revit truly finishes, not when the caller gives up.** A client that timed out at
   `still_running` has stopped waiting; Revit has not stopped working, and the screen follows the model.
   Two threads can both think a job is theirs to end, so one interlocked flag on the job decides it.
3. **It says READING or CHANGING**, blue or amber, taken from `HeronOperationRegistry` **by operation
   name** (Golden Rule 19). "Something is happening" is worth little to somebody whose real question is
   whether his model is being touched. Nothing on the wire can make a write wear the reading colour.

It also holds the outcome for about 1.4 s afterwards with how long the job took — twelve seconds of
freeze reads as a hang, the same twelve seconds labelled **12 s** reads as a duration.

**IT COMPILES — all eight releases, 2020 through 2027, every project, 0 warnings** (`B5`, closed
2026-09-07). **It has still never appeared on a screen.** `B6`–`B13` in
[NEEDS-CHECKING.md](../NEEDS-CHECKING.md) all need Revit open, and `B8` is the one that matters most: if
a write shows the blue READING card instead of amber CHANGING, stop and fix it before using Heron on
real work.

**AND THE COMPILER WAS HERE ALL ALONG.** This was written believing the container had none, because
`dot.net`'s install script is blocked by the egress proxy. **Ubuntu packages it:**
`apt-get install dotnet-sdk-8.0 dotnet-sdk-10.0`, about a minute, and `check-compile.py` runs. The
**10.0** package carries the WindowsDesktop targets and is what builds 2025–2027; 8.0 alone stops at
2024. `pip install --break-system-packages mcp` works too, which takes the suite from 20 passing to
23 of 24. [docs/30](../30-compiling-away-from-windows.md) had already established all of this and it
was re-learned from scratch — **a blocked download is not an absent toolchain.** The one test still
failing, `test_fragment_store`, fails identically on the base commit in a clean worktree and is the
fragment library's business, not this branch's.

One new setting, `ui.activityBanner`, declared in both halves of the config and **on by default** —
the only default in that table that is. The rest protect the model by staying off; this one protects
the person by staying on.

---
