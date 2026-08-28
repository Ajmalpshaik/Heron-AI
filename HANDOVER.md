# Heron AI — Session Handover

**Updated 2026-08-28, at the end of the fourth working session — the one that found a compiler, then
spent the rest of the day answering questions.** For whoever picks this up next: a fresh Claude session, a
person, or the owner on his phone.

> **If you read only one thing:** three walls fell on one day, none of which needed a Windows machine.
>
> **The C# compiles.** Revit 2020 through **2027**, every project, zero warnings — and the Revit-free bridge
> host *runs*, all 32 checks passing including the whole lease. It caught two real defects on its first
> run, both of which reading had already missed twice ([docs/30](docs/30-compiling-away-from-windows.md)).
>
> **Every open question is answered — 41 of 41**, and **24 decisions** were taken (D-20 to D-43). Nothing
> gates any phase. Several were settled by *looking* rather than deciding: at a system already doing the
> job, and at Heron's own code, where Q-13's answer had been running since Step 1.
>
> **The Constitution is accepted and binding** — all 30 Articles, after Ajmal asked for every one to be
> read out rather than tapping yes. Reading it aloud found three stale statements inside it.
>
> **PHASE 2 STARTED, 2026-08-28.** The owner has **no Revit for about a week** and said so plainly:
> *"checking in Revit is not possible within 1 week, so keep the checking process as a document and
> start Phase 2 — we need to finish that."* [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) is now a **record
> rather than a gate**, and [27 — Build Order](docs/27-build-order.md) carries **Steps 7 to 14**, written
> that day because Phase 2 had never been broken into steps. Seven of the eight need no Revit; only
> Step 14's proof does.
>
> **Step 7 is built and proven-as-far-as-it-can-be**: the fragment's shape on disk, and the validator
> that refuses a proof with no negative case. `python tests/test_fragment_store.py`, 22 checks. The two
> fragments in `brain/fragments/` are `DRAFT` and **neither has met a model** — which is what DRAFT
> means, and why they get no register row. **Step 8 is next.**
>
> **What that did NOT change:** `write.enabled` is still `false`, every parked item is still unproven,
> and `R1` — read the day's decisions back — **did not happen before Phase 2 began**, contrary to the
> instruction that asked for it. That was overridden by the owner, which is his to do; it stays on the
> list for the PC. **`A1`, `A2`, `A3` and `A5` are done** — the whole of Group A except the Windows named
> pipe in `A4` and the probe check in `A6`.
>
> Before writing any code, run `python tools/check-compile.py`. It takes minutes, it now covers **all
> eight releases**, and it is no longer somebody else's job. On a fresh Linux box it needs
> `apt-get install -y dotnet-sdk-10.0` first — the .NET 8 package builds only 2020–2024.

> ## ⚠️ READ THIS FIRST — you are probably on a machine with no Revit
>
> The owner is continuing this work **from mobile**, in Claude Code. **There is no Revit there**, and
> there is no Windows, so **nothing in this repository that touches Revit can be tested.**
>
> That does not mean stop. It means **be honest about which half you are in**, and mark every piece of
> Revit-side work as untested until it has met a real Revit on the owner's own machine.
>
> **What still works away from Revit:** [§5](#5-what-you-can-and-cannot-do-without-revit).
> **What must be re-tested on return:** [§6](#6-the-return-to-the-machine-checklist) — keep it up to date.

Then read [docs/README.md](docs/README.md) for the map and
[docs/27-build-order.md](docs/27-build-order.md) for what to build.

---

## 1. Where the project stands, in one paragraph

**Phase 0 is complete.** Steps 1 to 5 of 6 are built and **proven in real Revit 2020 and 2024**: the
bridge, the thread hop onto Revit's own thread, a working MCP server inside Claude Code, *"select all
ducts"* with an audit trail, and one-chat-one-Revit binding that fails closed. The repository is
**private**.

**Step 6 — the first write — is BUILT, COMPILED, AND STILL UNPROVEN, and the gap between those words is
the whole story of this section.** It was written on a phone, on a machine with no Revit, no Windows and
no .NET SDK. **A compiler has now read every line of it** — Revit 2020, 2021, 2022, 2023 and 2024, zero
warnings — which it had not on 2026-08-27, and which cost one 2020-only defect to find out. The chat half
is tested and passing. But it has still **never loaded into Revit and has never moved anything**. Do not
read "Step 6 compiles" as "Step 6 works": compiling proves the API surface agrees, and says nothing
whatever about whether a duct moves 200 millimetres or 200 feet.

**Heron can no longer be read-only by construction, so it is read-only by default instead.** That is a
real weakening and it was made deliberately: until 2026-08-27 there was no transaction code in the
repository at all, and the guarantee needed no trust. Now there is, and the guarantee rests on
`write.enabled` defaulting to `false` in `HeronPermissions`. **Leave it false until the write path has
been through a real Revit.** [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) is how that happens.

**Four things arrived after the write path, because auditing found them missing rather than anybody
remembering them.** The **lease** ([D-22](docs/DECISIONS.md)) — Step 5 deferred it to *"Phase 1 with
writes"* and Step 6 is that write; a second chat is now refused instead of cutting the first off mid-job.
The **Failure Analysis Agent**, which never blind-retries and fails closed. The **tool registry**, so risk
is declared in a table rather than as a literal buried in the write path. And **configuration and health**,
which between them exposed two silent bugs — `write.enabled` could never have been switched on, and the
two halves disagreed about how long to wait.

**All 21 Golden Rules are now official** ([Q-19](docs/OPEN-QUESTIONS.md), accepted 2026-08-28) — including
the four Step 6 was built to obey. They were accepted *before* Step 6 is proven, deliberately: a rule that
only binds once the code passes is not a rule the code was ever held to.

**PHASE 1 IS BUILT IN FULL — all eleven items, not just Step 6's seven.** The last three were found by
auditing Phase 1 against its own list rather than against the build order, and none of them had been
flagged anywhere:

| | |
|---|---|
| **The audit says WHICH elements** | It logged `moved: 4`. A count cannot answer the question anybody asks after something goes wrong — *which* ducts — and that is the entire point of an append-only record. It now carries every moved element's `UniqueId`, the document identity and the undo entry name, under one Workflow ID |
| **The Golden Test Library** | 17 cases: the 7 things Phase 0 proved, and 10 that have never met Revit. Each records the files its proof rested on, so `python tests/test_golden.py` reports the proofs that have gone **stale**. It currently reports all seven as stale, which is exactly true |
| **The Workflow Engine** | Checkpoints and resume. Built to spec, 18 checks — and **nothing calls it yet**, deliberately. See the untested table in [§3](#3-what-is-proven-and-what-is-only-built) |

**Phase 1's own definition of done is not met, and cannot be met here:** *"a wrong instruction can be
reversed with one Ctrl+Z, and a failed operation leaves the model untouched."* Both are written, both are
covered by reasoning, and **neither has been witnessed.**

**PHASE 2 IS NO LONGER GATED — and by the end of 2026-08-28 neither was anything else.** Twenty-four
decisions that day (D-20 to D-43) closed **every open question in the project, 41 of 41.**
None of it is built; what changed is that building it no longer waits on anybody.

| | |
|---|---|
| **D-23 / D-24** | The knowledge store is **SQLite, one file per scope**, decided on the per-user no-admin install constraint rather than on retrieval quality. Embeddings are **local** — now on the re-indexing argument, since D-26 narrowed the confidentiality one |
| **D-25** | The existing libraries are **studied and re-authored, never imported.** Ajmal's, and it deleted a planned import pipeline rather than choosing between libraries |
| **D-26** | **The model file is never uploaded.** Refined three times in one day, each time looser: the line is the `.rvt`/`.rfa`, not the information. Project names, counts, sizes and reasoning travel like any conversation |
| **D-27** | **There are no personas.** One voice; the *shape* of the answer follows the shape of the request. Settled by reading two assistants already doing this job daily, both of which switch nothing |
| **D-28** | Generated code is **Roslyn C#, in process** — not pyRevit, whose Routes server would put HTTP inside an add-in that has no network code at all. Closes Q-37 as not applicable |
| **D-29** | A fragment is a **composable piece**, not a whole answer: filter + action, with a recipe as a named third kind |
| **D-30** | A fragment is promoted by **one recorded proof with a negative case**, not by a count of runs — because the defect that matters is one that *succeeds while doing nothing*, and that passes a thousand runs |
| **D-31** | Product / data / derived were **already separated** since Step 1, and the updater half was verified rather than assumed |

**And the rest of the day closed every remaining question.** `D-32` to `D-43`, in the same conversation:

| | |
|---|---|
| **D-32** | **v1 must be able to change the model**, and reading is what gets used first. Recorded as read-only and **reversed within the hour** when the same question was asked again |
| **D-33** | **Heron never assumes an input. It asks — and it asks once.** There is no confidence threshold, because a number invented today is a number tuned tomorrow |
| **D-34** | Heron's wording is **English**; understanding the user is the host's job, not Heron's |
| **D-35** | A shared fragment may carry code, and an **unapproved one is refused, not warned about** — a warning hands the decision to whoever is least able to judge it |
| **D-36** | **No warranty**, and it was already in place twice. `DISCLAIMER.md` makes four promises that are **still unproven** |
| **D-37** | The name is **Heron AI** — *chosen, not cleared*. No trademark search has been done |
| **D-38** | **GitHub now**, App Store possible, nothing built for it. Autodesk's requirements have **not been read**, and were not written from memory |
| **D-39** | Shadow mode is approved on an **analysed disagreement**, never a count of agreements |
| **D-40** | The dependency graph is SQLite, and **an edge is derived before it is stored** |
| **D-41** | **Single-user now**; company knowledge is a git repo and the admin is the reviewer — D-35 at a smaller radius |
| **D-42** | The **public install command is deferred**; `setup.ps1` is what is proven |
| **D-43** | **The Constitution is accepted — all 30 Articles, binding** |

**Five decisions get one read-back at the PC before Phase 2 starts** — `R1`, Ajmal's own instruction —
**and `R1b` shows him the trust model working**, since [D-14](docs/DECISIONS.md) stays *Proposed* until he
has seen it. They are Accepted and are being built on; the review confirms each still says what he meant
and fills in detail left out. See the block at the top of [DECISIONS.md](docs/DECISIONS.md).

**Two were reversed within hours of being recorded**, D-26 three times and D-32 once. Neither was a
mistake — each was a first answer sharpened once its consequence was visible, which is the whole argument
for that read-back.

---

## 2. What exists

```
revit/          Part 1 — loads into Revit.exe. C#. Changing it needs a Revit RESTART
  Heron.Revit.Addin/   ribbon toggle, icons, the ExternalEvent dispatcher, the operations
  Heron.Bridge/        named-pipe server, per-session token. No Revit reference
mcp/            Part 2 — the bridge to the AI host. Python, outside Revit
  client/              discovery, retries, doctor. Dependency-free on purpose
  server/              the MCP server and the session binding
brain/          Part 3 — knowledge. EMPTY BY DESIGN until Phase 2
platform/       Part 4 — the Kernel
  Heron.Core/          HeronPaths, HeronConfig, HeronIdentity, HeronAudit
tests/          two suites, both runnable WITHOUT Revit
tools/          scripts that keep the repo honest, plus setup and deploy
docs/           40 documents — specification, decisions, questions, roadmap
.claude/        skills and agents that ship with the repo
```

Each part has a `README.md` saying what belongs there and **when to fix things there**.

---

## 3. What is proven, and what is only built

**This distinction matters more than anything else in this document**, and it matters more than ever
now that the next stretch of work happens where Revit cannot be reached.

### Proven against a real Revit

| | |
|---|---|
| The bridge answers `ping` | Revit 2020 and 2024 |
| One button connects **and** disconnects; the icon shows which | Toggled repeatedly, every transition logged |
| Per-session token, minted per connect | 2024's token on the 2020 pipe → `unauthorized` |
| Newest connection wins — **the pipe only, since Step 6** | *"A newer connection took the session"*, older one dropped. Still true of the transport; a lease now decides who may actually send anything ([D-22](docs/DECISIONS.md)). **The lease itself is unproven** |
| Two Revits at once, separate pipes | `heron.2024.*` and `heron.2020.*` together |
| **The thread hop** | `5,844 elements` from 2024, `3,167` from 2020 |
| **"Revit is busy" instead of a hang** | Dialog open → clean refusal after 10s, recovers by itself |
| **The MCP server inside Claude Code** | Answered from both live Revits, no command run |
| **"Select all ducts"** | 4 found and **highlighted on screen**, confirmed by eye |
| The audit trail | Every request recorded — op, outcome, document, timing |
| **Fails closed when the chosen Revit closes** | 2024 closed with 2020 open → Heron **stopped** |
| `setup.ps1` end to end | Detects, builds, deploys all three in one run |
| Installer skips only the **open** release | 2020 and 2024 open → both skipped by name, 2027 installed |
| Each build carries the right runtime | `net472` / `net48` / `net10.0-windows`, read from the deployed DLLs |

### Built, never met a real Revit

| | |
|---|---|
| **Revit 2027** | Builds and installs. Never launched — the owner says his 2027 does not work |
| **Naming the session in a selection answer** | Fixed after both models turned out to be called `Project1`. Needs a Claude restart to go live, then one look |
| **Anything committed from mobile after 2026-08-28** | **Assume untested** — but no longer assume unread: `python tools/check-compile.py` runs on mobile too. Add it to [§6](#6-the-return-to-the-machine-checklist) |
| **The whole of Step 6** | **Compiles** on 2020–2027 — all eight releases, 0 warnings (2026-08-28). Never loaded into Revit, and has never moved anything |
| `RevitWrite.cs` — preview, re-count, TransactionGroup, rollback | The one file that can change a model. It compiles; every **behavioural** claim in it is still unverified. Its `DocumentKey` was the one thing the compiler caught — `Document.CreationGUID` does not exist in Revit 2020 |
| `HeronUnits`, `HeronPermissions`, `HeronStop` | Kernel C#. Plain arithmetic and flags. Compiles; `HeronUnits` is exactly what `D3` exists to measure |
| The Emergency Stop ribbon button | New `PushButtonData` in a file whose ribbon currently works. **If Heron will not load after this, look here first** |
| **The lease** (`HeronLease`) | Refuses a second chat instead of cutting the first off. **Now exercised end to end against the compiled bridge** (2026-08-28) — claim, renew, refuse a second chat, never cut off the first, exempt `ping`/`info`. Not against Revit, and not over a Windows pipe: group H is what remains |
| **The audit's element list** | Every moved element's `UniqueId` now goes into the log. Never seen against a real model, and a move of several hundred elements writes a correspondingly long line |
| **The Workflow Engine** (`heron_workflow.py`) | Built to spec and covered by 18 checks, but **nothing calls it yet** — and that is deliberate, not an oversight. Phase 1's only multi-stage flow is preview→apply, which the add-in already sequences better because it is the side that can re-count against the live model. Its real customer is Phase 2's 18-stage pipeline. Proven by its tests, unproven in use |
| **Bridge protocol 2** | A request now carries a `client` id. An add-in still on protocol 1 will refuse to talk — which is correct, and means the add-in MUST be rebuilt and redeployed. The handshake itself is proven: `info` reports `protocolVersion 2` |
| `revit_preview_move`, `revit_apply_move`, `revit_use_this_model` | The three new MCP tools. Never seen by a running host |

> The chat side of Step 6 **is** tested and passing — distance parsing, document pinning, single-use
> approval: `python tests/test_write_safety.py`, 38 checks. That test says nothing whatsoever about the
> transaction, the rollback or the move, and says so itself.

### Does not exist at all

| | |
|---|---|
| **Any write to a model** | Step 6. There is no transaction code in the repository |
| **The brain** | `brain/` is empty by design until Phase 2 |

> Nothing here is known-broken. Several things are **untested**, which is different and more honest.

---

## 4. The things that will bite you

Each of these cost real time. They are in the order they were learned.

1. **The Revit API can only be called from Revit's own thread, inside an API context.** An MCP server is
   a separate process and cannot call it at all. Everything marshals through one `ExternalEvent`.
   [docs/03 §4](docs/03-heron-revit.md)

2. **A named pipe needs `CreateNewInstance`, not just `ReadWrite`.** Without it only the *first*
   listener can be created and every retry fails with "access denied".

3. **The discovery file must never carry the document name.** Storing it produced the stale-name trap in
   the owner's earlier work. [docs/25 §2a](docs/25-multi-session-and-binding.md)

4. **"No reply" does not mean "dead".** Test the *process*, not the reply — and check it is still the
   right *program*, because Windows reuses process ids.

5. **An assumption is not a choice.** If one Revit was open, Heron *assumed* it. If the user answered,
   they *chose*. Conflate the two and a second Revit opening mid-chat silently sends everything to the
   first one. [docs/25](docs/25-multi-session-and-binding.md), and `tests/test_session_binding.py`.

6. **Naming the document is not always enough.** Two sessions can both have `Project1` open — it
   happened. Name the session too.

7. **Two waits, not one.** *"Did Revit pick it up?"* and *"having started, did it finish?"* are
   different questions. Collapsing them tells the user Revit is busy while it is actually working.

8. **Retry only what never left the machine.** A failed connect can always be retried. A **lost answer**
   cannot — Step 6 writes must pass `idempotent=False` or a move happens twice.

9. **`Platform=x64` puts build output in `bin/x64/$Configuration`.** Scripts should *find* the
   assembly, not assume the path shape.

10. **An empty array passed to a PowerShell parameter arrives as `$null`**, and `@($null)` has one
    element. This turned "no Revit open" into "one Revit open" and blocked every install.

---

## 5. What you can and cannot do without Revit

### Works anywhere, including mobile

```bash
python tools/check-docs.py             # links, Golden Rule / decision / question references
python tools/check-metadata.py         # the standard, registry vs code, and version agreement
python tools/check-structure.py        # layout, layering, paths, and the PowerShell ANSI trap
python tests/test_session_binding.py   # one chat one Revit, all four cases — pure Python
python tests/test_write_safety.py      # Step 6's CHAT half — distances, pinning, approval
python tests/test_failure_analysis.py  # never blind-retries, and fails closed
python tests/test_tool_registry.py     # both languages agree on what may write
python tests/test_config_and_health.py # settings agree; health means something
python tests/test_workflow.py          # stages resume, stale inputs re-run, no blind retry
python tests/test_golden.py            # which proofs still stand against the current code
```

**232 checks, all passing.**

**And two more things that were believed to need Windows, and do not** (2026-08-28,
[docs/30](docs/30-compiling-away-from-windows.md)):

```bash
python tools/check-compile.py                  # Revit 2020-2027, all four projects, 0 warnings

dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=2024 -p:HeronTfm=net8.0
python tests/test_bridge_roundtrip.py          # 32 checks, including the whole lease
```

The first needs the .NET SDK, which Linux distributions package — Microsoft's CDN is often blocked from a
container and that is the wall earlier sessions hit. The second runs because `Heron.Bridge` has no Revit
reference, so it compiles for `net8.0` and .NET implements named pipes on Unix as a socket.

**What that still does not cover is the Windows named pipe itself** — its naming, its security
descriptor, and the `CreateNewInstance` flag in note 2 of [§4](#4-the-things-that-will-bite-you). `A4`
in the register means the Windows run, and a POSIX pass is a strong signal ahead of it rather than a
substitute for it.

**A third thing, added 2026-08-28** — it reads what the releases actually ship:

```bash
python tools/check-api-surface.py              # every Revit member Heron calls, on 2020 THROUGH 2027
```

It was built while 2025–2027 were being skipped by the compile gate, to leave them with something rather
than nothing. **That skip is gone** — see below — so this is no longer their only cover, and it is still
worth running: it reads the **shipped** assemblies for all eight releases, where a compile reads the
NuGet reference packages for the one release it is building. It looks up the **compiled** add-in's
reference tables — exactly what the code calls. **All 103 exist on every release from 2020 to 2027.** It
matches by name, so a changed signature would still only show up in a real compile; it supplements the
compile gate and says so on every run. **It was validated before its clean result was believed**, by
putting the `CreationGUID` defect back and watching it fail.

**And the skip itself turned out to be one more environment-specific claim, found the same day.** The
newest three releases were reported SKIPPED off Windows as *needing the Windows Desktop SDK*. Those
targets are a property of **the installed SDK package**, not of the operating system: Ubuntu's
`dotnet-sdk-10.0` ships them and its `dotnet-sdk-8.0` does not, and `check-compile.py` was passing
`-p:EnableWindowsTargeting=true` — the flag whose whole purpose is the non-Windows case — **only on
Windows**. With the .NET 10 SDK installed, **all eight releases compile here**, add-in included, 0
warnings. [docs/30 §2a](docs/30-compiling-away-from-windows.md) has the account and the two-directional
validation that was done before the eight greens were believed.

- **All documentation, decisions, specifications and open questions.**
- **The session binding**, in full. Its test fakes the world and exercises the real logic.
- **Reading and reasoning about the C#.** Just not running it.
- **Compiling it, on every supported release** — 2020 to 2027, since the .NET 10 SDK arrived on this
  side of the fence. Two things this repository "could not do here" have now turned out to be things
  nobody had attempted; assume the third is out there and go looking before writing the sentence.

### Cannot be done without Revit — and cannot be faked

- Anything that loads into `Revit.exe`: the ribbon, the icons, the dispatcher, the operations.
- `tools/setup.ps1` and `tools/deploy-addin.ps1` — they need Revit installed, and Windows.
- `test_bridge_roundtrip.py` needs **Windows named pipes**, so it will not run on a phone either.
- Every claim in [§3](#3-what-is-proven-and-what-is-only-built) marked *proven against a real Revit*.

> **If you change C# from mobile, you cannot know it works — but you can now know it BUILDS, so build
> it.** `python tools/check-compile.py` is minutes, needs nothing installed but the .NET SDK, and it
> catches the entire "worked in 2020, broke in 2024" class. Compiling is not testing: say which one you
> did in the commit message, and add the rest to the checklist below. A commit that reads as though it
> were tested is worse than one that admits it was not.

---

## 6. The return-to-the-machine checklist

**The list lives in [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md), not here.**

Every item is unproven, every item has an ID (`A1`, `D3`) so it can be named in a message without
being described again, and they are in dependency order — nothing in group D can be attempted before
group A passes.

It is kept as ONE register rather than a copy in each file, because two lists of the same thing drift
and this repository has been bitten by that more than once. Add to it every time something is built
away from Revit; delete an item only when it has actually passed.

**The single most important line in it is `D3`:** move the ducts 200 mm, then *measure one*. That is
what catches a unit error, and a unit error is the failure that looks fine until somebody measures it
months later.

The one-command path back to a working machine:

```powershell
powershell -ExecutionPolicy Bypass -File tools\setup.ps1
```

It checks Python first, detects installed Revit versions, refuses only the ones that are **open**, and
deploys per-user with no administrator rights. Then start Revit → **Heron AI** tab → **Heron** (click to
connect, again to disconnect), and:

```bash
python mcp/client/heron_bridge_client.py ping
python mcp/client/heron_bridge_client.py count
```

Anything wrong → `python mcp/client/heron_bridge_client.py doctor` prints everything needed to diagnose it.

**For unattended testing**, set `bridge.autoConnect = true` in `%APPDATA%\Heron\config\heron.config`
and the bridge starts without a button press. Default is `false` on purpose — a Revit that was never
connected being invisible is a safety property. **Put it back afterwards.**

---

## 7. The decisions you must not quietly undo

> ### ⛔ Heron is the only codebase you write to
>
> **The owner's instruction, 2026-08-28.** There are two other repositories on this machine — an existing
> Revit add-in and a knowledge package. Both are **reference only**:
>
> - **Never commit to them, never open a pull request on them, never update or upgrade them.** Every
>   change goes to Heron.
>
>   *One exception exists and is closed:* a commit and draft PR were made to the knowledge package on
>   2026-08-28 before this rule was given. They were withdrawn, and then the owner allowed that one
>   through — *"do it for AJ AI, this time only, next time no need"* — so
>   [AJ-AI-Brain#47](https://github.com/Ajmalpshaik/AJ-AI-Brain/pull/47) is open on purpose. **It is not a
>   precedent.** If you find yourself about to add a second one, the answer is no.
> - **Read them freely** — they solve overlapping problems and their scars are worth more than their
>   features. [PROPOSALS Part E](docs/PROPOSALS.md) is what that study produced.
> - **Never copy code or text out of them.** Understand the mechanism, then write it for Heron, in
>   Heron's shape, with Heron's reasoning. His words: *"study and use and make it part of our heron,
>   blindly copy paste dont do it."*
> - **Do not cite them as Heron's authority.** He stops using both once Heron is finished, so a note
>   whose argument is *"go read that other repository"* becomes worthless on that day. State the
>   reasoning here, in full, so it stands on its own.

Twenty-two are in [docs/DECISIONS.md](docs/DECISIONS.md). These are the load-bearing ones:

| | |
|---|---|
| **D-01** | Heron runs as a **Claude Code plugin** |
| **D-02** | **Named pipes**, per-PID. Local-only by construction — the add-in has *no network code at all* |
| **D-05** | **Revit 2020 → latest.** Never extrapolate the runtime table forward; an unlisted release is a build error |
| **D-06** | **C# for Revit, Python for everything outside it.** Settled again on evidence in [Q-39](docs/OPEN-QUESTIONS.md) |
| **D-09** | One `ExternalEvent`, one queue, one handler |
| **D-15** | **Where the field notes disagree with a specification, the field notes win.** Observed beats designed |
| **D-17** | Runtime state is machine-local; user data roams; the audit log stays with the data because it is evidence |
| **D-18** | The Transaction Agent belongs to **Step 6**, not Step 2 — a write path built before its rails ships without them |
| **D-19** | **Writing is off by default** until the write path has met a real Revit. Read-only stopped being structural the moment Step 6 existed; this is what replaced it |
| **D-20** | Millimetres to feet is **arithmetic, not `UnitUtils`** — exact, and nothing for Autodesk to move under it across 2020–2027 |
| **D-21** | Failure analysis is a **table, not a model call** — Heron's failures are its own bounded set of codes |
| **D-22** | A second chat is **refused, not allowed to take over**. *"Newest connection wins"* now describes the pipe only |
| **D-25** | The existing libraries are **studied and re-authored, never imported.** Ajmal's. Nothing is copied; a capability is written from scratch here and starts unproven, whatever status it held where it was read |
| **D-26** | **The model file is never uploaded** — the `.rvt`/`.rfa`, not the information. Refined three times in one day, each looser; read the final rule, not the earlier framings that same-day commits still quote |
| **D-27** | **There are no personas.** One voice; the answer's *shape* follows the request's shape |
| **D-28** | Generated code is **Roslyn C#, in process.** No Python runtime, and **no HTTP server enters the add-in** — the add-in having zero network code is a structural guarantee, not a setting |
| **D-30** | A fragment is promoted by **one proof containing a negative case**, never by a count of successful runs |
| **D-32** | **v1 must be able to change the model.** Reading is what gets used first, but a Heron that cannot change anything is a report tool, not the product |
| **D-33** | **Never assume an input — ask, and ask once.** No confidence threshold: a number invented today is tuned tomorrow, and the first tune to reduce interruptions starts it guessing |
| **D-35** | An unapproved shared fragment is **refused, not warned about**. A warning hands the decision to whoever is least able to judge it |
| **D-39** | Shadow mode is approved on an **analysed disagreement**, never a count of agreements. Agreement is weak evidence; a thing that does nothing agrees with everything |
| **D-43** | **The Constitution is binding** — all 30 Articles. Its own Amendment clause applies: never weakened silently, and never by an agent |

**Golden Rules** — **21, all official** — are in [docs/14](docs/14-golden-rules.md). 16–21 cover undo,
preview-before-modify, sandboxing, permission escalation, document pinning and stale reads, and were
**accepted on 2026-08-28** ([Q-19](docs/OPEN-QUESTIONS.md)) *while Step 6 remained unproven*. That order
was deliberate: a rule that only binds once the code passes is not a rule the code was ever held to.
**They are exactly what Step 6 builds.**

---

## 8. What is waiting on the owner

**Every question is answered — 41 of 41 — and nothing blocks any phase.** What is left is not decisions
the code is waiting on: **two review items at the PC, and nothing else.** The copyright line was the last
outstanding confirmation and it was given on 2026-08-28.

| | |
|---|---|
| **`R1` — read the day's decisions back** | **D-23 to D-43, at the PC, before Phase 2 work starts.** His own instruction: *"now we just recorded, but we will do it one more time."* They are Accepted and are being built on — this is a review, not a hold. A decision reviewed after the code exists gets defended rather than examined, and **two were already reversed within hours** (D-26 three times, D-32 once), which is the evidence for doing it |
| **`R1b` — show him the trust model working** | [D-14](docs/DECISIONS.md) stays **Proposed**. He agreed the direction and said *"show me it working at the PC first."* Use the framing that landed: a family has **a maker** and **an approval status**, and nobody would put those on one dropdown. Phase 2 may be designed against the two axes meanwhile; it may not be called settled |
| ~~Copyright~~ | **CONFIRMED 2026-08-28 — Ajmal PS is correct.** Checked consistent in all four places it appears: the Apache appendix in `LICENSE`, `NOTICE`, `<Company>` in `Directory.Build.props`, and `README.md`. The Apache appendix is filled in rather than left as the `[name of copyright owner]` placeholder, which is the one that is usually missed |

**Two things were answered by NOT answering them, and both are publication tasks rather than gaps:**
[Q-38](docs/OPEN-QUESTIONS.md) — the public install command — and the Autodesk App Store requirements in
[D-38](docs/DECISIONS.md). Both need **current documentation read at the time**, and writing either from
memory is the failure this repository has already had twice. `tools\setup.ps1` is the proven route
meanwhile.

---

## 9. Next — read the decisions back, then prove Step 6

**Step 6 is built. Do not build it again.** All seven items the build order asks for are in the
repository, and so is everything an audit turned up afterwards: the lease, the Failure Analysis Agent,
the tool registry, the configuration and health agents. 47 agents are assigned to a step and none is
unimplemented.

**The first thing at the PC is not a test.** `R1`: read **D-23 to D-43** back to Ajmal, confirm each still
says what he meant, and fill in the detail deliberately left out. Then `R1b`: show him the trust model
working, because [D-14](docs/DECISIONS.md) is still *Proposed*. His instruction — *"now we just recorded,
but we will do it one more time"* — and the timing matters: **before Phase 2 work starts**, because a
decision reviewed after the code exists gets defended rather than examined.

**The evidence for that pass is two reversals on the day itself.** D-26 moved three times, each looser.
D-32 was recorded as *"v1 is read-only"* and reversed to *"it must change things too"* when the same
question was asked again an hour later. Neither was a mistake — each was a first answer sharpened once its
consequence became visible, which is exactly what a read-back is for.

**Then the register, and a compiler is no longer what is missing.** `A1`, `A2` and `A3` are done — see
[docs/30](docs/30-compiling-away-from-windows.md) for how, in one command:

```bash
python tools/check-compile.py     # 2020-2027, all four projects, no Windows and no Revit needed
```

**It found the thing it was built to find, on its first run.** `RevitWrite.DocumentKey()` used
`Document.CreationGUID`, which **does not exist in Revit 2020** — it compiled clean on 2024 and failed on
2020. That property was named in the register as a *likely* problem spot, by reading; reading had already
passed it twice. The fix is not a `#if`: the Project Information element's `UniqueId` is created with the
document, survives save, rename and move, and exists on every release from 2020 to 2027.

**And running the bridge found a second one, in the test rather than the code.**
`test_bridge_roundtrip.py` asserted that nothing held the lease at a point where an earlier unknown-op
probe had already claimed it — the bridge was right and the check was false. It had been written from
reading, in a file that could not run on the machine it was written on. That file now runs on both
platforms, one shim, one set of assertions, and passes 32 checks including the whole lease.

**What no compiler will ever do is tell you a duct moved the right distance.** `D3` is still the line
that matters most in this repository.

### The two things left that need Windows but NOT Revit

```
A4   python tests/test_bridge_roundtrip.py    the WINDOWS named pipe itself
A6   python tools/check-compile.py 2025       that the new SDK probe reads Windows correctly
```

Everything in `A4` except the Windows pipe is already passing — its naming, its security descriptor and
the `CreateNewInstance` flag are what remain, and no amount of Linux gets at them.

**`A5` was here and is done.** All eight releases compile off Windows with the .NET 10 SDK; the row that
said otherwise had assumed an operating system where the answer was a package.

**`A6` is `A5`'s own bill.** Deciding what to skip is now done by looking for the WindowsDesktop targets
under each installed SDK rather than by asking what operating system this is — and that probe has only
ever run on Linux. If it misreads Windows it would skip the three releases **on the one machine where
they already built**, so it is worth thirty seconds at the PC. Writing a check for a machine you cannot
run it on is how this file gets its rows.

### Then the rest of the register, in order

47 items need a real Revit. They are in dependency order and each says what PASS actually looks like.
The three that matter most:

| | |
|---|---|
| **C3** | With `write.enabled` still false, a move must be **refused**, naming the setting. Prove the gate before testing the write, or a passing move proves nothing |
| **D3** | Move the ducts 200 mm, then **MEASURE ONE.** The single most important line in the register — a unit error is the failure that looks fine until somebody measures it months later |
| **D5** | **One** Ctrl+Z puts it all back, as a single undo entry. Two means Golden Rule 16 is broken |

### Record what you prove, as you prove it

`tests/golden/cases.py` is the permanent record — 17 cases, each with what to do and what PASS looks
like. When you prove one, set its `status` to `PROVEN`, the date, the Revit versions, and stamp its
`fingerprint` (run `python tests/test_golden.py --stamp` to see the value; it prints and writes nothing,
because a fingerprint is a claim that a person watched it work).

That is what makes the next regression findable. All seven Phase 0 proofs currently read **STALE** —
proven against a build that no longer exists — and that is the library doing its job, not a fault.

### One thing that is not in the register

**The add-in must be rebuilt and redeployed.** The bridge protocol is now **2** — a request carries a chat
id, and an add-in still on protocol 1 will refuse to talk. That refusal is correct behaviour, but if an
old DLL is still deployed it will look like Heron has stopped working.

---

## 10. How to work on this

- **The owner is a BIM modeller, not a developer.** Explain in BIM terms. He delegates technical choices
  and is right more often than not about product ones — the folder restructure, the metadata standard,
  the toggle button, *"prove it yourself"* and *"study it but take none of its names"* were all his.
- **Do not hand him commands to run when you can run them yourself.** He called that out, fairly.
- **Field evidence beats specification.** The sharpest findings in this repository came from running
  things, not from any document.
- **Ask what already exists before designing anything.** On 2026-08-28 it worked four times out of five:
  three questions were settled by reading a system already doing the job, and `Q-13`'s answer had been
  running inside Heron since Step 1 while the question sat open. Look first; decide second.
- **An environment-specific block belongs in a sentence that names the environment.** *"The C# cannot be
  compiled here"* was true of one container that could not reach one download server. Written without its
  environment it became a fact about the project, and three sessions inherited it.
- **Read a document aloud before asking anybody to accept it.** Ajmal asked for all 30 Constitution
  Articles to be read out rather than tapping yes. Reading them found **three stale statements inside**,
  including eight Articles citing a *"Proposed"* Golden Rule that had been official since that morning.
  None changed what an Article required; all would have been read as current by whoever implements it.
- **Explain in the user's own materials, not in the abstract.** The trust model was explained once as
  *"lifecycle and source axes"* and he said plainly he did not follow it. Explained as a Revit family
  having **a maker** and **an approval status** — two things nobody would put on one dropdown — he agreed
  at once. The second explanation is also a better argument, which is usually the way round it goes.
- **A count cannot express a nuance, and should not be taught to.** Q-34 is *agreed but not signed off*.
  The question counter reads it as answered; the prose beside it carries the rest. The moment a counter
  needs to understand nuance it stops being a fact about the rows.
- **Say what is untested.** "It builds" is not "it works". On mobile it can now genuinely be *built* —
  which is a real rung above where this project was, and still two below *proven*.
- **Reference material is studied, never copied.** His earlier repositories are read for their
  reasoning; none of their names, dependencies or branding come across.
- **Every number in the docs should be derived, not typed.** The tooling exists; use it.
- Commits are authored **Ajmal PS**. The repo is **private**.

---

*Phase 0 is finished and proven. Step 6 is finished and proven of nothing — built carefully, obeying
rules that are now binding, tested where testing was possible, and now compiled on every release from
2020 to 2027. That last clause read **"and never once compiled"** for a day after it stopped being true,
which is this repository's own recurring fault in miniature: the work moved and the sentence about it
stayed still. The documents say honestly which is which, and [`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) is
the list of everything that still owes a test. Nothing in this repository is waiting on another session;
it is waiting on a machine — and twice now, something believed to be waiting on a machine was waiting
on somebody trying it.*
