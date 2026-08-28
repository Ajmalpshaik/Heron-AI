# Needs checking — the register

**Everything in this file is UNPROVEN.** It was built on a machine with no Revit, no Windows and no
.NET SDK, so none of it has been compiled or run. This is the list of what has to happen on a machine
that has Revit before any of it can be called finished.

---

## How this file works

**It is the single register.** [`HANDOVER.md`](HANDOVER.md) §6 points here rather than keeping its own
copy, because two lists of the same thing drift and this repository has been bitten by that more than
once.

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

**Needs Windows and the .NET SDK. Does NOT need Revit.** Blocks everything below it — nothing else in
this file can be attempted until A2 passes.

A4 is the one worth doing before you ever open Revit: the round-trip test starts a Revit-free bridge
host and exercises the pipe *and* the lease. Half of group H is already covered there.

| ID | Do this | Pass looks like |
|---|---|---|
| **A1** | `dotnet --version` | A version number. If "not recognized", install the .NET SDK first — `tools/setup.ps1` checks this too and stops with the download link |
| **A2** | `dotnet build -p:RevitVersion=2020` | Builds. **Expect errors first time.** Most likely spots: `Document.CreationGUID`, `WorksharingUtils.GetCheckoutStatus`, `IFailuresPreprocessor`, `TransactionGroup.GetStatus`, `BuiltInCategory.INVALID`, `UIDocument.RefreshActiveView` |
| **A3** | `dotnet build -p:RevitVersion=2024` | Same, on net48. A 2024-only failure is the interesting kind — that is the version boundary that has bitten this work before |
| **A4** | `dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=2024` then `python tests/test_bridge_roundtrip.py` | **Passes — with NO REVIT OPEN.** The pipe round trip *and* most of the lease. This is the biggest single result you can get before touching Revit: if the lease is wrong, it says so here rather than three groups later |

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

---

## When everything above has passed

Step 6 is finished, and not before. At that point:

1. Change the default in `HeronPermissions` **only if you want writing on by default** — and
   [D-19](docs/DECISIONS.md) says why the answer is probably still no.
2. Delete the "never run" banners in `RevitWrite.cs` and `heron_mcp_server.py`.
3. Move the proven rows into [`HANDOVER.md`](HANDOVER.md) §3, under *proven against a real Revit*.
4. Delete this file.
