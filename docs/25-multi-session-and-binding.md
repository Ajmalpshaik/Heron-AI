# 25 — Multi-Session, Document Binding & Stale Reads

> Derived from [Field Notes — Proven Bridge Behaviour](00e-field-notes-proven-bridge.md), behaviour proven live
> 2026-08-20 in a **working implementation**.
> **[NOTE]** blocks are engineering commentary added during review.
>
> This document carries more weight than most in this repository, because it describes what a real
> bridge already does rather than what a specification hopes for. Where it disagrees with a design
> document, **it wins**.

---

## 1. What the working bridge already proves

Five things the four specification documents either got wrong, left open, or never mentioned — all
settled here by evidence:

| # | Proven | Consequence for Heron |
|---|---|---|
| 1 | **Per-process pipes work; a single shared pipe does not.** Before 2026-08-20 every Revit tried to use one shared line and the second Revit simply refused to start | [D-02](DECISIONS.md) is confirmed — pipe name must carry the process id. This was a real failure, not a hypothetical |
| 2 | **The AI's script runs on the thread that draws the screen. While it runs, Revit is genuinely frozen. No add-in can change that** | **[A1](PROPOSALS.md) confirmed from the field.** The Revit API threading constraint — absent from all four specifications — is real, and their own working code hit it |
| 3 | **If the user is mid-command, the AI cannot interrupt. It waits** | Confirms `ExternalEvent.Raise()` is a request, not a guarantee ([03 §4](03-heron-revit.md)). "Revit is busy" is a normal state, not an error |
| 4 | **The session list shows a stale file name** — whatever was in front when that Revit first connected. It does not update | Never identify a session by document name. **PID is the identity** |
| 5 | **One Revit can hold several projects open**, and commands land on whichever window is in front — which changes when the user clicks | Choosing the *Revit* is only half the problem. See §4 — this is the most dangerous finding in the note |

**[NOTE]** Point 2 deserves emphasis. Four specification documents, several hundred sections, and the
single hardest constraint in the platform never appears in any of them — but it is written plainly in a
note describing software that already works. That is the difference between designing a system and
running one, and it is why these field notes are treated as authoritative here.

---

## 2. Bridge discovery — the piece [D-02](DECISIONS.md) was missing

> Each Revit hosts its own private line named after its process number, and advertises itself in
> a per-process file under the bridge directory — in Heron, `%APPDATA%\Heron\bridges\<pid>.json`.

**[NOTE]** [D-02](DECISIONS.md) settled the *transport* (named pipes, per-PID naming) but never said how
the client **finds** the pipes. Named pipes cannot be enumerated usefully. The working bridge answers it:
a **discovery directory** — one small JSON file per live bridge, named by PID.

Adopted for Heron, with additions the field notes imply but do not state:

```text
%APPDATA%\Heron\bridges\<pid>.json
```

Each file should carry:

| Field | Why |
|---|---|
| `pid` | The identity. Everything else can change; this cannot |
| `pipeName` | What to connect to |
| `revitVersion` | Mixed versions run side by side — the client must know which it is talking to |
| `addinVersion` | The version triangle in [04 §5](04-heron-mcp.md) |
| `startedAt` | Distinguishes a fresh session from a stale file |
| `protocolVersion` | So an old add-in and a new server refuse cleanly |

**Two rules the field notes make necessary:**

1. **Do not put the document name in the discovery file.** That is exactly what produced the stale-name
   trap (§1.4). The document is queried live, on demand, or not at all.
2. **A JSON file is not proof the bridge is alive.** Revit crashes without cleaning up. The client must
   verify the PID is running *and* the pipe answers before listing a session as available — and remove
   the stale file when it does not.

---

## 3. Session binding — one chat, one Revit

Proven behaviour, adopted wholesale:

| Situation | Behaviour |
|---|---|
| One Revit connected | Bind automatically. Never ask |
| Two or more connected | **Nothing is sent to Revit at all** until the user chooses. Never guess |
| Already bound | Every command goes there. A third Revit opening later does **not** re-ask |
| The bound Revit closes | **Stop and say so.** Never slide onto a different session |
| Chat A → Revit 1, chat B → Revit 2 | Independent. No interference |
| Chat A **and** chat B → the same Revit | **They fight.** Last speaker wins and cuts the other off |

**[NOTE]** *"Nothing is sent to Revit at all until you say which one — the AI is not allowed to guess"*
is the correct default and it should be a hard rule, not a preference. Guessing which project to modify
is the single worst thing this platform could do. It belongs in the
[Constitution](../HERON_CONSTITUTION.md).

**[NOTE]** *"The Revit you chose closes → everything stops. It never slides onto a different project"*
is equally important and easy to get wrong. A naive implementation that re-runs discovery on
disconnection would silently rebind to whatever is left — and the user's next command would land on
someone else's model. The binding must be **explicit, sticky, and fail closed**.

### The one thing Heron should improve

> Chat A and chat B on the same Revit **fight**. Whichever speaks last takes over and cuts the other
> off. It is not a queue — a job running mid-way gets chopped.

This is the working bridge's known limitation, and the reason for the standing rule
*"don't go to Revit, another session is running."*

**[NOTE]** Nothing is corrupted, because each chat reconnects on its next call — but a half-finished job
being *chopped* is a genuine hazard once Heron can write to models. Chopping a read is harmless.
Chopping a `MODIFY` mid-transaction is not, and under [Golden Rule 16](14-golden-rules.md) it must roll
back cleanly rather than leave a partial change.

Three options, in increasing cost:

| Option | Behaviour | Assessment |
|---|---|---|
| **A. Warn only** | Detect the takeover, tell both chats plainly | Cheapest. Matches today's behaviour but stops it being silent |
| **B. Lease** *(recommended)* | A chat holds a short renewable lease. A second chat is **refused** with *"Revit 24312 is in use by another session"* rather than taking over | Small change, removes the hazard entirely. Fail closed, consistent with §3's binding rules |
| **C. Queue** | The second chat waits its turn | Sounds nicer, behaves worse — an invisible queue means a command runs minutes later against a model that has since changed. That is the stale-read problem (§5) with extra steps |

**Recommendation: B.** And a lease must never block a `MODIFY` **rollback** — cleanup always wins over
the lease, or an interrupted transaction could be stranded.

Tracked as [Q-36](OPEN-QUESTIONS.md).

---

## 4. Document binding — **the most dangerous finding**

> One Revit can hold several projects open. Picking the Revit is only half of it — commands land on
> whichever project window is in front, and that changes when you click.

**[NOTE]** This is the sharpest hazard in the entire repository, and no specification document mentions
it.

The failure is completely silent and entirely plausible:

```text
1. User binds chat to Revit pid 24312.                    Tower-A.rvt is in front.
2. User says "move all ducts up 200 mm".
3. While Heron works, the user clicks over to Tower-B.rvt to check something.
4. Heron's next step resolves "the active document" -> Tower-B.rvt.
5. 247 ducts move in the wrong building.
```

Every step was individually correct. Nobody made a mistake. The model was still damaged.

### Rules adopted

1. **Session binding is not document binding.** They are two separate decisions and Heron must make both.
2. **For any operation that writes, the document is pinned by identity at the start** — and every
   subsequent step **verifies** it, rather than re-resolving "whatever is active now".
3. **Pin by `Document.PathName` plus a document GUID**, never by window title or by "active".
   Title is display text; it is not identity.
4. **If the pinned document is no longer open, stop.** Do not fall back to the active one. Same rule as
   a closed session — fail closed.
5. **If the pinned document is open but not active**, that is fine — the Revit API can address a
   non-active document directly. What is never acceptable is *silently following* the user's clicking.
6. **Reads may use the active document**, but the result must **state which document it read**.
   *"Found 126 ducts in Tower-A.rvt"* is safe; *"Found 126 ducts"* is not.

**[NOTE]** Rule 6 costs one clause in a sentence and eliminates a whole class of confusion. It is the
same instinct as the Evidence System ([23 §6](23-heron-kernel.md)) — say what you acted on.

This becomes an addition to the [Constitution](../HERON_CONSTITUTION.md) and a candidate Golden Rule.

---

## 5. The stale read — **a hazard the specifications never named**

> The real danger is not the freeze, it is the stale read: the AI reads the model, you change something,
> and a later step acts on the old picture. That is why every step re-reads instead of trusting what it
> saw a minute ago.

**[NOTE]** This is an excellent piece of hard-won insight and it generalises further than the note
claims. The model can change between two steps for at least four reasons:

| Cause | Applies |
|---|---|
| The user edits between steps | Always — they share the machine |
| Another user syncs from central | Worksharing models |
| Heron's own earlier step changed it | Any multi-step operation |
| A linked model reloads | Coordination workflows |

**Rules adopted:**

1. **Never trust a read across an `ExternalEvent` boundary.** Each handler invocation re-reads.
   This is the general form of the existing rule *"never cache a `Document` or `Element` across
   handler invocations"* ([03 §7](03-heron-revit.md)) — it is not just object references that go stale,
   it is the whole picture.
2. **Store `UniqueId`, re-resolve every time.** Already the rule; the field notes explain *why* it
   matters beyond ID stability.
3. **A preview is a promise with a shelf life.** *"This will move 247 ducts"* is only true at the moment
   it was computed. Before executing, **re-count**. If the number changed, stop and re-present rather
   than proceeding. A preview the user accepted for 247 elements must not silently execute on 261.
4. **Read and write in the same handler invocation where possible.** The gap between reading and writing
   is exactly where the model changes. Closing that gap costs nothing and removes the race.

**[NOTE]** Point 3 is the important one, and it is where the stale read collides with
[Golden Rule 17](14-golden-rules.md). A preview that is not re-validated at execution time is worse than
no preview — it gives the user confidence in a number that is no longer true.

---

## 6. Turn-taking and the freeze

> Revit does one thing at a time, and the AI's script runs on the same thread that draws the screen.
> While it runs, Revit is genuinely frozen. No add-in can change that.

Observed in practice:

- Most jobs finish in **one or two seconds**, with a banner while they run.
- **Mid-command, the AI cannot interrupt** — a half-drawn wall or an open dialog makes it wait.
- **Genuine side-by-side work only exists across two Revits**, never inside one.

**[NOTE]** Three design consequences:

1. **Show the banner.** The working bridge already does. A frozen application with no explanation reads
   as a crash; the same freeze with a banner reads as progress. This is the cheapest trust feature in
   the platform.
2. **Chunk long operations and yield.** A 5,000-element operation done in one handler invocation freezes
   Revit for minutes. Broken into chunks that yield between them, the user keeps a responsive
   application — and each chunk re-reads, which also addresses §5.
3. **"Revit is busy" is a first-class state**, not an error. It is what happens when the user is
   mid-command, and it should be reported as *"waiting for you to finish"* rather than as a failure.
   [04 §6](04-heron-mcp.md).

---

## 7. Practical capacity

> No technical limit, only memory. On a 64 GB machine, **three or four Revits at once is comfortable**;
> five works if the models are small.

**[NOTE]** Useful for the Resource Manager ([23 §10](23-heron-kernel.md)): the constraint is RAM, and the
right behaviour is to watch actual memory rather than enforce a count. It also reinforces the
recommendation to prefer *Revit closed* for heavy background work
([11 §7](11-orchestration-and-workflows.md)) — background indexing competing with four open Revits is
the worst case for both.

---

## 8. What Heron inherits from the working bridge

The earlier connector work is already the reference implementation ([D-06](DECISIONS.md)). This note shows the bridge
problem is not only solved but **debugged** — the multi-instance failure was found and fixed in the
field, which is a year of pain Heron does not have to repeat.

Carried forward:

- Per-PID named pipes ✅ *(already [D-02](DECISIONS.md))*
- Discovery via `%APPDATA%\...\bridges\<pid>.json` ✅ **new — closes a gap in D-02**
- Two pipe server instances per bridge — one serving, one listening — so a new connection is instant ✅
- Explicit user connect per Revit; an unconnected Revit is invisible ✅
- One chat, one Revit; ask once; never guess; fail closed on disconnect ✅
- Progress banner during execution ✅

Changed:

- **Session takeover → lease** *(§3, [Q-36](OPEN-QUESTIONS.md))* — refuse rather than chop a running job
- **Document pinning for writes** *(§4)* — the gap the field notes identify but the old bridge does not
  yet close
- **Preview re-validation before execution** *(§5)* — required by [Golden Rule 17](14-golden-rules.md)
- **No document name in the discovery file** *(§2)* — removes the stale-name trap at its source
