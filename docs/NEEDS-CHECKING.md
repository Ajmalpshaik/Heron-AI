# Needs checking — the register

> | | |
> |---|---|
> | **Type** | **Permanent register.** Append-only, and **never deleted** — this is where work notes empty into |
> | **For** | Anyone about to believe a green gate — **this is the list of what nothing has proved** |
> | **Authority** | A row here **outranks any claim that something works**. Green is not proven — [D-30](DECISIONS.md) |
> | **Waiting on you?** | `python tools/owner-queue.py` — **never a list typed on this page** |
> | **Adding to it** | A row names the **command**, the **machine it needs**, and what **pass looks like** — including what a FAIL would prove, which is worth the same. **Its ID is bold, and used once in the register** — this page and every group's file — look for the letters in every form, bold, backticks and plain, before a new group takes them: `python tools/needs-checking-register.py | grep` searches all of it. **A results table names the row's ID in plain text, and a row that passed is struck at both ends** — the three readers of the register count by the bold ID, so anything else is a check the owner's queue cannot see, sees twice, or shows him again after it passed. FRAGMENT-ISSUES row 5b-156 |
> | **Its numbers** | **Do not read the stated totals as today's.** Derive the Group A ids: `grep -oE '\*\*A[0-9]+\*\*|~~\*\*A[0-9]+\*\*~~' docs/needs-checking/group-a.md | grep -oE 'A[0-9]+' | sort -uV` |
> | **Where each group lives** | Since 2026-09-23 **each group is its own file**, `needs-checking/group-x.md` — [Group A's](needs-checking/group-a.md), for one — and this page keeps the rules and, where each group was, its heading and a line naming its file. **A new row goes in its group's file.** A new group is written here in full, as always, and `python tools/split-needs-checking.py --write` moves it into its own file. Every tool reads this page and its groups as ONE text, exactly as it read the single file: `python tools/needs-checking-register.py` prints it |
> | **Done checks** | A row whose ID is struck through at both ends keeps its line in the register — the struck ID, a title, the opening of its result — and its full text moves to [`needs-checking-archive/`](needs-checking-archive/README.md). **Nothing is deleted, and a struck row that says something is still owed stays in full.** `python tools/archive-needs-checking.py` says what would move |

> **Read [FOR-THE-OWNER.md](FOR-THE-OWNER.md) first if you are the owner.** It is the one page that
> says what is waiting on you, across every register, without holding a list of its own.


> ## 📌 This is a RECORD now, not a gate — 2026-08-28
>
> **The owner's instruction:** *"checking in Revit is not possible within 1 week, so keep the checking
> process as a document and start Phase 2 — we need to finish that."*
>
> So: **no Revit until roughly 2026-09-04.** Nothing below has changed status, nothing has been
> downgraded, and nothing has been quietly assumed to pass. What changed is only this — **work no longer
> waits here.** Phase 2 began on that instruction ([27 — Build Order](27-build-order.md), Steps 7
> to 14), and Steps 7 to 13 were chosen and ordered so that none of them needs a Revit.
>
> **Two things this does NOT license:**
>
> - **`write.enabled` stays `false`.** [D-19](DECISIONS.md) is untouched. No Phase 2 step asks for
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
minutes ([docs/30](30-compiling-away-from-windows.md)). Group A below records what that settled and
what it did not.

**Most of what is left needs a real Revit on a real Windows machine** — 48 of the 53 remaining items, and
every one that matters most. Compiling is not behaving: `D3` is still the line that catches a unit
error, and nothing here has moved anything yet.

**63 rows, 19 closed, 44 left** — recounted on 2026-09-06 with the pattern named below, IDs printed
and read rather than the total trusted. **Do not read those three numbers as today's.** Derive the
Group A ids instead, which is the half that keeps moving:

```bash
grep -oE '\*\*A[0-9]+\*\*|~~\*\*A[0-9]+\*\*~~' docs/needs-checking/group-a.md | grep -oE 'A[0-9]+' | sort -uV
```

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
([D-46](DECISIONS.md)). A struck row is not automatically a passed one, and a single "done" figure
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
[`HANDOVER.md`](HANDOVER.md) said *49 items* and *45 need a real Revit* from the day they were written,
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

---

## How this file works

**It is the single register.** [`HANDOVER.md`](HANDOVER.md) §6 points here rather than keeping its own
copy, because two lists of the same thing drift and this repository has been bitten by that more than
once.

**Unproven FRAGMENTS are the one thing that does not get rows here** — for that same reason, not as an
exception to it. Every fragment carries its own `heron-status` and its own proof, and
`python brain/heron_fragment.py` lists them with what each still owes. Copying that list into this file
would create exactly the second list this paragraph warns about, and it would go stale first: fragments
are added and promoted far more often than register rows change. **A fragment below `PROVEN` has never
met a model** — that is what the status means, it is machine-checked, and it needs no row here to be
true.

**Every item has an ID** — `A1`, `D3` — so it can be named in a message without describing it again.
Say *"A1 passed"* or *"D3 failed, here is what it said"* and that is enough.

**They are in dependency order.** Nothing in D can be attempted before A passes. Work down, not around.

**Each item states what PASS actually looks like**, because "it seemed to work" is how an untested claim
becomes a proven one without anything being proven.

**Delete an item only when it has passed** — not when it has been read, and not when the code looks
right. Add an item every time something is built away from Revit.

> **A failure here is expected and is not bad news.** Code that no compiler has read almost never
> builds first go. Each item names one thing to look at so a failure is a five-minute fix rather than
> an afternoon of reading. Send the exact message and it gets fixed.

---

## Group A — does it build, and does the bridge work

**Its own file:** [`needs-checking/group-a.md`](needs-checking/group-a.md)

## Group K — the agent spine, which has only ever run here

**Its own file:** [`needs-checking/group-k.md`](needs-checking/group-k.md)

## Group J — the executor's inputs (needs Revit, and something selected)

**Its own file:** [`needs-checking/group-j.md`](needs-checking/group-j.md)

## Group R — read the decisions back (no Revit needed)

**Its own file:** [`needs-checking/group-r.md`](needs-checking/group-r.md)

## Group B — does Revit still load

**Its own file:** [`needs-checking/group-b.md`](needs-checking/group-b.md)

## Group C — the gate, before anything can move

**Its own file:** [`needs-checking/group-c.md`](needs-checking/group-c.md)

## Group D — the move itself

**Its own file:** [`needs-checking/group-d.md`](needs-checking/group-d.md)

## Group E — the refusals

**Its own file:** [`needs-checking/group-e.md`](needs-checking/group-e.md)

## Group F — Steps 1-5 are no longer proven on this build

**Its own file:** [`needs-checking/group-f.md`](needs-checking/group-f.md)

## Group H — the lease (needs TWO chats and one Revit)

**Its own file:** [`needs-checking/group-h.md`](needs-checking/group-h.md)

## Group L — linked models (needs Revit and a model that links something)

**Its own file:** [`needs-checking/group-l.md`](needs-checking/group-l.md)

## Group M — phases and design options (needs Revit and a real job file)

**Its own file:** [`needs-checking/group-m.md`](needs-checking/group-m.md)

## Group N — duct and pipe systems, and what is connected to nothing (needs Revit and a real MEP model)

**Its own file:** [`needs-checking/group-n.md`](needs-checking/group-n.md)

## Group P — parameters, and the two ways the answer is confidently wrong (needs Revit and a real model)

**Its own file:** [`needs-checking/group-p.md`](needs-checking/group-p.md)

## Group S — groups and assemblies, and the edit that lands in twelve places (needs Revit)

**Its own file:** [`needs-checking/group-s.md`](needs-checking/group-s.md)

## Group G — hard to force, do last

**Its own file:** [`needs-checking/group-g.md`](needs-checking/group-g.md)

## When everything above has passed

Step 6 is finished, and not before. At that point:

1. Change the default in `HeronPermissions` **only if you want writing on by default** — and
   [D-19](DECISIONS.md) says why the answer is probably still no.
2. Delete the "never run" banners in `RevitWrite.cs` and `heron_mcp_server.py`.
3. Move the proven rows into [`HANDOVER.md`](HANDOVER.md) §3, under *proven against a real Revit*.
4. **Do not delete this file.** That instruction was written on 2026-08-28, when this was a
   123-line Step 6 checklist and deleting it was the whole plan. It is now a **permanent
   register** — one of the four places a retiring work note's knowledge goes to survive
   ([`PROJECT-MAP.md` §D](PROJECT-MAP.md), [`work-notes/README.md`](work-notes/README.md)) — and
   a register that deletes itself files the destination inside the bin. **What ends is the Step 6
   content, not the file:** once step 3 has moved those rows out, this one holds whatever is
   unproven next.

---

## Group T — what `prove-agent.py track` cannot see, found 2026-09-19

**Its own file:** [`needs-checking/group-t.md`](needs-checking/group-t.md)

## Group I — the twenty-three that were tried and never passed, sorted by WHY

**Its own file:** [`needs-checking/group-i.md`](needs-checking/group-i.md)

## Group U — a review found six things in Group T's own work, and all six held

**Its own file:** [`needs-checking/group-u.md`](needs-checking/group-u.md)

## Group V — all 66 unproven fragments, sorted by WHY, read off disk 2026-09-19

**Its own file:** [`needs-checking/group-v.md`](needs-checking/group-v.md)

## Group W — three fragments stand between the skills and three more proofs

**Its own file:** [`needs-checking/group-w.md`](needs-checking/group-w.md)

## Group X — the SKILLS, and the one thing only the PC can answer — 2026-09-19

**Its own file:** [`needs-checking/group-x.md`](needs-checking/group-x.md)

## Group Y — what tracking proved, and the two walls it hit, 2026-09-20

**Its own file:** [`needs-checking/group-y.md`](needs-checking/group-y.md)

## Group Z - the renamed ribbon and the Bridge Status window, 2026-09-20

**Its own file:** [`needs-checking/group-z.md`](needs-checking/group-z.md)

## Group AA - Stage 2 of the installer plan: can two Heron tabs live in one Revit? 2026-09-21

**Its own file:** [`needs-checking/group-aa.md`](needs-checking/group-aa.md)

## Group AB - Stages 3 and 4 of the installer: the engine, and the window nobody has seen 2026-09-21

**Its own file:** [`needs-checking/group-ab.md`](needs-checking/group-ab.md)

## Group AG - the background Model Auditor, `heron-model-auditor` ([PR #298](https://github.com/Ajmalpshaik/Heron-AI/pull/298))

**Its own file:** [`needs-checking/group-ag.md`](needs-checking/group-ag.md)

## Group AH - the MCP server's read-only door, its rules and its labels (package C2, 2026-09-23)

**Its own file:** [`needs-checking/group-ah.md`](needs-checking/group-ah.md)

## Group AI - the development hooks, on the owner's PC ([PR #308](https://github.com/Ajmalpshaik/Heron-AI/pull/308))

**Its own file:** [`needs-checking/group-ai.md`](needs-checking/group-ai.md)

## Group AJ - Revit traps seen elsewhere, written into the fragments they affect (earlier-brain plan, package C4)

**Its own file:** [`needs-checking/group-aj.md`](needs-checking/group-aj.md)

## Group AK - the cloud setup script, pasted into the environment again (2026-09-23)

**Its own file:** [`needs-checking/group-ak.md`](needs-checking/group-ak.md)

## Group AL - duct routing in three calls: draw every run, fit every joint, join (2026-09-24)

**Its own file:** [`needs-checking/group-al.md`](needs-checking/group-al.md)

## Group AM - `report-connectors` in a chat: every connector in full, its system, and what a reply did not send (2026-09-23)

**Its own file:** [`needs-checking/group-am.md`](needs-checking/group-am.md)

## Group AN - family creation, from an empty template to a flexed family (2026-09-24)

**Its own file:** [`needs-checking/group-an.md`](needs-checking/group-an.md)

## Group AO - `tag-rooms`, `place-hosted-family` version 2 and `set-room-limits`: room tags on a plan's rooms, doors into the wall found at each point, and room tops tied to a level (2026-09-24)

**Its own file:** [`needs-checking/group-ao.md`](needs-checking/group-ao.md)

## Group AP - what three downloaded skill sets raised: a pitched roof, a name two parameters share, and two claims to settle (2026-09-24)

**Its own file:** [`needs-checking/group-ap.md`](needs-checking/group-ap.md)

## Group AQ - `set-air-terminal-flow`: each air terminal's design flow, written through the parameter its duct connector names (2026-09-25)

**Its own file:** [`needs-checking/group-aq.md`](needs-checking/group-aq.md)

## 2026-09-21 — THE INSTALLER CAN NOW INSTALL EVERY REVIT IN ONE PRESS, and the owner proved it

**Not a row in this register.** Nobody wrote this down as a thing to check, which is the point: it was
found by watching the owner use the window, after `Group AB` had already passed every row it asked for.

**What he did.** Ticked Revit 2020, 2024 and 2027, ticked `Heron` + `AI Bridge connector`, pressed
Install **once**. The window reported:

> `'AI Bridge connector' installed for Revit 2020.`
> `'AI Bridge connector' installed for Revit 2024.`
> `'AI Bridge connector' installed for Revit 2027.`

**Verified by reading the deployed files, not the success line** — the habit `A12` exists to enforce.
The `TargetFrameworkAttribute` was read out of each installed `Heron.Revit.Addin.dll`:

| Revit | Runtime actually installed | Expected |
|---|---|---|
| 2020 | `.NETFramework,Version=v4.7.2` | net472 — correct |
| 2024 | `.NETFramework,Version=v4.8` | net48 — correct |
| 2027 | `.NETCoreApp,Version=v10.0` | net10.0-windows — correct |

Three releases, three different runtimes, one press. **That sequence was impossible before `#242`**,
which gave every release its own build folder. Before it, each build overwrote the last and only the
newest release could be installed — he hit that refusal **five times**.
[the last failure](proof/multi-version-install-FAIL-debug-not-release.png),
[all three in one press](proof/multi-version-install-PASS-all-three-one-press.png)

### The failure in between was Debug-versus-Release, and this register already knew

The first attempt after `#242` still refused 2020 and 2024. The cause was **not** the fix: the builds
had been made in `Debug`, and `DeployScriptDeployer` (`WindowsAdapters.cs:415`) deploys `Release` and
always has. So nine correct builds sat in a folder the installer never opens.

**`AB5` above records this exact condition** — *"the window drives the deploy script with `-c Release`
and only a Debug build existed"* — and it was read as a one-off rather than as the standing rule it is.
Rebuilding in `Release` was the whole fix; no code changed.

**2027 appeared to work during that failure, and that is worth understanding.** With no
`Release/2020/` folder to find, the script fell back to the flat layout and found one leftover build
from earlier that day. It was genuinely a .NET 10 build, so the runtime guard passed it for 2027 and
refused it for the other two. **The guard behaved correctly and nothing wrong was installed.**

### A hole this exposed — FIXED 2026-09-21, and the fix is the section below

The fallback in `tools/deploy-addin.ps1` reaches for *any* build when it cannot find one for the
release it was asked about, and the runtime guard is what is supposed to make that safe. **The guard
reads the runtime, not the year.** Revit 2021, 2022, 2023 and 2024 all build `net48`, so a stale
`net48` leftover would deploy into any of them and report success — **the same class of defect `A12`
recorded**. It did not bite here only because the leftover happened to be .NET 10.

Now that every release has its own folder, the fallback buys nothing on a current checkout and can
still mislead. **That was proposed here and has since been done** — the script refuses by name and
prints the command that fixes it. See *THE TWO DEFECTS ABOVE ARE FIXED* below.

### Stale after #242 — CORRECTED 2026-09-21

Two comments still point at `Directory.Build.targets`, which does not exist — the fix landed in
`Directory.Build.props` after the `.targets` attempt was measured as a silent no-op. In
`tools/deploy-addin.ps1` (the block above the build discovery) and `tools/check-api-surface.py`. The
same `deploy-addin.ps1` block also still claims *"THE BUILD OUTPUT IS SHARED BETWEEN ALL EIGHT
RELEASES"*, which stopped being true in the commit that rewrote the lines above it.

**What was found when this was picked up, and it is half a correction of the paragraph above.**
`deploy-addin.ps1`'s wrong pointer had **already been corrected**, by [row 5b-99](FRAGMENT-ISSUES.md),
in the very commit that wrote this paragraph — so one of the two names here was stale before anyone
read it. **`tools/check-api-surface.py` still said it**, and nothing was looking: 5b-99's check is
written as *"asked of the repository rather than pinned to a name, so it also catches the next one"*
and it asked of **one file**, so the next one was already in the tree and invisible to it. Both are
corrected now and the check covers the second file — named there rather than swept repository-wide,
because `Directory.Build.props` itself names a `.targets` file legitimately, in the paragraph recording
the measurement that ruled it out.

---

## 2026-09-21 — THE TWO DEFECTS ABOVE ARE FIXED, and neither fix has been seen by a person

The section above recorded two things found and deliberately not fixed. Both are fixed here. **Nothing
below was run on Windows and nothing below has been drawn**, so every row that needs a screen is
`NEEDS REAL REVIT` and stays that way until Ajmal says otherwise. **The word PROVEN does not appear in
this section on purpose.**

### 1. The deploy script refuses by name instead of substituting another release's build

`tools/deploy-addin.ps1` took **any** build it could find when it had none for the release it was asked
about, and left the runtime guard to decide. **That guard reads the runtime, not the year.** Revit
2021, 2022, 2023 and 2024 all build `net48`, so one stale `net48` leftover passes it for all four,
deploys, and reports success — the class of defect `A12` recorded. It did not bite on 2026-09-21 only
because the leftover happened to be .NET 10.

**The fallback is gone.** With no build for the release asked for, the script now says which release,
what it did find, and the exact `dotnet build` line. **It also tells the two reasons apart**, which the
old message could not:

| what is on disk | what it now says |
|---|---|
| a build for that release, in the **other** configuration | *"Revit 2024 has been built, but not in Release — what is on disk is in ...\Debug\2024. The installer window always deploys Release, so a Debug build is invisible to it."* |
| builds for other releases only | *"There is no Release build for Revit 2021 ... The Release builds on disk are for: 2020, 2024, 2027."* |
| nothing at all | *"... Nothing at all has been built here in Release yet."* |

**The second row of that table is trap 1 in this repository's own history** — the installer window
deploys `Release`, the dev flow builds `Debug`, and the old wording read as *"your change failed"*
rather than *"you built the other one"*. It cost a full round trip on 2026-09-21, after `AB5` had
already written the condition down.

### 2. The window greys a Revit release it has no build for, before Install is pressed

`R-10` has said since Stage 4 that a row which cannot be installed is greyed **with the reason on it**,
and `AB2` proves the window does that for products. **The release tick boxes were the one row it had
never been applied to**: every Revit on the PC could be ticked whether or not anything had been built
for it, and the only way to find out was to press Install and read the refusal. **Ajmal hit that five
times in one evening.**

**What the window can cheaply know, and it is not a build.** `BuildsOnDisk` reads the directory the
deploy script reads and asks whether the built file is already there. **It starts no process, builds
nothing, and reads one directory per product, cached while the window is open.**

**It looks exactly where `tools/deploy-addin.ps1` looks, and that is the load-bearing part.** A window
looking somewhere else would grey a release the script would have installed happily — worse than the
defect it fixes, because it refuses work that was possible. The configuration is taken **from the
deployer** rather than written out a second time, which is the same word that cost the round trip
above. `tests/test_deploy_script.py` holds the two searches together.

**The rule, and the two cases it deliberately does not cover:**

- A release is greyed when **every** product that could go into it has no build. One product with a
  build keeps it tickable — that install really would work, and the rows below say what else would not.
- A release **no product supports** is left alone. It cannot be installed either, but *"nothing has
  been built"* is not the true reason and the product rows already carry the right one. Same ruling as
  [row 5b-80](FRAGMENT-ISSUES.md).
- **Not knowing leaves every release tickable.** Greying one that is installable costs a modeller an
  install they were entitled to, with a sentence telling them to build something already built.
  Leaving one tickable costs a refusal after the press — exactly where this stood before, so it is no
  worse than nothing.

**The one case this does NOT close, said plainly.** A release stays tickable when *one* product has a
build and *another* does not, and the second will still fail after the press. **It cannot happen
today** — `heron-bridge` is the only `SHIPPED` product, so there is never a second one to disagree —
and it is written here rather than quietly left, because the day a second product ships is the day this
rule needs looking at again.

### What was actually run, and what it said

| | |
|---|---|
| **PASS** | The **ten gates** `.github/workflows/gates.yml` decides on — `check-docs`, `check-metadata`, `check-structure`, `check-signatures`, `check-licence`, `check-narrow-errors`, `check-package`, `check-products`, `check-routing`, `check-intrusion`. All exit 0 |
| **PASS** | `tests/test_installer_engine.py` — the screen model and `BuildsOnDisk` against a fake Revit and a **real temporary folder**. The release greying, the command in the sentence, the configuration, and the whole-folder match all check out |
| **PASS** | `tests/test_deploy_script.py` and `tests/test_installer_window.py` |
| **PASS** | `tools/check-compile.py` — **all 13 projects on all eight releases, 2020 to 2027, 0 warnings**, the window included |
| **PASS** | **Every suite: 219 ran, 216 passed, 0 failed, 0 hung.** The 3 waiting are exactly the three `gates.yml` names as not runnable on a plain runner, and that list is unchanged |
| **PASS** | **And CI agreed, on a clean runner** — [#251](https://github.com/Ajmalpshaik/Heron-AI/pull/251), head `ca0e13a`, 2026-09-21: all five jobs green, no merge conflict, no review thread. That is the same ten gates, the same eight-release compile and the same sweep, run somewhere that has never had this container's `dotnet` installed by hand |
| **PASS** | **The checks were seen to FAIL, which is what makes them checks** — [heron-ship §2a](../.claude/skills/heron-ship/SKILL.md). Counted below |
| **NOT RUN** | **No window has been drawn and no PowerShell has executed.** `BuildsOnDisk` is the only one of the four Windows adapters that runs here at all, and what it proves is its path arithmetic, not that the folder it names is the one MSBuild wrote to on Ajmal's PC |
| **NEEDS REAL REVIT** | `AA11`, `AA12`, `AB9` and `AB10` below. **Screenshots are owed for `AB9` and `AB10` and cannot be taken here** — this is Linux and the window is WPF |

**Seen to fail, against the code as it stood:**

| what was put back or broken | checks that went red |
|---|---|
| `tools/deploy-addin.ps1`, `check-api-surface.py` and the four C# files reverted to `HEAD` | **18** — 12 in `test_deploy_script.py`, 6 in `test_installer_window.py` |
| the release greying deleted, types left in place | **5** in the test host |
| `BuildsOnDisk` made to ignore the configuration | **2** |
| the release matched as a substring instead of a whole folder | **1** |
| the `CanBeTicked` guard deleted from `ChosenReleases()` | **1** |

**Two of those found a check that was not checking, which is the point of running them.**

- *"prints the exact command that makes that build"* was **green against the broken script**, because
  `$rebuildLine` further down has carried that command since before the fallback existed. Narrowed to
  the refusal's own text; it now goes red.
- *"a greyed release installs nothing even if its tick arrives set"* was **green with the guard
  deleted**, because a greyed release also starts unticked, so `Chosen` alone dropped it. The check now
  puts the tick back on by hand first — which is what a window drawn before this rule would send.

**And a third, found while reading rather than by running.** `tests/test_installer_window.py` anchored
its `AB1` no-clipping checks on the first `var tick = new CheckBox` **in the file**, which is the
*release* box, not the product one — so the block it read spanned both and it passed either way. It is
narrowed to `ProductList()`. **Nothing in that section was relaxed**; it asks for strictly more than it
did.

### Rows for Ajmal's PC — four, and two of them need the window open

| ID | Do this | Pass looks like |
|---|---|---|
| **AA11** | **Close Revit.** Delete `revit\Heron.Revit.Addin\bin\x64\Release\2021\` if it is there. Then `.\tools\deploy-addin.ps1 -RevitVersion 2021 -Product heron-bridge -Configuration Release` | It **refuses, naming Revit 2021**, says which releases *are* built in Release, and prints the `dotnet build ... -p:RevitVersion=2021` line. **Nothing is copied.** Check `%APPDATA%\Autodesk\Revit\Addins\2021\` afterwards: unchanged. Before this fix it would have taken a 2024 build — same `net48`, same guard, reported success |
| **AA12** | Build **one** release in `Debug` only (`dotnet build revit\Heron.Revit.Addin\Heron.Revit.Addin.csproj -c Debug -p:RevitVersion=2020`), delete that release's `Release` folder, and deploy it with `-Configuration Release` | It says the release **has been built but not in Release**, and names the Debug folder it found. This is the sentence that would have saved the round trip on 2026-09-21 |
| **AB9** | **Close Revit.** Delete the `Release` build folder for **one** of the releases on the PC, keep the others, and open `HeronInstaller.exe` | That release's tick box is **greyed and unticked**, and the sentence under the release row says *"Nothing has been built for Revit NNNN on this PC..."* **with the `dotnet build` command on the line below it**. The others are still ticked. **Screenshot** — `docs/proof/AB9-*.png`. The failure to watch for: the sentence landing beside the wrong release, or the window taking noticeably longer to open |
| **AB10** | With all three releases built in `Release`, open the window again | **Every release is tickable and no sentence appears.** This is the half that matters more than `AB9`: a window that greys a release it could have installed is worse than the defect being fixed. **Screenshot** — `docs/proof/AB10-*.png` |

**`AB9` and `AB10` are a pair and both must be seen.** One of them says the grey appears; the other
says it does not appear when it should not. Either alone is half an answer.

### A tooltip on a greyed row does not show, and the PRODUCT rows have the same latent hole

WPF's `ToolTipService.ShowOnDisabled` is **false by default**, so a `ToolTip` set on a disabled control
never appears. The release boxes added here set it to true; **the product rows, which have carried a
tooltip since Stage 4, do not.**

**`AB2` still passed, and correctly** — what it asks for is the sentence **printed under the row**, and
that is there and is what a person reads. `R-10` is met either way. But the window's own comment says
*"THE REASON IS ON THE ROW"* next to a tooltip that cannot be seen on the one kind of row it is for, so
half of that line has never done anything.

**READ FROM THE DOCUMENTATION, NOT FROM A WINDOW.** Nothing here has drawn one. **Worth one hover on
Ajmal's PC** while `AB9` is being looked at: hover a greyed **product** row and a greyed **release**
box, and see whether only the second shows a tooltip. Not fixed for the product rows, because that is
a change to a row `AB2` has already passed and it is his call whether to make it.

### A THIRD stale claim, FOUND AND NOT TOUCHED — waiting on Ajmal

[`docs/07-installation-and-update.md` §10.1](07-installation-and-update.md) says, of the 2026-09-20
run: *"Each release was built **separately** and deployed before the next was built, **because the
build output folder is shared and the newest build wins the search**."* **The reason is in the present
tense and stopped being true at [#242](https://github.com/Ajmalpshaik/Heron-AI/pull/242)** — the same
sentence, in the same words, as the one corrected in `deploy-addin.ps1` here.

**It was deliberately not changed.** That section is a dated record of a run, and the block three
paragraphs below it says in as many words *"The paragraph above stays as written, because it is the
record of the gap being spotted a day before it bit."* Editing a run record to match today is a
different decision from correcting a live comment, and it is Ajmal's to make. **The smallest fix would
be one clause** — *"because the build output folder **was then** shared"* — which keeps the record
true as a record and stops it reading as a rule that still applies.

---

## 2026-09-21 — STAGE 5 IS BUILT, and nothing has ever been downloaded from GitHub

**Its own file:** [`needs-checking/group-ac.md`](needs-checking/group-ac.md)

## 2026-09-22 — STAGE 7: UNINSTALL IS THE SAME WINDOW, and the dangerous part was not the deleting

**Its own file:** [`needs-checking/group-ad.md`](needs-checking/group-ad.md)

## 2026-09-22 — STAGE 6's SECURITY GATE IS BUILT, and the door it guards is not

Stage 6 item 3 says *"read `Q-PE-10` before building this one — whether route 1 may act on any
repository, or only Heron's own signed release, is unresolved and it is a security question."*
**`Q-PE-10` was answered on 2026-09-21**: Heron's own signed release, never an arbitrary repository.

So the gate was built first, because it is the only part of the stage that is a security rule.

### What `InstallSource` refuses, and why each one is a real trick

| refused | why it matters |
|---|---|
| `https://github.com/someone-else/repo/releases/latest` | somebody else's code, running inside Revit with live models open |
| `https://evil-github.com/...` | **ends with** nothing useful — but a `Contains` check passes it |
| `https://github.com.evil.example/...` | **ends with** `evil.example`; a sloppy `EndsWith` on the wrong side passes it |
| `https://github.com/owner-a-evil/...` | **starts with** the real owner — a `StartsWith` check passes it |
| `https://github.com@evil.example/...` | reads as GitHub to a person, resolves to `evil.example` |
| `https://evil@github.com/...` | the host really **is** GitHub — only the user-info guard catches this |
| `http://` and `ftp://` | what arrives is whatever the network sent, and so is the checksum |
| `https://github.com:8443/...` | GitHub's releases never name a port |
| `https://github.com/owner-a/repo-a` | the **repository**, not a release — source code is whatever a branch says today |
| `install this repo` | not an address at all |

**Every refusal names what would be accepted.** A refusal that only says no leaves somebody guessing,
and the guess they make is usually to try harder rather than to try the right thing.

**It opens no socket.** `R-50` — *the AI never reads a repository's contents to decide what to
install* — is true by construction here rather than by discipline: `Judge` takes a string and the
product list, and could not fetch anything if it wanted to.

**Who Heron is comes from the manifest**, the `source` block added for Stage 5. A repository renamed is
a line in a file, not a rebuild.

### What was actually run, and what it said

| | |
|---|---|
| **PASS** | Four accepted shapes, **eighteen refused**, each refusal naming Heron's own release and never the word "error" |
| **PASS** | A local folder and a network share are told apart from a hostile address, and point at the other door |
| **PASS** | A manifest that does not know its own source refuses **everything** rather than guessing a repository name |
| **PASS** | Ten gates, 13 projects on 8 releases, every installer suite |
| **NOT STARTED** | **The door.** Routes 1 and 2 are the AI installing, and there is nothing for the AI to call — [Q-PE-16](work-notes/plans/plugin-extension/03-open-questions.md) |
| **NEEDS REAL REVIT** | Nothing yet. There is no row to give, because there is no route to run |

**Seen to fail — one break per guard, each letting exactly one attack through:**
`EndsWith` host → `evil-github.com` · `Contains` host → `github.com.evil.example` · `StartsWith`
owner → `owner-a-evil` · `http` allowed → `http://` and `ftp://` · user-info guard removed →
`https://evil@github.com/...` · `releases` not required → the repository itself.

### TWO THINGS THE BREAKS FOUND, and both were mine

**A guard nobody could tell was gone.** Deleting the user-info check broke **nothing**: every hostile
address tried also had a hostile host, which the host check caught. That made it unfalsifiable — code
that looks like a safeguard and is provably nothing. The case only it catches, where the host really
**is** `github.com`, was added; then it went red. **A security guard that cannot be shown to fire is
not a guard, it is a comment.**

**And a refusal that was true and useless.** A Windows path is a **valid absolute URI** —
`Uri.TryCreate` turns `D:\Heron-AI` into a `file:` address — so somebody pointing at their own clone was
told *"Heron will only fetch over https, and that address is file"*. Found by the check that asks for
the other door to be named, not by reading.

### The door, and the option to avoid

[Q-PE-16](work-notes/plans/plugin-extension/03-open-questions.md) holds four ways to give the AI
something to call. **The one to avoid is driving `deploy-addin.ps1` directly** — it works today, it
looks like progress, and it skips `InstallPlan` and `InstallEngine` entirely. **Stage 6's own opening
line calls that failure:** *"if this stage ends up re-implementing any install rule, it has failed,
however well it works."*

**Route 2 needs `Q-PE-12` as well as `Q-PE-16`.** Route 1 needs only the door.

---

## 2026-09-22 — THE DOOR EXISTS NOW: `heron-install`

**Its own file:** [`needs-checking/group-ae.md`](needs-checking/group-ae.md)

## 2026-09-22 — ONE DOWNLOAD, BOTH DOORS, AND AN UPDATE CHECK THAT ONLY CHECKS

**Its own file:** [`needs-checking/group-af.md`](needs-checking/group-af.md)
