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
| 4 | **The whole session list is a connect-time snapshot.** It is written once, when Connect is clicked, and never updates | The list must be **built live at ask-time**, not read from a cache. See §2a |
| 5 | **One Revit can hold several projects open**, and commands land on whichever window is in front — which changes when the user clicks | Choosing the *Revit* is only half the problem. See §4 — this is the most dangerous finding in the note |
| 6 | **The list never shows which Revit another chat is already using.** *"The information exists in Revit, it is just never written down where I can see it"* | The lease (§3) is not only a safety mechanism — it is the missing data that makes the list honest |
| 7 | **Making Revit not freeze cannot be done.** Working around it needs a whole separate process, already recorded as out of scope | Settled. Do not chase it. See §6a |

**[NOTE]** Point 2 deserves emphasis. Four specification documents, several hundred sections, and the
single hardest constraint in the platform never appears in any of them — but it is written plainly in a
note describing software that already works. That is the difference between designing a system and
running one, and it is why these field notes are treated as authoritative here.

---

## 2. Bridge discovery — the piece [D-02](DECISIONS.md) was missing

> Each Revit hosts its own private line named after its process number, and advertises itself in
> a per-process file under the bridge directory — in Heron, `%LOCALAPPDATA%\Heron\bridges\<pid>.json`.

**[NOTE]** [D-02](DECISIONS.md) settled the *transport* (named pipes, per-PID naming) but never said how
the client **finds** the pipes. Named pipes cannot be enumerated usefully. The working bridge answers it:
a **discovery directory** — one small JSON file per live bridge, named by PID.

Adopted for Heron, with additions the field notes imply but do not state:

```text
%LOCALAPPDATA%\Heron\bridges\<pid>.json
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

**[NOTE - changed during implementation, 2026-08-27]** The directory moved from `%APPDATA%`
(Roaming) to `%LOCALAPPDATA%` (Local) while building Step 1.

Roaming AppData **synchronises between machines** in a domain environment - which is exactly where
Heron's users work. A discovery file announcing process 24156 would follow the user to a different PC,
where that process does not exist. The client would try it, fail, and prune it, so the system
self-heals - but it is noise that never needed to exist.

Runtime state belongs to the **machine**, not to the person. Preferences and learned skills roam;
a live process id does not. Enforced by `HeronPaths` ([D-17](DECISIONS.md)).

**Two rules the field notes make necessary:**

1. **Do not put the document name in the discovery file.** That is exactly what produced the stale-name
   trap (§1.4). The document is queried live, on demand, or not at all.
2. **A JSON file is not proof the bridge is alive.** Revit crashes without cleaning up. The client must
   verify the PID is running *and* the pipe answers before listing a session as available — and remove
   the stale file when it does not.

---

## 2a. The session list — static facts in the file, dynamic facts on demand

> *"The list is written once, when you first click Connect. It never updates."*
>
> *"You close BL006A, open BL003A, and the list still says BL006A. This actually happened on 20 Aug.
> You'd be picking from a list that lies to you — at exactly the moment where being wrong is most
> expensive."*

**[NOTE — this corrects §1.4 of an earlier draft of this document.]** The stale name is a *symptom*.
The cause is that the entire list is a snapshot taken at connect time. Fixing the name field alone would
leave the same class of bug waiting in every other field.

One principle fixes all of it:

| Kind of fact | Where it lives | Examples |
|---|---|---|
| **Static** — true for the life of the process | The discovery file | `pid`, `pipeName`, `revitVersion`, `addinVersion`, `protocolVersion`, `startedAt` |
| **Dynamic** — can change at any moment | **Queried live, every time the list is built** | open documents, active document, lease state, health |

The discovery file is an **address book**, not a status report. It tells the client *where* the bridges
are. Everything about what a bridge is currently *doing* is asked of the bridge, at the moment the
question is asked.

That single rule removes both traps at once: the name cannot go stale because it is never stored, and
availability becomes reportable because it is fetched rather than remembered.

### Building the list costs one round trip per bridge

Discovery enumerates the files; the client then pings each live bridge in parallel for its current
document and lease state. Bridges that do not answer are dropped from the list **and their stale files
removed**. On a machine running three or four Revits this is milliseconds, and it is the one moment in
the whole platform where being wrong is most expensive — worth paying for.

### What the user sees — never process numbers

> *"Stop showing you process numbers like `39344`."*

```text
1) Revit 2024 — Tower A     (free)
2) Revit 2020 — Podium      (in use)
```

The user says `1`. Heron does the rest.

**[NOTE]** This corrects a mistake in the earlier draft, which said *"PID is the identity"* without
qualification. Both halves are true, of different audiences:

| | Identity used |
|---|---|
| **Internally** — binding, leases, pipe names, audit log | **PID.** Never the document name |
| **To the user** — the picker, confirmations, results | **Revit version + project + availability**, chosen by list number |

A process number is meaningless to a BIM modeller and asking them to read one is a small violation of
[Golden Rule 1](14-golden-rules.md). The list should show what they actually recognise — which building
they are working on — while Heron binds to the PID underneath.

**Requirements for that display:**

- **Project name comes from the live query**, never from the file. That is the whole point.
- **`(free)` / `(in use)` comes from the lease** (§3). Without a lease there is nothing truthful to show,
  which is why [Q-36](OPEN-QUESTIONS.md) is a display question as much as a safety one.
- **If a Revit holds several projects open, say so** — `Revit 2024 — Tower A (+2 more projects)` — because
  picking the Revit is only half the decision (§4).
- **List numbers are per-question, not identities.** They are never reused across prompts and never
  stored.

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

### A binding the user never made is not sticky

**[NOTE]** **Field-proven, and absent from every specification.** *"Explicit, sticky, fail closed"*
above covers a session the user actually chose. It says nothing about the far more common case: one
Revit was open, so the client simply used it. That is an **assumption**, not a choice, and the two must
not be treated alike.

Track **how** the current session was arrived at, and the four cases resolve differently:

| How it was bound | What changes | What must happen |
|---|---|---|
| Assumed — only one was open | a second Revit appears | **Ask.** The user never chose this one |
| Assumed — only one was open | it closes | Quietly take the remaining one. Nothing of theirs is contradicted |
| **Chosen** by the user | more Revits appear | Keep their choice. Do not ask again |
| **Chosen** by the user | it closes | **Stop and say so.** Never slide onto another project |

Without that distinction, *"sticky"* is implemented as *"keep whatever we picked first"* — and opening
a second Revit mid-conversation leaves every later command silently going to the first one. That is the
wrong-model failure this whole document exists to prevent, arriving through the mechanism meant to stop
it. **Found by running it, not by reasoning about it.**

Whichever way it refuses, the refusal must say **"nothing has been sent to Revit"** in as many words.
The user's first thought on any refusal is *did it half-do something?*, and answering that unasked is
the difference between a safe stop and a frightening one.

Settles part of [Q-11](OPEN-QUESTIONS.md); the mechanism is built in [Step 5](27-build-order.md).

### What the lease protects — **per process, not per document**

**[NOTE]** A natural assumption, and a wrong one: *"two chats on the same Revit are fine as long as
each talks about a different model."* They are not.

The contention is at the **bridge**, not the document. Both chats connect to the same named pipe on the
same Revit process, so the second still displaces the first regardless of which model each is discussing.
Two models open does not make two sessions.

> **One Revit is one door.** Two chats cannot walk through it at once, even heading for different rooms.

So the lease is scoped to the **Revit process (PID)** — the same unit as the pipe, the discovery file and
the session binding. One lease per Revit, held by one chat, covering everything that Revit has open.

The conflict matrix, stated once:

| Setup | Conflict | Why |
|---|---|---|
| Chat A → Revit 2020 · chat B → Revit 2024 | **No** | Different processes, different pipes |
| Chat A → Revit 2020, no other chat | **No** | One holder |
| Chat A → Revit 2024 · chat B → the **same** Revit 2024, **different models** | **Yes** | Same process, same pipe, same lease |
| Chat A → one Revit holding two models, both handled by that one chat | **No** | One holder. But document pinning still applies — see §4 |

**[NOTE]** Two documents in one Revit are also **not parallel**. They share the single Revit thread
(§6), so work on one freezes the other. Two models is not two workers. Real concurrency only exists
across two Revit processes — which is the same conclusion as §6a reaches from the freeze side.

**[NOTE]** One consequence for the UI: selection is per-document. If Heron sets a selection in a model
that is not in front, the operation succeeded but the user will not *see* it until they switch to that
model. Any result reporting a selection should name the document it applies to — which
[Golden Rule 20](14-golden-rules.md) requires anyway.

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

**[NOTE]** The addendum note makes the case for a lease stronger than safety alone:

> *"The list doesn't say which Revit another chat is already using. Nothing shows it. That's why you
> have to tell me 'don't go to Revit, another session is running' — the information exists in Revit,
> it is just never written down where I can see it."*

The standing rule *"don't go to Revit, another session is running"* is a **human being used as a
lock**. The user is manually carrying state the machine already has and simply never surfaces.

A lease removes that job from the user twice over — it prevents the collision, **and** it is the
thing that makes `(free)` / `(in use)` truthful in the picker (§2a). Without a lease there is no
honest availability column to show, and the user goes on being the lock.

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

## 6a. The freeze is out of scope — settled

> *"Making Revit not freeze **can't be done**. Revit runs one thing at a time by design — working around
> it needs a whole separate process, and my own code already records that decision as out of scope.
> Chasing it would be a lot of work for something that will still break."*
>
> *"**The second-Revit answer is the real one.**"*

**[NOTE]** Adopted as a scope decision, and worth recording explicitly so nobody re-opens it later.

The theoretical workaround — running Revit's work in a separate process, or driving several documents
concurrently — fights the Revit API's fundamental design. It is a large amount of work with a poor
success rate, and it would remain fragile across eight Revit versions ([D-05](DECISIONS.md)).

**Heron does not attempt it.** What it does instead, and what actually solves the user's problem:

| Instead of removing the freeze | Heron does this |
|---|---|
| Make Revit responsive during work | **Keep jobs short** — most finish in one or two seconds |
| Hide the freeze | **Show a banner.** A frozen app with no explanation reads as a crash; the same freeze with a banner reads as progress |
| Interrupt the user mid-command | **Wait.** *"Waiting for you to finish"* is a normal state, not an error |
| Fake concurrency inside one Revit | **Chunk and yield** so long jobs do not freeze it for minutes |
| Pretend side-by-side work is possible | **Tell the user to open a second Revit.** Real parallel work exists across two Revits, never inside one |

That last row is the actual product answer, and it should be said plainly in the docs and in the UI
rather than left for users to discover.

---

## 6b. Cross-document work — the trade-off that decides the setup

**[NOTE]** A real BIM workflow the four specifications never mention: **comparing two models**, or
**borrowing from one model into another** — a family, a type, a set of elements from a tower model
needed in a school model.

This is ordinary Revit API work. `ElementTransformUtils.CopyElements` copies elements between two open
documents; family symbols can be copied or loaded across; comparison is reading both and diffing.

**But it has a hard constraint that interacts directly with everything above:**

> The Revit API can only reach documents open in the **same Revit process**. It cannot copy or compare
> across two separate Revit windows — different processes, no shared API surface.

That produces a trade-off the user must actually choose between:

| Setup | Parallel work | Cross-document work |
|---|---|---|
| **Two Revits** — one model each | ✅ Genuinely side-by-side (§6a) | ❌ **Impossible.** Separate processes |
| **One Revit** — both models open | ❌ One thread; they take turns (§3) | ✅ Copy, compare, borrow families |

**You cannot have both at once.** Heron should say so plainly when the user's request implies one or
the other:

- *"Work on this while the AI does that"* → **two Revits**
- *"Take that family from Tower A into School B"* → **one Revit, both models open**

### What this requires of Heron

1. **Cross-document operations are a distinct capability**, not a variant of a single-document one. They
   take a **source** document and a **destination** document, and both must be pinned
   ([Golden Rule 20](14-golden-rules.md)) — the pinning problem doubles rather than disappearing.
2. **The transaction belongs to the destination document.** The source is read-only throughout.
3. **Report what did not copy.** Elements needing hosts absent from the target (levels, grids, walls),
   view-specific elements (dimensions, tags), and system family types already present with a different
   definition all fail or transform. Silently dropping them is the worst outcome — the user believes
   the copy is complete.
4. **Duplicate type names need a policy**, decided before the operation and stated in the preview:
   use the destination's existing type, or bring the source's in renamed.
5. **Comparison is read-only and safe**, and is a strong early capability — it needs no transaction, no
   `MODIFY` permission, and answers a question BIM coordinators ask constantly.

**[NOTE]** Model comparison deserves attention as a product feature, not just a mechanism. *"What
changed between this model and last week's?"* and *"how does the subcontractor's model differ from
ours?"* are daily coordination questions currently answered by hand or by expensive tooling. It is
read-only, needs no code generation, and fits [PROPOSALS B2 and B8](PROPOSALS.md).

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
- **The session list is built live at ask-time** *(§2a)* — static facts in the file, dynamic facts on
  demand. Fixes the whole class of staleness rather than one field
- **The picker shows version, project and availability — never a process number** *(§2a)*
- **`(free)` / `(in use)` shown in the picker** *(§2a, §3)* — stops the user having to act as the lock

Explicitly **not** attempted:

- **Removing the Revit freeze** *(§6a)*. Settled as out of scope. Short jobs, a banner, chunk-and-yield,
  and a second Revit are the real answers
