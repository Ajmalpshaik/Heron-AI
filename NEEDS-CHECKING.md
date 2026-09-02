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

**59 items, 6 done, 53 left** — counted from the rows on 2026-08-29, not carried forward. Of the 53:
**48 need Revit**, **3 need Windows but not Revit** (`A4`, `A6`, `A8`), **1 needs only a reachable
network** (`A7`), and **1 needs a screen in front of him** (`R1b` — seeing the trust model work).
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

**Does NOT need Revit.** This group is now **four rows done of eight**, and of the four left, three
(`A4`, `A6`, `A8`) each need Windows for one specific reason rather than in general. The fourth, **`A9`,
needs only a machine with the .NET SDK on it** - it is the batch of fragments written on 2026-09-02 in a
container where the SDK could not be installed, so for the first time in this library some C# has never
been near a compiler. `A8` arrived on
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
| **A4** | `dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=2024` then `python tests/test_bridge_roundtrip.py` — **on Windows** | **Mostly done 2026-08-28, and the remainder genuinely needs Windows.** All 32 checks pass on Linux: framing, the JSON parser, the token, newest-connection-wins, the toggle cycle and **the whole lease**. What a Linux run cannot touch is the Windows named pipe itself — its naming, its security descriptor, and the `CreateNewInstance` flag from [HANDOVER](HANDOVER.md) §4 note 2. Run it once on Windows and this row goes. **The runtime is now detected rather than pinned** (2026-09-02): the test asks `dotnet --list-runtimes` and builds for the newest `Microsoft.NETCore.App` the machine can actually run. It used to hardcode `net8.0`, so on a box carrying only the .NET 10 runtime the host built and then refused to start — *"You must install or update .NET"* — and the suite reported *not found* for four sessions running, which reads as a missing build rather than a missing runtime. A pinned framework in a test meant to run on whatever machine is in front of it is a machine-specific assumption written down as a constant. Nothing to remember now. Re-run on Linux 2026-08-29: **all 32 checks pass**, lease included |
| ~~**A5**~~ | ~~`python tools/check-compile.py 2025 2026 2027`~~ | **DONE 2026-08-28, and it did not need Windows either.** All four projects compile on 2025, 2026 and 2027 — `net8.0-windows` and `net10.0-windows`, 0 warnings — on Linux, with the .NET 10 SDK and `-p:EnableWindowsTargeting=true`. This row assumed the Windows Desktop SDK was a property of the operating system; it is a property of the **SDK package**, and Ubuntu's `dotnet-sdk-10.0` ships it while its `dotnet-sdk-8.0` does not. The script had the right MSBuild flag and applied it **only on Windows**, where it does nothing. Full account and the validation in [docs/30 §2a](docs/30-compiling-away-from-windows.md). `python tools/check-api-surface.py` still runs and still adds something a compile cannot — it reads the **shipped** assemblies, where a compile reads the NuGet reference packages |
| **A6** | On **Windows**, run `python tools/check-compile.py 2025` and read the first lines of output | It says **WindowsDesktop targets found in the .NET N SDK** and then builds. `check-compile.py` now decides whether it can build the WPF releases by looking for `Sdks/Microsoft.NET.Sdk.WindowsDesktop` under each installed SDK, instead of by asking whether it is on Windows — and **that probe has only ever been run on Linux.** Microsoft's SDK is expected to carry those targets on every platform, but expected is the word this register exists to distrust. If it wrongly reports them absent, 2025–2027 would be **skipped on the one machine where they used to build**, so this is a five-minute check with a real downside behind it. If it does misread, the script attempts the build anyway whenever it cannot read the SDK list at all — that fallback is also unproven here |
| **A8** | On the PC, with the Heron MCP server configured in Claude Code, ask it *"what can you do?"* and watch `heron_capabilities` run. Then `heron_lookup` with *"select all the ducts"* | **Mostly done 2026-08-31, and it did not need Windows either — read what moved and what did not.** This row existed because the three brain tools were written on a machine with **no MCP SDK installed**, so `FastMCP` had never served them. That was a missing **package**, not a missing operating system: the SDK is pure Python and installs from PyPI. Installed on Linux, a real SDK now serves **all ten** tools with their descriptions and argument schemas, and `heron_capabilities`, `heron_resolve` and `heron_lookup` all answer through the SDK's own dispatch with both refusals intact — [`tests/test_mcp_serves.py`](tests/test_mcp_serves.py), which is that check made permanent. **Installing the SDK immediately found a defect no text read could see**: `pip install --user mcp` now resolves to **2.x**, which deleted `mcp.server.fastmcp`, so Heron's server raised `ImportError` before registering a single tool — every Heron tool absent from the host. Fixed, and proven on **both** SDK majors side by side. **What genuinely remains needs the PC:** a real host over stdio — that Claude Code connects, renders the docstrings, and picks a tool from them. In-process dispatch is a strong signal ahead of that, not a substitute for it. **PASS is what the host shows:** ten jobs, **all ten** with every part provided and **none** unprovided (this row said *four* and *seven missing* until 2026-08-31 — it was written on 2026-08-29 **before** the seven capabilities were written later the same day, so as worded it would have failed a correct Heron), and every answer saying plainly that it **cannot run** any of them. **And one assertion here is RETIRED rather than repaired, with its reasoning, because quietly deleting it would look identical.** This row also required `heron_lookup` to answer `FILTER_ELEMENTS_BY_CATEGORY`. Measured, it answers `SET_SELECTION` (provided by `FRG-SEL-001`), with `FILTER_ELEMENTS_BY_CATEGORY` the runner-up and both routes agreeing. That is **the same assertion Steps 10 and 11 already retired**, written into the register and not retired alongside them: *"select all the ducts"* is filter-**then**-select, a composition, and a composition is what a **skill** names — `heron_capabilities` lists it as the skill *"Show me these on screen"*. Asking which single capability serves that sentence is asking the wrong layer, and of the two, `SET_SELECTION` is the one the sentence actually ends at. So the check is that `heron_lookup` **names a capability and its route**, not which one | 
| **A7** | On a machine that can reach **huggingface.co**, `pip install --user model2vec` then `python brain/heron_embed.py "stop the air going the wrong way"` | The first line reads **`Backend: model`**, not `lexical`, and the duct fragment comes back for a sentence that contains none of its words. **Step 10 shipped with two backends and only one of them has ever run.** The built-in `lexical` one works offline with nothing installed and is measured, in numbers, as *not meaning* — synonyms score zero. The trained one is the half that understands a grille and a diffuser are cousins. **Still blocked on 2026-08-31, on a second and different container, and the diagnosis is now sharper than "no network":** `pip install model2vec` **succeeds** — PyPI is reachable and the package is fine — and it is `StaticModel.from_pretrained("minishlab/potion-base-8M")` that fails, with `ProxyError: 403 Forbidden`. **So reaching PyPI is not the precondition; reaching the WEIGHTS HOST is**, and the row now names it instead of saying "a model host". **One thing this did prove.** With model2vec actually installed, `_load_model()`'s fallback ran against a **failed download** rather than a missing import — a path that had never once executed, since before this the package was simply absent — and `heron_embed.py` degraded to `lexical` and **said so on its first line**. **Needs no Revit and no Windows** — any machine that can reach huggingface.co will do | 
| **A9** | On any machine with the .NET SDK, run `python tools/check-fragments-compile.py` and read the result for **every** release | Every fragment compiles, as the tool itself reports - do not carry a number here, read its output. **This row exists because FORTY-EIGHT fragments were written on 2026-09-02 in a container with no .NET SDK, and none could be installed:** the SDK download host is refused by that network's policy, which answers `403` to the CONNECT itself. They are `CREATE_PIPE`, `SET_MEP_SLOPE`, `CONNECT_OPEN_ENDS`, `FIND_DUPLICATE_VALUES`, `FIND_OVERLAPPING_TAGS`, `COLOR_BY_PARAMETER`, `ADD_PROJECT_PARAMETER`, `DUPLICATE_TYPE`, `CREATE_CEILING`, `CREATE_ELECTRICAL_RUN`, `JOIN_GEOMETRY`, `CREATE_WORKSET`, `CREATE_SELECTION_FILTER`, `SET_VIEW_UNDERLAY`, `RELOAD_LINKS`, `REPLACE_MATERIAL`, `AUTO_SIZE_MEP`, `CONNECT_AIR_TERMINALS`, `HIGHLIGHT_VS_REST`, `APPLY_VIEW_FILTER`, `CREATE_FLOOR`, `RENAME_FAMILY`, `CREATE_LEGEND_VIEW`, `CHECK_ROOM_MEP_COMPLETENESS`, `CREATE_WALL`, `CREATE_MATERIAL`, `REPORT_VIEW_FILTERS`, `SET_DATUM_BUBBLES`, `CREATE_FILLED_REGION`, `CREATE_KEY_SCHEDULE`, `FIND_OVERLAPPING_LINES`, `COPY_FROM_LINK`, `COPY_VIEW_FILTERS`, `REMAP_LINE_STYLES`, `SET_VIEW_WORKSET_VISIBILITY`, `CREATE_SHEET_LIST`, `REPORT_GLOBAL_PARAMETERS`, `CREATE_LINE`, `ASSIGN_SCOPE_BOX_TO_VIEW`, `SET_VIEW_CROP_TO_SHAPE`, `READ_GRAPHIC_OVERRIDES`, `CHECK_SLEEVE_SIZE`, `CHECK_EQUIPMENT_CLEARANCE`, `CHECK_VALVE_ACCESSIBILITY`, `CHECK_VERTICAL_CLEARANCE`, `AUDIT_MEP_OPENINGS`, `CREATE_ROOM_ELEVATIONS` and `CREATE_HVAC_ZONE`. **What was done instead is worth knowing, and worth not mistaking for a compile:** nuget.org IS reachable from there, so the Revit reference assemblies for 2020, 2024 and 2027 were downloaded and every API member used was looked up in their own metadata - type present, member present, argument COUNT. That catches the failure this repository has actually had (a member a release does not ship, like `Document.CreationGUID`) and it catches nothing else: **argument TYPES and plain C# syntax are unchecked**, and no metadata read substitutes for a compiler. **It has now caught five real ones before they were written**: `ExternalDefinitionCreationOptions.Type` is gone by 2024; `Ceiling.Create` does not exist on 2020 at all; and `VariesAcrossGroups` is on `InternalDefinition` rather than `Definition`, so the obvious line compiles on no release. The fourth is a member that exists on ONE END ONLY and is the reverse shape of the others: `GlobalParameter.IsValidDataType` is present on 2020 and gone by 2024, so a reasonable-looking guard written against the oldest release would have turned every newer build red - a check that reads only the release in front of it finds nothing wrong. The fifth is a whole CAPABILITY a release takes away rather than a member: Revit **2027 removes HVAC zone creation from the API entirely** - the creation factory has no zone method left and `Zone.AddSpaces` is gone, while `Zone` itself remains, so the type existing proves nothing. `CREATE_HVAC_ZONE` reaches both vanished calls BY NAME for that reason: naming either directly would break the 2027 build, and a try/catch does not help because it never gets to run. **Walk the base types when checking** - `Space` declares neither `Number` nor `Area`, and `HostObjAttributes` does not declare `FamilyName`; both inherit them, and a check that stops at the declared type reports a false absence. **And match the FULL type name**: several short names resolve first to an internal marshalling struct rather than to the real class - `OverrideGraphicSettings` reads as having no constructors at all that way, and it has two. So this is the FIRST thing to run on a machine that has one, before any Revit work - a compile error found here costs a minute and the same error found at the PC costs a round trip through Revit |

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

A third ribbon button was added to a panel that already worked. If Heron disappears entirely, this is why.

| ID | Do this | Pass looks like |
|---|---|---|
| **B1** | Start Revit 2020 | The **Heron AI** tab is there |
| **B2** | Look at the panel | **Three** buttons: Heron, Bridge Status, Emergency Stop |
| **B3** | Press Heron | Connects, icon lights, as before. Step 5 behaviour must not have regressed |
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
| **C1** | Press Emergency Stop | Dialog says Heron is stopped, **and** says it cannot interrupt something already running, **and** names Ctrl+Z |
| **C2** | Press it again | Says Heron can work again |
| **C3** | With `write.enabled` still **false** (the default — do not change it yet), ask to move ducts | **Refuses**, and names `write.enabled` and the config file path. Nothing goes to Revit |
| **C4** | Press Emergency Stop on, then ask to move ducts | Refuses because of the stop, not because of the permission — two different refusals, and the message must say which |
| **C5** | With `write.enabled` still false, ask to **select** ducts | **Works.** The gate blocks MODIFY, not READ or EXECUTE — if selecting is refused, the levels are wrong |
| **C6** | Press Emergency Stop on, then ask to **count** elements | **Works.** The stop blocks changes only; taking away the read tools at the moment somebody is diagnosing would be the wrong help |
| **C7** | Now set `write.enabled = true` in `%APPDATA%\Heron\config\heron.config`, restart Revit | — |
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
