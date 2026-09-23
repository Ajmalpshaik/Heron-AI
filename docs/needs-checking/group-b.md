# Needs checking — Group B

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group B — does Revit still load

The panel was rebuilt: Heron and Bridge Status are now one split button, and a ribbon that throws
during `OnStartup` costs the whole add-in. If Heron disappears entirely, this is why.

| ID | Do this | Pass looks like |
|---|---|---|
| **B1** | Start Revit 2020 | The **Heron AI** tab is there |
| ~~**B2**~~ | Look at the panel — [full row](../needs-checking-archive/group-b.md#row-b2) | **DONE 2026-09-06, twice.** |
| ~~**B2a**~~ | Click the arrow under Heron — [full row](../needs-checking-archive/group-b.md#row-b2a) | **DONE 2026-09-06.** |
| ~~**B2b**~~ | Pick Bridge Status, then look at the top of the split button — [full row](../needs-checking-archive/group-b.md#row-b2b) | **DONE 2026-09-06.** |
| ~~**B3**~~ | Press Heron — [full row](../needs-checking-archive/group-b.md#row-b3) | **DONE 2026-09-06.** |
| **B3a** | Press Heron again to disconnect, then re-open the arrow and pick Bridge Status | Says **Not connected** — the item inside the list still runs its own command, it did not become part of the toggle |
| **B4** | `python mcp/client/heron_bridge_client.py ping` then `count` | Both answer, as they did before Step 6 |

### The activity banner — [D-50](../DECISIONS.md). IT HAS BEEN SEEN — 2026-09-07

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
stops at 2024. That is [docs/30](../30-compiling-away-from-windows.md)'s own finding, re-proved from
a different container — **do not conclude "no compiler here" from a failed download again.**

| ID | Do this | Pass looks like |
|---|---|---|
| ~~**B5**~~ | `python tools/check-compile.py` — [full row](../needs-checking-archive/group-b.md#row-b5) | **DONE 2026-09-07, on Linux, all eight releases.** |
| ~~**B5b**~~ | Build on Windows and deploy into Revit — [full row](../needs-checking-archive/group-b.md#row-b5b) | **DONE 2026-09-07, on the owner's PC.** |
| ~~**B6**~~ | Connect, then ask for a count — [full row](../needs-checking-archive/group-b.md#row-b6) | **PROVED 2026-09-07** |
| ~~**B7**~~ | Watch the same card after the answer arrives — [full row](../needs-checking-archive/group-b.md#row-b7) | **PROVED 2026-09-07** |
| ~~**B8**~~ | With `write.enabled = true`, ask to move ducts and approve — [full row](../needs-checking-archive/group-b.md#row-b8) | **PASSED 2026-09-07, after the token fix.** |
| ~~**B9**~~ | Click a ribbon button through the card while it is up — [full row](../needs-checking-archive/group-b.md#row-b9) | **PROVED 2026-09-07 by reading the window itself** |
| ~~**B10**~~ | ~~Ask something with a dialog open in Revit, so it refuses with `revit_busy`~~ | **PROVED 2026-09-07**, owner's Visibility/Graphics dialog left open on purpose. The card goes **RED** - red dot, red **STOPPED** chip - reading **"Heron AI stopped"** over **"Revit was busy - nothing was sent"**. **This is the case that was invisible before**: the chat got a sentence and the screen showed nothing. Run twice; the client waited the full **10.3 s** busy timeout both times before the refusal rendered, which is `revit.busyTimeoutSeconds` = 10 doing its job, not a hang |
| ~~**B11**~~ | Run a batch of fragments back to back — [full row](../needs-checking-archive/group-b.md#row-b11) | **PROVED 2026-09-07** |
| **B12** | On a 150% display, and with Revit on a second monitor | **HALF PROVED 2026-09-07.** **Second monitor: PASSES.** Revit was on DISPLAY1, the non-primary screen at **x=-1920** - negative coordinates, the exact case that sends a naive banner to the primary screen. Measured by pixel-diff against a clean frame, the card spans x=738..1196 of Revit's 1936-wide window: centre **967** against the window's **968**, so **1 px off**. It followed Revit, not the primary screen. **150% DPI: STILL UNPROVEN** - that monitor reports 96 DPI (100%). The DPI transform has not been exercised at all |
| ~~**B13**~~ | Put `ui.activityBanner = false` in `%APPDATA%\Heron\config\heron.config`, restart Revit, ask for a… — [full row](../needs-checking-archive/group-b.md#row-b13) | **PASSED 2026-09-07.** |

**A trap this session walked into, worth writing down.** `CLIENT_ID` in
[`heron_bridge_client.py`](../../mcp/client/heron_bridge_client.py) is `HERON_CLIENT_ID` **or a fresh
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
| [`BridgeServer.cs`](../../revit/Heron.Bridge/BridgeServer.cs) | 349 | the **session** token, from the discovery file — the auth gate, checked **before anything else** |
| [`RevitWrite.cs`](../../revit/Heron.Revit.Addin/RevitWrite.cs) | 103 | the **approval** token, minted by `preview_move` |

[`heron_mcp_server.py`](../../mcp/server/heron_mcp_server.py) line 464 sends the approval token as
`op_args={"token": token}`, and [`heron_bridge_client.py`](../../mcp/client/heron_bridge_client.py) line 240
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
[D-52](../DECISIONS.md) warns about. **This is a gap in the PROOF METHOD, not in the fragment**, and
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
with no `try`](../../brain/fragments/read-space-loads/impl/any/fragment.cs) around them.

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
