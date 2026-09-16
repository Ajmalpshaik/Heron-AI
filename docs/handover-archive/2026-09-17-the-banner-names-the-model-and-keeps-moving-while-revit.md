# The banner names the model, and keeps moving while Revit cannot

**2026-09-17.** Committed as `37cb6e2`, on `chain-provenance-design`, riding
[PR #165](https://github.com/Ajmalpshaik/Heron-AI/pull/165). Three files,
651 insertions.

It started as "show which model" and turned into finding that the banner had never animated
during a job and could not have.

## What was asked, and what it turned into

The owner asked for the activity banner on its own, to look at. Then: *"this talking to Revit
it's showing, but which project or file it's not showing — is it possible?"*

That is one afternoon's work. The rest of the session came out of one follow-up question.

## Naming the model

The awkward part is not the drawing, it is **where the name comes from**. `Begin` runs on a bridge
listener thread, where the Revit API may not be touched — so at the exact moment the name is
needed, it cannot be asked for.

So it is cached, and **what is cached is a string, never a `Document`.** A held `Document` goes
stale the instant the model closes and is the thing the conventions forbid outright; a name is
inert, and the worst it can be is out of date. Revit pushes every change in through `ViewActivated`
and `DocumentClosing`, and every finished job overwrites it with the model the work actually
reported.

That second source was **free**. `Execute` already pulled `document` out of the response for the
audit line — it had been reading the authoritative name all along, one line below where it was
needed. It is the only place in the whole path with both a valid API context and a finished job.

So: the announced name is the **last one seen**, the finished name is **authoritative**, and if they
disagree the second one wins. That is the honest arrangement rather than a clever one.

## Two lines, then 560 px, then three lines, then back to 460

The name went on the second line beside the job. That looked fine until a real project name was
used — `TRG-XX-ZZ-M3-M-0001-MEP-Model` is twenty-nine characters — and the line trimmed:
`Writing is switched off` became `Writing is switched...`, losing the one word that mattered. The
card went to 560 px wide to fit both.

Then the owner asked for three lines: what is happening, which model, everything else. That was
the better answer, and it made the extra width pointless — so the card went **back to 460** and
grew to 98 px tall instead. Widening and heightening for the same problem would have been paying
twice.

When no name is known the middle line **collapses** rather than sitting blank, and the other two
re-centre. An empty row in the middle of a card reads as something that failed to load.

## The question that changed the session

*"all of them it's not showing the time — 340, 3s?"*

The time only appeared on the finished card. The reason turned out to be worth more than the
question: **a timer on Revit's thread cannot fire while Revit is working**, because that is the
thread the job has taken.

Measured rather than reasoned about, with the thread blocked for 3000 ms the way `Execute` blocks
it:

```
timer ticks : 0
sweep       : 357.6 px before, 357.6 px after — did not move
```

The second line is the one that mattered. **The sweep bar had never animated during a job.** Every
result in `NEEDS-CHECKING`'s banner section is still true — it does not strobe, it holds, it cannot
eat a click — but the card was a **still picture** for exactly the stretch it exists to cover, and
nothing had caught it because no job run against it was ever slow enough to freeze anything.

## Its own thread

The fix is the only one available: the banner stops living on Revit's thread and starts its own
STA background thread with its own dispatcher. Everything else follows — `OnUi` already posted
across a thread boundary, it just posts across a different one now.

Proved by rebuilding the topology rather than approximating it: a stand-in main window on the main
thread so `Process.MainWindowHandle` resolves to it and the banner takes it as its **owner**, that
thread blocked, and the banner then interrogated **from a third thread** through its own
dispatcher. If it were still on the blocked thread every one of those calls would have hung.

```
sweep moved 5 of 5 samples
time ticked 5 of 5 — 1.1s → 1.5s → 1.9s → 2.4s → 2.8s → 3.2s
banner thread alive after Shutdown: False
```

Background thread, deliberately: it must never be the reason Revit's process refuses to close.

**A bug this immediately created.** The live counter starts when the banner goes up; the
operation's clock starts inside `Execute`. Revit being busy can put ten seconds between them, and
the first run showed **3.6 s** during the work then settled to **3.0 s** — a number going backwards,
which reads as a fault rather than an answer. The finished card now uses the banner's own clock.
The operation's real duration still goes to the audit, where precision is the point.

## Handing the look to another session

The visual work was given to a separate session as a self-contained package: the file, a harness
that renders every state to PNG, a second project that compiles it for all four Revit runtimes, and
a prompt carrying twelve rules with the reason for each.

**That was only possible because the file has no Revit dependency.** It compiles and runs with no
Revit installed, which gives an edit-run-look loop in seconds instead of a Revit restart per
attempt. Whatever else changes, keep that property.

What came back was good: shared brushes instead of a `new SolidColorBrush` per paint, a gradient
sweep with soft edges, a 200 ms slide-and-fade entrance, a breathing lamp, and tabular figures so
the ticking number does not jitter as digit widths change. Nobody asked for that last one.

The part worth recording is that they read **rule 12 properly**. Colour transitions animate over
250 ms — except between blue and amber, which **snap**. Read-versus-change is a safety signal, and
they understood an animation must never pass through a state where those two look alike.

## The defect found in review, and the diagnosis that was wrong first

The lamp pulse was restarted on every `Paint`.

The first measurement was a batch of `Begin`/`End` pairs, and it looked damning — the lamp cycling
`0.40 0.43 0.51 0.62` over and over, never past 0.62, against a full 0.40–1.00 sweep when left
alone. **That diagnosis was wrong.** In a batch the count genuinely reaches zero between jobs, so
the outcome *is* shown and the lamp is *meant* to go solid and restart. That is the state machine
working.

The real case is jobs that **overlap** — the count never reaches zero, `Paint` is reached again,
and a repeating animation restarted returns to its first frame:

```
as returned : 0.88 0.97 1.00 | 0.40 0.44 0.53 0.65 | 0.41 0.47 0.55 0.67 | 0.42 0.49 0.60 …
fixed       : 0.88 0.95 1.00 0.99 0.92 0.81 0.69 0.58 0.48 0.41 0.40 0.45 0.54 0.66 0.77 …
```

Overlap is not hypothetical — D-56's own test ran twelve jobs from four listener threads. The pulse
now starts once and is only restarted by the thing that genuinely stops it.

**The lesson is the measurement, not the fix.** A real number was read correctly and attributed to
the wrong cause, and it took a second experiment designed to separate the two to tell them apart.
One measurement that agrees with a hypothesis is not the same as a measurement that distinguishes
it from the alternative.

## Where it got to

Deployed to Revit **2020, 2024 and 2027** — all three installed on the owner's machine — and
verified by reading the literals back out of each deployed DLL. Note that the strings are UTF-16 in
.NET metadata; an ASCII `grep` reports them missing and that is the grep being wrong, not the
deploy.

An open Revit blocks **only its own release**, so 2020 and 2027 went in while 2024 was open. The
guard that actually caught a bad attempt was not that one: it was the runtime check noticing that
`bin` still held the .NET 10 build when 2024 wanted .NET Framework. **Build and deploy as a pair,
one release at a time** — the output folder is shared.

**Seen in Revit 2024 by the owner the same day**, during unrelated work. Revit accepted a second UI
thread, which was the real risk. It was not inspected further; `B14`–`B17` in `NEEDS-CHECKING` are
what is left.

## One thing left undone

The harness that proved all of this lives in a session temp folder and will be deleted. It builds
the real file for every Revit runtime and renders every state to PNG without Revit running, and it
would earn its place for any future UI change. Nobody has decided whether it belongs in `tools/`.
