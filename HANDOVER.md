# Heron AI — Session Handover

**Written 2026-08-27, at the end of the first working session.**
For whoever picks this up next — a fresh Claude session, or a person.

Read this first. Then [docs/README.md](docs/README.md) for the map, and
[docs/27-build-order.md](docs/27-build-order.md) for what to build.

---

## 1. Where the project stands, in one paragraph

Heron AI is specified in full (four documents plus field notes, ~13,000 lines), reviewed, and has its
first working code. **Step 1 of 6 is built and proven in real Revit** — the bridge answers `ping` from
Revit 2024, and the add-in builds for Revit 2020, 2024 and 2027 across all three .NET runtimes. Seventeen
decisions are recorded, 26 questions remain open and none of them block work. The repository is
**private** and stays that way until there is more working code.

**The owner has said Step 1 is not finished and will specify what to add.** Do not assume it is closed.

---

## 2. What exists

```
revit/          Part 1 — loads into Revit.exe. C#. Changing it needs a Revit RESTART
  Heron.Revit.Addin/   ribbon, Connect, Status
  Heron.Bridge/        named-pipe server. No Revit reference — testable without Revit
mcp/            Part 2 — the bridge to the AI host. Python, outside Revit
  client/              discovery + ping + doctor
brain/          Part 3 — knowledge. EMPTY BY DESIGN until Phase 2
platform/       Part 4 — the Kernel
  Heron.Core/          HeronPaths, HeronConfig, HeronIdentity
tests/          acceptance test + the Revit-free host
tools/          five scripts that keep the repo honest
docs/           40 documents — specification, decisions, questions, roadmap
```

Each part has a `README.md` saying what belongs there and **when to fix things there**. That is the
answer to "where do I go for this bug".

---

## 3. What is proven, and what is only built

**This distinction matters more than anything else in this document.**

| | Status |
|---|---|
| Bridge answers `ping` from **real Revit 2024** | ✅ **Proven.** `pong <- Revit 2024, session 24336 (0 ms)` |
| **One button connects and disconnects**, icon shows which | ✅ **Proven in real Revit.** Toggled repeatedly; every transition in the log |
| **Disconnect withdraws the announcement** | ✅ **Proven in real Revit.** Discovery file gone, client says nothing is connected |
| **The token is enforced** | ✅ **Proven in real Revit.** Wrong token → `unauthorized`, and it never leaks whether the op exists |
| **The newest connection wins** | ✅ **Proven in real Revit.** *"A newer connection took the session"*, the older one dropped |
| **Daily log, in real UTC** | ✅ **Proven in real Revit.** `addin-<date>.log`; `21:28Z` is 00:28 local, so the `Z` is honest |
| Add-in builds for Revit **2020, 2024, 2027** | ✅ **Proven.** `net472`, `net48`, `net10.0-windows` — the whole span of D-05, read back out of each deployed DLL |
| Deploy per-user, no admin, no Autodesk DLLs shipped | ✅ **Proven.** 34 KB deployed, all three versions |
| `tools/setup.ps1` end to end | ✅ **Proven.** Detects, builds and deploys all three in one run |
| Acceptance test — 20 cases incl. parser, token, preemption, toggle | ✅ **Proven**, runs without Revit |
| Discovery survives Revit startup, prunes only dead processes | ✅ **Proven** by planting live and dead entries |
| **Two Revits at once, different releases** | ✅ **Proven.** 2024 and 2020 connected together, separate pipes, separate tokens |
| **A session's token is worthless on another session** | ✅ **Proven in real Revit.** 2024's token on the 2020 pipe → `unauthorized` |
| **The installer skips only the open release** | ✅ **Proven against real open Revits.** 2020 and 2024 open → both skipped by name, 2027 installed |
| **Revit 2020 running the `net472` build** | ✅ **Proven.** Add-in loaded, bridge answers on `heron.2020.27160` |
| **Revit 2027 actually running** | ⚠️ **Built and installed, never launched** |
| **Anything touching the Revit API** | ⛔ **Does not exist.** That is Step 2 |

> Nothing here is known-broken. Several things are **untested**, which is different and more honest.

---

## 4. The five things that will bite you

Learned the hard way this session. Each cost real time.

1. **The Revit API can only be called from Revit's own thread, inside an API context.** An MCP server is
   a separate process and cannot call it at all. Everything must marshal through one `ExternalEvent`.
   **This is absent from all four specification documents** and is the whole of Step 2.
   [docs/03 §4](docs/03-heron-revit.md)

2. **A named pipe needs `CreateNewInstance`, not just `ReadWrite`.** Without it only the *first*
   listener can be created and every retry fails with "access denied". Cost an hour.

3. **The discovery file must never carry the document name.** Storing it produced the stale-name trap in
   the owner's earlier work — the list said `BL006A` after `BL003A` was opened. Static facts in the file;
   everything dynamic is queried live. [docs/25 §2a](docs/25-multi-session-and-binding.md)

4. **"No reply" does not mean "dead".** Revit takes a minute to start and publishes its discovery file
   before the listener is ready. Test the *process*, not the reply. Never delete on a guess.

5. **`Platform=x64` puts build output in `bin/x64/$Configuration`.** Scripts should *find* the assembly,
   not assume the path shape.

---

## 5. Run these before you change anything

Five seconds, and they have caught four real bugs a person would not have.

```bash
python tools/check-docs.py        # links, Golden Rule / decision / question references
python tools/check-metadata.py    # the standard, AND registry vs code in both directions
python tools/check-structure.py   # layout, layering, adapter boundary, path ownership
python tests/test_bridge_roundtrip.py   # the bridge, no Revit needed
```

Things they have caught: **166 agents claimed against 196 actual** · three source files missing metadata ·
a path built in two places · a file referencing `Autodesk.Revit` outside `revit/`.

**A stated count is a claim. A derived count is a fact.** `tools/recount-agent-registry.py` rewrites every
number in the agent registry from its own rows — never type one by hand.

---

## 6. To test in Revit yourself

```powershell
powershell -ExecutionPolicy Bypass -File tools\setup.ps1
```
Detects installed Revit versions, refuses while Revit is running, builds and deploys each.

Then start Revit → **Heron AI** tab → **Heron** (click to connect, again to disconnect), and:

```bash
python mcp/client/heron_bridge_client.py ping
```

Anything wrong → `python mcp/client/heron_bridge_client.py doctor` prints everything needed to diagnose it.

**For unattended testing**, set `bridge.autoConnect = true` in `%APPDATA%\Heron\config\heron.config`
and the bridge starts without a button press. Default is `false` on purpose — a Revit that was never
connected being invisible is a safety property.

---

## 7. The decisions you must not quietly undo

Seventeen are in [docs/DECISIONS.md](docs/DECISIONS.md). These are the load-bearing ones:

| | |
|---|---|
| **D-01** | Heron runs as a **Claude Code plugin** |
| **D-02** | **Named pipes**, per-PID. Local-only by construction — the add-in has *no network code at all* |
| **D-05** | **Revit 2020 → latest.** Three runtimes, one source tree, adapters absorb the differences |
| **D-06** | **C# for Revit, Python for the brain** |
| **D-08** | **Apache 2.0** |
| **D-09** | One `ExternalEvent`, one queue, one handler |
| **D-15** | **Where the field notes disagree with a specification, the field notes win.** Observed beats designed |
| **D-17** | Runtime state is machine-local (`%LOCALAPPDATA%`); user data roams (`%APPDATA%`); the audit log stays with the data because it is evidence |

**Golden Rules** — 15 official plus 6 proposed — are in [docs/14](docs/14-golden-rules.md). The proposed
ones (16–21) cover undo, preview-before-modify, sandboxing, permission escalation, document pinning and
stale reads. **None of the four specifications mention them**, and they are what would let a BIM manager
approve Heron for live project work.

---

## 8. What is waiting on the owner

| | |
|---|---|
| **Step 1 additions** | He has said it is not finished and will specify. **Ask before assuming it is closed.** |
| [Q-34](docs/OPEN-QUESTIONS.md) | Confirm the unified trust model (D-14, still Proposed) |
| [Q-35](docs/OPEN-QUESTIONS.md) | Confirm the [Constitution](HERON_CONSTITUTION.md) — 30 Articles |
| [Q-19](docs/OPEN-QUESTIONS.md) | Confirm Golden Rules 16–21 |
| [Q-38](docs/OPEN-QUESTIONS.md) | The exact install command — needed before going public |
| Copyright | `LICENSE` and `NOTICE` say **Ajmal PS**. Confirm that is right |

---

## 9. Next, when Step 1 closes

**Step 2 — the thread hop.** Add one `ExternalEvent`, one request queue, one `IExternalEventHandler`, and
one operation: *count the elements in the active document*.

**Prove it:** a real number comes back from a real model. Then, with a modal dialog open in Revit, ask
again and get a clean *"Revit is busy"* rather than a hang.

Small in code. **The highest-risk step in the project** — everything Heron will ever do passes through
that one mechanism, and getting it wrong means every later feature inherits the mistake.

Watch for: never cache a `Document` across invocations · nothing blocks the main thread · `Raise()` is a
request, not a guarantee.

---

## 10. How to work on this

- **The owner is a BIM modeller, not a developer.** Explain in BIM terms. He delegates technical choices
  and is right more often than not about product ones — the folder restructure, the metadata standard,
  the report-checking agent and *"prove it yourself"* were all his, and all correct.
- **Do not hand him commands to run when you can run them yourself.** He called that out, fairly.
- **Field evidence beats specification.** Three of the sharpest findings in this repository came from his
  earlier project, not from any document.
- **Say what is untested.** "It builds" is not "it works".
- **Every number in the docs should be derived, not typed.** The tooling exists; use it.
- Commits are authored **Ajmal PS**. The repo is **private**.

---

*Nothing in this repository is finished except Step 1's bridge, and the owner says even that has more to
come. Everything else is specified, decided, or waiting — and the documents say honestly which.*
