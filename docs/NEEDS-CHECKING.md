# Needs checking — the register

> | | |
> |---|---|
> | **Type** | **Permanent register.** Append-only, and **never deleted** — this is where work notes empty into |
> | **For** | Anyone about to believe a green gate — **this is the list of what nothing has proved** |
> | **Authority** | A row here **outranks any claim that something works**. Green is not proven — [D-30](DECISIONS.md) |
> | **Waiting on you?** | `python tools/owner-queue.py` — **never a list typed on this page** |
> | **Adding to it** | A row names the **command**, the **machine it needs**, and what **pass looks like** — including what a FAIL would prove, which is worth the same |
> | **Its numbers** | **Do not read the stated totals as today's.** Derive the Group A ids: `grep -oE '\*\*A[0-9]+\*\*|~~\*\*A[0-9]+\*\*~~' docs/NEEDS-CHECKING.md | grep -oE 'A[0-9]+' | sort -uV` |

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
grep -oE '\*\*A[0-9]+\*\*|~~\*\*A[0-9]+\*\*~~' docs/NEEDS-CHECKING.md | grep -oE 'A[0-9]+' | sort -uV
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

**Does NOT need Revit** — except what is left of `A11`, `A12` and `A13`, which do. This group is **nine rows done of NINETEEN** — `A10` and `A11` were added 2026-09-11, `A12` and `A13` on 2026-09-12 with the packaging gate, and `A14` on 2026-09-12 with the path-separator fix. *(This line said "of ELEVEN" while thirteen rows sat under it — the third time, and caught the same way again: by counting the rows. It then said **"of FOURTEEN" while NINETEEN rows sat under it** — the fourth time, corrected 2026-09-15, and caught the same way yet again. A1–A18 plus A16b is nineteen; derive it, do not read it off this sentence: `grep -oE 'A[0-9]+b?' docs/NEEDS-CHECKING.md | sort -u | wc -l`)* `A6` and `A9` both closed on
2026-09-06 on the owner's PC, which carries the .NET SDK. *(This line said "of eight" while nine rows
sat under it, which is the same drift as the summary above and was caught the same way: by counting
the rows in this section rather than believing the sentence over them.)*

**A1 TO A9 ARE ALL CLOSED**, every one of them on 2026-09-06 on the owner's PC. `A4`, `A6`, `A7`, `A8`
and `A9` fell in a single afternoon — rows parked for months on *a machine*, closed by somebody sitting
at one. **`A11` was added the same day, filed as needing a compiler, and stopped needing one within the hour** — the .NET 10 SDK was in the Ubuntu archive all along, so its compile half closed here and only the Revit half is left. That is the **sixth** time something filed under *needs a machine* was waiting on somebody trying it, and the first five are above. **`A10` was added on 2026-09-11 and needs a network rather than a machine** — the re-ranker
measurement Stage 7 asks for, which the container it was built in cannot take.

**`A8` is the one that earned its place.** It did not pass on the first attempt: it exposed a hang that no
existing test could see, because every one of them called the tools **in this process**, where the failing
import is harmless. Only a real subprocess over real stdio shows it. **And the hang was created by closing
`A7` an hour earlier** — see that row, and [D-49](DECISIONS.md). The fourth, **`A9`,
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
[docs/30 §2a](30-compiling-away-from-windows.md), and run it yourself with one command:

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
| ~~**A4**~~ | ~~`python tests/test_bridge_roundtrip.py` — **on Windows**~~ | **DONE 2026-09-06 on the owner's PC. All 32 checks pass against a REAL Windows named pipe** — `heron.2024.21512` — which is the one thing the Linux runs could never touch: the pipe's actual naming, its security descriptor, and the `CreateNewInstance` flag from [HANDOVER](HANDOVER.md) §4 note 2. Framing, the JSON parser, the token, newest-connection-wins, the toggle cycle and **the whole lease** all behave on Windows exactly as they did on Linux, so the shim was faithful. **One result is worth naming separately:** *"disconnect stops it and withdraws the announcement"* passes — which is the control the owner named as his stop when [D-46](DECISIONS.md) was read back to him, now proven on the platform it has to work on. The Emergency Stop button it replaced never had a passing test. **What this still does NOT touch is Revit**: the host is a test host, no model is open, and `D3` is untouched by all of it |
| ~~**A5**~~ | ~~`python tools/check-compile.py 2025 2026 2027`~~ | **DONE 2026-08-28, and it did not need Windows either.** All four projects compile on 2025, 2026 and 2027 — `net8.0-windows` and `net10.0-windows`, 0 warnings — on Linux, with the .NET 10 SDK and `-p:EnableWindowsTargeting=true`. This row assumed the Windows Desktop SDK was a property of the operating system; it is a property of the **SDK package**, and Ubuntu's `dotnet-sdk-10.0` ships it while its `dotnet-sdk-8.0` does not. The script had the right MSBuild flag and applied it **only on Windows**, where it does nothing. Full account and the validation in [docs/30 §2a](30-compiling-away-from-windows.md). `python tools/check-api-surface.py` still runs and still adds something a compile cannot — it reads the **shipped** assemblies, where a compile reads the NuGet reference packages |
| ~~**A6**~~ | ~~On **Windows**, run `python tools/check-compile.py 2025` and read the first lines of output~~ | **DONE 2026-09-06, on the owner's PC — the probe reads the SDK correctly on Windows.** Output: *"Compiling with .NET SDK 10.0.303 on Windows. WindowsDesktop targets found in the .NET 10 SDK"*, then all four projects **ok** on Revit 2025. This row existed because `check-compile.py` decides whether it can build the WPF releases by **looking for** `Sdks/Microsoft.NET.Sdk.WindowsDesktop` under each installed SDK rather than by asking whether it is on Windows — and **that probe had only ever run on Linux**, where a false negative costs nothing. On Windows a false negative would have **silently skipped 2025-2027 on the one machine where they used to build**, which is why a five-minute check was worth keeping. It did not misread, so the fallback path (attempt the build anyway when the SDK list cannot be read at all) is **still unproven** — it was never reached |
| ~~**A8**~~ | ~~On the PC, with the Heron MCP server configured in Claude Code, ask it *"what can you do?"*~~ | **DONE 2026-09-06 on the owner's PC — and it FAILED first, which is the only reason the row was worth keeping.** A real host over stdio: `initialize` 1.7 s, **all ten tools offered**, `heron_capabilities` **6.1 s** — *"10 jobs Heron knows by name. 10 have every part they need, 0 are waiting on something nobody has built"*, 343 capabilities with a provider, 0 unprovided — and `heron_lookup` 4.4 s naming `SELECT_BY_CATEGORIES` via `FRG-SEL-024`. Every answer still says plainly it cannot RUN any of them. **Before the fix, `heron_capabilities` never replied at all**: a real Claude Code tool call sat on it for **thirty minutes**. The stack, taken with `faulthandler` rather than guessed, was `heron_capabilities → catalogue → index → backend → _load_model → import model2vec → import numpy → loading numpy's native extension`, **on the asyncio event loop** — an import costing **1.0 s** in a fresh process and still running **40 s** later there. **Closing `A7` is what broke this**: until model2vec was installed the import failed instantly and Heron degraded to `lexical`, so the handler always answered. Two rows, each correct alone, never run together — **a register of independent rows cannot see that, and this is the first time it has bitten.** Fixed by loading the encoder on a background thread at startup ([D-49](DECISIONS.md)); until it is ready every answer is lexical and says so. `tests/test_mcp_stdio.py` is the check made permanent, and it holds the reply to a **deadline** — a test that waits forever cannot tell a slow answer from no answer |
| ~~**A7**~~ | ~~`pip install --user model2vec` then `python brain/heron_embed.py "stop the air going the wrong way"`~~ | **DONE 2026-09-06 on the owner's PC. `Backend: model`, 343 fragments embedded** — the weights host is reachable from here, which it was not from either container. **The check was not stopped at that line**, because *the model loaded* and *the model helps* are different claims. Scored against candidates sharing **no word** with the query: the model ranks `check flow direction` **first at 0.391** and `rename a sheet` at **0.001**; the built-in `lexical` backend ranks the same correct answer **LAST at 0.038**, below `rename a sheet` at 0.048. **The old engine's best guess was `find dead ends` and its worst was the right answer** — which is what *tolerant of spelling but not of meaning* costs, in numbers. **Two things recorded rather than smoothed over.** In the full index `find-dead-ends` (0.478) edged `check-flow-direction` (0.475) by **0.003** — too close to call, and both are defensible readings of that sentence, so this row proves the backend understands meaning and does **not** prove any particular ordering. And **this row's PASS wording is retired with its reasoning**: it asked for *"the duct fragment"*, written when the library held seven and no damper/flow fragment existed. At 343 the honest form is *a flow-direction fragment ranks top-2 with no shared words*, which is what happened |
| ~~**A9**~~ | ~~On any machine with the .NET SDK, run `python tools/check-fragments-compile.py`~~ | **DONE 2026-09-06, on the owner's PC, at 343 fragments — `Every fragment compiles on every release it claims`, all eight, 2020 through 2027.** This row had been run green once before at **329** and left open because the library kept growing; it is closed now because the machine that can re-run it in minutes is the owner's own, so it stops being a row and becomes a command. **What it proves is the API surface agreeing and the contract being kept** — each fragment leaving what it promised at the declared type. **It proves nothing about behaviour**, which needs a real model and a proof carrying a negative case ([D-30](DECISIONS.md)). The nine version defects this check caught before they were written are recorded in the git history of this row rather than repeated here |
| **A10** | `pip install --user sentence-transformers` then `python brain/heron_rerank.py`, then the tracked question and the twelve of the Stage 0b run — **on a machine that can reach `huggingface.co`** | **Stage 7's after-measurement, and it is the only thing standing between R-41 and DONE.** The seam is built and the absent half is tested; no cross-encoder has ever run. Pass looks like: `Backend: cross-encoder`, then the same tracked question at 360 fragments and 62 chunks with the top five and the winner's lead recorded beside the 2026-09-11 `absent` row in [`brain/retrieval-history.md`](../brain/retrieval-history.md) — **whichever way it comes out.** A re-ranker that does not improve the order is a result worth the same as one that does, and R-55 says a threshold is never moved to make a report look better. Also record the **real download size** against the 500 MB – 2 GB this repository has only ever quoted from a field reading |
| **A11** | In Revit, run any Heron read tool and look at the reply the server receives — **on the owner's PC** | **The project key reaching the server, which is what names a project knowledge store.** `RevitOperations.ProjectKey()` was added on 2026-09-11; the add-in already computed this identity for its own preview/commit pairing (`RevitWrite.DocumentKey`) and never sent it, so `heron_standards` either skipped the project scope or would have named a store after a file path. **The compile half is closed.** This row was filed saying it had *never been compiled* — and the .NET 10 SDK turned out to be one `apt-get install dotnet-sdk-10.0` away in the Ubuntu archive, which [docs/30 §2a](30-compiling-away-from-windows.md) had already measured and this row assumed was unavailable. `tools/check-compile.py`: **COMPILED 2020 through 2027**, all four projects, 0 warnings. `tools/check-api-surface.py` against the **shipped** assemblies: *every referenced type and member exists*, all eight. **What is left needs Revit**: a `projectKey` field present in the `count_elements` and `select_by_category` replies, its value **the same string across a save, a rename and a move of the model**, and `pinned.project_key` non-`None` in a conversation that has run **only read tools**. A compile proves the member exists and the signature matches; it is not evidence that the value is stable across a rename |
| ~~**A12**~~ | ~~On the PC: `.\tools\deploy-addin.ps1 -RevitVersion 2024`, start Revit, and look for the **Heron AI** tab~~ | **DONE 2026-09-19 on the owner's PC — and on ALL THREE releases installed there, 2020, 2024 and 2027, rather than on 2024 alone.** Written up in full, with the journal lines and the hashes, in [07 section 10](07-installation-and-update.md). Built from commit `48ae1dc`, one release at a time because the build output folder is shared, and placed by the documented script. **Two of the four questions are answered by Revit rather than by Heron**, which is the part this row was really asking for: every journal records `API_SUCCESS { Starting External Application: Heron AI, Class: Heron.Revit.Addin.HeronApplication, Vendor : AJPS(Ajmal PS - Heron AI) }`, and **no journal contains *"cannot run the external application"***. The ribbon built on all three — `HeronBridgeToggle`, `HeronStatus` and `HeronWriteToggle` under tab **Heron AI**, panel **Bridge** — and the owner clicked **Changes** on 2020, which logged `Write permission set to False from the ribbon` and `True` two seconds later, so the icon drew well enough to find and press. **The manifest question is answered three ways**, because one line naming a path is not the same as nothing else being able to supply it: every pushbutton line names `%APPDATA%\Autodesk\Revit\Addins\<release>\Heron\Heron.Revit.Addin.dll`; `%ProgramData%\Autodesk\Revit\Addins` and all three `Program Files` `AddIns` folders hold nothing called `Heron*`, so the per-user manifest is the only one on the machine; and each deployed assembly was **file-locked** by its own Revit while running. **Revit 2027 says it outright** in two lines the older releases do not emit — `The add-in folder '...\Addins\2027\Heron' was registered for the context 'DEFAULT'` and `[Jrn.AddInManifest] Rvt.Attr.AddInManifest: Heron.addin ... Rvt.Attr.AddInLoadFailureMessage: NoError , 0.248000`. **The no-admin promise held**: `IsInRole(Administrator)` was `False` throughout, six deploys and three rollbacks wrote only to `%APPDATA%` and `%LOCALAPPDATA%`, and **no UAC prompt appeared at any point**. **And the runtimes really are three different ones**, read back out of the deployed bytes rather than out of a build log: `.NETFramework,Version=v4.7.2` on 2020, `v4.8` on 2024, `.NETCoreApp,Version=v10.0` on 2027 — the thing a single-release test can never show. **One gap in the script's own guard, found by doing this:** it separates builds by whether a `deps.json` exists, which catches a 2027 build going into 2024 but **cannot tell net472 from net48**, because neither emits one. A 2024 build deployed into 2020 would pass every check. It did not happen here, but nothing stands behind that guard |
| ~~**A13**~~ | ~~On the PC, with a previous Heron already installed: deploy over it, then start Revit~~ | **DONE 2026-09-19 on the owner's PC, on all three releases — and the rollback half could not be done at all until it was built, which is the finding.** Written up in [07 section 10](07-installation-and-update.md), filed as [row 147](FRAGMENT-ISSUES.md). **The upgrade half passed cleanly.** Every release already carried an install, so each of the six deploys was an upgrade rather than a first install, and `%APPDATA%\Heron\config\heron.config` hashed `9FC06D52CEBB099977BF0EBF9C68389652D717A84A9F536351ADBDF7FBC7FB39` **before and after every one of them**. The file's own first line claims *"Owned by you. Never overwritten by an update"* and that is now measured rather than asserted — it carries `write.enabled = true`, which is the switch [D-19](DECISIONS.md) puts writing behind. `audit/` stayed 3 files / 4,049,586 bytes and `knowledge/` 4 files / 4,235,331 bytes across it. **The rollback half had no mechanism.** This row's own recipe — *"`-Remove` followed by a redeploy of the previous build"* — assumes the previous build still exists somewhere, and it did not: `deploy-addin.ps1` overwrote it with `Copy-Item -Force` and kept nothing, `heron-backup.py` covers only the DATA class and says so, and `brain/heron_update.py` meanwhile **refuses** any release that cannot show a recorded rollback test. Built into `deploy-addin.ps1` — every deploy now keeps the install it replaces in `%LOCALAPPDATA%\Heron\install-backup\<release>\` with a `replaced.json`, and `-Rollback` puts it back. **Proved as a round trip with a genuinely different build**, not a file copy checked by eye: a second build at `-p:Version=0.1.1` deployed over the good one to stand in for a bad update, then rolled back, then every file hashed — **2020 22 files restored, 0 differing; 2024 22 and 0; 2027 15 and 0**, manifest restored all three times. The counts differ because the Framework releases carry `System.*` assemblies .NET 10 has in the box and 2027 carries a `deps.json` they do not. **What it does NOT prove, said plainly:** it proves the FILES come back, and no more. The restored install is byte-identical to the one proved to load, so it loads for the same reason — **inference, not an independent observation**, and no release was restarted a second time to make it one. **The interesting case this row named — somebody forgetting to close Revit — is now guarded on `-Remove` and `-Rollback` too**, which it was not: `Remove-Item` on a loaded assembly fails part-way through the folder and reads as a broken uninstall rather than as *"close Revit first"* |
| **A14** | On the PC: `python tests/test_context.py`, then `for %t in (tests\test_*.py) do @python %t >nul 2>&1 || echo FAIL %t` | **That the suite total is the same number on Windows as on Linux.** It was not: `test_context` failed there on one check — *a part read from disk cites the file* — because the check compared a source against a hardcoded `/` while the value arrives from `heron_fragment.repo_relative()`, which is `os.path.relpath` and spells it `brain\fragments\...`. **So the machine saw 38 of 41 where every document promised 39**, and the `heron-ship` skill's whole job is telling a later session which failures are theirs. Fixed 2026-09-12 by normalising at the comparison — the same choice `heron_fragment.fingerprint()` already made for the same reason, and NOT inside `repo_relative()`, whose value is written into the store's `fragments.folder` column. Pass looks like: `test_context.py` **exits 0 on Windows**, and the failing set is exactly the three that need the MCP SDK and a built .NET test host — no fourth. **Fixed on Linux, where the defect cannot appear**, so this row is the proof, not the fix. **PARTLY RUN 2026-09-12 on the owner's Windows checkout, and the answer is that it does NOT pass yet** — `check-gaps.py` reported **three** unfinished suites and **not one of them is the MCP-SDK-or-.NET trio this row predicts**: `test_ingest` dies on `UnicodeEncodeError: 'charmap' codec can't encode character '→'` — it prints `→` and the cp1252 console cannot, **the same Linux-only assumption as the `/` this row was opened for**; `test_reachable` fails **five** checks, having been recorded as *fixed rather than excused* on 2026-09-12; and `test_document_retrieval` fails **one real assertion** — *the best hit is the thickness clause 9.1.1, not the section above it*. **That third one is NOT this row's kind of defect and was filed here in error on 2026-09-12, corrected the same day by testing it instead of reasoning about it:** forcing the fallback with `HERON_EMBED_MODEL=definitely/not-a-real-model-xyz` on **the same machine, same OS, same run** makes it **exit 0**, and the default `model` backend makes it **exit 1**. It is a **backend** difference, not an operating-system one — CI passes it only because CI cannot reach `huggingface.co` and falls back to `lexical`. Its real record is the **2026-09-12 section of [`brain/retrieval-history.md`](../brain/retrieval-history.md)**. The other two fail on **both** backends and do belong here. `test_context` itself was not re-run. **Recorded, not fixed** **`test_context.py` ITSELF WAS RE-RUN 2026-09-19 ON THE OWNER'S WINDOWS PC AND EXITS 0**, which is the one half of this row recorded as *"not re-run"* and is now settled — the `/`-versus-`\` fix from 2026-09-12 holds on the machine where the defect can actually appear, and the run reports `every one of the 395 cases.yaml files yields an abstract`. **The other three suites this row names were NOT re-run and nothing here claims anything about them.** |

| **A15** | On the machine that can reach `huggingface.co`: `python brain/heron_embed.py` (expect `Backend: model`), then re-ask the **twelve questions** of the Stage 0b run at 360 fragments and record every column | **Whether a confidence floor can be derived at all — which is what `R-56` to `R-59` stand on.** Carried in from `docs/work-notes/plans/rag/03-working-note.md` as `W-8` when that note was retired 2026-09-12; it was the only record of it. [R-60](work-notes/plans/rag/01-requirements.md) says the floor comes from a measurement and **from nothing else**, and on the `lexical` backend at 360 fragments the measurement **does not separate a BIM question from a question about cats**: twelve questions, **every column overlapping**, and *"how do I bake sourdough bread"* holding the widest winning gap of all twelve. Two reasons, both structural — **reciprocal rank fusion keeps order and discards strength**, so the fused score never could carry it; and `heron_embed`'s own docstring says the built-in backend **"IS NOT MEANING"**. Pass looks like: the twelve questions on `model`, with the BIM columns and the cat column **visibly apart**, recorded in [`brain/retrieval-history.md`](../brain/retrieval-history.md). **A fail is worth as much as a pass** — if the trained backend does not separate them either, then `R-56` to `R-59` are not blocked on the network, they are wrong, and [R-55](work-notes/plans/rag/01-requirements.md) forbids moving a threshold to make that look better |
| **A16** | On the PC, with a MODIFY fragment and something selected: `HERON_CLIENT_ID=<one id> python mcp/client/heron_bridge_client.py fragment <name> --write` — **no `--apply`** — then read the element's own value back, not the count | **THE ROLLBACK, IN FRONT OF A MODEL. The oldest open wound in this repository, and no container could close it.** Three rollbacks failed before: the worst took a model from **9,628 placed elements to 3,966 with every floor plan gone**, the third was **seventeen renamed sheets** — so it was never about size. `SafeRollBack` returned `void`, a group whose status was not `Started` skipped silently, and a `RollBack()` that threw was swallowed, so **a failed rollback and a clean one produced byte-identical output**. Fixed 2026-09-09, deployed 2026-09-10, and **never seen working**. **DONE 2026-09-12, on the owner's PC, against `Project1 work_ajmal.al` (Revit 2024, 3,447 elements) — `align-mep-elevation`, 9 ducts, `edge=top targetZ=20`.** It ran for real (`aligned 9`, `notCurveBased 0`), and **three independent witnesses agree**: Heron said *"NOTHING WAS KEPT... Revit reported the transaction group rolled back"*; the ducts read **3100.0 / 2950.0 / 2800.0** before and after, unchanged, **read off the Properties palette rather than counted** — the three failures prove a count cannot see an edit; and **Revit's own dirty flag stayed clean**, which is the one witness with no reason to agree with Heron. Held twice, through two paths — the direct `fragment --write` run and a `validate --write` run with `--setup select-by-category-name --setup set-selection --keep-chain`. **WHAT THIS DOES NOT SAY.** The **negative case could not be arranged**: `Duct Tags` resolves but holds **0 elements** in that model, so the refusal would have been empty for the wrong reason, which is not evidence. **`--apply` was never sent**, so the KEEP half of *preview → apply → rollback* is still unwitnessed and `D1`–`D6` remain untouched. **9 ducts is not 9,628**, and this exercised the **fragment executor's** group (`RevitFragment.cs`), **not** the move path's (`RevitWrite.cs`). `write.enabled` was set true for the run and **back to false afterwards**. Evidence: `brain/proof-drafts/runs/align-mep-elevation.json` |
| **A16b** | Run ONE rename through `validate --write` ten times in a row, reading the name back after each with `find-views`. Then the same for a move. Find out whether the failures cluster or scatter | **A16 IS NOT THE GENERAL CLAIM IT READS AS, AND 2026-09-13 SHOWED IT.** A16 watched a MOVE — `align-mep-elevation`, 9 ducts — and it held, twice. **It was never evidence about a rename, and it was read as if it were.** On 2026-09-13 `rename-elements` was run through `validate` with **no `--apply`** against 11 FloorPlan views in `Snowdon-scratch_ajmal.al`. Heron printed *"Revit reported the transaction group ROLLED BACK afterwards"* and **the 8 views were still renamed** — `L2` read `HERONL2` — checked minutes later from a separate process. **Element count 9,638 before and after**, which is why no count-based check saw it, exactly as the three earlier failures. **THEN IT GOT WORSE.** The same fragment, same job file, minutes later, planned `HERONL2` → `HERONHERONL2` (the double prefix is recorded verbatim in the signed proof's `planned` list) and **that one rolled back correctly**. And in the same sitting `duplicate-views` created 11 views and `hide-elements` hid 191 ducts — **both rolled back cleanly**, read back and confirmed. So it is not size, not the kind of edit, and not creating-versus-naming: **it is intermittent**, which is harder to design around than a reliable failure because it earns trust before it breaks it. Full detail and the disproved narrowing: FRAGMENT-ISSUES row 19 | **OPEN, 2026-09-13.** Until this is settled, every MODIFY proof's *"nothing was kept"* is a report of what Revit SAID, not of what happened — and two proofs signed on 2026-09-13 carry that caveat in writing inside the fragment |
| ~~**A17**~~ | ~~On the PC, from a clean tree, **in PowerShell** — `git checkout -- .; foreach ($f in Get-ChildItem tests\test_*.py) { python $f.FullName *> $null }; git status --short`~~ **— DONE 2026-09-15 ON THE OWNER'S PC, AND IT PASSES. All 99 tests ran (18:10:34 to 18:25:27, exit 0) and the tree was clean after every single one** — the run reported the changed-file set after each test and it never once differed from the empty baseline, so this is 99 observations rather than one at the end. `git status --short` **empty**, and the fragment **byte-identical** to before the suite: `sha256 b2599e4878a6f72d`, 4384 bytes, 102 CRLF, **0 double-CR**, the same numbers measured before the run. **So the reported phantom modification does not exist on current HEAD, and the cause named in the brief was disproved separately** (see below — `core.autocrlf=true` makes a CRLF-vs-LF difference invisible to git here, so the check that raised it could never have detected a write). **Nothing was fixed, because nothing was broken** — no test was changed and **no `.gitattributes` was added**, which the brief was right to forbid. **WHAT THIS DOES NOT SAY:** it is one run of the suite in file-name order on one machine, so it cannot speak for a test that only writes on a different order, a different Revit, or a failing path — and two tests **do** touch the live library without dirtying it (`test_scope_store` creates and removes `brain/fragments/zz-broken-temp/`, `test_embed` `utime`s a real fragment). Neither is the reported fault; the first is still worth a decision.** Not the bash form.** This row first carried `for t in tests/test_*.py; do ...; done`, which PowerShell 5.1 rejects at parse time (`&&` is not a valid statement separator, `<` is reserved, `/dev/null` is not a path) — so **nothing ran at all, including the `git checkout` at the front**, and the failure looks alarming while being harmless | **WHETHER THE SUITE DIRTIES THE TREE AT ALL - and the reported cause is already DISPROVED.** Opened 2026-09-15 from a brief reporting that the suite left `brain/fragments/filter-elements-by-type/fragment.yaml` modified, diagnosed as LF in HEAD against CRLF on disk. **That diagnosis cannot be right on this machine.** `core.autocrlf=true` is set in the SYSTEM gitconfig (`Git/etc/gitconfig`, the Git-for-Windows default) and **no `.gitattributes` is tracked anywhere in the repo**, so git normalises CRLF to LF on the way in and a pure line-ending difference **cannot** show as modified. Measured on an untouched tree with no test run: HEAD blob **4282 bytes, 0 CR**; working copy **4384 bytes, 102 CRLF**; `git status` **clean**. That difference is the PERMANENT resting state of every checkout here - true before the suite and after it - so `cmp` reporting DIFFERENT while `diff <(tr -d '\r' ...)` reports nothing is **evidence of Windows, not of a test writing**. Pushed each variant through git's own clean filter to find what would genuinely dirty it: `lf`, `crlf` and `mixed` **all hash to the HEAD blob** `0462ad5d`; only `\r\r\n` differs (`0a877af4`). Double-CR needs a CR-preserving read paired with a translating write, and a static sweep finds **no such pair on this path** - there are no `newline=''` reads in the repository, the only two `newline=''` writes (`tools/resign-machine-proofs.py:100`, `tools/generate-decision-summary.py:159`) are the safe non-translating kind, and the **only** writer of a real `fragment.yaml` in the whole codebase is `heron_validate.accept`/`restamp`, which reads and writes both in text mode and so round-trips CRLF unchanged. **WHAT IS NOT SETTLED, AND IS WHY THIS ROW IS OPEN:** the run that would actually answer it - every test alone, fingerprinting the fragment after each - **was killed at roughly 10 minutes of 99 tests** when the sitting ended. **It had reported no change to any byte up to that point and the tree was clean**, but it never reached `DONE`, so *"no test writes to that file"* is **unproven and must not be quoted as proven**. Pass looks like: the suite ends with `git status --short` **empty**. **A fail is worth more than a pass** - it names the test, and that test is then the real defect the brief was reaching for: a test writing into `brain/fragments/`, which is library source and not scratch. **Do NOT close this with a `.gitattributes` rule for that one file** - the file is not the problem, and a rule would hide whatever is |
| **A18** | On the PC: `foreach ($f in Get-ChildItem tests\test_*.py) { python $f.FullName *> $null; if ($LASTEXITCODE -ne 0) { $f.Name } }` — then fix `test_builder` and `test_instructions`, which are **NOT** the machine | **TWO SUITES ARE FAILING ON REAL ASSERTIONS AND NOTHING HERE SAID SO. Measured 2026-09-15, 19:29 to 19:43: 99 suites, 3 failing.** One is expected and already recorded — `test_bridge_roundtrip` needs a built .NET test host ([HANDOVER §Tests](HANDOVER.md)). **The other two are not on that list and need no special machine.** `test_builder`: *"one file is planned, and it is not a test"*, then `KeyError: 'brain/heron_duct_sizing_reviewer.py'`. `test_instructions`: two checks — *"a duplicate id is refused by name"* and *"and both files are named, so neither is the silent loser"* — while its own 16 evaluation assertions still pass. **Both `tests/test_builder.py`+`brain/heron_builder.py` and `tests/test_instructions.py`+`brain/heron_instructions.py` were last touched by `45a6746` (*"The seams first, then the departments they made cheap"*, #141)**, so that is where to look first. **NOT FIXED, AND DELIBERATELY** — the owner's instruction on 2026-09-15 was to note them and not fix them; they are another session's work, and quietly patching someone else's failing test buries the problem instead of closing it. **HOW THIS WAS MISSED UNTIL NOW, WHICH MATTERS MORE THAN THE TWO FAILURES:** `A17`'s command runs every suite and checks `git status`, and it **throws every exit code away** — `python $f.FullName *> $null` with no `$LASTEXITCODE` test. It answers *does the suite dirty the tree* and **cannot see a failing test at all**, so a green A17 says nothing whatever about pass or fail. *(This row used to say `tools/check-gaps.py` **returns 1 on a perfectly healthy tree and a reader learns to ignore it**. That was WRONG, and believing it would have cost a day building a second runner. `check-gaps.py:131-181` runs every suite, reads every return code, prints `FAIL <name>` and appends it to UNFINISHED, handles a `SKIPPED=3` sentinel and a 300s hang bound. It would have named both failures in plain sight. The instrument existed and worked - **nobody had run it on this PC**, which is what `K1` below asks for and it was still open. Procedural, not instrumental.)* **BOTH SUITES ARE NOW FIXED - 2026-09-15.** One bug in two files, and neither was a regression: `git log` shows `45a6746` CREATED all four files, so neither check had ever passed on Windows. `brain/heron_builder.py:224` built a dict key with `os.path.join` (`brain\heron_x.py`) while line 257 built `test_owed` with a literal `/` - one function, two conventions, and the test asserts both in the same shape. `brain/heron_instructions.py:127` called `os.path.relpath` unguarded BEFORE the duplicate-id check, and on Windows that RAISES across drives (the test's `mkdtemp()` is on `C:`, the repo on `D:`), so the loop died on the first file and the duplicate check never ran; the test's bare `except ValueError` could not tell that crash from the module's own deliberate refusal, which is why it reported itself as a duplicate-detection failure. Fixed with `heron_fragment.repo_relative()`, the answer this repo already owns. **`test_instructions` 27 assertions, `test_builder` 49 - and about 25 of the builder's had NEVER RUN on Windows, because the KeyError killed the process at check 141.** Pass looks like: **0 failing suites other than the recorded machine ones**, and the count derived rather than read off this page |
| **A19** | On the PC with a real model open: prove the **twenty-three fragments added 2026-09-19** the ordinary way - `python tools/batch-prove.py` with a job file, or one at a time - reading each one's `tests/cases.yaml`, which already carries both legs | **WHETHER ANY OF THEM DOES THE RIGHT THING. Nothing here knows.** All twenty-three compile against the real Revit reference assemblies on every release they claim, and that is the API surface agreeing and nothing more - a compile is not a proof (D-30). Pass looks like: a recorded proof each, with a named model, a positive case, a NEGATIVE case and a fingerprint. **Four of them carry a trap that only a model can settle, and those are the ones to run first because a wrong answer there is silent:** `measure-perimeter` must agree with Revit's OWN room schedule with a Perimeter field added - if it disagrees, the boundary location is not at the wall centre and the fragment took the wrong route; `set-datum-extent-type` claims that setting an end back to `Model` reaches EVERY view showing that datum, which is either true or a sentence that will mislead somebody into changing twenty plans; `move-annotation` on a DIMENSION may simply land in `notMovable`, which is an honest answer worth knowing before anybody relies on it; and `place-structural-family` must put columns in reading as STRUCTURAL in the schedule - if they read non-structural it has reproduced the exact defect it was written to fix, while placing the right number of things in the right places. **A fail is worth as much as a pass here.** Two of the twenty-three cannot be proved at all yet: `save-document` and `sync-with-central` are `PUBLISH` risk, which Phase 0/1 refuse unconditionally, so they are unreachable by design until the register says otherwise - do not set `write.enabled` to reach them. **And `create-radial-dimension` needs Revit 2025 or later**: its contract claims 2025-2027 only, because `RadialDimension.Create` is absent from 2020-2024 and no project-side radial route exists there at all |

## Group K — the agent spine, which has only ever run here

**Added 2026-09-14** with the agents of the [seam-first build
order](work-notes/plans/agent-build-order-2026-09-13.md) — the agent contract, the kernel seams, the
agent spine and the factory. **Every assertion in them passed on Linux, in a container, against a
dictionary a test wrote itself.** *(This paragraph said "the fourteen agents" and "481 assertions"
for the few hours between the agent spine and the factory. Both are derived now, because a number
typed into prose is the one failure this whole file is a register of:)*

```bash
ls tests/test_{contract,instructions,events,router,availability,budget,secrets,agents,workforce,\
sandbox,validation,deployment,retirement,chain,hr,architect,trainer,mentor,evaluator,optimizer,\
creator,builder}.py 2>/dev/null | wc -l          # the suites this group covers
for t in tests/test_*.py; do python "$t"; done | grep -c '^  ok'   # every assertion in the tree
``` `tests/test_chain.py` narrows that a little: it walks one agent through
its whole life on the real modules, each step taking the previous step's output, so the parts do fit each
other. **Fitting each other is a smaller claim than working**, and these four rows are the difference.

**Three of the four need no Revit** — two need a Windows box and one needs a network, which is the shape
`A6`, `A14` and `A10` already have. The fourth cannot be *attempted* until something is PROVEN against a
real model, because a real proof is precisely what it is waiting to be handed.

> **That sentence drifted the same day it was written — the eighth time in this file — and it is left
> standing.** `K5` was added within hours of `K1`–`K4`, so "the four" became five and "the fourth" became
> the fourth of five. Nothing about the individual rows is wrong; the sentence counting them is, which is
> the failure this file exists to record. It needs neither Revit nor Windows nor a network — **it needs
> the first thing that writes a flag table**, which is a fourth shape none of the three above has. Derive
> the group before quoting it:
>
> ```bash
> awk '/^## Group K/,/^## Group J/' docs/NEEDS-CHECKING.md | grep -o '^| \*\*K[0-9]*\*\*'
> ```

| ID | Do this | Pass looks like |
|---|---|---|
| **K1 — DONE 2026-09-15, AND IT FAILED ITS OWN PASS CONDITION, WHICH IS WHY IT EXISTED** | On the PC: `python tools/check-gaps.py` — **run, 188 suites, exit 1.** The pass condition was *"no new name among them"*. **There were two new names**, neither wanting a machine: `test_authoring.py` and `test_mcp_stdio.py`. `test_authoring` was the Windows cross-drive `os.path.relpath` defect for the **third** time that day, and a **fourth** unbitten copy was found beside it in `heron_tag.py`; `test_mcp_stdio` asserted a limit that #144 had removed. Both fixed, both recorded in [HANDOVER.md](HANDOVER.md). `check_steps()` contributed **nothing**, so the exit code was driven entirely by those two and returns 0 once they are green — which also disproves the idea that this tool's exit code is uninformative. | **That every new suite passes on Windows too, and `A14` is why this is a row rather than an assumption.** That row exists because the suite total came out **38 of 41 on Windows** where every document promised 39 — a comparison against a hardcoded `/`, in a check written on Linux where it cannot fail. Every new suite in this group was written on the same kind of machine. Every path they build goes through `os.path.relpath(...).replace(os.sep, "/")`, which is the fix `A14` landed, so the expectation is that they pass — **and an expectation is exactly what this row is for.** Pass looks like: `check-gaps.py` names the same unfinished set this container names — two suites wanting the MCP SDK, one wanting a built .NET test host — with **no new name among them**. A FAIL is worth as much: it names the first line in the agent spine written for one operating system and believed of both |
| **K2** | On the PC: `python tests/test_secrets.py`, and read the line *"case is compared the way this platform compares it"* | **The one line in the credential store written FOR Windows, which has only ever run where it does nothing.** `heron_secrets._inside()` compares a store path against the workspace with `os.path.normcase(os.path.realpath(path))`, component by component — and on Linux `normcase` is the **identity function**, so the case-folding half of that check has never once folded a case. The suite knows this and asserts the platform's own answer rather than a fixed one, which is honest and is also why a green run here proves only the Linux branch. Pass looks like: `test_secrets.py` **exits 0 on Windows**, where that same line now asserts the opposite thing — a store named in different case IS inside the workspace, and a handle for it is refused. `realpath` following a **junction** rather than a symlink sits in the same two lines and is untested for the same reason; a `.secrets` junction pointing into the checkout is the case to try if one can be made |
| **K3** | On a machine with a network, **once one model adapter exists**: `python brain/heron_availability.py` and `python brain/heron_budget.py` against that adapter rather than against their built-in demos | **A provider's real reply, which neither the availability judge nor the budget ledger has ever seen.** `judge()` sorts a probe into unreachable / auth / slow / too small / `PROBE_FAILED`, and `record()` takes **only what a provider reported**, because Heron has no tokeniser ([D-58](DECISIONS.md)) — both written from documentation, against probes a test wrote. **An adapter exists since 2026-09-17** - [`brain/heron_provider.py`](../brain/heron_provider.py), local, Ollama, stdlib only ([docs/37](37-the-provider-adapter.md)). This row is therefore RUNNABLE now and no longer names something to build. **What it still needs is Ollama installed with one model pulled** - nothing is listening on this machine, which `tests/test_provider.py` proves by getting a real refused connection and a real `UNREACHABLE` out of `judge()`. **What that suite already settled, over a real socket rather than a fixture:** a 401 judged as auth rather than down, a late answer judged slow rather than down, `auth` left absent when nothing asked and `judge()` refusing to read that as a yes, and usage passed through under the provider's own field names with `usage_tokens()` refusing to invent a number when none was reported. **It also found one real shape mismatch, which is what this row was for:** `counts_as_evidence()` reads `adapter` as well as `degraded`, and the adapter's first result carried neither, so a perfectly good answer would have counted toward nothing, silently. **What is still owed and cannot be faked:** a live Ollama's actual usage field names against what `record()` asks for - the stub can only be as right as the documentation it was written from, which is the same sentence this row already says about the three agents underneath. Pass looks like: a reachable provider judged reachable, a real **401** judged as auth rather than as down, a real timeout judged slow, and **the usage field names in a real reply matching what `record()` asks for**. A FAIL here is the cheapest it will ever be — a shape mismatch found before any number depends on it |
| **K4** | Once any fragment or agent is PROVEN against a real model, hand its proof draft to the PROVEN gate rather than a dictionary: `python brain/heron_deployment.py` walks the demo, and the real call is `activate(agent, "PROVEN", evidence={"real-runs": [<the proof's runs>]})` | **The PROVEN gate meeting real evidence, which is the one gate no container can feed.** It refuses a count and wants recorded runs against a **named** real model, each with a negative case and a staleness fingerprint, not degraded and not sandboxed ([D-30](DECISIONS.md)) — and every run it has judged so far was written by `tests/test_deployment.py`. The register has **no agent above DRAFT**, so this ladder has never been climbed by anything real. Pass looks like: a proof draft from a real Revit sitting — the shape `brain/proof-drafts/runs/*.json` already has, which `A16` produced — accepted **without the record being reshaped to fit**, and PRODUCTION still refusing until a person signs. **If the proof has to be edited to get past the gate, the gate is wrong and this row has done its job**: those fields were chosen from D-30's wording, never from a file a real run produced |
| **K5** | Once anything writes a real flag table — the add-in's configuration, or whatever docs/21 §9 turns into — run `python brain/heron_safemode.py` against **that file** rather than its built-in demo, and read the `could_not` list | **The flag entry shape has never met a real configuration, and Safe Mode's whole answer turns on one field in it.** `HERON-OPS-FLG-009` records a change as `{to, was, by, at}` appended to a flag's `changes` list, and `HERON-OPS-SAF-008` decides what to undo by comparing each `at` against the moment the user names — **as text**. That works for `2026-09-14T10:00Z` and fails closed for anything else: an epoch number, a .NET `DateTime.ToString()`, a local time with no zone, all land in `could_not` as `UNDATED_CHANGE` rather than being compared wrongly. Fail-closed is the right behaviour and is also why this row exists: **a Safe Mode that refuses every flag it is given is indistinguishable from a healthy system to anyone reading only the counts.** Nothing in this repository writes a flag table at all — the three names in docs/23 §11 appear only in the two modules and their two suites, which is the whole of the evidence. Pass looks like: the real table's timestamps compared rather than refused, `could_not` **empty or explained**, and a flag swept back to what it actually held. A FAIL names the mismatch on the day the store is written, when changing its format still costs nothing |

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
| ~~**J1**~~ | ~~Select a few ducts, then `prove count-elements`~~ | **DONE 2026-09-07.** Five ducts selected; `<- elements from the selection (5)`, `count 5`, `countedNothing false`. The number matched the selection and the answer said where it came from. **The first fragment needing an input ever to run in this project** |
| ~~**J2**~~ | ~~Select nothing, then run J1 again~~ | **DONE 2026-09-07 — the row that mattered most, and it held.** `needs_unbound`: *"'elements (IList&lt;Element&gt;)' was never supplied. Nothing is selected in Revit, and no earlier fragment in this session left a value of that name. Running anyway would report 0 results, which reads as 'there was nothing to find' rather than 'nobody was asked'."* **Exit code 1**, so a script cannot read it as success either. It did NOT report `count 0` |
| ~~**J3**~~ | ~~`prove <a filter> count-elements`~~ | **DONE 2026-09-07, with `list-levels` rather than `filter-elements-by-category`** — the latter needs caller values and is `J5`. `list-levels` found 2 levels; `count-elements` then read `<- elements from list-levels (2)`, `count 2`. **Five ducts were selected at the time and it did not use them**, which is the part worth having: the chain takes precedence over the selection and says which it used. [D-29](DECISIONS.md)'s filter-feeding-action, running for the first time |
| ~~**J4**~~ | ~~`prove unjoin-geometry` with a selection~~ | **DONE 2026-09-07.** `needs_unbound` naming both `first (IList<Element>)` and `second (IList<Element>)`: *"There is a selection, but this fragment needs 2 separate sets of elements and one selection cannot say which is which."* It counted the ambiguity rather than picking one |
| ~~**J5**~~ | ~~`prove` a fragment needing a category or a name~~ | **DONE 2026-09-07.** `filter-elements-by-category` refused with `needs_request_values`, naming **both** `category (BuiltInCategory)` and `levelId (ElementId)` with their types. **278 fragments are behind this**, and it is the next unlock — bigger than this one was |
| ~~**J6**~~ | ~~Run a batch, then run another, and check it does not inherit~~ | **DONE 2026-09-07, and run the sharper way round.** After the `J3` batch left 2 levels carried, a FRESH `prove count-elements` returned `<- elements from the selection (5)` — the five ducts, **not** the two carried levels. Had `chain: reset` not fired it would have said `from list-levels (2)`, so the two outcomes are distinguishable rather than both plausible |
| **J7** | With two models open, select in one and `prove --in "<the other>" count-elements` | **NOT RUN — only one model was open.** It **must refuse** rather than bind a selection belonging to a different document. This is the case `READ_SELECTION`'s own proof flagged as unresolved: aimed off-screen it returns a bare `0`, and *"whether the selection was genuinely cleared, or whether a UIDocument built for an off-screen document cannot see a selection at all, was NOT established"*. The binding refuses instead of resolving it, and **that refusal is still unproven** |
| **J8** | Break a fragment's C# deliberately, with a bound need, and run it | **NOT RUN.** It would mean damaging a library fragment to see the message, and no fragment happened to fail on its own. The compile error is written to say how many **generated lines** sit in front of the snippet; **that wording has never been seen** |
| ~~**J9**~~ | ~~Write a fragment whose lambda calls itself with no floor - `Func<int,int> f = null; f = n => { return f(n + 1); };` - and run it through `run_fragment_read`~~ | **RUN 2026-09-16 - PASSED.** Revit 2024, session 33812, started 20:13:52 against an add-in deployed 19:54:19, so the guard was in the loaded build rather than only on disk. Verbatim: *`[fragment_threw]` 'zz-j9-stack-guard-probe' threw while running: **Insufficient stack to continue executing the program safely.**'* - which is `InsufficientExecutionStackException`, the catchable one `EnsureSufficientExecutionStack` raises, arriving through `RunScript`'s ordinary catch. **Revit survived: same PID before and after, `ping` 1 ms, and a real read returned 3,565 elements.** The probe fragment was deleted straight after and the library is back to 372. **THE MODEL WAS NOT BLANK** - `test projject.rvt`, 3,565 elements, open throughout. That was not the arrangement asked for and it makes the result stronger rather than weaker, but it is said out loud because a FAIL would have taken that session with it. **WHAT IT STILL DOES NOT COVER:** an EXPRESSION-bodied recursive lambda, which `HeronStackGuard` leaves alone on purpose ([row 104](FRAGMENT-ISSUES.md)). That case is unguarded and unrun - running it is expected to end Revit, which is why it was not run unasked |

**None of this proves any fragment does the right thing**, and the six ticks above must not be read as if
it did. It proves the inputs arrive, or are refused with a reason. [D-30](DECISIONS.md) still wants a
proof with a negative case, per fragment, and that is 335 rows this group does not touch.

**`COUNT_ELEMENTS` in particular is still `DRAFT`.** It returned 5 for 5 and 2 for 2, which is evidence
and not a proof: no second route reached the same number, and the negative case above is the HOST
refusing before the fragment ran rather than the fragment handling an empty set. Whoever proves it
properly can use these numbers; they are not the proof.

## Group R — read the decisions back (no Revit needed)

**Not a test — a conversation**, and the only item in this file that is not about code behaving. It is
here because this file is what gets opened at the PC, and a review nobody is reminded of does not happen.

| ID | Do this | Done looks like |
|---|---|---|
| ~~**R1**~~ | ~~Read **D-23 to D-27** back to Ajmal~~ | **DONE 2026-08-29 — and it covered D-23 to D-43, not just the five.** All twenty-one were read back and he confirmed them. **In conversation rather than at the PC**, which is worth recording: the instruction said *at the PC*, and what that was for — him sitting with them rather than tapping yes — did happen. Three were put to him individually because they carried real consequence, and all three came back unchanged: **D-33's boundary** (Heron never invents a Revit number and does decide its own code — the line the decision file itself flagged as never actually stated by him), **D-26** (the model file never leaves; names, counts, sizes and reasoning are fine), and **D-32** (v1 both reads and writes, reading first, writing off by default). The remaining eighteen were confirmed as a block |
| **R1b** | Show him **[D-14](DECISIONS.md), the trust model**, working on a screen with his own fragments in it | He agreed the direction on 2026-08-28 — *"yes, but show me it working at the PC first"* — so D-14 stays **Proposed** until he has seen it. Use the framing that worked: a family has **a maker** and **an approval status**, and nobody would put those on one dropdown. Phase 2 may be designed against the two axes meanwhile; it may not be called settled |
| ~~**R2**~~ | ~~Pay particular attention to **D-26 and D-32**~~ | **DONE 2026-08-29.** Both were put to him as separate questions with their reversal history stated, and both were confirmed as they stand. The reason this row existed — that a first answer sharpened once its consequence was visible — held up: neither moved a fourth time |
| **R3** | Give somebody who has **never seen this repository** only `README.md`, `AGENTS.md` and [`docs/PROJECT-MAP.md`](PROJECT-MAP.md), and ask them the five below. Record which document they found each in, and how long it took | **The cold read, and it is the LAST thing the 2026-09-10 housekeeping run left open** (its §28.7). The five: **(a)** what is Heron for, and who is it for; **(b)** how many fragments are proven, and what command derives that; **(c)** which folder owns the Revit add-in, and what must never go in it; **(d)** where does work in progress live, and where did the last session stop; **(e)** what must be run before pushing. **All five were answered from those three documents alone**, every one FOUND EASILY, no question NOT FOUND — so the routes hold and the answers exist. **But the reader had already worked in this repository for a long session, and this row asks for a stranger.** That is the whole point of it and it is why nobody inside a working session can close it — the 2026-09-12 reviewer declined it for the same reason, one step further removed. Pass looks like: a person with no prior exposure finds all five, and anything they could not find is written down as a route to repair rather than as their failure |

**R1 and R2 are done; R1b needs a screen and R3 needs a stranger.** The review happened *after* Phase 2 was built rather than
before, which is weaker than intended and was the owner's own call to override. Worth recording that it
changed nothing: twenty-one decisions read back, twenty-one confirmed. The two that had been reversed
within hours on the day did not move again.

## Group B — does Revit still load

The panel was rebuilt: Heron and Bridge Status are now one split button, and a ribbon that throws
during `OnStartup` costs the whole add-in. If Heron disappears entirely, this is why.

| ID | Do this | Pass looks like |
|---|---|---|
| **B1** | Start Revit 2020 | The **Heron AI** tab is there |
| ~~**B2**~~ | ~~Look at the panel~~ | **DONE 2026-09-06, twice.** First as *two* controls. Re-checked after Emergency Stop was removed ([D-46](DECISIONS.md)): the panel showed **one** control, as intended. Revit 2020 and 2024 were both open at that moment and which one was looked at was not established — the two carry the same source, so this proves the ribbon is right, not which release it is right on |
| ~~**B2a**~~ | ~~Click the arrow under Heron~~ | **DONE 2026-09-06.** The list opens with Bridge Status in it |
| ~~**B2b**~~ | ~~Pick Bridge Status, then look at the top of the split button~~ | **DONE 2026-09-06.** Top stayed **Heron**, icon intact. `IsSynchronizedWithCurrentItem = false` does take effect in a real Revit, which a compile could not have told us. **The release it was run on was not recorded** — re-run on the other two before this counts for all of 2020-2027 |
| ~~**B3**~~ | ~~Press Heron~~ | **DONE 2026-09-06.** Connects, icon lights, goes dark again on the second press. The state indicator survived the move into a split button — which is what `IsSynchronizedWithCurrentItem = false` was there to protect, now proven from the user's side rather than from the flag |
| **B3a** | Press Heron again to disconnect, then re-open the arrow and pick Bridge Status | Says **Not connected** — the item inside the list still runs its own command, it did not become part of the toggle |
| **B4** | `python mcp/client/heron_bridge_client.py ping` then `count` | Both answer, as they did before Step 6 |

### The activity banner — [D-50](DECISIONS.md). IT HAS BEEN SEEN — 2026-09-07

**It compiles on all eight releases, 2020 through 2027, every project, 0 warnings** — `B5`, closed
2026-09-07. That was the API surface agreeing and nothing more.
**On 2026-09-07 it was finally put on a screen** — Revit 2024, `Snowdon Towers Sample HVAC` and
later `Project1 work_ajmal.al`, on the non-primary monitor — and **B6, B7, B8, B9, B10, B11 and
B13 all passed; B12 half-passed.** It appears where it
should, says the right words, holds for the right time, does not strobe across a batch, and
cannot eat a click.

**`B8` — the one that mattered — passed, and it took a bug with it.** The first ever attempt to run
the write path failed outright, on a name collision no compiler could see; that is written up
below. Once fixed, **3 ducts moved and the card went amber**, while the *preview* of the same
move seconds earlier stayed blue. Read and write are told apart correctly.

**What is STILL not settled, and must not quietly become "done":**

- **Nothing has ever been frozen.** Every job measured took **3–339 ms**. The pre-`Raise`
  *ordering* is proved — the reading card was seen before the finished card, which could not
  happen if it were raised late — but *"a card visible through a long freeze"* has never been
  observed, because no job here was slow enough to freeze anything.
- **150% DPI has never run.** Half of `B12`. The monitor used reports 96 DPI.
- **Only Revit 2024.** All eight releases compile; one has been seen.

**The compiler was there all along.** This work was written believing the container had no .NET SDK,
because `dot.net`'s installer script is blocked by the egress proxy — but **Ubuntu packages it**, and
`apt-get install dotnet-sdk-8.0 dotnet-sdk-10.0` puts it on the PATH in about a minute. The 10.0
package is the one that carries the WindowsDesktop targets, so it is what builds 2025–2027; 8.0 alone
stops at 2024. That is [docs/30](30-compiling-away-from-windows.md)'s own finding, re-proved from
a different container — **do not conclude "no compiler here" from a failed download again.**

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**B5**~~ | ~~`python tools/check-compile.py`~~ | **DONE 2026-09-07, on Linux, all eight releases.** `Heron.Core`, `Heron.Bridge`, `Heron.Revit.Addin` and `Heron.Bridge.TestHost` — **ok on 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027**, and the add-in builds **0 warnings, 0 errors** on 2024. The first WPF-heavy file in the add-in does not break the build on any release it claims. **It says nothing about whether the banner appears, where, or in the right colour** |
| ~~**B5b**~~ | ~~Build on Windows and deploy into Revit~~ | **DONE 2026-09-07, on the owner's PC.** `dotnet build -c Debug -p:RevitVersion=2024` — **0 warnings, 0 errors**, the first time the banner has been compiled by the **Windows** toolchain rather than Linux/NuGet reference assemblies. Deployed with `tools/deploy-addin.ps1 -RevitVersion 2024` while Revit was closed, and the **deployed** `Heron.Revit.Addin.dll` (68,608 bytes, was 53,760) was read back off disk to confirm it carries `HeronActivityBanner` and the literals `is reading your model`, `is changing your model`, `READING`, `CHANGING`. `ui.activityBanner` is absent from `%APPDATA%\Heron\config\heron.config`, so the **default `true`** applies and the banner is armed. **The bits Revit will load are now on disk — that is all this says. It still has never appeared on a screen; B6-B13 are untouched** |
| ~~**B6**~~ | ~~Connect, then ask for a count~~ | **PROVED 2026-09-07**, Revit 2024, `Snowdon Towers Sample HVAC` (9,628 elements), on DISPLAY1 - the **non-primary** monitor at x=-1920. Captured by screen-grabbing Revit's own window every ~33 ms while the job ran. **SEEN.** Dark card, **top centre of Revit's window**, **"Heron AI is reading your model"**, sub-line **"Counting what is in the model"**, blue **READING** chip. It appeared **before** the green finished card, which is what settles the ordering: had `Raise` come after the work there would have been no reading frame at all, and there was one. **Caveat on the word 'frozen'** - the Revit-side work was **12 ms**, so nothing was frozen long enough to see. The pre-`Raise` ordering holds; "visible during a long freeze" still wants a genuinely slow job |
| ~~**B7**~~ | ~~Watch the same card after the answer arrives~~ | **PROVED 2026-09-07**, Revit 2024, `Snowdon Towers Sample HVAC` (9,628 elements), on DISPLAY1 - the **non-primary** monitor at x=-1920. Captured by screen-grabbing Revit's own window every ~33 ms while the job ran. **Turns green**, **"Heron AI has finished"**, sub-line **"Done - 12 ms"**, green **DONE** chip. Held **1,533 ms** measured frame-to-frame, then gone - the spec said about 1.4 s. Neither firing early nor failing to fire: `End` is reached |
| ~~**B8**~~ | ~~With `write.enabled = true`, ask to move ducts and approve~~ | **PASSED 2026-09-07, after the token fix.** Revit 2024, `Project1 work_ajmal.al`, **3 ducts moved up 200 mm** - the first time Heron has ever changed a Revit model. The card went **AMBER**: amber dot, **"Heron AI is changing your model"**, sub-line **"Moving elements"**, amber **CHANGING** chip. **The distinction is proved, not assumed:** seconds earlier the *preview* of the same move showed **BLUE READING** with *"Working out what a move would do"*. Same feature, same session, same category - read and write told apart correctly, from `HeronOperationRegistry` by operation name. Found by scanning all 918 captured frames for RGB(240,163,44); amber held ~9 frames, about 300 ms, because the write itself is that fast. `write.enabled` was returned to false immediately afterwards. **The owner confirmed that model is scrap** - opened for this test, not real work - so the 3 ducts were left 200 mm up rather than moved back. The name looks like a workshared local and is not one to worry about |
| ~~**B9**~~ | ~~Click a ribbon button through the card while it is up~~ | **PROVED 2026-09-07 by reading the window itself**, which is stronger than a click and touches nothing. The live banner window - WPF, titled **"Heron AI"**, class `HwndWrapper[DefaultDomain;;...]`, 460x78 at -1190,4 - has `exStyle` **0x80800A8**: **`WS_EX_TRANSPARENT` (0x20) is SET**, so clicks pass through and it cannot eat one. Also `WS_EX_NOACTIVATE` - never steals focus - and `WS_EX_TOOLWINDOW` - stays out of Alt+Tab. **`SetWindowLongPtrW` was found on 2024**, so the clickable-banner fallback was not taken. Older releases still unproven |
| ~~**B10**~~ | ~~Ask something with a dialog open in Revit, so it refuses with `revit_busy`~~ | **PROVED 2026-09-07**, owner's Visibility/Graphics dialog left open on purpose. The card goes **RED** - red dot, red **STOPPED** chip - reading **"Heron AI stopped"** over **"Revit was busy - nothing was sent"**. **This is the case that was invisible before**: the chat got a sentence and the screen showed nothing. Run twice; the client waited the full **10.3 s** busy timeout both times before the refusal rendered, which is `revit.busyTimeoutSeconds` = 10 doing its job, not a hang |
| ~~**B11**~~ | ~~Run a batch of fragments back to back~~ | **PROVED 2026-09-07**, Revit 2024, `Snowdon Towers Sample HVAC` (9,628 elements), on DISPLAY1 - the **non-primary** monitor at x=-1920. Captured by screen-grabbing Revit's own window every ~33 ms while the job ran. **ONE steady card, no strobe.** Five jobs back to back: the banner went up once and stayed up **2,664 ms** through all five, never once returning to the no-banner frame, and changed appearance only **twice** (reading, then finished). The hide timer is being cancelled by the next `Begin`. **Needs `HERON_CLIENT_ID` pinned** or each CLI call is a new chat and the lease refuses the second - see the note under this table |
| **B12** | On a 150% display, and with Revit on a second monitor | **HALF PROVED 2026-09-07.** **Second monitor: PASSES.** Revit was on DISPLAY1, the non-primary screen at **x=-1920** - negative coordinates, the exact case that sends a naive banner to the primary screen. Measured by pixel-diff against a clean frame, the card spans x=738..1196 of Revit's 1936-wide window: centre **967** against the window's **968**, so **1 px off**. It followed Revit, not the primary screen. **150% DPI: STILL UNPROVEN** - that monitor reports 96 DPI (100%). The DPI transform has not been exercised at all |
| ~~**B13**~~ | ~~Put `ui.activityBanner = false` in `%APPDATA%\Heron\config\heron.config`, restart Revit, ask for a count~~ | **PASSED 2026-09-07.** Revit restarted at 18:47:09, ten minutes after the setting was written, so it is a genuine cold read of the value. The count answered normally - **3,435 elements in `Project1 work_ajmal.al`, 339 ms** - and **no card appeared**. Checked by scanning **all 359 captured frames** for the card's own colours: **0 blue, 0 amber, 0 green, 0 red pixels** anywhere in the banner region. The only frames that differed were the ribbon redrawing. Proves both halves: the switch is read, and the banner is not on the answer's path. Setting removed afterwards, so the default `true` applies again |

**A trap this session walked into, worth writing down.** `CLIENT_ID` in
[`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py) is `HERON_CLIENT_ID` **or a fresh
`uuid4()` per process**. So running the CLI five times in a row is **five different chats**, and the
second one is refused with *"This Revit is in use by another chat"* while the first one's lease is
still alive. That is the lease working exactly as designed, not a bug — but it makes a batch look
broken from a shell. **Pin `HERON_CLIENT_ID` to one value** for a batch driven from the command
line, or drive it from a single process, which is what *ONE PROCESS, ONE LEASE, MANY FRAGMENTS*
at line 823 of that file already says.

Separately: `validate <fragment>` refused all five fragments with *"Could not identify the active
model. Refusing to record evidence"* while plain `count` named `Snowdon Towers Sample HVAC` from
the same Revit, same moment. **Nothing reached Revit and no banner was raised**, so it cost
nothing here — but the proof harness could not name a model that the bridge could. Unexplained,
and it blocks fragment proofs from the command line.

#### It was rebuilt on 2026-09-17, and four of those answers no longer apply

Everything above was measured against a banner that lived on **Revit's own thread**. It does not
any more, and two of its claims were quietly wrong the whole time.

**The sweep never animated during a job, and could not have.** Revit draws on the thread it works
on, so while a job runs nothing on that thread can be painted. Measured with it blocked for
3000 ms: a `DispatcherTimer` on it fired **0 times** and the sweep did not advance one pixel -
**357.6 px before, 357.6 px after**. Every "it does not strobe" and "one steady card" result above
is still true; what was never true is that anything on the card was *moving* while Revit was
frozen. *Nothing has ever been frozen* above is the note that should have caught this, and did
not - because nothing slow enough was ever run.

**So the banner now runs its own STA background thread with its own dispatcher.** Same test, with
the **owner** thread blocked and the banner interrogated from a third thread: sweep moved **5 of 5**
samples, elapsed time ticked **5 of 5**, 1.1 s to 3.2 s. It is a background thread, so it can never
hold Revit's process open.

Three things were added on top: the card **names the model** on a line of its own, it shows a
**live elapsed time** that counts while Revit is frozen, and it was given a proper visual pass -
gradient sweep, entrance animation, colour transitions, a breathing lamp. Colour changes
deliberately **snap** between blue and amber rather than animating, because read-versus-change is a
safety signal and must never pass through a state where the two look alike.

Committed as `37cb6e2`. All of it was proved by driving the **real file** in a WPF harness, which is
possible only because it still has no Revit dependency - keep it that way.

**SEEN IN REVIT 2024 on 2026-09-17**, by the owner, during unrelated work. That closes the thing
that mattered most: **Revit accepted a second UI thread**, the add-in loaded, and the card drew over
a live Revit window rather than a harness stand-in. It was not inspected beyond "it came and it
looks good".

| ID | Do this | Pass looks like |
|---|---|---|
| **B14** | Read from two different models, switching between them | **PASS 2026-09-17** for the model name; the no-model half is harness-only. Two models open in Revit 2024 session 64384. This chat was pinned to `test projject`; the owner switched Revit to `Snowdon-scratch_ajmal.al` and Heron **refused** rather than answering about a model it was not pointed at (Golden Rule 20 working). After `use this model`, the card named **`Snowdon-scratch_ajmal.al`** on every read - captured from Revit's own window, not judged by eye. So the name follows the model in front, not the one opened first. **THE SECOND HALF IS NOT PROVED IN REVIT**: `Heron.Banner.TestHost shots` renders `5-no-name-known.png` with the middle line **absent** - not blank, not stale - and the two remaining lines re-centred, so the BANNER handles it. Whether the ADD-IN reports no-model when every document is closed was not tested; it needs the owner to close all models |
| **B15** | Run something slow enough to freeze Revit - a fragment that has to compile, or a large model | **PASS 2026-09-17, on the harness, corroborated in Revit.** `Heron.Banner.TestHost freeze`, built for 2024: banner on **thread 6**, host on thread 1, host BLOCKED 3000 ms. Across six samples taken during the block the card read **1.0, 1.5, 1.9, 2.3, 2.8, 3.2 s** - climbing, monotonic, never backwards - with **sweep changed 5 of 5** and **time changed 5 of 5**. Banner thread did not outlive its owner. Exit 0. **IN REVIT**, a batch against `Snowdon-scratch_ajmal.al` caught the card mid-job reading *"Running a job: list-levels - 2.9 s"*, so the clock runs there too. The 3000 ms hard freeze itself is the harness's, not Revit's - it reproduces the topology (owner window on a blocked thread) and that is what the claim is about |
| **B16** | Five reads back to back | **PASS 2026-09-17**, `Snowdon-scratch_ajmal.al`, Revit 2024, session 64384. Six `list-levels` reads with no gap between them, screen sampled at ~22 fps: **881 frames, card present in 71, in ONE unbroken run of 71**. One steady card across all six, and down afterwards - 810 frames with no card. Mid-batch it read *"Running a job: list-levels - 2.9 s"*, so it changed what it said without going away. **A SEPARATE RUN LOOKED LIKE A FAILURE AND WAS NOT**: five reads issued as separate calls seconds apart gave five runs of 26, 25, 25, 24 and 32 frames - the card correctly coming down between genuinely idle gaps. The B16 question needs the jobs truly back to back, or it measures the gaps instead. D-56 has not returned |
| **B17** | Revit on a **150%** display | Still the other half of `B12`. **HALF ANSWERED 2026-09-17, and the half that was answered is the harder one.** Revit was on a NON-PRIMARY monitor at negative coordinates - window 1936x1048 at **-1928,-8** - and the card centred at x=965 against a window centre of 968, **3 px out**, over Revit's own window and not the primary screen. That is the multi-monitor half. **THE SCALING HALF IS STILL UNRUN**: `GetDpiForMonitor` reports **96x96 (100%) on BOTH monitors**, so the code path still has not executed. Checked rather than assumed |

**Pin `HERON_CLIENT_ID`** before any of these from a command line - see the trap above, it makes a
working banner look broken.

If it misbehaves badly, `ui.activityBanner = false` in `%APPDATA%\Heron\config\heron.config` plus a
Revit restart switches it off without touching the add-in. That switch was proved on 2026-09-07
(`B13`) and nothing since has moved it.

### The write path was broken and is now fixed — found and closed 2026-09-07

**`revit_apply_move` cannot succeed, on any model, with any settings.** This is not a tuning problem
and not a Revit problem. The same JSON key means two different things at two different layers:

| Where | Line | Reads `token` as |
|---|---|---|
| [`BridgeServer.cs`](../revit/Heron.Bridge/BridgeServer.cs) | 349 | the **session** token, from the discovery file — the auth gate, checked **before anything else** |
| [`RevitWrite.cs`](../revit/Heron.Revit.Addin/RevitWrite.cs) | 103 | the **approval** token, minted by `preview_move` |

[`heron_mcp_server.py`](../mcp/server/heron_mcp_server.py) line 464 sends the approval token as
`op_args={"token": token}`, and [`heron_bridge_client.py`](../mcp/client/heron_bridge_client.py) line 240
does `body.update(op_args)` — so the approval token **overwrites** the session token that line 239 had
just put in the body. `BridgeServer` then sees a token that is not the session's and refuses.

**There is no value that works.** Send the session token and authentication passes but `ExecuteMove`
receives the wrong secret and rejects the approval. Send the approval token and authentication fails
first. The write path is structurally impossible until one of the two is renamed.

**It was never going to be caught by a compiler or a test.** Both sides compile perfectly; both read a
string called `token` from a JSON object. Only running it end-to-end against a live Revit shows it,
which is exactly what the server's own docstring warned: *"The add-in code behind revit_apply_move has
never been compiled or run."* It has now, and this is what it found.

**The fix is four edits and a rename**, keeping `token` for authentication because that is what the
bridge checks first and what every other operation already sends:

1. `RevitWrite.cs` 172 — preview returns `Json.Str("approvalToken", preview.Token)`
2. `RevitWrite.cs` 103 — `ExecuteMove(app, Json.ReadString(request, "approvalToken"))`
3. `heron_mcp_server.py` 422 — `approval.offer(reply.get("approvalToken"), summary)`
4. `heron_mcp_server.py` 464 — `op_args={"approvalToken": token}`

Then rebuild, redeploy, and **restart Revit** — the add-in half is compiled in. Worth doing in the same
restart as `B13`.

**FIXED AND PROVED THE SAME DAY.** The approval is now `approvalToken` at both ends and `token` means only what the bridge authenticates on. `preview_move` returns `"approvalToken": "4093166e70b6"`, `move_elements` accepts it, and the reply was `ok: true, moved: 3, blocked: 0, skipped: 0, warnings: 0` with a single undo entry, **"Heron: move ducts up 200 mm"**. `body.update(op_args)` now refuses `op`, `token` and `client` outright, so the same shape of mistake fails locally and loudly instead of arriving at Revit as an unexplained refusal.

**A trap that outlived the fix, and will catch the next person.** The MCP server is a long-running process. Redeploying the add-in and restarting Revit does **not** reload its Python, so `revit_apply_move` kept answering *"There is nothing waiting to be approved"* - the old code was reading `token` from a reply that now carries `approvalToken`, storing None. Nothing was wrong with the fix; the server had not been restarted. **B8 was proved by driving the bridge from a fresh process instead.** Until this MCP server restarts, its `revit_apply_move` is still running the pre-fix code.

**A second thing to decide while fixing it:** `body.update(op_args)` lets any caller silently overwrite
`op`, `token` or `client`. Renaming the key fixes today's bug; making `update` refuse to overwrite the
three reserved keys would stop the next one.

### Clearing the selection is NOT a negative case — learned 2026-09-07, on 36 fragments at once

**36 unproven READ fragments were run against `Project1 work_ajmal.al` with 5 ducts selected, and all
36 executed.** Not one failed to compile, bind or return. That is the first time most of them have ever
run against a real model, and it is worth having — but **it is not a D-30 proof of any of them**, and
the attempt to get the second leg in the same pass is what taught the lesson below.

**The selection was then cleared, and all 36 refused with `needs_unbound`:**

> *Cannot run: 'elements (IList<Element>)' was never supplied. Nothing is selected in Revit, and no
> earlier fragment in this session left a value of that name. **Running anyway would report 0 results,
> which reads as "there was nothing to find" rather than "nobody was asked".***

**The executor is right and the test design was wrong.** An empty selection does not produce an empty
answer; it produces a refusal, because the need cannot be bound at all. A refusal is not evidence that
a fragment reports honestly — the fragment never ran.

**So a fragment fed by `elements` needs a negative case shaped differently: a selection that CONTAINS
NONE OF WHAT IT REPORTS.** Ducts selected, and `REPORT_CURTAIN_ELEMENTS` returns
`curtainWallsFound: 0` — that is a real empty answer from a fragment that really ran.

And that had already happened in the same pass, which is the useful part. With 5 ducts selected:

| Behaved as a POSITIVE case | Behaved as a NEGATIVE case |
|---|---|
| `count-elements` 5, `describe-elements` 5, `read-element-level` 5, `read-mep-system` 5, `report-connectors` 5, `report-location` 5, `measure-element-lengths` 3 | `report-curtain-elements` 0, `measure-room-dimensions` 0, `measure-ceiling-height` 0, `read-space-loads` 0, `report-schedule-definition` 0, `report-sheet-title-blocks` 0 |

**Neither column is a proof on its own.** A fragment needs BOTH from its own arrangement: curtain walls
selected AND ducts selected proves `REPORT_CURTAIN_ELEMENTS`; ducts selected AND something without a
level proves `READ_ELEMENT_LEVEL`. **That is per-fragment work and it does not batch**, which is the
honest cost of D-30 and the reason 16 of 349 are proven rather than 300.

**`read-selection` is the exception and shows why:** it takes `uidoc`, not `elements`, and reads the
selection itself — so an empty selection reaches it as a real empty answer rather than an unbound need.
A fragment that is handed the selection cannot tell "nothing selected" from "not asked"; one that reads
it can.

The 36 run records are in `brain/proof-drafts/runs/`. **Their positive leg is real evidence and their
negative leg is a refusal, so drafting them as they stand produces 36 drafts that each say the negative
case was never established.** That is honest and it is not progress.

### A defect-finder cannot be proved on a model with no defects — 2026-09-07

**`CHECK_FLOW_DIRECTION` was offered as a candidate four times and rejected four times, and the fifth
selection finally explained why.** On duct taps, spaces, walls and sheets it reported
`jointsChecked: 0` — it never examined a single joint, which looked like a broken fragment. On **37
mechanical equipment items it reported `jointsChecked: 57`**, with `bothIn: 0` and `bothOut: 0`.

**It works.** It examined 57 joints and found nothing wrong with any of them. Snowdon Towers is a
well-built sample model, so there is no flow-direction fault in it to find.

**That is a NEGATIVE case with proof it really ran** — `jointsChecked: 57` is exactly the accounting
that distinguishes *"looked and found none"* from *"never looked"*. What it has no route to, in this
model or any correct one, is a **POSITIVE** case. To prove it, somebody has to deliberately build the
fault: two connectors both set to flow OUT, joined together.

**This is not one fragment's problem. 37 of the 349 are defect-finders** — `check-*`, `find-*`,
`audit-*`, `validate-*` — and **34 of them are unproven.** Every one needs a model containing the
defect it hunts. Selecting different categories in a clean model can never prove any of them, however
many selections are made, because the thing they look for is not there.

**So the library splits into two kinds of proving work:**

| Kind | How to prove it | Cost |
|---|---|---|
| **Reporters** — *"what is the level of this"* | Two selections in a rich model, one that has the thing and one that does not | Cheap. **16 proved this way on 2026-09-07 from five selections** |
| **Defect-finders** — *"where is the fault"* | A model with the fault deliberately built into it | Expensive. One arrangement per defect, and somebody has to break something on purpose |

`EXTRACT_DATES_FROM_TEXT` is a third and simpler case: `datesFound` was **0 in all five selections**,
including the 12 sheets. Either no sheet in this model carries a date in the text it reads, or the
fragment does not read what it thinks it does. **Unproven and unexplained** — and worth an hour before
it is trusted, because a date-reader that never finds a date is indistinguishable from one that is
broken.

### The owner built two faults on purpose — one was caught, one cannot be built that way

**2026-09-07. He created a duct left unconnected, and joined two ducts intending them to both push
out.** Selected 12 elements and both fault-finders were run against them.

**`FIND_SYSTEM_ISLANDS` CAUGHT IT.** `islandCount: 4`, `sourceless: 4` — four separate pieces, none of
them fed from anything. Against the 37 mechanical equipment items it had reported `sourceless: 0`. The
fault signal moved with the model, which is exactly what a proof needs.

**But it still cannot be proved under the current negative-case rule, and the reason is structural
rather than anybody's mistake.** It declares `islandOf`, `islandSizes`, `sourceless` and `islandCount`,
and **every set of elements is at least one island** — 12 sheets are 12 islands, 4 walls are 4 islands.
There is no arrangement that makes all four fields zero, short of giving it nothing, which is an
unbound need rather than an empty answer.

**So a third kind of fragment exists, alongside reporters and defect-finders: a STRUCTURE REPORTER.**
It always describes what it was given, and its fault signal is ONE FIELD among several — `sourceless`
here. "The negative case must come back empty" cannot express that, because the honest empty answer is
*"four islands, none of them sourceless"*, which is not empty at all.

**Nothing was relaxed to accommodate this.** Four rules were already loosened on 2026-09-07 and the
fourth was flagged as a habit forming; a fifth on the same day, for the same reason, is exactly what
[D-52](DECISIONS.md) warns about. **This is a gap in the PROOF METHOD, not in the fragment**, and
it wants deciding cold: does D-30's negative case mean "every output empty", or "the fault signal
absent while the fragment demonstrably ran"?

**`CHECK_FLOW_DIRECTION` cannot be faulted with ducts at all**, and that is worth writing down before
somebody else spends an evening trying. Line 82 of its implementation skips any joint where **either**
connector is `Bidirectional` — correctly, because bidirectional is not a fault. **Plain Revit duct
curve connectors ARE bidirectional**, so joining two ducts produced `bidirectionalSkipped: 8` and
`jointsChecked: 0`. The fragment was right and the fault was unbuildable.

Its positive case needs two connectors with an explicit direction — **equipment or family connectors,
both set to `Out`, joined**. The equipment selection reached `jointsChecked: 57`, so those connectors do
carry direction; none of them was wrong. **The fault has to be built into a family's connectors, not
drawn in the model**, which is a different and slower job.

### `READ_SPACE_LOADS` throws on the one selection it exists for — found 2026-09-07

**It is the only fragment of the 36 that FAILED rather than answered.** Against 16 spaces in
`Snowdon Towers Sample HVAC` it returned `fragment_threw`:

> `'read-space-loads' threw while running: Not Computed!`

**"Not Computed!" is Revit's own exception**, raised by `Space.DesignHeatingLoad`,
`CalculatedHeatingLoad`, `DesignSupplyAirflow` and their siblings when the model's Areas and Volumes
computation is off, or the space is unbounded. [The implementation reads all six of those properties
with no `try`](../brain/fragments/read-space-loads/impl/any/fragment.cs) around them.

**The bitter part is that the fragment already knows about this case and cannot reach its own handler.**
Twenty lines further down it writes:

> *"NO LOAD. Either the heating and cooling analysis has never been run, or **the space is unbounded and
> has no volume to load**"*

That branch tests the values for zero — but the property access throws before any value exists, so the
sentence has never once been printed. **A guard placed after the thing it guards against.**

**What it means in use:** ask Heron about space loads on a model where volume computation is off, and
instead of *"the analysis has not been run"* you get a crash. That is the difference between a fragment
that tells a modeller what to fix and one that looks broken.

**FIXED 2026-09-08.** A `try` around the six reads, routing a throw into the existing `noLoad` list with the reason - which is where it was always meant to end up. Re-run against 18 spaces in the same model: **`ok`, all 18 reported, none lost**, each one saying *"NO LOAD READABLE. Revit refused the figures, which it does when the model's Areas and Volumes computation is off or the space is unbounded. Turn on Area and Volume Computations, or bound the space, and ask again."* A crash became instructions.

**STILL UNPROVEN, and the reason is worth keeping.** A D-30 proof needs a POSITIVE case and this model cannot supply one: every space returns `noLoad`, because volume computation is off and no design figure has been typed on any of them. To prove it somebody must either turn on Area and Volume Computations and run the analysis, or type a **Design Heating Load** on one space in Properties - a value a person enters, which is then exactly what the fragment reads back. **The fix is verified; the fragment is not proved.**

`MEASURE_ROOM_DIMENSIONS`, proved earlier the same day, gets this right — it carries a
`volumeComputationOff` flag and reports it. The two fragments read the same models and only one of them
survives a model with volumes switched off.

### `REPORT_SCHEDULE_DEFINITION` cannot be reached by any route a person has — 2026-09-08

**The owner placed a schedule on a sheet and clicked it. The fragment skipped it.**

`Heat Recovery Unit Summary` selected → `skippedNotSchedules: 1`, `fieldNames: 0`. Not a fault in the
selection and not a fault in the fragment's logic. Clicking a schedule on a sheet selects a
**`ScheduleSheetInstance`** — the PLACEMENT — and the implementation does `element as ViewSchedule`,
which is a different object, so the cast fails and the row is skipped.

**There is no other way in.**

- **A view cannot be selected as an element.** Opening the schedule selects its rows, not the schedule.
  The Project Browser is not a model selection.
- **~~No fragment provides one.~~ THAT WAS WRONG, and correcting it matters more than the claim did.**
  `FIND_SCHEDULES` provides them — as `elements: IList<Element>`, which is why a search for provides
  *named* or *typed* schedule found nothing. **A grep for the word missed a fragment whose own routing
  note names it**: *"which schedules are there → FIND_SCHEDULES, which is what hands the schedules to
  here."* The chain route exists for a real request. What does not exist is an AUTOMATED one, because
  `FIND_SCHEDULES` needs `nameContains`, a value only a caller's sentence carries.

So the fragment is correct in isolation and **unusable in practice**: the only way a person points at a
schedule is by clicking it on a sheet, and that is the one input it refuses.

**FIXED 2026-09-08, and proved.** Selecting the placed schedule now reads it: 6 columns, 1 filter, 2 sort fields. The fix was three lines and belonged in the fragment, not in the proof method: accept a
`ScheduleSheetInstance` and resolve it through its `ScheduleId` before the cast, so that clicking the
thing on the sheet does what a modeller means by it. Not made here — this session was proving
fragments, and editing an implementation mid-proof is how a proof stops meaning anything.

**`READ_SCHEDULE_CONTENTS` had the same problem and got the same fix.** It cannot be proved the same
way: it needs `maxRows`, a value the caller supplies, so it refuses with `needs_request_values` rather
than running. Fixed and compiling on all eight releases; **unproven**.

## Group C — the gate, before anything can move

**Do not skip to D.** C3 is what proves the write path cannot fire by accident; testing the move before
it is pointless, because a passing move tells you nothing about whether the gate works.

The gate now sits at **one** place — every operation passes through it before routing, and its risk
comes from `HeronOperationRegistry` rather than from a literal inside the write path. C5 and C6 are what
prove it blocks the right level rather than simply blocking everything, which would pass C3 while being
useless.

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**C1**~~ | ~~Press Emergency Stop~~ | **CANNOT BE RUN from 2026-09-06.** The button was removed ([D-46](DECISIONS.md)). `HeronStop` and both gates survive, but nothing can switch the stop on, so this step has no way to start |
| ~~**C2**~~ | ~~Press it again~~ | **CANNOT BE RUN.** Same reason |
| **C3** | With `write.enabled` still **false** (the default — do not change it yet), ask to move ducts | **Refuses**, and names `write.enabled` and the config file path. Nothing goes to Revit |
| ~~**C4**~~ | ~~Press Emergency Stop on, then ask to move ducts~~ | **CANNOT BE RUN.** The two refusals are still written and still distinct in the code ([RevitOperations.cs](../revit/Heron.Revit.Addin/RevitOperations.cs)), but with no way to set the stop, only the permission refusal can be reached. **Untested from here on** |
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
| ~~**E11**~~ | With **Project1 unsaved and never saved**, run a read tool (`revit_select_by_category`), then `revit_change` on that same model | It **runs**. Until 2026-09-15 it refused every write with *"This chat has been working on Project1, but Project1 is in front in Revit now"* — the same name on both sides of the *but*, because the pin held `project:<uid>` from the read tool and read `title:Project1` back off the fragment reply, which was the one operation sending no key. `revit_use_this_model` could not clear it: it repins from `count_elements`, which sends the key again. **An unsaved model is the exposed case** — with no `documentPath` there was nothing to stand in for the missing key. Both halves are fixed (`RevitFragment.Report` now sends `documentPath` and `projectKey`; `DocumentPin.check` compares the strongest field BOTH replies carry) and **neither half has met a Revit**. `tests/test_document_pin.py` reproduces the refusal against the old code and is a text check, not a proof **RUN 2026-09-15 - PASSED, by the same run as E15.** Project1 and Project2 were both blank and never saved, so `revit_select_by_category` then `revit_change` in Project2 IS this row's arrangement: `Selected 2 ducts in Project2` then `CREATE_LEVEL ran in Project2.` One run, two rows, said so rather than recorded twice |
| ~~**E12**~~ | ~~Run `revit_change` in Project1, click into a **second open project**, ask for the same change again~~ **RUN 2026-09-15 - FAILED.** Pinned to Project1, switched to Project2, asked again: **`CREATE_LEVEL ran in Project2.`** It wrote a level into the model it was NOT pointed at. **Cause: `ProjectKey` is not unique.** It is `doc.ProjectInformation.UniqueId`, which comes from the TEMPLATE - Project1, Project2 and an unrelated `PIPE.rvt` open in Revit 2020 all report `8764c510-57b7-44c3-bddf-266d86c26380-0000c160`. `here != expectProject` is therefore false between any two projects from one template, which is most projects, and the guard passes the exact case it exists to stop. See the finding below |
| ~~**E13**~~ | ~~The same, but click into a **family editor** rather than a second project~~ **RUN 2026-09-15 - PASSED.** Verbatim: *"This chat has been working on a project, and \"M_Rectangular Elbow - Radius.rfa\" has no Project Information - a family, or something Heron cannot identify as the model this chat was pointed at. NOTHING was written."* The family path works because a family key is `None`, which IS distinguishable - unlike two projects, which are not |
| ~~**E14**~~ | ~~In a **fresh chat**, make `revit_change` the **first** thing asked~~ **RUN 2026-09-15 - PASSED.** Pin before: `title=None project_key=None`. Result: `CREATE_LEVEL ran in Project1.` Empty really does mean *do not check*, and no chat is bricked |
| ~~**E15**~~ | ~~`revit_select_by_category`, then `revit_change`, **same model, no switching**~~ **RUN 2026-09-15 - PASSED, and it proves less than it looks.** `Selected 2 ducts in Project2` then `CREATE_LEVEL ran in Project2.` Both tools DO now produce the same key - but every project on this machine produces the same key, so this row cannot tell a working identity from a colliding one. It is only meaningful once E11 is fixed |

### E11-E15 were RUN on 2026-09-15 and the queue kept asking for them until 2026-09-16

**Struck 2026-09-16. Nothing about them was re-run — only the mark was wrong.**

The session that ran them struck the **`Do this` cell** and left the **ID** alone. A reader
sees a crossed-out instruction and correctly reads *done*. `tools/owner-queue.py` only ever
looks at the ID — `^\|\s*(~~)?\*\*([A-Z]\d+[a-z]?)\*\*` — so for eight days it printed all
five back to the owner as work still waiting on him, beside the genuinely open `E16`-`E18`.
**Four of the five had PASSED.**

This is the same shape as the drift this file records about its own prose totals, one layer
down: **a mark a person reads and a mark a tool reads, kept in two places, updated in one.**
The prose drifts were caught by running the count; this one was invisible to the count, because
the count was the thing that was wrong.

- `E11`, `E13`, `E14`, `E15` — **PASSED**, verbatim results in each row.
- `E12` — **FAILED**, and closed rather than passed. Its re-run after the fix is **`E16`**,
  which is open. Per this file's own rule, *a struck row is not automatically a passed one*.

**If you strike a row, strike the ID.** Anything else is a comment.

### What has been observed so far, and it is NOT any of E11-E15

**2026-09-15, Revit 2020 session 8084, model `PIPE` (3,287 elements, on the
owner's Desktop), add-in deployed from `claude/friendly-hypatia-196de4`.** One
model was open and it was the owner's own file, so no row above could be run:
E12 needs a second project, E13 needs a family, and E14/E15 write for real.
**No row above is ticked.**

> **Renumbered 2026-09-16.** This paragraph said *"E11 needs a second project,
> E12 needs a family, and E13/E14"* — the numbering from before #147's row
> became `E11`. See the correction under [`E12 FAILED`](#e12-failed-and-the-cause-is-that-projectkey-is-not-a-key)
> below. The observation itself is unchanged and still stands.

What WAS exercised is the **add-in half**, directly over the bridge, read-only.
Four `run_fragment_read` calls of `count-elements` against the same model at the
same moment, differing **only** in `expectProject` - which is what makes it a
contrast rather than four separate observations:

| `expectProject` | Result |
|---|---|
| omitted entirely | ran (the proving client's own path - it sends no key) |
| `""` | ran, reached need-binding - `needs_unbound` on `elements`, a LATER stage |
| the real key, `8764c510-...-0000c160` | ran, reached need-binding - the same later stage |
| `00000000-dead-beef-...` | **REFUSED** - `wrong_document`: *"the one this would have run in is PIPE. NOTHING was read."* |

So the guard is **deployed and live**; **empty really does mean do not check**,
which is E13's mechanism exercised rather than reasoned about; and **a matching
key really does pass** - worth its own row, because a guard that refused
everything would also have refused the wrong key, and the two would be
indistinguishable from the refusal alone. The sentence says *read* rather than
*written* because it was on the read path.

**And the half that makes the guard reachable a second time:** a SUCCESSFUL
fragment run (`list-levels`) now answers with `documentPath` and `projectKey`,
and that key is **byte-identical** to `count_elements`' on the same model. That
is E14's root cause measured as fixed - the reply used to carry the title alone,
so `DocumentPin` fell to its `title:` fallback, `project_key` stayed `None`, and
`expectProject` went out EMPTY on every later call, switching the guard off for
exactly the chat that needed it.

Error replies report `projectKey = None`, correctly rather than as a gap: they
return from `Json.Error` and never reach `Report`.

### E12 FAILED, and the cause is that `ProjectKey` is not a key

> **This heading said `E11` until 2026-09-16, and every E number in the three sections
> above it was one too low.** The rows were renumbered on `main` when #147's unsaved-model
> row became `E11` and pushed the four below it down; the prose was written before that and
> was merged without being re-read against the table it describes. [`the 2026-09-16 sitting`](handover-archive/2026-09-16-e11-e14-renumbered-mid-flight.md)
> warned about exactly this — *"If you remember E11 failed, that row is E12 now"* — and the
> warning did not reach the file it was warning about. **The numbers below are corrected to
> match the table; the fact that they were wrong is left here on purpose**, because a
> register whose prose and whose rows disagree is the failure this whole file exists to catch.

**Observed 2026-09-15, Revit 2024.3 session 2924, two blank projects and a
family, add-in deployed from `claude/friendly-hypatia-196de4`.**

The guard compares `RevitOperations.ProjectKey(doc)`, which is
`doc.ProjectInformation.UniqueId`. **That value is inherited from the template**,
so it does not identify a project at all:

| Document | `projectKey` |
|---|---|
| Project1 (Revit 2024) | `8764c510-57b7-44c3-bddf-266d86c26380-0000c160` |
| Project2 (Revit 2024) | `8764c510-57b7-44c3-bddf-266d86c26380-0000c160` |
| `PIPE.rvt` (Revit **2020**, a different model in a different release) | `8764c510-57b7-44c3-bddf-266d86c26380-0000c160` |
| the family | `None` |

So `here != expectProject` is **false between any two projects made from the
same template**, and the write goes through. E13 passes only because `None` is
genuinely different; E15 passes for a reason that is indistinguishable from the
bug.

**This is not fixed by tightening the comparison.** There is nothing to tighten:
both sides are equal and both are wrong.

### The direction the owner asked for, and it is already half-built

Asked on the day: *"if I open project 1 and I tell it to pin the model, you have
to work only in project 1 and refer to project 2, and I move to project 3 -
Heron needs to work on project 1... that is agentic work."*

That inverts the question from *"did the user move?"* to *"which model was I told
to work on?"*, and it does not depend on a key at all.

**`RevitFragment.Run` already accepts it.** `RevitFragment.cs:105` reads a
`"document"` argument, finds that document among the open ones by title, and
refuses with `no_such_document` listing what is open. `revit_change` simply never
sends one, so the add-in falls back to the active document.

**Demonstrated the same day, end to end, with the FAMILY in front the whole
time:** `list-levels` was run against Project2 by name to read the owner's level
(`ajmal testing level @ 12500 mm`), then `create-levels` was run against Project1
by name to add ten levels following it - `ajmal testing level - 01` at 15000 mm
through `- 10` at 42000 mm, verified by reading back (`created 10 item(s)`,
`nameRefused 0`, Project1 3 -> 13 levels, **Project2 untouched at 5**). Every
reply reported `wasActiveDocument = False`.

**The remaining question was ambiguity, not mechanism, and it now has a rule.**
Two open models can share a TITLE, which is the case `DocumentPin` was built
around.

### Done in code the same day, and NOT yet run against Revit

`revit_change` now sends `document` (the pinned title) and `documentPath` (the
pinned path) with every write, so the add-in works on the model the chat was
pointed at instead of falling back to the active one. `RevitFragment.Run`
resolves the path FIRST - unique among open models - and the title second.

**A title that matches TWICE is refused**, `ambiguous_document`, rather than
letting the last document round the loop win. That silent tie-break was already
there and is the worst answer available on a path about to commit: it cannot be
told from a correct one, and which model it picks depends on the order Revit
hands its documents back.

`no_such_document` and `ambiguous_document` were both added to the failure
table. `no_such_document` PREDATES all of this and was never in it - the same
gap `wrong_document` had, sitting unnoticed until its sibling was added beside
it. Both return above the transaction group, so both are REFUSED rather than
an unknown outcome that tells the user to go and check the model.

`expectProject` stays. It is no longer the mechanism, and it still catches the
family case (E13) and any two models whose keys genuinely differ.

**E16 to E18 below are what this owes.** Nothing in this section has met Revit:
the owner was working in Revit 2020 when it was written, and it was not
deployed.

| ID | Do this | Pass looks like |
|---|---|---|
| **E16** | Repeat **E12** exactly - pin to Project1, click into Project2, ask for the same change | The change lands in **Project1**, the model that was named. Not a refusal any more: the write is AIMED, so moving the screen is no longer an error to report. Check Project1 gained it and **Project2 did not** |
| **E17** | Pin to a project, **close it** in Revit, then ask for a change | `no_such_document`, naming what IS open, and nothing written. Heron must not open a project by itself, and must not fall back to whatever is in front |
| **E18** | Open **two** models both called `Project1` (one per Revit session, or a detached copy), pin one, ask for a change | `ambiguous_document` - it refuses rather than picking one. **Save one of them and repeat:** the paths now differ, so it must resolve cleanly and write into the right one |

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
| **H10** | First chat mid-request, second chat connects and is refused. Watch the FIRST chat | It loses that one reply and recovers on the next. Known limitation, [docs/25](25-multi-session-and-binding.md): the pipe is displaced at connect, before the lease can speak. If a **write** was in flight it must report the outcome as *unknown*, never as failed |

## Group L — linked models (needs Revit and a model that links something)

`HERON-REVIT-LNK-015` — [`revit/Heron.Revit.Addin/RevitLinks.cs`](../revit/Heron.Revit.Addin/RevitLinks.cs).
Compiles on 2020–2027, 0 warnings, 2026-09-15. **Never run.** Read-only, so nothing here can damage a
model — but every claim below is a claim about an API surface, not about behaviour.

Use a real job file: one with an architectural or structural link, and ideally one broken link.

| ID | Do this | Pass looks like |
|---|---|---|
| **L1** | Open a model with links and ask Heron *"what links does this model have?"* | Every link in Manage Links is listed, with the same names. A link Revit shows and Heron does not is the finding |
| **L2** | Compare `hostElements` against `count_elements` for the same model | **The same number.** If they differ, one of the two collectors is not doing what its comment says |
| **L3** | Unload one link in Manage Links, ask again | That link reads `unloaded` and its element count reads **not known**, never `0`. Revit's own Manage Links still shows it as unloaded afterwards — Heron must not have reloaded it |
| **L4** | Rename or move a linked file on disk, reopen the host, ask again | The link reads `not found`. This is the status a real job hits most often |
| **L5** | A model with one link placed twice | `linkTypes` is 1 and `placements` is 2. They are different facts and the answer must not merge them |
| **L6** | A nested link (a link inside a link) | It is listed and marked `[nested]`. A nested link is not attached to this model, so *"reload it"* is answered somewhere else |
| **L7** | Check the path shown | It matches what Manage Links shows — including the `RSN://` or BIM 360 form for a server model, not a raw internal path |

---

## Group M — phases and design options (needs Revit and a real job file)

`HERON-REVIT-PHS-032` — [`revit/Heron.Revit.Addin/RevitPhases.cs`](../revit/Heron.Revit.Addin/RevitPhases.cs).
Compiles on 2020–2027, 0 warnings, 2026-09-15. **Tracked against two models 2026-09-19 and signed** ([`brain/agent-proofs/HERON-REVIT-PHS-032.yaml`](../brain/agent-proofs/HERON-REVIT-PHS-032.yaml)) - so it is no longer NEVER RUN. That proof is D-53 tracking and it is NOT these rows: the cases below are still unchecked, and tracking says the answer follows the model, not that any one of them is right. Read-only — nothing here sets a view's
phase or activates an option, because both change what everybody else on the job sees in that view.

Use a real job file: one with Existing and New Construction at least, and ideally one design option set.
A single-phase model with no options proves nothing, and the tool says so rather than pretending.

| ID | Do this | Pass looks like |
|---|---|---|
| **M1** | Open a phased model and ask Heron *"what phases does this model have?"* | Every phase in Manage → Phasing is listed, in the same order. Order matters: Existing comes before New Construction and a list that reorders them is the finding |
| **M2** | Add the `created` counts across all phases, plus `withNoPhase` | It should equal `placedElements`. If it does not, an element is being counted in two phases or in none, and the breakdown cannot be trusted |
| **M3** | Compare `placedElements` against `count_elements` for the same model | **The same number.** Both claim to collect placed elements and exclude types |
| **M4** | A model with a design option set holding two options | Both options are listed, each naming its set, and exactly one is marked `primary` |
| **M5** | Add the `elements` across all options, plus `inMainModel` | It should equal `placedElements`. This is the check that says whether the breakdown is a partition or an overlap — **and the answer deliberately asserts nothing about how a collector treats a non-primary option**, so M5 is what settles it |
| **M6** | Note the active view, then check `viewPhase` and `viewPhaseFilter` | They match what the view's Properties palette shows. A number read off a plan has been through both |
| **M7** | Switch the active view to one on a different phase and ask again | `viewPhase` changes and the phase COUNTS do not — the counts are of the model, not of the view. If the counts move, the collector is view-scoped and the whole answer means something else |
| **M8** | A model with demolished elements | `demolished` is non-zero on the phase they were demolished in, not on the one they were built in |

**Why this group exists at all:** the same reason as Group L. A count given without its phase and its
design option is a number a person will act on. On a real job the Existing phase holds the survey, New
Construction holds the work, and an option set can hold two complete alternative arrangements of the
same shafts — and *"all ducts"* means something different in each.

---

## Group N — duct and pipe systems, and what is connected to nothing (needs Revit and a real MEP model)

`HERON-REVIT-SYS-030` — [`revit/Heron.Revit.Addin/RevitSystems.cs`](../revit/Heron.Revit.Addin/RevitSystems.cs).
Compiles on 2020–2027, 0 errors, 2026-09-16. **Tracked against two models 2026-09-19 and signed** ([`brain/agent-proofs/HERON-REVIT-SYS-030.yaml`](../brain/agent-proofs/HERON-REVIT-SYS-030.yaml)) - so it is no longer NEVER RUN. That proof is D-53 tracking and it is NOT these rows: the cases below are still unchecked, and tracking says the answer follows the model, not that any one of them is right. Read-only — nothing is connected, renamed
or put on a system, and no transaction is opened.

Use a real MEP job, not a sample: one with duct AND pipe systems, and ideally one you already know has
a loose element in it. **A clean model proves the least here** — the whole row is about what is missing
from a total, and a model with nothing missing cannot show that.

**The distinction the whole group turns on:** an OPEN CONNECTOR is normal — the end of every run is one,
and a stub waiting for next week's coordination is one on purpose. An element CONNECTED TO NOTHING in
any direction is not, and it is on no system by definition.

| ID | Do this | Pass looks like |
|---|---|---|
| **N1** | Open an MEP model and ask Heron *"what systems does this model have?"* | Every system in the System Browser is listed, with the same names, and marked `duct` or `pipe` to match. A system Revit shows and Heron does not is the finding |
| **N2** | Compare each system's `elements` against what the System Browser shows for it | The same number. This is the one that says whether `MEPSystem.Elements` means what the browser means |
| **N3** | Add `elements` across every system and compare with `mepElementsExamined` | They will NOT match and that is expected — an element can be on no system, and fittings may report differently. **What matters is the direction:** examined should be the larger. If it is smaller, elements are being counted on two systems |
| **N4** | Draw one duct on its own, joined to nothing, and ask again | It appears in `connectedToNothing`, with its category and its level. **This is the row the agent exists for** |
| **N5** | Take a properly connected run and check it does NOT appear in `connectedToNothing` | Only its end connectors are open, so it counts in `withAnOpenConnector` and not in the loose list. A run that appears in both is a false positive and makes the whole answer noise |
| **N6** | A model with no MEP at all — an architectural or structural file | It says so in words rather than returning an empty list. An empty answer and *"this file has no MEP in it"* must not read the same |
| **N7** | Check `reportedNoConnectors` against what you expect | Some MEP families genuinely have none. A large number here means the connector question is being asked of things that cannot answer it, and `mepElementsExamined` is then the wrong denominator for N3 |
| **N8** | A duct connected only to itself, or a fitting whose connectors reference its own owner | It is still reported as connected to nothing — `IsJoined` skips references back to the same owner. If it comes back joined, the self-reference check is not doing its job |
| **N9** | Compare `onNoSystem` with `connectedToNothingCount` | `onNoSystem` should be the LARGER of the two, and the gap is the interesting number: an element joined to its neighbour but sitting on no system is a half-built run. If they are equal on a real job, either every loose element is also the only unsystemed one — possible — or `MEPSystem.Elements` is not returning what the System Browser shows, which N2 is what settles |

**Why this group exists at all:** a duct that LOOKS joined on screen and is not carries no flow, appears
on no system, and is missing from every number downstream — the schedule prints, the sizing calculates,
the System Browser shows a tidy tree, and none of them mentions it. On a federated Qatar job that is the
difference between a riser that balances and one that is found on site.

**What it deliberately does not do:** judge. It reports counts and one list, and asserts nothing about
what Revit considers a valid system. The compiler agreed about every name it calls and can say nothing
about what any of them returns.

---

## Group P — parameters, and the two ways the answer is confidently wrong (needs Revit and a real model)

`HERON-REVIT-PAR-011` — [`revit/Heron.Revit.Addin/RevitParameters.cs`](../revit/Heron.Revit.Addin/RevitParameters.cs).
Compiles on 2020–2027, 0 errors, 2026-09-16. **Proved 2026-09-19 and signed** ([`brain/agent-proofs/HERON-REVIT-PAR-011.yaml`](../brain/agent-proofs/HERON-REVIT-PAR-011.yaml)) - so it is no longer NEVER RUN. It went signed, then WITHDRAWN, then signed again the same day: the first proof rested on two models and claimed no empty input existed, which [U5](#) disproved. The one that stands varies `category` across Ducts, Pipes and Air Terminals, and the last of those IS the empty case. That proof is D-53 tracking and it is NOT these rows: the cases below are still unchecked, and tracking says the answer follows the model, not that any one of them is right. Read-only — no parameter is written, and no
transaction is opened.

Use a real project, not a sample: one with shared parameters loaded, at least one project parameter
bound to a category, and a schedule you already trust. **The schedule is the instrument** — almost every
row below is settled by putting Heron's answer next to a schedule of the same thing.

Two tools, one operation. `revit_parameters(category)` with no parameter named gives the COVERAGE answer
— which parameters these elements carry and how many are filled in. Naming one gives the VALUES.

| ID | Do this | Pass looks like |
|---|---|---|
| **P1** | Ask for the coverage of a category you know well — *"which parameters are filled in on the ducts?"* | Every parameter you can see in the Properties palette appears, plus the type ones. A parameter Revit shows and Heron does not is the finding |
| **P2** | **The row this agent exists for.** Ask for a parameter that lives on the TYPE — Fire Rating on doors, Assembly Code on anything | It comes back with `where` reading **type**, and a count that matches the schedule. A confident *"0 of 340"* here means the type is not being read, and that is the plausible zero this whole file was written against |
| **P3** | Take one length parameter — a duct width, a wall height — and compare `value` with the schedule | **The same string, units and all.** If it reads `0.656` where the schedule reads `200 mm`, `AsValueString` is not doing what this code assumes and every number it has ever given is in decimal feet |
| **P4** | Check the project's units are NOT millimetres, then repeat P3 | The value follows the PROJECT, not a fixed unit. This is the row that catches a conversion baked in by accident |
| **P5** | On a type row in the coverage answer, compare `onElements` with the number of TYPES in that category | They match. A type row reporting 900 against 900 doors means types are being counted once per element — the answer is not wrong, but it is not the number the column says |
| **P6** | Make a schedule of one parameter, sort by it, and count the blanks. Compare with `noValue` + `blank` | The same total. **Then check the split:** `blank` should be the ones holding a space or an unset material, `noValue` the ones nobody ever touched. A schedule cannot tell them apart and this is the only place the difference shows |
| **P7** | Put a single SPACE into a text parameter on one element and ask again | It moves from `noValue` to `blank` — not to filled in. Counting a typed space as data is how a completeness check passes when it should fail |
| **P8** | Load a shared parameter with the SAME NAME as a built-in one and bind it to a category | `sameNameOnOneElement` reads 2 on that row, and asking for it by name reports `ambiguous`. If it silently answers with one of them, the clash detection is not working and writing by name later would hit the wrong parameter |
| **P9** | A category with several thousand elements — the ducts on a real tower | It answers in a few seconds. Both shapes read each TYPE once; if it hangs, the per-type caching is not being hit and every element is re-reading its type |
| **P10** | A category with more than 500 elements carrying a named parameter | `listed` reads 500, `notListed` carries the rest, and the reply says so. A truncated answer that does not admit it is worse than a slow one |
| **P11** | Ask for a category that has no elements at all in this model | It says so in words. An empty answer and *"this model has none of those"* must not read the same |
| **P12** | Ask for a parameter that exists nowhere — a typo | `withoutTheParameter` equals the element count, and the wording says it is probably an unbound project parameter or a different family, not missing data. **That distinction is the whole value of the answer** |
| **P13** | Pin to one project, switch Revit to another, and ask again | Refused, and nothing is read. A completeness report from the wrong building reads exactly like one from the right building |
| **P14** | Check a read-only parameter — Area, Volume, an elevation | `readOnly` is true. If it reads false, a later write agent would try it and fail at the transaction |
| **P15** | Check a Yes/No parameter and a material parameter | The Yes/No prints as Revit prints it; the material prints the material's NAME, not a number. An ElementId pointing at nothing counts as `blank` |

**Why this group exists at all:** *"is it filled in?"* is the question asked before every schedule, IFC
export and hand-over, and today the only way to answer it is a throwaway schedule and a pair of eyes.
The two ways a machine gets it wrong are both silent — reading only the instance when the data sits on
the type, and printing decimal feet where the project says millimetres — and both produce an answer that
is formatted, confident and believed.

**What it deliberately does not do:** write, and report a parameter's data type. `Definition.ParameterType`
is **not in 2023** and `Definition.GetDataType()` is not in 2020, so no single member answers across the
eight releases and [D-05](DECISIONS.md) does not extrapolate one. `storageType` is the narrower fact
reported instead.

---

## Group S — groups and assemblies, and the edit that lands in twelve places (needs Revit)

`HERON-REVIT-GRP-033` — [`revit/Heron.Revit.Addin/RevitGroups.cs`](../revit/Heron.Revit.Addin/RevitGroups.cs).
Compiles on 2020–2027, 0 errors, 2026-09-16. **Tracked against two models 2026-09-19 and signed** ([`brain/agent-proofs/HERON-REVIT-GRP-033.yaml`](../brain/agent-proofs/HERON-REVIT-GRP-033.yaml)) - so it is no longer NEVER RUN. That proof is D-53 tracking and it is NOT these rows: the cases below are still unchecked, and tracking says the answer follows the model, not that any one of them is right. Read-only — nothing is grouped, ungrouped
or edited, and no transaction is opened.

Use a model that actually uses groups: one group type placed several times, ideally one nested group,
and if you have one, an assembly. **A model with no groups proves almost nothing here.**

**This group closes a guess that is already in shipped code.** `E10` above is proved: a move of a group
member returns cleanly and shifts nothing, no exception and no warning, so `RevitWrite` compares
positions either side and then reports *"almost certainly inside a group"*. That hedge is there because
nothing could check beforehand. These rows are whether it can now.

| ID | Do this | Pass looks like |
|---|---|---|
| **S1** | `revit_groups` with no category, on a model you know | Every group type in the Project Browser is listed, with the same names, and `placements` matches how many you can count. A group Revit shows and Heron does not is the finding |
| **S2** | Compare `members` against what you see when you edit the group | The same number. It is counted off ONE instance — if it disagrees with the browser, either a member id is resolving to nothing or instances of one type do not hold the same content, and the second would be news |
| **S3** | Check a group definition that is in the browser but placed nowhere | `placements` reads 0 and `members` is **absent, not zero**. There is no instance to count off, and a zero there would mean "empty group" rather than "unknown" |
| **S4** | **The row this agent exists for.** Put two ducts in a group, place that group 12 times, then ask `revit_groups("ducts")` | `mostPlacements` reads 12 and the ducts are listed with their group type. **Then do E10's move and see the two answers agree** — this one before, RevitWrite's after |
| **S5** | Ask for a category with no grouped elements in it | It says so in words: an edit reaches exactly one place. An empty list and *"nothing here is grouped"* must not read the same |
| **S6** | Pin one duct, group another, and ask | They come back as **two different rows with different reasons** — `pinned` true on one, `inAGroup` true on the other. A group member is NOT pinned, and merging the two would send somebody to unpin something that needs ungrouping |
| **S7** | Nest a group inside another group and put a duct in the inner one | `nested` is true and `chain` has two entries, innermost first, each with its own `placements`. **Then check the arithmetic by hand** — how many places does editing that duct really reach? This is the row that settles whether the counts multiply, which the code deliberately refuses to assume |
| **S8** | A detail group and a model group in one model | `kind` tells them apart, using Revit's own category names. A detail group lives in one view and a model group does not |
| **S9** | An assembly with elements in it | It appears under `assemblies` with a member count. **Then edit a member and check whether another assembly of the same type changed** — this is the question the code refuses to answer and this row is where it gets answered |
| **S10** | A category of several thousand elements across a few group types | It answers in a few seconds. The placement count is cached per group type; if it hangs, the cache is not being hit |
| **S11** | Pin to one project, switch Revit to another, ask again | Refused, nothing read. Approving an edit against the wrong model's placement count is how twelve of the wrong rooms change |

**Why this group exists at all:** editing one element inside a group edits it everywhere that group is
placed. That is what groups are *for* — and it is a bad surprise when nobody told you the element was in
one. Revit raises no error, so the only defence today is knowing your own model.

**What it deliberately does not do:** multiply the nested counts, or explain assemblies. Whether an inner
group's placement count already includes the copies carried inside its parent is a question about Revit,
not about this code, and `S7` is what settles it. Whether an edit inside one assembly travels to another
of the same type is `S9`. Both would have been easy to guess at and a wrong number here reads exactly
like a right one — [D-52](DECISIONS.md).

---

## Group G — hard to force, do last

Not blocking. Listed so they are not mistaken for tested.

| ID | Do this | Pass looks like |
|---|---|---|
| **G1** | Force a failure mid-move (the build order asks for this — e.g. a duct that cannot move) | The model is **untouched**, and the message says it was rolled back. Not a partial move |
| **G2** | Kill the bridge between approve and the answer coming back | Heron says it **cannot tell** whether it ran, and refuses to retry on its own. This is the `unknown_outcome` path — the one the Failure Analysis Agent exists for |
| **G3** | After all of the above passes | Set `write.enabled` back to **false** until you actually want Heron writing |
| **G4** | After a move, open the newest file in `%APPDATA%\Heron\audit` | The entry carries **every moved element's UniqueId**, the document identity and the undo entry name, all under one Workflow ID. A count alone cannot answer *"which ducts?"* |
| **G5** | `python tests/test_golden.py` after re-proving anything | The re-proved case stops reading **STALE**. Seven Phase 0 proofs are stale right now — they were taken against a build that no longer exists |
| **G6** | `python tests\test_walk.py` from an account that is **not** an administrator, on a folder holding a subfolder you have no permission to open | Section 9 stops saying *UNTESTED* and names the folder in `unreadable`. `os.walk` throws such a folder away in silence by default; [`HERON-IMP-FIL-002`](../brain/heron_walk.py) supplies `onerror` so it does not, and that branch has never actually run — a mode-0 folder does not stop root here, and does not stop an administrator on Windows either |

---

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

*Lettered T because Group H was already taken by the lease (H1-H10). Written as H first, which put two H1 rows in one register and made `check-gaps` count both - fixed the same day.*

Added while proving the four owed add-in agents against the two live Revit 2024 sessions
(`20472` *Project1 work_ajmal.al*, 3,529 elements; `36908` *test projject*, 3,594 elements).

Five agents were proved and signed that day — SHT-029, DIM-031, GRP-033, PHS-032, SYS-030. Two
could not be, and **neither failure is about the agent**. Both are about the tool that judges it.
They are recorded here because a reader coming back to `IMP-019` and `LVL-027` will otherwise
re-run them, get the same verdict, and conclude the agents are broken.

### T1 — `compare()` reads only top-level numbers, so a list that moves is invisible

[`tools/prove-agent.py`](../tools/prove-agent.py) `compare()` skips any value that is not an `int`
or `float` at the top level of the reply. Its docstring gives the reason, and the reason is sound
*for a string*: `"A string that differs is usually the document name, which is the input rather
than the answer"`. It does not extend to a **list of things found in the model**, which is the
answer and nothing else.

**`IMP-019` is the case.** `track` reported `moved: NOT ESTABLISHED` — every comparable number
identical, `importedCount: 0, linkedCadCount: 1, revitLinks: 0`. The raw replies, read by hand the
same minute:

| | session 20472 | session 36908 |
|---|---|---|
| `linkedCad[0].name` | `box.dwg` | `Project1 - Section - Section 1.dwg` |
| `document` | `Project1 work_ajmal.al` | `test projject` |

**The agent reads the model.** It names a different DWG in each, correctly. The count is 1 in both
because each model happens to hold exactly one CAD link, and a count of one against a count of one
is the only thing `compare()` was allowed to look at.

**`LVL-027` is the same shape, one level down.** `track` found one number moving — `onNoLevel:
3513 -> 3558`, which is really the element total wearing a different hat. Inside `levels[]`, which
`compare()` never opens:

| | session 20472 | session 36908 |
|---|---|---|
| Level 1 | **8** elements | **29** elements |
| Level 2 | **8** elements | **7** elements |

Identical names, identical elevations (0 and 4000), different contents. The per-level counts are
the agent's actual answer and they move in both directions at once — which is *harder* to fake
than a single total, not easier.

**So the standing advice to "add or rename a level in one model" would work, and it treats the
symptom.** `levelCount 2 -> 3` is a top-level number, so `compare()` would see it. But the evidence
that the agent reads the model is already in hand; what is missing is a tool that can record it.

### T2 — `track` cannot pass an argument, so an agent that needs one cannot be tracked at all

`track` takes `--operation`, `--first`, `--second`, `--client-id`. There is no way to send an
operation argument, and it sends the bare op. Three operations refuse without one:

| Operation | Agent | Bare-op result |
|---|---|---|
| `read_parameters` | `HERON-REVIT-PAR-011` | `no_category` |
| `select_by_category` | `HERON-REVIT-CAT-009`, `HERON-REVIT-SEL-008` | `operation_failed` |

**`PAR-011` was then driven by hand with `category=Pipes`, and it is one of the strongest readers
measured all day:**

| | session 20472 | session 36908 |
|---|---|---|
| `elements` | 0 | **2** |
| `distinctParameters` | 0 | **99** |
| `listed` | 0 | **99** |
| `typesRead` | 0 | **1** |

Every number moves. It cannot be signed, because the tool that writes drafts has no way to make
that call.

### What would settle T1 and T2

Both are one change to `tools/prove-agent.py`, and **neither has been made** — the file was not
this session's to edit:

| | |
|---|---|
| ~~**T1**~~ | **DONE 2026-09-19.** Teach `compare()` to descend one level into a list: compare its length, and compare each item's own numbers and names positionally or by key. `linkedCad[0].name` and `levels[n].elements` both become visible, and `IMP-019` and `LVL-027` can be judged on their real answers |
| ~~**T2**~~ | **DONE 2026-09-19.** An `--arg key=value` option, repeatable, passed through to `op_args`. Unlocks `PAR-011`, and `CAT-009` / `SEL-008` if a category is supplied for each model |

### T3 — the rest of the twenty-three are mostly not trackable, and that is correct

Of the 23 add-in agents with no proof, the file headers show they are **not 23 separate readers**.
Seven share `RevitWrite.cs` (`TSA-006`, `TRN-005`, `WRN-016`, `CTX-007`, `ELE-010`, `KRN-EVD-014`,
`KRN-HUM-018`) and are the machinery *inside* a write — transactions, warning capture, context,
evidence, human wording. Four share `Commands.cs` (`CON-001`, `HLT-025`, `UI-022`, `OPS-STP-007`).
Two share `HeronApplication.cs` (`RIB-023`, `VER-002`). `CMP-021` compiles C#. `APP-003` is the
dispatcher every operation already passes through.

**None of those has a read operation to send**, so `track` cannot reach them and no amount of
model would help. They need a different kind of proof — one that observes them doing their job
during somebody else's operation — and that does not exist yet. Recorded rather than left looking
unproved by neglect.

`LNK-015` *is* reachable (`list_links`) and *is* thin: both models hold **zero Revit links**, so
only `hostElements` moves. Note this is not the reason sometimes given — the linked **CAD** files
belong to `IMP-019`, not to `LNK-015`. Proving `LNK-015` needs a model with a real Revit link in
it.

### T1 and T2 were fixed the same day, and T3 stands

**Read this rather than the two rows above**, which say the changes were not made. They were, an hour
later and on the owner's say-so. `tools/prove-agent.py` now does both:

- **`compare()` descends one level into a list** — its length, then each item's own numbers *and
  names*, first `LIST_ITEMS = 5` items.
- **`track --arg KEY=VALUE`**, repeatable, passed through to `op_args`. The **same** arguments go to
  both models, deliberately: varying them would let a different answer come from a different input
  rather than a different model, which is the one thing tracking exists to rule out.

Four agents were then proved and signed that could not be before:

| Agent | What moved, once the tool could see it |
|---|---|
| `IMP-019` | `linkedCad[0].name: box.dwg -> Project1 - Section - Section 1.dwg` |
| `LVL-027` | `levels[0].elements: 8 -> 29`, `levels[1].elements: 8 -> 7`, `onNoLevel: 3513 -> 3558` |
| `PAR-011` | `distinctParameters: 95 -> 90`, `listed: 95 -> 90`, `parameters[]: 95 -> 90` (`--arg category=Ducts`) |
| `SEL-008` | `selected: 18 -> 8` (`--arg category=Ducts`) |

**The guard that mattered was checked first:** `compare(A, A)` — the same reply against itself —
still returns nothing moved. A change that widens what counts as movement has to be shown not to
manufacture it.

**Ducts were chosen over pipes on purpose.** `category=Pipes` gave `0 -> 2` elements and `0 -> 99`
parameters, and a zero is the weaker half of a pair: an agent that failed to read would also return
zero. `Ducts` gives **18 against 8** and **95 against 90** — both sides non-zero and different,
which no fallback and no constant can produce.

#### One honest side effect: the thin-tracking flag now over-fires on list-heavy replies

`TRACKING IS THIN` compares how many things moved against how many held. Descending into lists adds
every matching item field to the *held* side, and on a parameter report those are **Revit's own
parameter names, identical by nature**. `PAR-011` reads *3 moved, 44 held* and is flagged thin,
while its real argument — 95 distinct parameters against 90, both non-zero — is strong.

**No threshold was invented to paper over it.** The gap text already says the reader judges, and
that is what happened here. Recorded so the next person reading `PAR-011`'s proof knows the flag is
inflated rather than the evidence weak.

#### `CAT-009` is genuinely not trackable, and that is the correct answer

`select_by_category` returns `categories: 1` in **both** models, and it should. `CAT-009` maps a BIM
word to a Revit category — *"Ducts"* to `OST_DuctCurves` — and that answer does not depend on which
model is open, because it is a lookup rather than a reading. Tracking cannot prove it and proving it
by tracking would mean nothing. It needs a different kind of test: a table of words against the
categories they must resolve to.

`DOC-004` is the same shape from the other end. It chooses *which document*, and `document` is
excluded from `compare()` as the input. Every one of the fifteen signed proofs names the right model,
which is real evidence that it works — but it is evidence sitting in fifteen other agents' files,
not a proof of its own.

---

## Group I — the twenty-three that were tried and never passed, sorted by WHY

Read 2026-09-19, from the run records in `brain/proof-drafts/runs/` rather than by re-running
anything. **71 fragments are DRAFT. 44 cannot be arranged at all** (`generate-jobs.py`, measured over
all 71 rather than only the never-tried ones). **27 can be, and 23 of those already have a run
record** — they were put in front of a real model between 9 and 17 September and no signature
followed. Nobody had been back to ask why.

> **THE TWO COUNTS ABOVE WENT STALE THE SAME DAY. THE SPLIT BELOW DID NOT.**
> Re-derived 2026-09-19 on a Linux container at `main`: `heron_fragment.load_all` counts **395
> fragments, 328 PROVEN, 67 DRAFT**, and `python tools/generate-jobs.py` reports **37 unarrangeable
> of 67** — not 71 and 44. The seven that moved are named by the commit that freed them:
> [PR #196](https://github.com/Ajmalpshaik/Heron-AI/pull/196) is titled *"Five of the six proposed
> rules are built, and **seven fragments can be arranged**"*, and `git merge-base --is-ancestor`
> puts it **after** the commit carrying this paragraph. **Derive both rather than reading them
> here** — this file's own rule, and [FRAGMENT-ISSUES row 143](FRAGMENT-ISSUES.md) is the row.
>
> **AND NOTHING BELOW CAN BE RE-DERIVED ANYWHERE BUT THE PROVING MACHINE.** The sort was read from
> `brain/proof-drafts/runs/`, which is **gitignored**: on a fresh clone it does not exist, which is
> why `generate-jobs.py` measured all 67 DRAFT above rather than the 27 this paragraph describes.
> So I1, I2 and I3 are a **one-machine fact recorded in a shared file** — no clone and no CI run can
> check a single row of them. The split is still the useful part; the totals are not.

The answer is not one answer. It is three, and only the first is proving work.

### I1 — the arrangement was wrong, and that is all (6)

Each of these ran, or failed to run, for a reason that is fixed by typing different values. **Every
one still has to be re-run before anyone believes it**; what follows is a diagnosis, not a result.

| Fragment | What the record says | What to change |
|---|---|---|
| `auto-size-pipe` | positive `setup_failed - the arrangement could not be re-made`; the **negative ran fine** and did real work (`noFlow 14`, `refused 8`) | Only the positive selection broke. The model it needs is `Snowdon-scratch_ajmal.al` — it has the pipes; Project1 has none |
| `set-wall-constraints` | positive `rehosted: 4`, negative `notAWall: 3, rehosted: 0` — **both legs already look right** | Nothing. This is the one STALE signature: it passed, then the code changed under it. It needs re-proving, not re-arranging |
| `set-design-option` | `available: 0, wasPossible: false` on both legs | It ran against a model with **no design options**. `Project1 work_ajmal.al` has **1** |
| `create-workset-3d-views` | both legs `created: 2` — identical | The negative was not a different case. It needs a value-driven one |
| `place-accessory-on-run` | `bad_request_value: No family type called "Damper"` | Name a family type the model actually carries |
| `switch-active-project` | `switched: false`, *"Revit refused to change to the view"* | The view name was wrong. Note this one changes which project is in front, so it wants care rather than a batch |

### I2 — the model has not got it (13)

**Not failures, and re-running them changes nothing.** Each needs content no open model carries, and
`fragment-proving` rule 2 is exactly this: a fragment asked for what the model has not got returns
POSITIVE EMPTY and wears the blame.

| Needs | Fragments |
|---|---|
| **An IMPORTED CAD** — a link is not an import, and both said so in the same words: `notAnImport: 307`, `importsRead: 0` | `convert-cad-to-directshape`, `extract-cad-curves` |
| **Rooms, and doors between them** | `report-door-room-links`, `center-room-tags` (room tags) |
| **Tags or dimensions** — `notMovable: 15`, *"0 by its tag head, 0 by its location point"* | `move-annotation` |
| **Assemblies** — `notAnAssembly: 22` | `create-assembly-views` |
| **A named DWG export setup** — *"name the DWG export setup; Revit's defaults..."* | `export-views-to-dwg` |
| **The flow-arrow family loaded** — `No symbol 'M_Air Flow Arrow : Standard' is loaded` | `place-flow-arrows` |
| **An area scheme with areas** — `areas: 0, placedCount: 0` | `report-areas` |
| **Electrical circuits** — `panels: 0, elements: 0` | `select-by-electrical-circuit` |
| **Openings** — `found: 0` across all five opening categories | `select-openings` |
| **MEP runs that can take an offset** — `set: 0, withoutOffsets: 0` | `set-mep-justification` |
| **A section mark placed on a sheet** — `markers: 4` but `hidden: 0, shown: 0` | `set-section-mark-visibility` |

**ONE MODEL COULD ANSWER MOST OF THIS.** Rooms with doors, a section on a sheet, an imported DWG
beside the linked one, and an area scheme would clear **six** of the thirteen at a stroke. That is a
request to the owner, not a job for a batch.

### I3 — blocked on something being built (4)

| Fragment | What stops it |
|---|---|
| `find-clashes` | `'against' is an id, and Heron resolves one by NAMING the thing it belongs to. There is no rule for this name yet` |
| `place-rooms` | `levelId`, `phaseId`, `planViewId` are all `ElementId` |
| `set-global-parameter` | `"ParameterValue" is not one of them yet` — D-54 takes a view, a level, a category, a name, a number and true/false |
| `place-structural-family` | Not an input problem — [row 128](FRAGMENT-ISSUES.md) of FRAGMENT-ISSUES. It placed a column when told `structuralType=Beam`, and which of two causes that is has not been settled |

The first three are the same shape as the 44, and **PR #190 proposed rules for six such shapes on the
same day** — proposals only, no code. If those land, this group shrinks.

### What this changes about the count

**The honest figure for "worth re-running" is 6, not 23 and not 41.** Thirteen are waiting on a
model and four on a decision. Saying 23 without the split would send somebody to re-run thirteen
fragments that cannot pass and to blame them when they do not.

---

## Group U — a review found six things in Group T's own work, and all six held

Codex reviewed [PR #189](https://github.com/Ajmalpshaik/Heron-AI/pull/189) on 2026-09-19 and left six
comments. **Every one was checked against the code and every one was right.** Four are fixed below;
two are open and one of those is the reason two signatures were withdrawn the same day.

Recorded here rather than only in the PR because a review comment disappears into a merged branch and
a register does not.

| ID | What it found | State |
|---|---|---|
| ~~**U1**~~ | `elements` sat in the unconditional envelope skip, so a **list** by that name was never traversed - and `read_parameters` and `list_groups` both return their per-element answer under exactly that name. Two models with equal totals and different contents compared as *nothing moved*. A skip inherited from the number-only behaviour, silently cancelling the list traversal added the same day | **FIXED.** `SCALAR_ONLY_SKIP` - skipped where it is the scalar total, compared where it is a list. Both cases tested, and the same-reply-twice guard re-checked |
| ~~**U2**~~ | `track --arg` made the arguments part of the test and the draft recorded only `operation`, so `PAR-011` and `SEL-008` never said `category=Ducts` was supplied. Not reproducible, and a later run with a different category would be indistinguishable | **FIXED.** The draft carries an `arguments:` line |
| ~~**U3**~~ | Four register sections still read **Never run** for agents that had been tracked and signed hours earlier | **FIXED.** `PHS-032`, `SYS-030`, `PAR-011`, `GRP-033` corrected - and corrected *without* closing their detailed cases, which tracking does not touch. `LNK-015` still reads Never run, correctly |
| ~~**U4**~~ | Row 118 was claimed twice - by this branch and by [PR #191](https://github.com/Ajmalpshaik/Heron-AI/pull/191) the same day | **FIXED** in the merge. Theirs keeps 118-125, mine is **126** |
| ~~**U5**~~ | **`PAR-011` and `SEL-008` DO have an empty case, and their signed proofs said no input could produce one.** `category=Pipes` returned `elements 0, distinctParameters 0` on session 20472 - measured in the very session that signed them. So D-53 tracking is not the right substitute: D-30's real negative leg is possible and owed | **FIXED the same day.** Both were withdrawn, then re-proved properly with the new `vary` mode and re-signed. `read_parameters` across `Ducts / Pipes / Air Terminals` on session 36908 returned **8 elements and 90 parameters / 2 and 99 / 0 and 0** - three inputs, three distinct answers, and **`Air Terminals` is a real empty case**. D-30's negative leg is now RUN and recorded rather than substituted, and the review was right that it was one argument away the whole time |
| **U6** | **Nine agent proofs were signed on TWO models. The repository's own gate refuses two for a fragment**, in these words: *"the tracking set has only 2 row(s). D-53 asks for the answer to follow the input across SEVERAL different inputs; two cannot show that"* ([`brain/heron_validate.py`](../brain/heron_validate.py)) | **HALF FIXED, and the other half is bigger than it looked.** `prove-agent.py vary` now runs one agent across several values of ONE ARGUMENT on ONE model and **refuses fewer than three**, the same number the fragment gate enforces. That settles every argument-driven agent and needs no second Revit. It cannot settle an agent that reads the WHOLE MODEL - for those a third input is a third model. **THIRTEEN proofs are in that position, not the seven first counted:** the six signed before 2026-09-19 used the same two sessions, 36860 and 71340. Each now carries the caveat in its own `gaps`, recorded rather than withdrawn - the evidence that the answer moved is real, and it is thinner than this library's own standard |

**U6 is the one that matters most and it is not a bug in anybody's code.** It is one decision, D-53,
implemented twice at different strengths, and nothing compares the two. The fragment path enforces
three rows in code; the agent path never had a threshold at all because the tool's whole shape is
two-models-at-once.

**A third input need not be a third model.** For an argument-driven agent it can be a third
**argument** - `category=Ducts`, `Duct Fittings`, `Pipes` - which is closer to what D-53 asks
(*the answer follows the input*) than a third file would be. That is a change to `track`, not a
request for another Revit.

## Group V — all 66 unproven fragments, sorted by WHY, read off disk 2026-09-19

**NOT ONE REVIT CALL WAS MADE TO PRODUCE THIS TABLE.** Every fragment that has
ever been run leaves a record in `brain/proof-drafts/runs/`, and the record
holds both phases with what each one bound and what each one returned. Sorting
them is reading, not proving — and it turns *"66 left"* into six lists with a
different owner each. The same method turned 23 agents into 6-13-4 on the same
day, and it costs a minute.

**`runs/` IS GITIGNORED, SO THIS IS A MEASUREMENT OF ONE CHECKOUT.** 326 run
records existed here when this was taken. **THE NUMBERS BELOW CANNOT BE
REPRODUCED FROM A FRESH CLONE AND NO GATE CAN CATCH THEM GOING STALE** - said
plainly because a committed table of derived counts is normally a thing this
repository refuses. It is kept because the SORT is the value and the sort does
not go stale: which bucket a fragment belongs in is a property of the fragment.
Re-derive the counts where you will actually prove, with:

```
python - <<'EOF'
import json, io, os, glob, yaml, re, collections
def empty(v):
    s = str(v).strip()
    if s in ("", "0", "(null)", "None", "False", "false", "[]", "{}"): return True
    return bool(re.match(r"^0 (item|entry|entrie)", s))
b = collections.defaultdict(list)
for f in sorted(glob.glob("brain/fragments/*/fragment.yaml")):
    slug = os.path.basename(os.path.dirname(f))
    text = io.open(f, encoding="utf-8").read()
    if "heron-status: DRAFT" not in text: continue
    rec = os.path.join("brain/proof-drafts/runs", slug + ".json")
    if not os.path.isfile(rec): b["no run record"].append(slug); continue
    d = json.load(io.open(rec, encoding="utf-8"))
    ph = {p.get("phase"): p for p in d.get("phases", [])}
    if "positive" not in ph or not ph["positive"].get("ok"):
        b["positive refused"].append(slug); continue
    if "negative" not in ph: b["no negative"].append(slug); continue
    y = yaml.safe_load(text)
    res = [p["name"] for p in ((y.get("contract") or {}).get("provides") or [])
           if p.get("role") == "result"]
    pv, nv = ph["positive"].get("provides") or {}, ph["negative"].get("provides") or {}
    pe = all(empty(pv.get(r)) for r in res) if res else None
    nf = any(not empty(nv.get(r)) for r in res) if res else None
    b["positive empty" if pe else ("negative not empty" if nf else "re-run")].append(slug)
for k in sorted(b, key=lambda k: -len(b[k])):
    print("%-22s %d  %s" % (k, len(b[k]), " ".join(b[k])))
EOF
``` A different worktree will sort the
same 66 differently — the DRAFT/PROVEN split is committed and shared, the
records are not. Re-derive it where you will actually work.

| | | |
|---|---|---|
| **26** | **no run record here** | Nothing has tried them in this checkout. `generate-jobs.py` offers **4** and marks **22** unarrangeable with a reason each |
| **19** | **POSITIVE EMPTY — it ran and found nothing** | So the arrangement was wrong rather than the fragment. **17 need MODEL CONTENT** — CAD imports, groups, design options, openings, electrical circuits, areas, annotation. **2 need a PERMISSION PHASE THAT DOES NOT EXIST** and were miscounted here first: `export-families` and `export-schedule-to-csv` are `risk: PUBLISH`, which `HeronPermissions` puts out of reach for Phase 0 and Phase 1, so nothing is sent to Revit at all. No model will ever unblock those two — checked by reading `risk:` on all nineteen rather than assuming the bucket was uniform |
| **13** | **POSITIVE never ran or was refused** | The binder said no before the fragment started. A refusal to start is not an answer, and it is the cheapest of these to read: the message names the need |
| **7** | **NEG NOT EMPTY — the negative was arranged wrong** | Listed below, because this is the group a session can fix at a desk |
| **1** | **judged by nothing at all** | `describe-blank-parameters` |

### The 7 whose negative found something, and what is actually wrong with each

**None of these is a fragment defect.** Every one is a proof that was arranged
badly, and three of them are the same mistake: **the negative changed something
the answer does not depend on.**

| fragment | what the negative did | why it could never work |
|---|---|---|
| `add-project-parameter` | same `categories=Ducts` in both legs | not a contrast at all. `bound true` both times because nothing differed |
| `create-workset-3d-views` | changed the CATEGORY | it makes one view per WORKSET. A category cannot move that answer |
| `find-unused-materials` | changed the region box | *unused in the project* is a project-wide fact. `unusedMaterials 60` both times |
| `report-open-documents` | changed the category | it lists what is OPEN IN REVIT. No selection on earth changes that |
| `trace-connectivity` | changed the region, kept `start=selected` | `reached` is seeded with the START, so it can NEVER be empty. Read below — this entry was wrong first |
| `place-structural-family` | Text Notes in a Legend | already [row 128](FRAGMENT-ISSUES.md) — it placed a column when asked for a beam |
| `report-findings` | nothing matched, honestly | the negative is **CORRECT** — it says *"NOTHING WAS CHECKED"*. `report` is PROSE, so a non-empty string reads as a find |

**FOUR OF THE SEVEN NEED TRACKING RATHER THAN A BETTER NEGATIVE. THREE ARE MODEL-INDEPENDENT FOR A SELECTION** — `create-workset-3d-views`,
`find-unused-materials`, `report-open-documents`. Handing them a different
selection is the wrong experiment, and no amount of re-arranging fixes it.
[D-53](DECISIONS.md) is what they need: vary an INPUT the answer genuinely
depends on, across **three or more** values, and show the answer following it.
That is the same ruling `prove-agent.py track` already applies to agents that
can never return empty, and `refresh-view` and `save-document` are marked the
same way by `generate-jobs.py` today.

**`trace-connectivity` IS A FOURTH TRACKING CASE, AND THIS ENTRY SAID THE
OPPOSITE FOR AN HOUR.** It was written as *"the one that can be fixed with one
value — give it a different `start`"*, and then the code was read. Two things
make that wrong:

```
fragment.cs:54   queue.Enqueue(start);
fragment.cs:61   reached.Add(current);      <- the start, on the first pass
fragment.cs:95   foreach (var candidate in elements)   <- GEOMETRIC route only
```

**`reached` ALWAYS CONTAINS THE START, so it can never come back empty** while
anything binds at all — which is the exact shape [D-53](DECISIONS.md) exists
for, and no choice of start changes it.

**And `elements` does not constrain the walk.** The declared-connector route
follows `Connector.IsConnected` wherever it goes; the pool is consulted only
when looking for a geometric neighbour within `tolerance`. So the old negative
— which moved the region and kept `start=selected` — was changing something
that can only move `joinedByGeometry`, and `reached 25` in both legs was the
fragment behaving exactly as written.

**`generate-jobs.py` CANNOT SEE THIS SHAPE.** It marks `refresh-view` and
`save-document` for tracking because they *take* nothing but the document; this
one takes three things and still cannot return empty, because it seeds its own
answer. Worth a rule there. **THE REST OF THE LIBRARY WAS SWEPT FOR THE SAME SHAPE AND
IT IS A SINGLETON** — every fragment's `role: result` fields were matched against
its own need names across all 395, looking for a result seeded directly from an
input, and `trace-connectivity` is the only one (`reached` and `joinedByGeometry`,
both from `start`). So this is one fragment to mark, not a class to design for.

### The singleton, and it was measured rather than assumed

`describe-blank-parameters` is **the only fragment of 395 that declares no
`role: result` at all** — checked across the whole library, DRAFT and PROVEN.
So `batch-prove` cannot judge it: its rule is *did a declared result come back
empty*, and there is no declared result to look at.

**ITS RUN RECORD IS ALREADY A TEXTBOOK D-53 PROOF and nothing can score it.**
On `Snowdon-scratch_ajmal.al`, 9,638 elements:

```
positive   blank 22, absent 0    "22 element(s) have Comments but it is empty"
negative   blank 0,  absent 22   "22 element(s) do NOT have ZZZ NO SUCH PARAMETER at all"
```

The inputs moved and the answer moved with them, **in opposite directions**.
That is exactly what tracking asks for. **What it needs is a way to RECORD
that, not another run** — and whoever builds it should check whether
`heron_validate` can accept a tracked proof for a fragment, the way
`prove-agent.py vary` already does for an agent.

## Group W — three fragments stand between the skills and three more proofs

Measured 2026-09-20 against the eleven model-half job files the skills session
generated (#199). Of the ten skills, **seven have no unproven step at all** —
every fragment in their plan is already `PROVEN`, so `batch-prove` reports
ALREADY and sends nothing, and what they are waiting for is a runner that can
execute a COMPOSITION ([row 141](FRAGMENT-ISSUES.md)).

**Three skills have unproven steps, and between them those are only THREE
fragments:**

| skill | the step that is not proven | risk |
|---|---|---|
| `check-connectivity` | `trace-connectivity`, `report-findings` | READ, SUGGEST |
| `find-blank-parameters` | `describe-blank-parameters`, `report-findings` | ANALYZE, SUGGEST |
| `trace-system` | `trace-connectivity` | READ |

**AND ALL THREE ARE THE SAME KIND OF THING, WHICH IS THE POINT OF THIS GROUP.**
Every one of them sits in a Group V bucket that `batch-prove`'s rule cannot
judge, and not one is a defect:

* **`trace-connectivity`** seeds `reached` with its own `start`, so the answer
  can never come back empty whatever it is handed.
* **`report-findings`** answers with PROSE. Its negative is *correct* — it says
  *"NOTHING WAS CHECKED"* — and a non-empty string reads as a find.
* **`describe-blank-parameters`** is the only fragment of 395 declaring no
  `role: result` at all, so there is nothing for the rule to look at. Its run
  record is already a textbook tracking proof and nothing can score it.

**SO ONE PIECE OF MACHINERY UNBLOCKS THREE FRAGMENTS AND THREE SKILLS**: a
tracking mode for fragments, the same shape `tools/prove-agent.py vary` already
gives agents — run one fragment across **three or more** values of one input on
one model and show the answer FOLLOWING the input. [D-53](DECISIONS.md) is the
rule, `prove-agent.py` is the working example, and `MIN_TRACKING_ROWS = 3` is
already enforced in `brain/heron_validate.py` for agents.

**IT IS THE HIGHEST-LEVERAGE THING LEFT IN THE PROVING MACHINERY** and it is
deliberately not started here: PR #198 was already large and carrying the
row 136 root-cause fix, and a feature this size belongs on its own branch
rather than delaying seventeen repairs. Recorded so the next session does not
have to re-derive which three fragments matter or why.
---

## Group X — the SKILLS, and the one thing only the PC can answer — 2026-09-19

**THIS WAS WRITTEN AS GROUP V, THEN W, AND IS X. THE REASON IS THE ROW BELOW IT.** A brief asked
for a "Group V" on 2026-09-19 when none existed — the thirteen it was after are
**[Group I2](#i2--the-model-has-not-got-it-13)** — so this session took the free letter. Another
session took the same free letter the same afternoon for a larger sort, merged first, and keeps it.
Same hazard as the row numbers, one namespace along.

**Ten skills have been DRAFT since Step 14 and nothing could say why.**
[`tools/prove-skill.py`](../tools/prove-skill.py) now asks, and measures every half that a machine
with no Revit can measure. It reports **BLOCKED 2, CROSSING 1, NOT UNDERSTOOD 6, UNDERSTOOD 1** —
and **it cannot print PROVEN**, because a skill is proved when a model has answered.
[`tests/test_skill_proving.py`](../tests/test_skill_proving.py) pins that by reading the verdict
function's own returns.

**Everything below is what is left, and every row of it needs the PC.**

| | What to run, and what it would settle |
|---|---|
| **X1** | **The ten job files.** `tools/jobs/skills/<skill>-model-half.yaml`, one per skill, in `tools/jobs/example.yaml`'s shape, steps in an order that composes and `keep-chain: true` exactly where a need can only come down the chain. `python tools/batch-prove.py <file> --dry-run` first. **Read each header before running it** — it says how many of its own steps `batch-prove` will refuse as `ALREADY`, which for seven of the ten is all of them |
| **X2** | **And that refusal is the finding, not a fault — [row 141](FRAGMENT-ISSUES.md).** `batch-prove` refuses **32 of the 37 steps**, correctly: each capability's own model half is already evidenced. What is NOT evidenced is the **composition**, and nothing in this repository can run one. `prove` in `mcp/client/heron_bridge_client.py` does run a chain — first fragment resets it, the rest continue, one lease — but calls `run_fragment_read` only, so the four MODIFY skills have no chain runner at all, and it judges nothing: no negative case, no draft, no signature. **Chain, write, judgement — no one runner has all three.** Each job file carries the `prove` command line for its own chain, which is as far as today's tooling reaches |
| **X3** | **Two skills need a hand on the mouse before their batch runs.** `check-connectivity` and `trace-system` both go through `trace-connectivity`, whose `start` is one PARTICULAR element. Select it in Revit and type `selected` in the blank — the add-in refuses if none or several are selected, so the batch has to be arranged around that one pick |
| **X4** | **`trace-system` cannot run its own plan at all — [row 142](FRAGMENT-ISSUES.md).** It declares `REPORT_FINDINGS`, and neither `filter-elements-by-id` nor `trace-connectivity` provides the `findings` that fragment needs. Two plausible repairs, both the owner's, neither guessed at. **No Revit needed to decide it; a Revit needed to prove whichever is chosen** |
| **X5** | **The routing half is a RECORDING and will go stale.** `tools/jobs/skills/routing-2026-09-19.json` is what `prove-skill.py --routing-from` and the skill catalogue both read. Re-take it with `python tools/prove-skill.py --routing-to tools/jobs/skills/routing-<date>.json` — about twenty-five minutes for forty-three sentences — and **compare the index fingerprint before comparing any count** ([row 116](FRAGMENT-ISSUES.md)). It needs no Revit, only time |

### What is NOT waiting on the PC, and is waiting on a person

Three findings are one-line edits in `brain/fragments/`, which the session that found them was told
not to touch. They need no Revit at all — only whoever owns those files.

- **[Row 146](FRAGMENT-ISSUES.md)** — seven sentences with an owner named per sentence. The repair
  is [row 116](FRAGMENT-ISSUES.md)'s *declaring, not demoting*: one line added to one `utterances:`
  block, after which identity fires before ranking runs.
- **[Row 134](FRAGMENT-ISSUES.md)** — *"change the insulation thickness"* is the dangerous one.
  `set-compound-layer-width` edits a wall, floor, roof or ceiling **TYPE**; `set-mep-insulation`
  edits what was handed in. A modeller means the pipe.
- **[Row 140](FRAGMENT-ISSUES.md)** — four declarations in three skills name a capability that
  outranks the skill's own risk. **The obvious repair is the one that must not be taken**: raising
  the skill's risk removes it from `check-skill-routing.py`'s `ASKS_A_QUESTION`, and its crossings
  stop being reported.

**And four of [row 146](FRAGMENT-ISSUES.md)'s crossings cannot be repaired by anybody's edit**,
because nothing in the library reads the thing: an MEP element's size, which workset an element is
on, an insulation thickness, and a view's scale. Those are fragments somebody has to write, and then
a Revit to prove them.

> **THE NUMBER COLLIDED TWICE MORE WHILE THIS WAS BEING WRITTEN, AND THEN THE LETTER DID.** This
> session's row was 147 against [PR #200](https://github.com/Ajmalpshaik/Heron-AI/pull/200), then 148
> against [PR #198](https://github.com/Ajmalpshaik/Heron-AI/pull/198), and is **150**; #198's own
> [row 149](FRAGMENT-ISSUES.md) counts the same hazard four times in one day from the other side.
> Then this group, written as V, met a larger Group V from #198 - and its Group W as well, so it is X.
> [U4](#group-u--a-review-found-six-things-in-group-ts-own-work-and-all-six-held) records it happening
> to row 118. **Nothing sees an in-flight branch**, so no local tool can prevent it, and announcing a
> number is only half a fix — it does not help when two sessions announce at the same hour. What
> actually held every time: **keep both, renumber the later one, and check for duplicates
> afterwards** — no row and no group has been lost yet.
