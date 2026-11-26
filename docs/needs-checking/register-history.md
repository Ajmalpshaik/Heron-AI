# The register's own history - why this page does not quote its totals

> Moved here from the top of [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) on 2026-11-24, **word for word**: only its
> relative links were re-pointed one folder deeper. It is a record of two things - the owner's 2026-08-28
> instruction that the register stopped being a gate, and the times a total typed in this page's prose outlived
> the rows beneath it. **It is not a list of what is waiting and it holds no row.** Where it says *below*, *above* or
> *this page*, it means the register page it came from; where it says *Group A*, the file is
> [`group-a.md`](group-a.md). What is open is derived: `python tools/owner-queue.py`.

> ## 📌 This is a RECORD now, not a gate — 2026-08-28
>
> **The owner's instruction:** *"checking in Revit is not possible within 1 week, so keep the checking
> process as a document and start Phase 2 — we need to finish that."*
>
> So: **no Revit until roughly 2026-09-04.** Nothing below has changed status, nothing has been
> downgraded, and nothing has been quietly assumed to pass. What changed is only this — **work no longer
> waits here.** Phase 2 began on that instruction ([27 — Build Order](../27-build-order.md), Steps 7
> to 14), and Steps 7 to 13 were chosen and ordered so that none of them needs a Revit.
>
> **Two things this does NOT license:**
>
> - **`write.enabled` stays `false`.** [D-19](../DECISIONS.md) is untouched. No Phase 2 step asks for
>   it, and a step that started asking would be the wrong step.
> - **A parked item is still unproven.** The single failure this file exists to prevent is an untested
>   claim ageing into a believed one. A week of not looking at it does not make `D3` any more true.
>
> **`R1` was meant to happen first** — read the day's decisions back before Phase 2 work begins — and it
> has not. That was the owner's own instruction and this one supersedes it, which is his to do. It stays
> below as a review to do at the PC. The reason it was wanted has not gone away: **two decisions were
> reversed within hours of being recorded**, and Phase 2 is being built on five of that set.

**Almost everything in this file is UNPROVEN**, and what changed on 2026-08-28 is worth stating
precisely rather than generally.

It was written on a machine with no Revit, no Windows and no .NET SDK. Two of those three turned out
to be avoidable: **the C# now compiles, and the bridge now runs** — on Linux, from NuGet, in about five
minutes ([docs/30](../30-compiling-away-from-windows.md)). Group A below records what that settled and
what it did not.

**Most of what is left needs a real Revit on a real Windows machine** — 48 of the 53 remaining items, and
every one that matters most. Compiling is not behaving: `D3` is still the line that catches a unit
error, and nothing here has moved anything yet.

**63 rows, 19 closed, 44 left** — recounted on 2026-09-06 with the pattern named below, IDs printed
and read rather than the total trusted. **Do not read those three numbers as today's.** Derive the
Group A ids instead, which is the half that keeps moving:

```bash
grep -oE '\*\*A[0-9]+[a-z]?\*\*|~~\*\*A[0-9]+[a-z]?\*\*~~' docs/needs-checking/group-a.md | grep -oE 'A[0-9]+[a-z]?' | sort -uV
```

*2026-10-18: the pattern above read `A[0-9]+` until today and printed no `A16b` - the row ID with a letter on
the end, which is the `R1b` drift recorded further down this page. `[a-z]?` is what lets it see one.*

**This sentence said "Two rows have been added since that recount: `A10` and `A11`" until 2026-09-12,
and by then it was SIX** — `A12` and `A13` (the deploy and the upgrade), `A14` (the Windows suite
total) and `A15` (whether a confidence floor can be derived at all, carried in from a retired work
note). **Each was added by a session that read this paragraph, believed the "two", and did not notice
it was describing a moment that had passed.** The last of them was added and the paragraph left
untouched in the same change, which is the failure happening while its own warning was on screen.

The totals are still **left as recounted rather than adjusted by hand** — that part was right, and a
total nobody recounted is exactly what this paragraph has now been wrong about three times. What was
wrong was pairing an honest un-recounted total with a *typed list* that goes stale silently. **A list
is a count with extra steps.** **And 19 closed does not mean 19 proved:** 15 passed
(`A1`, `A2`, `A3`, `A4`, `A5`, `A6`, `A7`, `A8`, `A9`, `R1`, `R2`, `B2`, `B2a`, `B2b`, `B3`) and **4 are
RETIRED unproven** —
`C1`, `C2`, `C4` and `C6` cannot be run at all since the Emergency Stop button was removed
([D-46](../DECISIONS.md)). A struck row is not automatically a passed one, and a single "done" figure
cannot say which.

Of the 44 left: **43 need Revit**, and **1 needs a screen in front of him** (`R1b` — seeing the trust
model work). **Group A was finished on 2026-09-06**, all nine rows — and reopened on 2026-09-11 by `A10`, which needs `huggingface.co` reachable rather than a Windows machine, and by `A11`, whose compile half closed the same day and whose remaining half needs a model open in Revit.

**Everything that remains needs Revit, except one conversation.** That is a different shape of backlog from
the one this file has carried since it was written, and it is worth saying once: no row is now waiting on a
compiler, a network, a Windows box or a host.

> **AND THAT SENTENCE IS FALSE AGAIN — 2026-09-14.** It was already false when `A10` and `A15` were
> added (both need a **network**, neither needs Revit), and **Group K** now makes it false three more
> times: `K1` and `K2` need a **Windows box**, `K3` needs a **network**, and only `K4` needs Revit.
> The sentence is left standing above rather than quietly corrected, because this is the **seventh**
> time this file has recorded a paragraph outliving the rows beneath it, and a correction that removes
> the evidence removes the lesson with it. **Derive the IDs. Do not read the prose for a count.**

**Nothing is waiting on a compiler, on Windows, or on a pipe any more.** `A4`, `A6` and `A9` all closed on
2026-09-06 on the owner's own PC, which carries .NET 9 and 10 and Revit 2020, 2024 and 2027 — three rows
that had been parked for a machine, closed in an afternoon once somebody was sitting at one. **That is the
fifth time something filed under *needs a machine* turned out to be waiting on somebody trying it**, and
this time the machine really was the blocker for all three. The distinction is worth keeping: these were
correctly parked. `A8` was not.

> **This paragraph has now drifted FIVE times, and the fifth is the most instructive.** It said
> *60 items, 6 done, 54 left* against **63 rows and 15 struck** — wrong in both directions at once.
> Rows were added by another session while `B2`, `B2a`, `B2b` and `B3` were struck in Revit and
> `C1`/`C2`/`C4`/`C6` were retired, and **no single edit was careless**: each hand updated the rows it
> touched and left the summary to somebody else. That is what makes this failure recurrent rather than
> sloppy. The previous note blamed one invisible row (`A9`); the real cause is that **a prose total is a
> cache with no invalidation**, and this repository has never once caught its own drift by reading the
> sentence — only by running the count.
>
> **`A9` closed later the same day** — re-run at **343** fragments on the owner's own PC, green on all
> eight releases. It had been left open after a green run at 329 because the library kept growing, which
> was right at the time and stopped being right the moment the machine that can re-run it in minutes
> became the one in front of him. **A row whose check takes two minutes on the available hardware is a
> command, not a row.**
`R1` and `R2` were closed on 2026-08-29; `E10` was added the same day, when studying the owner's
earlier library found a case Heron's write path did not handle.

**Count the rows before quoting a number here, and check the pattern you count with.** This file and
[`HANDOVER.md`](../HANDOVER.md) said *49 items* and *45 need a real Revit* from the day they were written,
while the rows summed to 51 and 47 even then — rows were added and the sentences were not. Then group `R`
was added and a count matching `[A-H]` silently skipped it and reported 52. **A count that quietly omits a
whole group is worse than no count**, which is the same lesson as a grep that finds nothing: prove the
pattern can see what you know is there.

**It happened a third time, and the same way.** The line above read *54 items, 3 done* until 2026-08-28,
against **55** rows — a pattern matching `[A-Z][0-9]+` cannot see `R1b`, the one ID with a letter on the
end, and `R1b` is also why *"2 need only a conversation"* was three. The pattern that gets this right is
`^\| (~~)?\*\*[A-Z][0-9]+[a-z]?\*\*`. The discipline that gets it right whatever the pattern is to
**print the IDs it matched and read them** rather than trusting the total — all three drifts recorded in
this section were visible in the list and invisible in the number.
