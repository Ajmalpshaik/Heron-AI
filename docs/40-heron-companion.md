# 40 — Heron Companion: a window beside the chat that shows what Heron does and lets you edit a table

> **Status: A DESIGN, NOT A DECISION. Nothing here is built.** Written 2026-09-29 as Phase 0 of the
> owner's Companion request. No decision number has been spent on it — the decision it needs is drafted
> in §19 and is filed only on the owner's word, the way [36](36-remembering-between-steps.md) was.
> **Phase 1 does not start until the owner has answered the questions in §18**, because §2 lists
> places where the request and Heron's own rules disagree, and those are the owner's to settle, not this
> document's.
>
> **Since then - read §21 first.** Question 1 was answered yes on 2026-09-29 and filed as
> [D-108](decisions/D-108.md); the other questions are still open.

---

## 0. The idea in Revit words

Today you type in Claude Code, Claude calls Heron, and Revit changes. You cannot see what Heron read or
changed except by reading the chat, and every small correction is another message.

**The Companion is a web page on your own PC, open beside Revit.** It does three things:

1. **Shows what Revit is doing now** — which model, which view, what you have selected.
2. **Shows what Claude asked Heron to do** — each read and each change, with the result, as a list.
3. **Gives you an editable table when Claude opens one.** Think of a Revit schedule with an extra
   column you can type into. Claude opens *"Ducts in this view — Current colour — New colour"* with
   one call. You change the cells. You press **Apply**. Revit changes, as one entry in Revit's undo
   list. Claude is not asked, and no tokens are spent on your edits.

**What the Companion never does:** it never talks to Claude or any AI, never reads your Claude login,
and never opens Revit to the network. It is a viewer and an editor. The chat stays the only place
you talk to Heron.

---

## 1. What was asked, and the rules it must keep

**The goal, confirmed:** a local page (localhost now, perhaps a desktop window later) that sits between
Claude and Revit and gives (1) live Revit status and selection, (2) every Heron action Claude takes,
(3) an editable table Claude opens with **one** tool call whose edits go to Revit with **no** Claude
involvement, and (4) an undo for every change made from the table.

**The owner's hard rules for it**, each checked against the code in this document:

| Rule | Where this design keeps it |
|---|---|
| Never sends chat or prompts to any AI; never uses a Claude login, token or API key | §5 — the Companion has no code that reaches Claude; its only doors are the page and the pipe Heron already uses |
| Follows AGENTS.md, the Constitution, PROJECT-MAP and every folder README | §2 lists every place they disagree with the request, rather than choosing |
| Revit's API only inside `revit/` | §15 — the only Revit code is one new file in `revit/Heron.Revit.Addin/` and two fragments in `brain/fragments/` (fragments are compiled inside Revit by the add-in, as every fragment already is) |
| The same write gate, no second weaker path | §8.4 — Apply sends the same operation `revit_change` sends, `run_fragment_write`, through the same add-in gate |
| One named transaction per user action, so Ctrl+Z works | §10 — one Apply is one fragment run, one `TransactionGroup`, one undo entry |
| Localhost only, protected from other websites | §11 |
| Existing tools, pipe protocol and Revit 2020 → latest unbroken | §15 — no existing tool changes behaviour; no pipe message changes in Phases 1–3 |
| Replies to Claude are short | §13 — each new tool answers in one or two lines |

---

## 2. Where the request and Heron's rules disagree — the owner decides these

**AGENTS.md: "Where framing and the specification disagree, the specification wins" — and a policy
conflict is left open, never closed by whoever is building.** These are the conflicts found. None is
resolved here; each has a recommendation and a question in §18.

| # | The request says | Heron's rule says | Source | Recommendation |
|---|---|---|---|---|
| **C1** | Build a separate window the modeller works in | *"The modeller works with Heron in the Claude chat, as D-01 chose"*, and D-01's *"nothing is being built for it"* stands again since Talk was removed | [D-105](decisions/D-105.md), [D-01](decisions/D-01.md) | **A new decision is needed before any code.** The Companion is not Talk: it carries no words to Claude, so the failure that removed Talk (a message taken and lost, NEEDS-CHECKING row AS4) cannot happen here. But it IS a second place to work with Heron, and D-105 closed that door on 2026-09-27. Only the owner reopens it. Draft in §19 |
| **C2** | A localhost web page that can cause Revit changes | D-02 chose named pipes *over* localhost HTTP because HTTP *"brings port conflicts, firewall prompts, and requires authentication or any local process could drive Revit"* | [D-02](decisions/D-02.md) | Keep D-02 exactly as it is — **Revit still opens no socket**. The HTTP server lives in the Python MCP process and binds `127.0.0.1` only. The three costs D-02 named are answered in §11 (port 0, loopback only so no firewall prompt, a per-session secret). The decision in §19 must say so in writing |
| **C3** | Track the selection on Revit's Idling event | *"`Idling` is used only for a lightweight liveness heartbeat"* | [D-09](decisions/D-09.md) | Revit **2023 and later** have a `SelectionChanged` event and need no Idling at all. **Measured** by reading the shipped reference assemblies: `SelectionChangedEventArgs` is present in RevitAPIUI for 2023, 2024, 2025, 2026 and 2027 and absent for 2020, 2021 and 2022. Only 2020–2022 need an Idling check, throttled. That is an amendment to D-09 and goes in the §19 decision |
| **C4** | *"Before applying: re-check elements; **skip and report** stale rows"* | Article 12c: *"re-count — if the number changed, **stop and re-present**. A preview accepted for 247 elements must never execute on 261"* | [Constitution 12c](../HERON_CONSTITUTION.md), [Golden Rule 21](14-golden-rules.md) | **Follow 12c: if any row is stale, change nothing, mark the stale rows, and let the modeller press Apply again.** One extra click. Skipping silently-changed rows and applying the rest is exactly the "preview that expired" 12c forbids. Rows that were never changeable (a linked element, an element owned by someone else in a workshared model) are different — they are known when the table opens and are shown greyed, never offered |
| **C5** | An **Undo button** per applied action | Heron's server tells every chat: *"Taking a change back is Revit's own Undo … Never make a second change to reverse the first"*. The Revit API has no safe way to press Revit's Undo for the user | [D-99](decisions/D-99.md) (the rule's table), the MCP server's instructions | **No Undo button that writes a reversing change.** Each Apply is one undo entry; the Companion says *"Press Ctrl+Z once in Revit to take this back — the entry is named …"* and warns when that entry is no longer on top. See §10 |
| **C6** | *"Color" = per-element override in the active view, projection line + surface pattern colour* | The owner's own colour standard is **per duct system, by view filter**, on **projection AND cut** lines and surfaces (Supply Air blue 0,0,255 · Return Air magenta 255,0,255 · Exhaust Air saddle brown 139,69,19). The library routes *"these, now"* to per-element overrides and *"the colour standard for this project"* to view filters | the owner's system table (2026-09-27); `create-view-filter` and `create-view-filters-by-value` routing; `override-graphics-in-view` routing | **NEEDS_REVIEW.** V1 as requested (per element, active view) is a legitimate *"these, now"* tool and the library already has that meaning. But it is not how the owner's standard colours a system, and a table of per-element colours could drift from the standard. Recommend V1 colours **projection line, surface foreground and cut line + cut foreground together** (what `highlight-vs-rest` and `color-by-parameter` already do), and leave system colours to the filters. Question in §18 |
| **C7** | Transaction named *"Heron Companion: Color 12 ducts"* | Every fragment write today is named `"Heron: " + <fragment folder name>` | `RevitFragment.cs` line 532 | **NEEDS_REVIEW.** Naming it differently needs a small add-in change (an optional label on the request). Without it the undo entry reads *"Heron: set-element-overrides-by-id"*. §10 and §18 |
| **C8** | Put the UI server "somewhere" | `mcp/` is **"Transport only. No BIM logic"** | [mcp/README.md](../mcp/README.md) rule 1 | The Companion server holds **no BIM knowledge**: what a column means, and what "colour" means in Revit, lives in the two fragments. The server moves rows between the page and the pipe. That fits `mcp/` as a new sub-folder `mcp/companion/`, with a row in `mcp/README.md`. §4.4 |
| **C9** | Table edits change the model | Article 9: a `MODIFY` from a non-`PRODUCTION` fragment needs *"a preview the user has accepted"*. D-99's exception covers **only `revit_change` from a chat** and says *"a new tool that changes a model needs a preview, or a decision of its own"* | [Constitution Art. 9](../HERON_CONSTITUTION.md), [D-99](decisions/D-99.md) | **The table IS the preview**: each row shows the element, its current value and its new value, the count of changed rows is on the Apply button, and pressing Apply is the acceptance — then 12c re-checks before anything is written. This keeps Article 9 rather than excepting it. The §19 decision records it |
| **C10** | Claude opens a table of every duct | A fragment's reply is **cut to three items per list** and a dictionary to a count | `RevitFragment.cs` `Describe`, lines 4698–4759 | Not a rule conflict, a technical blocker. The table's read fragment returns its rows as **one JSON string** — the workaround `report-category-overrides` and `create-view-filters-by-rule` already use — so no add-in change is needed. If the central reply fix lands first, the table uses that instead. §8.2 |

---

## 3. How it works today — the flow the Companion has to fit into

```text
The modeller types           "make these ducts red"
   |
Claude Code                  decides what was meant (D-01). Heron never classifies intent
   |  MCP over stdio
mcp/server/heron_mcp_server.py   ONE process PER CHAT, started by Claude Code
   |   revit_change(capability, values)          line 1039
   |   1. finds the fragment folder from the capability          _fragment_for
   |   2. refuses a PUBLISH/ADMIN fragment unless allowed          risk_refusal  (client line 1100)
   |   3. names the operation from the fragment's OWN card         write_operation (client line 1141)
   |      -> run_fragment_write for MODIFY and below
   |   4. reads the fragment's C# source and its contract (needs)
   |   5. binding.resolve()  -> which Revit (docs/25; never guessed)
   |   6. _aim_at_pin(args)  -> document + documentPath of the PINNED model (D-74)
   |   7. session.request(op, source, needs, values, apply="true", idempotent=False)
   |   8. session.close()    -> the connection is dropped after every tool call
   |  named pipe  heron.<version>.<pid>   (local only; ACL = this Windows user; per-session token)
revit/Heron.Bridge/BridgeServer.cs
   |   token check -> ping/info/release answered here -> LEASE claim (D-22) -> hand to the add-in
revit/Heron.Revit.Addin/RevitDispatcher.cs     queue, banner "READING"/"CHANGING" (D-50), audit line
   |  ExternalEvent (D-09) — Revit's own thread
RevitOperations.Gate(op)     line 265: the op's risk from HeronOperationRegistry, by NAME
   |   run_fragment_write is MODIFY -> refused unless the owner's Changes switch is ON
   |   (write.enabled, read fresh by HeronPermissions.Allows, D-19)            -> "write_disabled"
RevitFragment.Run            compiles the C#, finds the pinned document by path then title,
   |                         TransactionGroup("Heron: <fragment>")   line 532
   |                         apply=true -> Assimilate (one undo entry) ; otherwise RollBack (D-55)
The model
```

**How a write is approved today, in order:** the owner's **Changes** switch on the ribbon
(`write.enabled`, off by default, read inside the add-in); **Admin** and **Publish** switches for those
levels ([D-106](decisions/D-106.md)); the operation's risk read **by name** from the registry — nothing
on the wire says how dangerous a request is ([Golden Rule 19](14-golden-rules.md)); the pin
(Article 12b); one named `TransactionGroup` rolled back on any failure (Article 8). `revit_change`
has no preview by the owner's choice ([D-99](decisions/D-99.md)); `revit_apply_move` keeps its
preview, one-use approval and re-count (`PendingApproval`, `heron_write.py`). Article 7's
"explicit yes for bulk parameter writes" is carried only by the chat's instructions today —
[FRAGMENT-ISSUES row 5b-161](FRAGMENT-ISSUES.md).

**Two facts about the pipe decide where the Companion can live:**

1. **The newest connection takes the pipe.** `BridgeServer` keeps two pipe instances: one serving,
   one waiting. When a second client connects, the first one's pipe is disposed
   (`ListenLoopAsync`, lines 224–238). If the first had a request in flight, **Revit still finishes
   the work, but the answer is lost** — the client reports `unknown_outcome` and must not retry a
   write (`heron_bridge_client.py`, `request`).
2. **The lease decides who may send anything** ([D-22](decisions/D-22.md), `HeronLease.cs`): one
   client id per Revit, renewed on every request, five minutes by default. The owner runs every
   door with the same id, `HERON_CLIENT_ID=ajmal-pc`, so the owner's own chats do not refuse each other —
   which also means **they can displace each other mid-request** (the comment in
   `heron_bridge_client.py` above `CLIENT_ID` says this trade was accepted).

---

## 4. Where the Companion lives

### 4.1 The two options the request named

**(a) A web server inside the MCP server process.** Each chat's MCP process starts its own small web
server.

**(b) One long-lived Companion process that owns the only Revit connection**; the MCP servers and the
page both go through it.

| | (a) inside each MCP process | (b) one broker owning the pipe |
|---|---|---|
| **Pipe displacement** | None added. The page's requests go through **the same process, the same client id, one at a time** behind a lock — the pipe never sees a second client | None, by construction — but only because every chat's tools now go through the broker |
| **Port conflict** | None. Each server asks Windows for any free port (port 0) | One fixed port to find, or a discovery file; a second broker must refuse to start |
| **When Claude Code closes** | The MCP process ends, the page's server ends, and the page says so. An unapplied table is lost | The broker and page stay up |
| **Changes to existing tools** | None. The tools stay exactly as they are | **Every Revit tool's transport changes**: `mcp/client` talks to the broker, not the pipe. That is a new IPC layer beside [D-02](decisions/D-02.md) and a new always-running program to install, start and update ([D-90](decisions/D-90.md)–[D-96](decisions/D-96.md)) |
| **Lease (D-22)** | Unchanged | The broker is one client for all chats, so a second chat is no longer refused — D-22's protection is gone for everyone, not only for the owner's own id |
| **Size of change** | Small, all in Python except the live-state file | Large, and it touches the part of Heron with the most field proof |

### 4.2 Why a plain (a) that asks Revit through the pipe is still wrong

If the page asked Revit for the selection every 200 ms through the pipe, then every 200 ms:

- the add-in would raise its **READING banner** ([D-50](decisions/D-50.md)) — the screen would flicker
  for as long as the page was open;
- a job would be queued onto **Revit's own thread** through the ExternalEvent — the same thread that
  draws the screen, so the page would slow the modeller down ([Golden Rule 8](14-golden-rules.md),
  Article 26);
- if it were a separate process, it would **displace the chat's pipe** — exactly why D-104 rejected
  polling the pipe for Talk.

### 4.3 Recommended: (a), with the live view read from a file, not the pipe

```text
            REVIT.EXE
            ├─ Heron add-in (unchanged pipe, dispatcher, gate)
            └─ NEW  HeronLiveState ── writes ──► %LOCALAPPDATA%\Heron\live\<pid>.json
                    (SelectionChanged 2023+, Idling-throttled 2020–2022,            ▲
                     ViewActivated, DocumentChanged, DocumentClosing)               │ reads (no pipe,
                                                                                    │  no Revit thread)
CLAUDE CODE ──stdio──► MCP SERVER PROCESS (one per chat) ────────────────────────────┤
                        ├─ every existing tool, unchanged                           │
                        ├─ NEW activity recorder (watches its own tool calls)        │
                        └─ NEW mcp/companion  http://127.0.0.1:<any free port>  ◄───┘
                             ├─ GET  live state  ◄── the file above
                             ├─ GET  activity    ◄── the recorder
                             ├─ GET  table       ◄── rows held in memory
                             └─ POST apply ──► same bridge object, same client id, one lock
                                                ──► run_fragment_write ──► pipe ──► gate ──► Revit
BROWSER (Edge/Chrome) ◄── the modeller
```

**Why this and not (b):**

- **It changes no existing tool and no pipe message.** The only way the page reaches Revit is the
  path `revit_change` already takes, from inside the same process.
- **The live view costs Revit almost nothing.** Revit writes a small file only when something changes;
  the page reads the file. No banner, no queue, no lease.
- **Nothing displaces anything.** The Companion never opens a second pipe connection. Within one
  process, a chat's tool call and an Apply wait for each other on one lock, so they cannot collide.
- **The costs are stated, not hidden:** the page lives only as long as the chat that opened it
  (§4.5), and two chats open means two Companions, each showing its own chat's activity.

### 4.4 Which folder

**`mcp/companion/`**, a new sub-folder beside `client/` and `server/`, named in
[`mcp/README.md`](../mcp/README.md). `tools/check-structure.py` places layers by top folder, and `mcp`
may depend on `platform` and `brain` — the Companion needs neither directly; it reuses
`mcp/client` and `mcp/server` helpers. A new top-level folder was considered and rejected: it would
need a new layer in `check-structure.py`, a new row in [PROJECT-MAP](PROJECT-MAP.md) §B, and it would
still have to live inside the MCP process to avoid the pipe problems in §4.2.

**No BIM logic goes in it.** The column list, what "colour" means, how a value is read and written —
all of that is in the two fragments (§8). The page renders column **types** (text, number, colour,
choice) and nothing else.

### 4.5 What happens when …

| Event | What the modeller sees |
|---|---|
| Claude Code closes | The page turns grey: *"The chat that opened this Companion has closed. Nothing more can be applied from this page. Open a new one from the chat."* An unapplied table is lost — nothing was sent |
| A second chat opens | It gets its own Companion on its own port, only when asked. The first is untouched |
| Revit closes | The live file is deleted on shutdown; the page shows *"Revit is not connected"*. A table open for that Revit can no longer be applied, and says why |
| The pinned model closes | Apply is refused before anything is sent (`document_closed`, already in `heron_failure.py`) |
| Changes is OFF | Apply is refused by the add-in (`write_disabled`) and the page shows the existing message naming the ribbon switch |
| Another chat holds this Revit | Apply is refused (`session_in_use`, D-22); the page says so. Nothing was sent |
| Revit is busy (a dialog open, mid-command) | `revit_busy` — *"Revit is waiting for you to finish"* — nothing ran |

---

## 5. The rule the Companion cannot break: it never talks to an AI

- **No code path to Claude.** The Companion's server has two doors: HTTP from the page on
  `127.0.0.1`, and the existing bridge client to Revit. It imports nothing that speaks MCP outward,
  reads no Claude credential, and makes no outbound network call. `tests/` gets a test that fails if
  `mcp/companion/` imports `urllib.request`, `http.client`, `socket.create_connection`, `requests`, or
  anything named for a model provider.
- **The page loads nothing from the internet.** All HTML, script and style ship in `mcp/companion/`;
  the page's Content-Security-Policy is `default-src 'self'`, so a browser would refuse even a
  mistaken external load.
- **Claude learns only what a tool returns to it.** When the modeller applies an edit, Claude is not
  told. That is the point (no tokens) and it has a consequence: Claude's picture of the model is out
  of date until it reads again — which Golden Rule 21 already requires it to do. A short
  `heron_companion` status call (§13) lets Claude ask *"what did the modeller change?"* in one line.

---

## 6. Phase 1 — the live view

### 6.1 What Revit writes

A new add-in class, `HeronLiveState`, keeps **one file per Revit process**:
`%LOCALAPPDATA%\Heron\live\<pid>.json`, beside `bridges\` (the discovery folder), under Heron's
**derived** area — throwaway state, never user data ([D-17](decisions/D-17.md), [D-31](decisions/D-31.md)).
The path is added to `HeronPaths` (the only thing that builds a Heron path) and mirrored in the Python
client the way `DISCOVERY_DIR` already is.

It is written with `HeronAtomicWrite.WriteAllText` (write a temp file, then replace), so the page never
reads half a file.

| When | Revit event | Versions |
|---|---|---|
| The selection changes | `UIApplication.SelectionChanged` | 2023 → latest (measured, §2 C3) |
| The selection changes | `Idling`: at most every 200 ms, compare the selection's id list with the last one; write only if different | 2020, 2021, 2022 only — a version adapter, as [revit/README](../revit/README.md) rule 4 requires |
| The view or model in front changes | `ViewActivated` (already subscribed, `HeronApplication.cs` line 191) | all |
| The model was edited | `DocumentChanged` — only a counter goes up, no ids | all |
| A model closes / Revit shuts down | `DocumentClosing` (already subscribed) / `OnShutdown` — the file is deleted | all |

**Cost rules** (Golden Rule 8): nothing is written unless something changed; writes are throttled to
one per 200 ms; the category tally stops at 5,000 elements and says *"5,000+"*; the id list stops at
200. **The real cost on a big selection is NOT_VERIFIED** — it is measured on a named model as part of
Phase 1's proof.

**No network, no pipe, no transaction, no model change.** Reading the selection and the active view
needs no transaction. A setting `ui.liveState` (default **on**, like `ui.activityBanner`) switches it
off; it is declared in both halves of the config, as row 5b-29 requires of every setting.

### 6.2 What the page shows

*Connected to Revit 2024 · Model: heron ai bulding · View: Level 1 - Mechanical (Floor Plan) ·
Selected: 24 — Ducts 18, Duct Fittings 6 · updated 2 s ago · model edited since the table was read: no*

When no live file exists: *"Revit is not connected to Heron — press the Heron button in Revit."*
When several Revits are live, the page shows the one this chat is bound to (`binding`), never a guess
(Article 12a).

---

## 7. Phase 2 — the activity log

Every Heron tool call this chat makes appears as a line: **time · tool · one-line summary · OK /
refused / failed · how long**. For example:
*14:02:11 · revit_change · OVERRIDE_GRAPHICS_IN_VIEW ran in heron ai bulding · OK · 1.8 s*.

- **Where it comes from:** the `_Labelled` subclass that already wraps every `@server.tool()` in
  `heron_mcp_server.py` records the tool's name, start, end and the **first line** of its reply
  (capped at 200 characters) into an in-memory ring of the last 500 calls. It uses
  `functools.wraps`, so each tool's signature — which `tests/test_mcp_serves.py` reads back from the
  real SDK — is unchanged. A fault in the recorder is caught and dropped; it can never change a tool's
  answer.
- **What it does not show:** anything another chat or a command-line run did. The add-in's audit
  trail (`audit-YYYYMM.jsonl`, one line per request, all clients) has that, and a later step can show
  it read-only. **NEEDS_REVIEW** whether V1 should include it.
- **Nothing new is written to disk.** The audit trail already records every request that reached
  Revit (Golden Rule 14); the recorder is display only.

---

## 8. Phase 3 — the editable table

### 8.1 How it opens

Claude calls **`revit_edit_table`** once:

```text
revit_edit_table(kind="colour", category="Ducts", scope="view")
  -> "Table opened in Heron Companion: 24 ducts in Level 1 - Mechanical. Edit it there."
```

The rows go to the page. **Claude receives the one line above**, nothing more.

### 8.2 How the rows are read — a new READ fragment

**`READ_ELEMENT_TABLE`** (new, `risk: READ`, starts `DRAFT`), run through `run_fragment_read` exactly
as `revit_read` runs a fragment: no transaction, so Revit itself refuses any change, and it works with
Changes OFF.

- **Inputs:** `elements` (the selection, or the chain), `view` (by name, as `"ViewType: Name"`, the way
  `OneView` already resolves one), `columns` (a list such as `system, projection-line-colour`),
  `maxRows` (default 2,000).
- **Scope `view`** collects the category's elements **visible in that view** (a view-scoped collector,
  so what the view hides is not listed — and the page says so).
- **Output:** one string `tableJson` (§12.3), because a list output is cut to three items (§2 C10).
- **Per row:** element id and UniqueId, category, family and type, and each requested column's
  **current value** plus whether it is **editable**, with the reason when it is not (a linked element,
  owned by another user, a view that does not allow overrides — `AreGraphicsOverridesAllowed`).
- **General from day one.** A column has a type — `text`, `number`, `colour`, `choice` — and a
  reader. Colour is the first reader; a parameter reader (instance first, then type, display units,
  the way `read-element-parameters` reads) is the second, in a later step.

**Why a new fragment rather than widening one:** `read-graphic-overrides` (PROVEN) reads the right
values but returns them as sentences, and changing its output would change what its proof describes.
`read-element-parameters` (PROVEN) reads parameters but returns a count. A new reader that **calls the
same API the same way** leaves both proofs intact. **NEEDS_REVIEW** against the owner's rule *"widen
an existing fragment first"*.

### 8.3 What the modeller sees

| ☐ | Element | Type | System | Current colour | New colour |
|---|---|---|---|---|---|
| ☐ | 351204 | TRG_Rect Duct | SA-01 | ■ none (by view) | [ picker ] |
| ☐ | 351229 | TRG_Rect Duct | RA-02 | ■ 255,0,255 | [ picker ] |
| ✕ | 351310 | TRG_Rect Duct | SA-01 | — linked element, cannot change here | |

- A **colour picker** per row, **fill down** for a selected block of rows, and **Select in Revit**
  (runs the PROVEN `set-selection` — EXECUTE, not a change).
- The Apply button always carries the count: **Apply 12 changes**.
- Rows that cannot be changed are shown greyed with the reason, never offered.
- For parameter columns later: `Mark` and `Type Mark` never get fill-down — the owner's rule that Mark
  stays unique.

### 8.4 What Apply does, step by step

1. **The page sends only changed rows** (id, UniqueId, the value it showed as current, the new value).
2. **The Companion server re-checks locally:** the table still belongs to the bound Revit and the
   pinned model; the rows are within the table; each colour is three whole numbers 0–255.
3. **The same checks `revit_change` makes, by calling the same functions — never a copy:**
   `bridge.risk_refusal(root, folder, switched=True)`, `bridge.write_operation(root, folder)`,
   `bridge.undeclared_values(...)`, `binding.resolve()`, `_aim_at_pin(args)`. These move out of
   `revit_change` into one shared helper so both callers use one copy (the refactor keeps
   `revit_change`'s behaviour byte-for-byte; a test holds that).
4. **One request:** `run_fragment_write` with the write fragment, `apply="true"`,
   `idempotent=False`, the pinned `document` and `documentPath`, under the process's bridge lock.
5. **In Revit**, unchanged machinery: token, lease, the **Changes switch** gate, the pinned model, the
   CHANGING banner, one `TransactionGroup`, the audit line.
6. **Inside the write fragment, before it writes anything (Article 12c):** every row's element must
   still exist, be in the same model, and still have the **current value the table showed**. If any
   row fails, the fragment changes nothing and returns the stale rows. The page marks them and asks
   the modeller to look and press Apply again (§2 C4).
7. **The result** comes back to the page (changed, stale, refused — with Revit's own words where it
   refused) and into the activity log. Each changed row is **read back** in the same run and shows
   the value Revit now holds, not the value that was asked for.

### 8.5 The new write fragment

**`SET_ELEMENT_OVERRIDES_BY_ID`** (new, `risk: MODIFY`, starts `DRAFT`): one run, many elements, **a
different value per element**, merged into each element's existing override.

- **Why new:** no fragment takes a per-element colour map today. `override-graphics-in-view` (DRAFT)
  applies **one** settings object to every element and **replaces** each element's whole override —
  its header says so; turning it into a per-element merge would change what it is for.
  `color-by-parameter` merges, but chooses the colours itself.
- **What it sets for a colour (V1, pending §2 C6):** projection line colour; surface foreground
  pattern colour **with the solid fill pattern** (a colour with no pattern shows nothing); and — if
  the owner agrees — cut line and cut foreground too. **Merges**: starts from
  `GetElementOverrides`, so an element's halftone, weight or transparency survive. Insulation and
  lining follow the duct they wrap, the library's standing rule.
- **Refuses:** a view where `AreGraphicsOverridesAllowed` is false; a linked element; a stale row
  (the whole run, §8.4 step 6).

### 8.6 What Claude gets back

Nothing, unless it asks. `heron_companion` (§13) answers *"2 applies since the table opened: 12
ducts recoloured in Level 1 - Mechanical; 1 refused (Changes was off)."*

---

## 9. What "colour" means in V1

**Per element, in one named view, as a graphic override** — the owner's request. The view is the one
the table was opened for, **named on the page and pinned**: if the modeller switches views before
pressing Apply, the colours still go to the view the table showed, the same way a write is aimed at
the pinned model rather than the one in front ([D-74](decisions/D-74.md)).

**NEEDS_REVIEW (§2 C6):** which of the four colour members; and whether the Companion should also
offer a *system* colour table that edits the owner's view filters instead — the lasting way the owner's
standard colours a system.

---

## 10. Phase 4 — undo

**Revit's own Ctrl+Z covers it, by design.** One Apply is one fragment run, one `TransactionGroup`
assimilated, one entry in Revit's undo list ([D-55](decisions/D-55.md), Article 8). After each Apply
the page says:

*"Changed 12 ducts in Level 1 - Mechanical. To take it back, press Ctrl+Z once in Revit — the entry
is named 'Heron: set-element-overrides-by-id'. If you have done anything in Revit since, undo that
first."*

- **No Undo button that makes a second, reversing change** — Heron's own instructions forbid it
  (§2 C5), and it would leave two entries where the modeller expects none.
- **No button that presses Revit's Undo for you.** The API has no supported way, and it would undo
  whatever is on top — possibly the modeller's own last action.
- **The page can tell when the entry is probably no longer on top**: the live file's edit counter
  (§6.1) moves when anything else changes the model, and the page then adds *"You have made other
  changes since — Ctrl+Z will undo those first."*
- **Undo entry name — §2 C7.** An optional `label` on the request (for example
  *"Heron Companion: colour 12 ducts"*), read by `RevitFragment.Run` and used only as display text,
  capped at 60 characters, always starting `Heron`. An older add-in ignores the key. That is an
  add-in change and a Revit restart; **the owner chooses** whether it is worth it.

**Refresh:** when the edit counter moves while a table is open, the page shows *"The model changed
since this table was read — Refresh"*. Refresh re-runs the READ fragment (one pipe call, the READING
banner shows once). Never automatic — the page does not reach Revit on its own.

---

## 11. Protecting the page from other programs and websites

The threat: any website open in the same browser, or any program on the PC, trying to make Heron
change the model through the Companion.

| Protection | How |
|---|---|
| **Not on the network** | Binds `127.0.0.1` only, never `0.0.0.0`. Windows Firewall does not prompt for a loopback-only listener |
| **No port clash** | Port 0: Windows picks a free port each time |
| **A secret per Companion** | 32 random bytes from `secrets`, made when the server starts. The browser is opened **by the MCP process itself** at `http://127.0.0.1:<port>/open#<one-time code>`; the page trades the one-time code (valid once, for 2 minutes) for a cookie `HttpOnly; SameSite=Strict; Path=/`, and the address bar is cleared. The secret never appears in a tool reply, so it never reaches Claude's context or a transcript |
| **DNS rebinding** | Every request's `Host` must be exactly `127.0.0.1:<port>` or `localhost:<port>`; anything else is refused before it is read |
| **Other websites** | Every `POST` must carry `Origin` equal to the Companion's own origin **and** a custom header (`X-Heron-Companion`), which a cross-site form cannot send and a cross-site script cannot send without a CORS pre-flight the server never answers. No CORS header is ever sent |
| **Clickjacking the Apply button** | `X-Frame-Options: DENY` and CSP `frame-ancestors 'none'` |
| **Script injection from model text** | Element and type names are model text — data, never instruction ([Golden Rule 19](14-golden-rules.md)). The page inserts them with `textContent`, never `innerHTML`; CSP forbids inline script |
| **The MCP protocol itself** | The MCP server speaks to Claude Code over **stdout**. The web server must never print: its request logging is switched off and anything it reports goes to stderr. A test starts the server, makes requests, and fails if one byte reached stdout |

**What this does not stop, said plainly:** a program already running as the same Windows user can read
that user's files and could find a way in — the same limit the pipe's discovery file lives with
(`BridgeServer.cs` header, D-104). **The Apply button is a person's action by design, but it is not
proof of a person:** an AI agent on the same PC with browser or screen control could press it, just as
it could press the Changes switch on the ribbon. The gate that holds either way is the Changes switch
inside Revit.

---

## 12. Message formats

### 12.1 The live file (Revit → page), format 1

```json
{
  "format": 1,
  "pid": 23816, "revitVersion": "2024",
  "updatedUtc": "2026-09-29T10:02:11Z",
  "document": { "title": "heron ai bulding", "path": "D:\\Models\\heron ai bulding.rvt" },
  "view": { "name": "Level 1 - Mechanical", "type": "FloorPlan", "id": 312 },
  "selection": {
    "count": 24,
    "categories": { "Ducts": 18, "Duct Fittings": 6 },
    "ids": [351204, 351229],
    "idsTruncated": false,
    "countCapped": false
  },
  "editCounter": 41
}
```

### 12.2 The page's HTTP API (all JSON; all need the cookie; `POST` also needs `Origin` and `X-Heron-Companion`)

| Method and path | Returns / does |
|---|---|
| `GET /` | The page |
| `GET /open` | Trades the one-time code for the cookie, then shows the page |
| `GET /api/state` | `{ revit: <live file or null>, bound: bool, chatAlive: true }` — the page asks once a second |
| `GET /api/activity?since=<n>` | Activity lines after sequence number `n` |
| `GET /api/table` | The open table: `{ tableId, view, columns, rows, openedUtc, editCounterAtRead }` |
| `POST /api/table/apply` | `{ tableId, changes: [{ id, uniqueId, column, was, value }] }` → §12.4 |
| `POST /api/table/refresh` | Re-runs the read; returns the new table |
| `POST /api/select` | `{ ids: [...] }` → selects them in Revit (`set-selection`, EXECUTE) |

### 12.3 The table, as the READ fragment returns it (`tableJson`)

```json
{
  "format": 1,
  "view": "FloorPlan: Level 1 - Mechanical",
  "columns": [
    { "key": "system", "label": "System", "type": "text", "editable": false },
    { "key": "colour", "label": "Colour", "type": "colour", "editable": true }
  ],
  "rows": [
    { "id": 351204, "uniqueId": "…", "category": "Ducts", "type": "TRG_Rect Duct",
      "values": { "system": "SA-01", "colour": null }, "editable": true },
    { "id": 351310, "uniqueId": "…", "category": "Ducts", "type": "TRG_Rect Duct",
      "values": { "system": "SA-01", "colour": null }, "editable": false,
      "why": "a linked element - change it in its own model" }
  ],
  "truncated": false, "hiddenByView": "elements the view hides are not listed"
}
```

A colour is `"R,G,B"`, three whole numbers 0–255 — the form every colour input in Heron already takes
(`OneColour`, `RevitFragment.cs` lines 3372–3399). `null` means no override.

### 12.4 Apply (page → Companion server → fragment → page)

The server turns the changes into the write fragment's values, one `name=value` per line as
`revit_change` does:

```text
view=FloorPlan: Level 1 - Mechanical
rows=351204|<uniqueId>|none|0,0,255;351229|<uniqueId>|255,0,255|0,0,255
```

The fragment's reply, returned to the page:

```json
{ "applied": true, "changed": 12, "stale": [], "refused": [],
  "readBack": [{ "id": 351204, "colour": "0,0,255" }],
  "undoEntry": "Heron: set-element-overrides-by-id",
  "verdict": "<the add-in's own verdict line>" }
```

or, when any row is stale, `"applied": false` with the stale rows listed and nothing changed.

---

## 13. New MCP tools and their risk levels

Declared in `mcp/server/heron_tools.py` `TOOLS`, the one place a tool's risk lives; the MCP safety
labels are derived from it as for every tool.

| Tool | Risk | Add-in operation | What it does | Reply to Claude |
|---|---|---|---|---|
| `heron_companion` | `READ` | none | Starts this chat's Companion if it is not running, opens it in the browser, and answers what has happened on it | *"Heron Companion is open in your browser."* or a one-line summary of applies |
| `revit_edit_table` | `ANALYZE` | `run_fragment_read` | Runs `READ_ELEMENT_TABLE`, holds the rows for the page, opens the Companion | *"Table opened in Heron Companion: 24 ducts in Level 1 - Mechanical."* |

**The Apply action is not an MCP tool** — Claude cannot call it. Its risk is still declared in one
place: a `COMPANION_ACTIONS` table in `heron_tools.py` with `companion_apply` at `MODIFY` through
`run_fragment_write`. **Selecting rows in Revit is NEEDS_REVIEW:** `set-selection` is `EXECUTE`, but
a fragment can only be sent today through `run_fragment_read` (which it would exceed) or
`run_fragment_write` (which needs Changes ON just to select). `tests/test_tool_registry.py` gets a case
that every Companion action appears there with a risk.

The names follow the existing prefixes: `revit_*` touches Revit, `heron_*` does not.

---

## 14. What is reused, widened, or new

| Need | Already exists | Use |
|---|---|---|
| Read the selection | `read-selection` (READ_SELECTION, PROVEN); the executor fills `elements` from the selection | **Reused** by `revit_edit_table` with `scope=selection` |
| Select rows in Revit | `set-selection` (SET_SELECTION, PROVEN, EXECUTE) | **Reused** — which door it goes through is §13's open point |
| Read current overrides | `read-graphic-overrides` (PROVEN) — sentences, not rows | Its API calls are **re-used in method** by the new reader; the fragment itself is untouched |
| Write one override to many | `override-graphics-in-view` (DRAFT), `highlight-vs-rest` (PROVEN), `color-by-parameter` (DRAFT) | Not suitable — one value for all, or colours chosen by the fragment. **New** `SET_ELEMENT_OVERRIDES_BY_ID` |
| Read parameters for many | `read-element-parameters` (PROVEN) — count only in the reply; bridge `read_parameters` — real rows, per category, max 500 | Later parameter columns reuse its reading rule inside `READ_ELEMENT_TABLE` |
| Write a parameter per element | `import-parameter-values` (DRAFT) — per element, from a CSV file | Later: **widen** it to take rows from the request as well as a CSV (the "widen first" rule) |
| Colour as text | `OneColour` in the executor: `"R,G,B"` | **Reused**, no new format |
| Colour-by-standard | `create-view-filter`, `create-view-filters-by-value`, `apply-view-filter` | Not V1; a possible later "system colours" table (§9) |
| The write path and its checks | `revit_change`: `risk_refusal`, `write_operation`, `undeclared_values`, `binding`, `_aim_at_pin`, `analyse`/`explain` | **Reused** through one shared helper (§8.4 step 3) |
| Plain-language errors | `heron_failure.py`: `write_disabled`, `session_in_use`, `revit_busy`, `document_closed`, `no_such_document`, `model_moved_on` … | **Reused** — the page shows the same sentences the chat would |
| Atomic file write | `HeronAtomicWrite` (platform) | **Reused** for the live file |
| Which model is in front | `ViewActivated` / `DocumentClosing` handlers in `HeronApplication.cs` | **Extended** to also feed `HeronLiveState` |
| Undo | One `TransactionGroup("Heron: " + name)` per run | **Reused**; optional label §10 |

---

## 15. Files, by phase

**Phase 0 (this document):** `docs/40-heron-companion.md` (new) · `docs/README.md` (one index row).

| Phase | Create | Change | Needs a Revit restart |
|---|---|---|---|
| **1 — live view** | `revit/Heron.Revit.Addin/HeronLiveState.cs` · `mcp/companion/heron_companion.py` (server) · `mcp/companion/static/index.html`, `companion.js`, `companion.css` · `mcp/companion/README.md` · `tests/test_companion_security.py` · `tests/test_companion_live.py` · a Revit-free test host for the live-state tally and throttle | `revit/Heron.Revit.Addin/HeronApplication.cs` (subscribe, version adapter) · `platform/Heron.Core/HeronPaths.cs` (`Live`) · `platform/Heron.Core/HeronConfig.cs` + `mcp/client/heron_config.py` (`ui.liveState`) · `mcp/server/heron_mcp_server.py` + `heron_tools.py` (`heron_companion`) · `mcp/README.md` · `revit/README.md` · `docs/DECISIONS.md` + `docs/decisions/` (the §19 decision, once accepted) · `docs/NEEDS-CHECKING.md` (a new group) | **Yes**, all three installed releases |
| **2 — activity** | `tests/test_companion_activity.py` | `mcp/server/heron_mcp_server.py` (`_Labelled` records) · `mcp/companion/*` | No — Python only; the Claude app restarts the MCP server |
| **3 — table** | `brain/fragments/read-element-table/` · `brain/fragments/set-element-overrides-by-id/` (each `fragment.yaml`, `impl/any/fragment.cs`, proof plan) · `tests/test_companion_table.py` | `mcp/server/heron_mcp_server.py` (`revit_edit_table`; the shared write helper) · `heron_tools.py` (`revit_edit_table`, `COMPANION_ACTIONS`) · `tests/test_tool_registry.py` · `mcp/companion/*` · the fragment store row for each new card (never `--rebuild` the shared store) | No — fragments are compiled at run time |
| **4 — undo and polish** | — | `mcp/companion/*` · optionally `RevitFragment.cs` (the undo `label`, §10) | Only if the label is chosen |

**Not changed in any phase:** the pipe protocol and `BridgeServer.cs`; `HeronLease`; `HeronPermissions`
and every switch default; `HeronOperationRegistry` (no new add-in operation); every existing MCP tool's
behaviour; `.claude/settings.json` and its hooks; the Revit version targets.

---

## 16. Test plan

### 16.1 Automated, no Revit (CI and the owner's PC)

| Test | Proves |
|---|---|
| `test_companion_security.py` | Binds `127.0.0.1` only; wrong `Host` refused; `POST` without `Origin`/custom header refused; one-time code works once and expires; no CORS headers; frame headers present; **nothing written to stdout** |
| `test_companion_live.py` | Reads a live file; a missing, half-written or foreign-`pid` file is refused cleanly; the bound session is the one shown |
| `test_companion_activity.py` | Every tool is still registered with an unchanged signature; a recorder fault never changes a reply |
| `test_companion_table.py` | Only changed rows are sent; a colour outside 0–255 is refused before any request; the request built for Apply has exactly the keys `revit_change` sends (`apply`, `document`, `documentPath`, `expectProject`, `idempotent=False`) and the same operation from the card; unpinned or unbound → refused, nothing sent; the lock serialises a tool call and an Apply |
| `test_tool_registry.py` (extended) | The two new tools and every Companion action have a declared risk; labels match |
| No-outbound test | `mcp/companion/` imports nothing that reaches a network or a model provider |
| The four gates and CI's eleven | `check-docs`, `check-metadata`, `check-structure`, `check-package`, then CI's `check-signatures`, `check-licence`, `check-narrow-errors`, `check-products`, `check-decision-titles`, `check-routing`, `check-intrusion` |
| `check-compile.py` | `HeronLiveState.cs` compiles on every release 2020–2027, with the `SelectionChanged` adapter taking the Idling branch for 2020–2022. **A compile is not a proof** |
| `check-fragments-compile.py`, `check-routing.py` | The two new fragments compile and do not steal routing from existing cards (compare `find` on a scratch store against main) |
| `tools/check-change.py`, `tools/change-evidence.py` | The diff touched only the areas each phase declared; before/after evidence recorded — a change with no evidence is not a pass |

### 16.2 Real Revit — a new NEEDS-CHECKING group, letter assigned when built

Every row names the model and the release. **Everything here is NOT_VERIFIED until run.**

| Row | Check | Negative case |
|---|---|---|
| L1 | Select 3 ducts in Revit 2024 → the page shows 3 within a second, categories right | Select nothing → the page shows 0, not the old 3 |
| L2 | The same in Revit 2020 (the Idling branch) | Leave Revit idle 5 minutes → no file writes when nothing changes (file time unchanged) |
| L3 | Select ~5,000 elements → Revit stays responsive; the page says 5,000+ | Close the model → the page says no model; the file is gone |
| T1 | Open a colour table for ducts in a plan, recolour 3, Apply → one undo entry, 3 ducts coloured, read back by `read-graphic-overrides` | Changes switch OFF → refused, nothing changed, message names the switch |
| T2 | Stale row: open the table, change one duct's colour by hand in Revit, then Apply | Whole apply refused, that row marked, nothing changed (12c) |
| T3 | Pinned view: open the table in Level 1, switch Revit to Level 2, Apply | The colours land in Level 1, not Level 2 |
| U1 | After T1, one Ctrl+Z → all 3 back to before | Make another change in Revit after Apply → the page warns Ctrl+Z will undo that first |

Fragments are proved the usual way ([fragment-proving](../.claude/skills/fragment-proving/SKILL.md)):
a named test model (the owner's *heron ai bulding* test copy, or `Snowdon-scratch`), a positive and a
negative case, a staleness fingerprint ([D-30](decisions/D-30.md)).

### 16.3 Manual tests for the owner, one per phase

**Phase 1 — live view.** (1) Open Revit 2024 and your test copy. Press the Heron button. (2) In the
chat, ask *"open the Heron Companion"*. Your browser opens a page. (3) Select three ducts. Within a
second the page should say *Selected: 3 — Ducts 3*. (4) Switch to another view; the page should name
it. **Negative:** press Esc to clear the selection — the page must say *0*, not keep showing 3. Then
close the model — the page must say no model is open.

**Phase 2 — activity.** (1) Ask the chat *"how many ducts are there"*. A line appears on the page with
the tool, the answer's first line and *OK*. (2) Ask for something that is refused (turn Changes OFF
and ask it to colour something). **Negative:** the page must show *refused* with the reason, not *OK*.

**Phase 3 — the table.** (1) Turn Changes ON. (2) Ask the chat *"open a colour table for the ducts in
this view"*. The chat should answer in one line; the table appears on the page. (3) Give three ducts
blue, press **Apply 3 changes**. (4) In Revit the three ducts turn blue, and Edit → Undo shows one
entry. **Negative:** open the table again, then in Revit change one of those ducts' colour by hand
(right-click → Override Graphics in View → By Element), then press Apply on the page. Nothing should
change, and the page should mark that duct as changed in Revit since the table was read.

**Phase 4 — undo.** (1) After an Apply, press Ctrl+Z once in Revit. All the ducts go back.
**Negative:** Apply, then move any element in Revit, then look at the page — it must warn that Ctrl+Z
will undo the move first.

---

## 17. Risks

| Risk | How bad | What reduces it |
|---|---|---|
| **The owner's own chats share one client id**, so two chats can displace each other mid-request — the Companion adds a new moment for that (an Apply while another chat works) | An Apply's answer lost → `unknown_outcome`; the page must say *"check the model before trying again"* and never retry | Same as today between two chats; D-22's id note records the trade. The page never retries a write |
| **Carried values are shared per Revit** — an Apply resets the chain, so a chat mid-chain (`expect_from`) is refused and must re-run | A refusal, never a wrong write | The page's Apply goes through `chain: reset` like every `revit_change`; the chat's `expect_from` catches it |
| **Claude's view goes stale** after table edits | Claude might describe colours that changed | Rule 21: Claude re-reads before acting; `heron_companion` summarises what changed |
| **Model text in the page** (a type named like a script) | Injection in the page | `textContent` only; CSP forbids inline script |
| **Idling on 2020–2022 costs Revit time** | A slower Revit | 200 ms throttle, write only on change, measured in L2/L3 |
| **The page lives only as long as the chat** | An unapplied table lost when Claude Code closes | Stated on the page; nothing half-applied, because nothing is sent until Apply |
| **A same-user program or agent presses Apply** | A change the modeller did not make | The Changes switch in Revit still gates it; one Ctrl+Z still undoes it |
| **Revit refuses a view override** (a view template, a schedule, a sheet) | Rows not editable | The reader marks them with the reason before the modeller edits; **whether a view template blocks element overrides is NOT_VERIFIED** and is a proof row |
| **The two new fragments are unproven** | A wrong write on a live model | Article 11: first runs on a test copy; the page shows the same *"not PROVEN"* warning `revit_change` gives |
| **`tableJson` in one string** is a workaround | Large tables slow; a format two places must agree on | `maxRows` 2,000; `format: 1`; replaced by the central reply fix when that lands |

---

## 18. Questions for the owner — asked one at a time, in this order

1. **Reopen the door D-105 closed?** The Companion is a second place to work with Heron. It never
   carries words to Claude, so Talk's failure cannot happen, but D-105 says *"the modeller works with
   Heron in the chat only"*. **Yes** files the decision in §19; **no** stops here.
2. **Stale rows (C4):** refuse the whole Apply and mark the stale rows (Article 12c, recommended), or
   skip them and apply the rest (your original wording, which would need 12c amended)?
3. **Colour (C6):** projection line + surface only (your wording), or also cut line + cut pattern like
   your system standard (recommended)?
4. **Undo name (C7):** keep *"Heron: set-element-overrides-by-id"* (no add-in change), or add a label
   so it reads *"Heron Companion: colour 12 ducts"* (a small add-in change and a Revit restart)?
5. **Activity from other chats (§7):** this chat's calls only (recommended for V1), or also every
   request any chat or command sent to this Revit, from the audit trail?
6. **Widen or new (§8.2, §8.5):** two new fragments (recommended, keeps two PROVEN fragments' proofs
   intact), or widen `read-graphic-overrides` and `override-graphics-in-view`?
7. **Selecting rows in Revit (§13):** leave it out of V1 (recommended), or send it through the write
   door so it needs Changes ON?

---

## 19. The decision this needs — DRAFT, not filed

> **Title:** *The modeller can see and edit Heron's work in a local Companion page, and the chat stays
> the only place the modeller talks to Heron*
>
> **Supersedes:** D-105's point 2 *only as far as* "works with Heron in the chat only" — the chat
> stays the only place to **talk** to Heron. **Amends:** D-09 (a selection watch on Idling for Revit
> 2020–2022 only). **Keeps:** D-01, D-02 (Revit still opens no socket), D-19, D-22, D-99, D-106,
> Articles 7, 8, 9, 12a–c.
>
> **Decision.** (1) Each chat's MCP server may serve a Companion page on `127.0.0.1`, on request,
> protected as §11 says. (2) The page never sends anything to any AI. (3) Revit publishes a read-only
> live-state file; nothing reads Revit's state through the pipe for the page. (4) A change from the
> page goes through the same operation, gate, switches, pin and single undo entry as `revit_change`.
> (5) **The table is the preview Article 9 requires**: current and new values shown, count on the
> button, Apply is the acceptance, and Article 12c's re-check refuses the whole Apply when any row is
> stale. (6) Taking a change back is Revit's own Ctrl+Z.
>
> **Why this is not Talk (D-105 point 4).** Talk carried the modeller's words into a chat and lost one.
> The Companion carries no words anywhere; its failure modes are a page that stops updating or an
> Apply that is refused, and both are visible on the page.

---

## 20. What this Phase 0 did not do

- **No code was written or changed.** Only this document and its index row.
- **Nothing was run against Revit.** Every Revit behaviour described is **NOT_VERIFIED**. The one
  measurement taken is the `SelectionChanged` presence per release, read from the Revit SDK reference
  packages in this PC's NuGet cache, one per release.
- **No decision number was spent**, no register row was added, and no question was filed in
  [OPEN-QUESTIONS](OPEN-QUESTIONS.md) — §18 is asked in the chat first, one question at a time.

---

## 21. Decided since this was written

| Date | Question | Answer | Recorded as |
|---|---|---|---|
| 2026-09-29 | 1 - reopen the door D-105 closed? | **Yes** - *"YES YOU CAN DO IT"* | [D-108](decisions/D-108.md), accepted. §19's draft is filed there; it leaves question 2 open and holds Article 12c as it stands |

**Still open:** questions 2 to 7 in §18, asked one at a time. **Nothing is built yet.**
