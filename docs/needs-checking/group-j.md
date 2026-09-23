# Needs checking — Group J

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group J — the executor's inputs (needs Revit, and something selected)

**SIX OF THE EIGHT ARE DONE, 2026-09-07, and they were run within the hour of the group being written.**
The executor could supply three names — `doc`, `uidoc`, `app` — so the 328 fragments declaring anything
else compiled green against a scope the machine could not reproduce. The host now binds every declared
need from the fragment's own contract, which is what the compile gate has always done with its method
parameters.

**The run:** Revit 2024, session 20704, `Project1 work_ajmal.al` (3,435 elements, unsaved), active view
`1 - Mech`, five ducts selected. Deployed from `main` at `9dfdf07`. **`J7` and `J8` were NOT run and are
still open** — see their rows.

**This is the first time in this project's life that a fragment needing an input has run at all.**

Each row here needs Revit open **and something selected**, which no earlier group has required.

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**J1**~~ | Select a few ducts, then `prove count-elements` — [full row](../needs-checking-archive/group-j.md#row-j1) | **DONE 2026-09-07.** |
| ~~**J2**~~ | Select nothing, then run J1 again — [full row](../needs-checking-archive/group-j.md#row-j2) | **DONE 2026-09-07 — the row that mattered most, and it held.** |
| ~~**J3**~~ | `prove <a filter> count-elements` — [full row](../needs-checking-archive/group-j.md#row-j3) | **DONE 2026-09-07, with `list-levels` rather than `filter-elements-by-category`** |
| ~~**J4**~~ | `prove unjoin-geometry` with a selection — [full row](../needs-checking-archive/group-j.md#row-j4) | **DONE 2026-09-07.** |
| ~~**J5**~~ | `prove` a fragment needing a category or a name — [full row](../needs-checking-archive/group-j.md#row-j5) | **DONE 2026-09-07.** |
| ~~**J6**~~ | Run a batch, then run another, and check it does not inherit — [full row](../needs-checking-archive/group-j.md#row-j6) | **DONE 2026-09-07, and run the sharper way round.** |
| **J7** | With two models open, select in one and `prove --in "<the other>" count-elements` | **NOT RUN — only one model was open.** It **must refuse** rather than bind a selection belonging to a different document. This is the case `READ_SELECTION`'s own proof flagged as unresolved: aimed off-screen it returns a bare `0`, and *"whether the selection was genuinely cleared, or whether a UIDocument built for an off-screen document cannot see a selection at all, was NOT established"*. The binding refuses instead of resolving it, and **that refusal is still unproven** |
| **J8** | Break a fragment's C# deliberately, with a bound need, and run it | **NOT RUN.** It would mean damaging a library fragment to see the message, and no fragment happened to fail on its own. The compile error is written to say how many **generated lines** sit in front of the snippet; **that wording has never been seen** |
| ~~**J9**~~ | ~~Write a fragment whose lambda calls itself with no floor - `Func<int,int> f = null; f = n => { return f(n + 1); };` - and run it through `run_fragment_read`~~ | **RUN 2026-09-16 - PASSED.** Revit 2024, session 33812, started 20:13:52 against an add-in deployed 19:54:19, so the guard was in the loaded build rather than only on disk. Verbatim: *`[fragment_threw]` 'zz-j9-stack-guard-probe' threw while running: **Insufficient stack to continue executing the program safely.**'* - which is `InsufficientExecutionStackException`, the catchable one `EnsureSufficientExecutionStack` raises, arriving through `RunScript`'s ordinary catch. **Revit survived: same PID before and after, `ping` 1 ms, and a real read returned 3,565 elements.** The probe fragment was deleted straight after and the library is back to 372. **THE MODEL WAS NOT BLANK** - `test projject.rvt`, 3,565 elements, open throughout. That was not the arrangement asked for and it makes the result stronger rather than weaker, but it is said out loud because a FAIL would have taken that session with it. **WHAT IT STILL DOES NOT COVER:** an EXPRESSION-bodied recursive lambda, which `HeronStackGuard` leaves alone on purpose ([row 104](../FRAGMENT-ISSUES.md)). That case is unguarded and unrun - running it is expected to end Revit, which is why it was not run unasked |

**None of this proves any fragment does the right thing**, and the six ticks above must not be read as if
it did. It proves the inputs arrive, or are refused with a reason. [D-30](../DECISIONS.md) still wants a
proof with a negative case, per fragment, and that is 335 rows this group does not touch.

**`COUNT_ELEMENTS` in particular is still `DRAFT`.** It returned 5 for 5 and 2 for 2, which is evidence
and not a proof: no second route reached the same number, and the negative case above is the HOST
refusing before the fragment ran rather than the fragment handling an empty set. Whoever proves it
properly can use these numbers; they are not the proof.
