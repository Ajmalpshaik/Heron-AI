# Needs checking — the register

> ## 📌 This is a RECORD now, not a gate — 2026-08-28
>
> **The owner's instruction:** *"checking in Revit is not possible within 1 week, so keep the checking
> process as a document and start Phase 2 — we need to finish that."*
>
> So: **no Revit until roughly 2026-09-04.** Nothing below has changed status, nothing has been
> downgraded, and nothing has been quietly assumed to pass. What changed is only this — **work no longer
> waits here.** Phase 2 began on that instruction ([27 — Build Order](docs/27-build-order.md), Steps 7
> to 14), and Steps 7 to 13 were chosen and ordered so that none of them needs a Revit.
>
> **Two things this does NOT license:**
>
> - **`write.enabled` stays `false`.** [D-19](docs/DECISIONS.md) is untouched. No Phase 2 step asks for
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
minutes ([docs/30](docs/30-compiling-away-from-windows.md)). Group A below records what that settled and
what it did not.

**Most of what is left needs a real Revit on a real Windows machine** — 48 of the 53 remaining items, and
every one that matters most. Compiling is not behaving: `D3` is still the line that catches a unit
error, and nothing here has moved anything yet.

**63 rows, 18 closed, 45 left** — recounted on 2026-09-06 with the pattern named below, IDs printed
and read rather than the total trusted. **And 18 closed does not mean 18 proved:** 14 passed
(`A1`, `A2`, `A3`, `A4`, `A5`, `A6`, `A7`, `A9`, `R1`, `R2`, `B2`, `B2a`, `B2b`, `B3`) and **4 are RETIRED
unproven** —
`C1`, `C2`, `C4` and `C6` cannot be run at all since the Emergency Stop button was removed
([D-46](docs/DECISIONS.md)). A struck row is not automatically a passed one, and a single "done" figure
cannot say which.

Of the 45 left: **43 need Revit**, **1 needs a host on the PC** (`A8`), and **1 needs a screen in front
of him** (`R1b` — seeing the trust model work). **Group A is finished except `A8`.**

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

**Does NOT need Revit.** This group is **seven rows done of NINE** — `A6` and `A9` both closed on
2026-09-06 on the owner's PC, which carries the .NET SDK. *(This line said "of eight" while nine rows
sat under it, which is the same drift as the summary above and was caught the same way: by counting
the rows in this section rather than believing the sentence over them.)*

**Eight of the nine are closed.** `A4`, `A6`, `A7` and `A9` all fell on 2026-09-06 on the owner's PC —
four rows parked for months on *a machine*, closed in one afternoon by somebody sitting at one.
**`A8` is the only row left in this group**, and it needs a real MCP host connecting over stdio, which
means Claude Code configured on that PC rather than any further code. The fourth, **`A9`,
needs only a machine with the .NET SDK on it** - it is the batches of fragments written on 2026-09-02,
2026-09-04 and 2026-09-05 in containers where the SDK could not be installed, so for the first time in
this library some C# has never been near a compiler. `A8` arrived on
2026-08-29 with the MCP tools that made the brain reachable: they were written where no MCP SDK is
installed, so nothing has ever served them.

**A1, A2, A3 and A5 are done, and A4 all but its last mile** — all on 2026-08-28, on Linux, in a
container, which nobody had tried because "Revit is Windows-only" had been carried for three sessions as
"so the C# cannot be compiled anywhere". The Revit API reference assemblies come from NuGet, which this
repository's own add-in project already used by default, and Linux distributions package the .NET SDK.

**A5 fell to the same mistake wearing different clothes, later the same day.** With 2020–2024 compiling,
the three newest releases were still skipped as *needing the Windows Desktop SDK* — and that turned out
to be a fact about **which SDK package was installed**, not about the operating system. Ubuntu's
`dotnet-sdk-10.0` carries those targets; its `dotnet-sdk-8.0` does not. **All eight releases now compile
here**, and the lesson is the one this project keeps re-learning: an environment-specific block belongs
in a sentence that names the environment, or it hardens into a fact about the project. See
[docs/30 §2a](docs/30-compiling-away-from-windows.md), and run it yourself with one command:

```bash
apt-get install -y dotnet-sdk-10.0     # the .NET 8 package builds 2020-2024 only
python tools/check-compile.py          # 2020 through 2027, all four projects, 0 warnings
```

**Two real defects came out of it, both of which reading had already missed twice:**

- `Document.CreationGUID` **does not exist in Revit 2020.** It compiled clean on 2024 and failed on
  2020. Fixed in `RevitWrite.DocumentKey()` with the Project Information element's `UniqueId`, which
  exists on every release from 2020 to 2027 — so there is no `#if` and no version branch to maintain.
- `test_bridge_roundtrip.py` asserted that **nothing held the lease**, at a point where an earlier
  unknown-op probe had already claimed it. The bridge was right; the check had been written from
  reading, in a file that could not run on the machine it was written on.

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**A1**~~ | ~~`dotnet --version`~~ | **DONE 2026-08-28.** SDK 8.0.130. Still worth one run on the owner's PC, where `tools/setup.ps1` checks it anyway and stops with the download link |
| ~~**A2**~~ | ~~`dotnet build -p:RevitVersion=2020`~~ | **DONE 2026-08-28.** Compiles, 0 warnings, after the `CreationGUID` fix above. The other five spots this row used to warn about — `WorksharingUtils.GetCheckoutStatus`, `IFailuresPreprocessor`, `TransactionGroup.GetStatus`, `BuiltInCategory.INVALID`, `UIDocument.RefreshActiveView` — are all **clean on 2020 and 2024** |
| ~~**A3**~~ | ~~`dotnet build -p:RevitVersion=2024`~~ | **DONE 2026-08-28.** So are 2021, 2022 and 2023 — all four projects, 0 warnings each |
| ~~**A4**~~ | ~~`python tests/test_bridge_roundtrip.py` — **on Windows**~~ | **DONE 2026-09-06 on the owner's PC. All 32 checks pass against a REAL Windows named pipe** — `heron.2024.21512` — which is the one thing the Linux runs could never touch: the pipe's actual naming, its security descriptor, and the `CreateNewInstance` flag from [HANDOVER](HANDOVER.md) §4 note 2. Framing, the JSON parser, the token, newest-connection-wins, the toggle cycle and **the whole lease** all behave on Windows exactly as they did on Linux, so the shim was faithful. **One result is worth naming separately:** *"disconnect stops it and withdraws the announcement"* passes — which is the control the owner named as his stop when [D-46](docs/DECISIONS.md) was read back to him, now proven on the platform it has to work on. The Emergency Stop button it replaced never had a passing test. **What this still does NOT touch is Revit**: the host is a test host, no model is open, and `D3` is untouched by all of it |
| ~~**A5**~~ | ~~`python tools/check-compile.py 2025 2026 2027`~~ | **DONE 2026-08-28, and it did not need Windows either.** All four projects compile on 2025, 2026 and 2027 — `net8.0-windows` and `net10.0-windows`, 0 warnings — on Linux, with the .NET 10 SDK and `-p:EnableWindowsTargeting=true`. This row assumed the Windows Desktop SDK was a property of the operating system; it is a property of the **SDK package**, and Ubuntu's `dotnet-sdk-10.0` ships it while its `dotnet-sdk-8.0` does not. The script had the right MSBuild flag and applied it **only on Windows**, where it does nothing. Full account and the validation in [docs/30 §2a](docs/30-compiling-away-from-windows.md). `python tools/check-api-surface.py` still runs and still adds something a compile cannot — it reads the **shipped** assemblies, where a compile reads the NuGet reference packages |
| ~~**A6**~~ | ~~On **Windows**, run `python tools/check-compile.py 2025` and read the first lines of output~~ | **DONE 2026-09-06, on the owner's PC — the probe reads the SDK correctly on Windows.** Output: *"Compiling with .NET SDK 10.0.303 on Windows. WindowsDesktop targets found in the .NET 10 SDK"*, then all four projects **ok** on Revit 2025. This row existed because `check-compile.py` decides whether it can build the WPF releases by **looking for** `Sdks/Microsoft.NET.Sdk.WindowsDesktop` under each installed SDK rather than by asking whether it is on Windows — and **that probe had only ever run on Linux**, where a false negative costs nothing. On Windows a false negative would have **silently skipped 2025-2027 on the one machine where they used to build**, which is why a five-minute check was worth keeping. It did not misread, so the fallback path (attempt the build anyway when the SDK list cannot be read at all) is **still unproven** — it was never reached |
| **A8** | On the PC, with the Heron MCP server configured in Claude Code, ask it *"what can you do?"* and watch `heron_capabilities` run. Then `heron_lookup` with *"select all the ducts"* | **Mostly done 2026-08-31, and it did not need Windows either — read what moved and what did not.** This row existed because the three brain tools were written on a machine with **no MCP SDK installed**, so `FastMCP` had never served them. That was a missing **package**, not a missing operating system: the SDK is pure Python and installs from PyPI. Installed on Linux, a real SDK now serves **all ten** tools with their descriptions and argument schemas, and `heron_capabilities`, `heron_resolve` and `heron_lookup` all answer through the SDK's own dispatch with both refusals intact — [`tests/test_mcp_serves.py`](tests/test_mcp_serves.py), which is that check made permanent. **Installing the SDK immediately found a defect no text read could see**: `pip install --user mcp` now resolves to **2.x**, which deleted `mcp.server.fastmcp`, so Heron's server raised `ImportError` before registering a single tool — every Heron tool absent from the host. Fixed, and proven on **both** SDK majors side by side. **What genuinely remains needs the PC:** a real host over stdio — that Claude Code connects, renders the docstrings, and picks a tool from them. In-process dispatch is a strong signal ahead of that, not a substitute for it. **PASS is what the host shows:** ten jobs, **all ten** with every part provided and **none** unprovided (this row said *four* and *seven missing* until 2026-08-31 — it was written on 2026-08-29 **before** the seven capabilities were written later the same day, so as worded it would have failed a correct Heron), and every answer saying plainly that it **cannot run** any of them. **And one assertion here is RETIRED rather than repaired, with its reasoning, because quietly deleting it would look identical.** This row also required `heron_lookup` to answer `FILTER_ELEMENTS_BY_CATEGORY`. Measured, it answers `SET_SELECTION` (provided by `FRG-SEL-001`), with `FILTER_ELEMENTS_BY_CATEGORY` the runner-up and both routes agreeing. That is **the same assertion Steps 10 and 11 already retired**, written into the register and not retired alongside them: *"select all the ducts"* is filter-**then**-select, a composition, and a composition is what a **skill** names — `heron_capabilities` lists it as the skill *"Show me these on screen"*. Asking which single capability serves that sentence is asking the wrong layer, and of the two, `SET_SELECTION` is the one the sentence actually ends at. So the check is that `heron_lookup` **names a capability and its route**, not which one | 
| ~~**A7**~~ | ~~`pip install --user model2vec` then `python brain/heron_embed.py "stop the air going the wrong way"`~~ | **DONE 2026-09-06 on the owner's PC. `Backend: model`, 343 fragments embedded** — the weights host is reachable from here, which it was not from either container. **The check was not stopped at that line**, because *the model loaded* and *the model helps* are different claims. Scored against candidates sharing **no word** with the query: the model ranks `check flow direction` **first at 0.391** and `rename a sheet` at **0.001**; the built-in `lexical` backend ranks the same correct answer **LAST at 0.038**, below `rename a sheet` at 0.048. **The old engine's best guess was `find dead ends` and its worst was the right answer** — which is what *tolerant of spelling but not of meaning* costs, in numbers. **Two things recorded rather than smoothed over.** In the full index `find-dead-ends` (0.478) edged `check-flow-direction` (0.475) by **0.003** — too close to call, and both are defensible readings of that sentence, so this row proves the backend understands meaning and does **not** prove any particular ordering. And **this row's PASS wording is retired with its reasoning**: it asked for *"the duct fragment"*, written when the library held seven and no damper/flow fragment existed. At 343 the honest form is *a flow-direction fragment ranks top-2 with no shared words*, which is what happened |
| ~~**A9**~~ | ~~On any machine with the .NET SDK, run `python tools/check-fragments-compile.py`~~ | **DONE 2026-09-06, on the owner's PC, at 343 fragments — `Every fragment compiles on every release it claims`, all eight, 2020 through 2027.** This row had been run green once before at **329** and left open because the library kept growing; it is closed now because the machine that can re-run it in minutes is the owner's own, so it stops being a row and becomes a command. **What it proves is the API surface agreeing and the contract being kept** — each fragment leaving what it promised at the declared type. **It proves nothing about behaviour**, which needs a real model and a proof carrying a negative case ([D-30](docs/DECISIONS.md)). The nine version defects this check caught before they were written are recorded in the git history of this row rather than repeated here |

## Group R — read the decisions back (no Revit needed)

**Not a test — a conversation**, and the only item in this file that is not about code behaving. It is
here because this file is what gets opened at the PC, and a review nobody is reminded of does not happen.

| ID | Do this | Done looks like |
|---|---|---|
| ~~**R1**~~ | ~~Read **D-23 to D-27** back to Ajmal~~ | **DONE 2026-08-29 — and it covered D-23 to D-43, not just the five.** All twenty-one were read back and he confirmed them. **In conversation rather than at the PC**, which is worth recording: the instruction said *at the PC*, and what that was for — him sitting with them rather than tapping yes — did happen. Three were put to him individually because they carried real consequence, and all three came back unchanged: **D-33's boundary** (Heron never invents a Revit number and does decide its own code — the line the decision file itself flagged as never actually stated by him), **D-26** (the model file never leaves; names, counts, sizes and reasoning are fine), and **D-32** (v1 both reads and writes, reading first, writing off by default). The remaining eighteen were confirmed as a block |
| **R1b** | Show him **[D-14](docs/DECISIONS.md), the trust model**, working on a screen with his own fragments in it | He agreed the direction on 2026-08-28 — *"yes, but show me it working at the PC first"* — so D-14 stays **Proposed** until he has seen it. Use the framing that worked: a family has **a maker** and **an approval status**, and nobody would put those on one dropdown. Phase 2 may be designed against the two axes meanwhile; it may not be called settled |
| ~~**R2**~~ | ~~Pay particular attention to **D-26 and D-32**~~ | **DONE 2026-08-29.** Both were put to him as separate questions with their reversal history stated, and both were confirmed as they stand. The reason this row existed — that a first answer sharpened once its consequence was visible — held up: neither moved a fourth time |

**R1 and R2 are done; R1b needs a screen.** The review happened *after* Phase 2 was built rather than
before, which is weaker than intended and was the owner's own call to override. Worth recording that it
changed nothing: twenty-one decisions read back, twenty-one confirmed. The two that had been reversed
within hours on the day did not move again.

## Group B — does Revit still load

The panel was rebuilt: Heron and Bridge Status are now one split button, and a ribbon that throws
during `OnStartup` costs the whole add-in. If Heron disappears entirely, this is why.

| ID | Do this | Pass looks like |
|---|---|---|
| **B1** | Start Revit 2020 | The **Heron AI** tab is there |
| ~~**B2**~~ | ~~Look at the panel~~ | **DONE 2026-09-06, twice.** First as *two* controls. Re-checked after Emergency Stop was removed ([D-46](docs/DECISIONS.md)): the panel showed **one** control, as intended. Revit 2020 and 2024 were both open at that moment and which one was looked at was not established — the two carry the same source, so this proves the ribbon is right, not which release it is right on |
| ~~**B2a**~~ | ~~Click the arrow under Heron~~ | **DONE 2026-09-06.** The list opens with Bridge Status in it |
| ~~**B2b**~~ | ~~Pick Bridge Status, then look at the top of the split button~~ | **DONE 2026-09-06.** Top stayed **Heron**, icon intact. `IsSynchronizedWithCurrentItem = false` does take effect in a real Revit, which a compile could not have told us. **The release it was run on was not recorded** — re-run on the other two before this counts for all of 2020-2027 |
| ~~**B3**~~ | ~~Press Heron~~ | **DONE 2026-09-06.** Connects, icon lights, goes dark again on the second press. The state indicator survived the move into a split button — which is what `IsSynchronizedWithCurrentItem = false` was there to protect, now proven from the user's side rather than from the flag |
| **B3a** | Press Heron again to disconnect, then re-open the arrow and pick Bridge Status | Says **Not connected** — the item inside the list still runs its own command, it did not become part of the toggle |
| **B4** | `python mcp/client/heron_bridge_client.py ping` then `count` | Both answer, as they did before Step 6 |

## Group C — the gate, before anything can move

**Do not skip to D.** C3 is what proves the write path cannot fire by accident; testing the move before
it is pointless, because a passing move tells you nothing about whether the gate works.

The gate now sits at **one** place — every operation passes through it before routing, and its risk
comes from `HeronOperationRegistry` rather than from a literal inside the write path. C5 and C6 are what
prove it blocks the right level rather than simply blocking everything, which would pass C3 while being
useless.

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**C1**~~ | ~~Press Emergency Stop~~ | **CANNOT BE RUN from 2026-09-06.** The button was removed ([D-46](docs/DECISIONS.md)). `HeronStop` and both gates survive, but nothing can switch the stop on, so this step has no way to start |
| ~~**C2**~~ | ~~Press it again~~ | **CANNOT BE RUN.** Same reason |
| **C3** | With `write.enabled` still **false** (the default — do not change it yet), ask to move ducts | **Refuses**, and names `write.enabled` and the config file path. Nothing goes to Revit |
| ~~**C4**~~ | ~~Press Emergency Stop on, then ask to move ducts~~ | **CANNOT BE RUN.** The two refusals are still written and still distinct in the code ([RevitOperations.cs](revit/Heron.Revit.Addin/RevitOperations.cs)), but with no way to set the stop, only the permission refusal can be reached. **Untested from here on** |
| **C5** | With `write.enabled` still false, ask to **select** ducts | **Works.** The gate blocks MODIFY, not READ or EXECUTE — if selecting is refused, the levels are wrong |
| ~~**C6**~~ | ~~Press Emergency Stop on, then ask to **count** elements~~ | **CANNOT BE RUN.** Same reason. The rule it proved — the stop blocks changes only, never reads — is still in the code and is now **unproven by test** |
| **C7** | Now set `write.enabled = true` in `%APPDATA%\Heron\config\heron.config`. **No restart** — `HeronPermissions.Allows` reads that file fresh on every check, so the change lands on the next request. The instruction to restart was here, and in the refusal message, until 2026-09-06; both said it, neither needed it | — |
| **C8** | Ask to move ducts again | **It is now permitted** (a preview appears). If it still refuses, `write.enabled` is not being read — that exact bug existed until 2026-08-28: the key was read but never *declared*, so `Load()` dropped it silently and the refusal told you to set the thing you had just set |
| **C9** | `revit_health` | First line is a four-state rollup — `Heron: HEALTHY / WARNING / DEGRADED / FAILED`. With writing on it must show **WARNING** on the write gate and say the path is unproven |
| **C10** | Set `revit.operationTimeoutSeconds = 120`, restart Revit, then make Revit busy long enough to time out | The message is *"Revit started the request but has not finished"* — **not** *"no answer"*. Proves the client's deadline follows the add-in's setting instead of the old hardcoded 90 s |

## Group D — the move itself

**On a scratch model. Not a real project.** Nothing below has ever run.

| ID | Do this | Pass looks like |
|---|---|---|
| **D1** | *"move the ducts up 200 mm"* | A **preview**. A count you can check by eye. **Nothing moves yet** |
| **D2** | Say yes | They move |
| **D3** | **MEASURE ONE.** | **200 mm.** Not 200 feet, not 0.656 of anything. **This is the single most important line in this file** — it is what catches a unit error, and it is the whole reason `HeronUnits` exists |
| **D4** | Look at the screen without clicking or zooming | The change is **visible**. That is `TryRefresh` |
| **D5** | **One** Ctrl+Z | Everything back, in **one** step, named "Heron: move ducts up 200 mm". If it takes two, Golden Rule 16 is broken |
| **D6** | *"move them down 50 mm"* | Negative distances work; down is a direction, not an error |

## Group E — the refusals

Each of these is a rail. A rail that has never been tested is decoration.

| ID | Do this | Pass looks like |
|---|---|---|
| **E1** | Preview, wait **over 2 minutes**, then approve | Refuses as **expired**. Nothing moves |
| **E2** | Preview, draw one more duct, then approve | Refuses — **the model moved on**. Names both counts |
| **E3** | Preview, click into a **second open project**, approve | Refuses, names **both** models, and says the approved one is **still open** |
| **E4** | Preview, then **close** the first project, approve | Refuses and says it was **CLOSED** — a different message from E3. Golden Rule 20 treats these differently |
| **E5** | E4 again on a model that has a **link loaded** | Still says closed. Proves the `IsLinked` filter — a loaded link must not count as "still open" |
| **E6** | Approve **twice** in a row | The second finds nothing to approve. It must **not** move them a further 200 mm |
| **E7** | Pin a duct, then preview | Reported as **skipped**, and left alone after the move |
| **E8** | On a **workshared** model with a duct owned by another user, preview | Reported as skipped, and the move does not fail because of it |
| **E9** | Open a dialog in Revit, then ask to move | *"Revit is busy"* — a clean refusal, not a hang. Recovers by itself |
| **E10** | Put two ducts in a **Revit group**, then ask to move that category up 200 mm | They are reported as **did NOT move at all**, by count, with the words *"almost certainly inside a group"* — and the ungrouped ones still move. **This is the case Revit will not tell you about**: `MoveElements` returns normally and moves nothing for a group member, no exception and no warning, so counting "it did not throw" as "it moved" reports a clean success for elements that have not shifted a millimetre. Proved against a real model in the owner's earlier work; Heron now compares positions either side instead of trusting the call. A group member is **not** pinned, so `E7`'s skip filter does not catch it |

## Group F — Steps 1-5 are no longer proven on this build

**This group got more important on 2026-08-28, and it is not a leftover any more.**

Steps 1, 4 and 5 were proven in real Revit — but against a build that no longer exists. Step 6 modified
six files that those proofs covered:

| File | Belongs to | What changed |
|---|---|---|
| `HeronApplication.cs` | Step 1 | a third ribbon button |
| `Commands.cs` | Step 1 | the Emergency Stop command |
| `HeronConfig.cs` | Step 1 | `write.enabled` declared |
| `heron_bridge_client.py` | Step 1 | the response deadline is now derived, not constant |
| `RevitOperations.cs` | Step 4 | the gate, and a shared category resolver |
| `heron_mcp_server.py` | Step 5 | three new tools and a health rollup |

None of those changes has been compiled. **A proof against an older build is not a proof of this one**,
so until F3 passes, "Steps 1 to 5 are proven" describes history rather than the current code.

| ID | Do this | Pass looks like |
|---|---|---|
| **F1** | Restart Claude Code, ask to select ducts | The answer names the **session** as well as the document. Both models being called `Project1` is why |
| **F2** | Launch Revit 2027 | Unknown. The owner says his install does not work — find out whether that is the install or Heron |
| **F3** | Re-run everything in [`HANDOVER.md`](HANDOVER.md) §3 once, in one sitting | All still true against the current build |

## Group H — the lease (needs TWO chats and one Revit)

Changes behaviour proven in Step 1, so it is the group most likely to surprise you.

**Do `A4` first.** The round-trip test already covers the refusal, the `ping`/`info` exemption, the
`inUse`/`mine` reporting and that the first chat is never cut off — all without Revit. What is left here
is only what genuinely needs real Revits and real chats: the picker column, expiry over real time, the
button releasing it, and two Revits not interfering.

| ID | Do this | Pass looks like |
|---|---|---|
| **H1** | One chat, one Revit. Ask anything | Works exactly as before. The lease is claimed silently — you should notice nothing |
| **H2** | Open a **second** Claude chat, connect to the **same** Revit, ask anything | **Refused**, saying the Revit is in use by another chat and roughly when it frees. It must NOT cut the first chat off. *(Mostly covered by `A4` — this confirms it end to end through a real chat)* |
| **H3** | Go back to the **first** chat, ask again | Still works. It never lost its hold |
| **H4** | In the second chat, run `revit_health` | Shows the Revit — `ping`/`info` are lease-exempt, so *looking* must never claim it. *(The exemption itself is covered by `A4`; this checks the tool uses it)* |
| **H5** | Two Revits open, one held by another chat. Ask for the picker | The rows read `(free)` and `(in use by another chat, ~N min left)`. **This is the column that did not exist before** |
| **H6** | Leave the first chat idle over 5 minutes, then ask from the second | Now granted — the lease lapsed. An abandoned chat must not hold a Revit forever |
| **H7** | First chat holding it, press the **Heron button** to disconnect, then ask from the second | Granted immediately. Pressing the button releases it rather than making anyone wait out the timer |
| **H8** | Two chats, **different** Revits (2020 and 2024) | No interference at all. The lease is per process |
| **H9** | `revit_health` from a chat holding **nothing**, with a free Revit open. Then ask from a *second* chat | The second chat is **granted**. A health check must NOT have claimed the free Revit — that bug existed for one commit: it called `count_elements` on every session, which is not lease-exempt |
| **H10** | First chat mid-request, second chat connects and is refused. Watch the FIRST chat | It loses that one reply and recovers on the next. Known limitation, [docs/25](docs/25-multi-session-and-binding.md): the pipe is displaced at connect, before the lease can speak. If a **write** was in flight it must report the outcome as *unknown*, never as failed |

## Group G — hard to force, do last

Not blocking. Listed so they are not mistaken for tested.

| ID | Do this | Pass looks like |
|---|---|---|
| **G1** | Force a failure mid-move (the build order asks for this — e.g. a duct that cannot move) | The model is **untouched**, and the message says it was rolled back. Not a partial move |
| **G2** | Kill the bridge between approve and the answer coming back | Heron says it **cannot tell** whether it ran, and refuses to retry on its own. This is the `unknown_outcome` path — the one the Failure Analysis Agent exists for |
| **G3** | After all of the above passes | Set `write.enabled` back to **false** until you actually want Heron writing |
| **G4** | After a move, open the newest file in `%APPDATA%\Heron\audit` | The entry carries **every moved element's UniqueId**, the document identity and the undo entry name, all under one Workflow ID. A count alone cannot answer *"which ducts?"* |
| **G5** | `python tests/test_golden.py` after re-proving anything | The re-proved case stops reading **STALE**. Seven Phase 0 proofs are stale right now — they were taken against a build that no longer exists |

---

## When everything above has passed

Step 6 is finished, and not before. At that point:

1. Change the default in `HeronPermissions` **only if you want writing on by default** — and
   [D-19](docs/DECISIONS.md) says why the answer is probably still no.
2. Delete the "never run" banners in `RevitWrite.cs` and `heron_mcp_server.py`.
3. Move the proven rows into [`HANDOVER.md`](HANDOVER.md) §3, under *proven against a real Revit*.
4. Delete this file.
