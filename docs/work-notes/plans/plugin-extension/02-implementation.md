# Heron Plugin Extension — the implementation note

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active — nothing built yet. Every stage below is NOT STARTED.** Opened 2026-09-20.
> **Owner:** Ajmal PS.
> **Read [`00-structure.md`](00-structure.md) and [`01-requirements.md`](01-requirements.md) first.**

---

## 1. How to use this note

Ten stages, in order. **Each one proves something the next stage depends on.**

Every stage has:

| | |
|---|---|
| **Do** | The steps, in order |
| **Done when** | The evidence. Not "it looks right" — a named file, a command output, or something seen on screen |
| **Cannot prove** | What the stage genuinely does not answer, said out loud so nobody assumes it did |

**A stage is not done because the code compiles.** A compiler proves the API agrees. It does not prove
an add-in loads, a tab appears, or a file landed in the right folder.

> **Three words used exactly:**
> **BUILT** — the code exists and compiles.
> **PROVEN** — it has been seen working, on the real thing, and the evidence is recorded.
> **NOT STARTED** — nothing exists.
> These are different words on purpose. [`docs/HANDOVER.md §3`](../../../HANDOVER.md) keeps the same
> distinction for the rest of the project.

---

## 2. What this container cannot do

This repository is worked on from Linux. **There is no Windows, no Revit and no .NET SDK for the Revit
targets here.** So these stages **must be proved on the owner's machine**, and saying so up front stops
a stage being marked done on a compile alone:

- Stage 2 — a tab appearing in Revit
- Stage 3 — files landing in the Addins folder
- Stage 4 — the window drawing and behaving
- Stage 6 — routes 1 and 2 installing on a real machine
- Stage 7 — an uninstall leaving the user's data intact
- Stage 8 — the signed installer running past SmartScreen

Everything else can be written and checked here.

---

## Stage 0 — Record the decisions

**Status: NOT STARTED**

Six structural decisions were taken in conversation on 2026-09-20 and currently live **only in
[`00-structure.md`](00-structure.md)**. A decision that lives in a work note disappears when the work
note is deleted.

### Do

1. Write S1 to S6 from [`00-structure.md`](00-structure.md) into
   [DECISIONS.md](../../../DECISIONS.md) as new numbered decisions, in the house format —
   Context, Decision, Consequences.
2. The next free number is **after D-86**. Check it; do not assume.
3. Cross-link them back here.

### Done when

`grep -c '^## D-' docs/DECISIONS.md` has risen by the number written, and each new decision names the
owner instruction and its date.

### Cannot prove

Nothing technical. This is bookkeeping — but it is the bookkeeping that stops the next AI session
inventing a different structure.

---

## Stage 1 — The product manifest

**Status: NOT STARTED**

**The file that makes future expansion real.** Without it, adding *Heron Structure* means rebuilding the
installer, which is exactly what [R-3](01-requirements.md) forbids.

### Do

1. Define **one manifest that lists every Heron product**. It is read as **data, never executed**
   ([R-13](01-requirements.md)).
2. Each product entry carries, at minimum:

   | Field | Why |
   |---|---|
   | `id` | Stable name, e.g. `heron-doc`. Never changes |
   | `name` | What the user reads, e.g. `Heron Doc` |
   | `description` | One line in the installer window |
   | `tab` | The Revit ribbon tab it creates |
   | `addin` | Its `.addin` file name |
   | `assembly` | Its DLL name |
   | `addInId` | Its own GUID — **two products sharing one is a load failure** |
   | `revit` | Which releases it supports, from 2020 to 2027 |
   | `requires` | Other product ids it needs, or empty. **See [Q-PE-2](03-open-questions.md) — do not fill this in for Doc and MEP until the owner rules** |
   | `version` | The release it came from |
   | `partOf` | The tab this piece joins, when it is **not** a tab of its own. Only the `Heron` tab has two pieces today ([S3](00-structure.md)) — `heron-bridge` and `heron-tools` both carry `partOf: heron` |

   **`partOf` is what keeps the window honest.** Without it the installer cannot know that two ticks
   belong under one heading, and the `Heron` tab's two pieces would show as two unrelated products.
   With it, the same field drives the window's indentation and nothing is special-cased in the code.
3. Ship the manifest **as a release asset**, so the installer reads the list of products from the same
   release it downloads them from.
4. Add a checker — the shape of [`tools/check-package.py`](../../../../tools/check-package.py) — that
   refuses a manifest with a duplicate `addInId`, a missing asset, or an unsupported Revit version.
5. The checker also refuses a `partOf` naming a product that is not in the manifest, and a `requires`
   that points at a product not in it. Both are typos that would otherwise surface as a missing tab.

### Done when

- The manifest exists, with the `Heron` product in it and the fields above.
- The checker passes on it, **and fails** on a hand-made copy with a duplicated `addInId`. Both results
  recorded. A checker that has never failed has never been tested.

### Cannot prove

That Revit accepts the GUIDs. Only a real Revit does that — Stage 2.

---

## Stage 2 — Prove the shape with a second tab

**Status: NOT STARTED**

**The cheapest way to find out the whole plan works.** Before any Doc or MEP tool is written, build a
second product that does almost nothing, and see whether **two Heron tabs can live in one Revit**.

### Do

1. Create `Heron.Doc` — a new project, its own `.addin` with **its own new GUID**, its own DLL.
2. Its `OnStartup` builds a tab called `Heron Doc` with **one panel and one button**. The button shows a
   message and nothing more.
3. Multi-target it the way the add-in already is — the eight releases and their runtimes in
   [`docs/16`](../../../16-version-support-strategy.md).
4. Deploy both products by hand with
   [`tools/deploy-addin.ps1`](../../../../tools/deploy-addin.ps1).
5. Start Revit. Look at the ribbon.
6. Delete `Heron.Doc.addin` and `Heron.Doc.dll`. Start Revit again.

### Done when

- **Two tabs are on screen** — `Heron` and `Heron Doc` — and a screenshot is recorded.
- The `Heron` tab's three buttons **still work** with the second product installed.
- After the delete, **`Heron Doc` is gone and `Heron` is untouched.** This is the uninstall story proved
  before any uninstaller exists.

**And the second half of the same stage, added 2026-09-21 — two pieces into ONE tab.**
[S3](00-structure.md) lets the `Heron` tab be built by the AI Bridge piece, the tools piece, or both. So
build a throwaway `Heron.Tools` that adds one panel with one dummy button to the tab named `Heron`, and
prove all three combinations:

| Installed | Expected |
|---|---|
| AI Bridge only | `Heron` tab, AI Bridge panel only — what exists today |
| Tools only | `Heron` tab exists **with no AI Bridge panel**, tools panel present |
| Both | one `Heron` tab carrying both panels — **not two tabs with the same name** |

**Tools-only is the case that will break first**, and it is the one [R-34](01-requirements.md) promises.
Whichever piece loads first has to create the tab and the other has to join it; Revit's
`CreateRibbonTab` throws when the tab already exists, which `BuildRibbon()` already catches. Load order
is Revit's to choose, so **neither piece may assume it is first**.

### Cannot prove

That a real tool works. The button does nothing on purpose — this stage is about the **shape**, and
mixing a real tool into it would leave both unproven when it failed.

> **If this stage fails, stop.** Everything after it assumes separate add-ins can coexist. Find out here,
> at the cost of one dummy button, not after the window is built.

---

## Stage 3 — The installer core, with no window at all

**Status: NOT STARTED**

**Separate the engine from the face.** A window on top of a broken engine is two problems that look
like one.

### Do

1. Write the install engine as something runnable with **no UI** — given a manifest, a list of product
   ids and a list of Revit versions, it installs them.
2. It **drives [`tools/deploy-addin.ps1`](../../../../tools/deploy-addin.ps1)** rather than
   re-implementing the copy. Two copies of a deploy rule drift, and the drift is found by a user.
3. Implement **Revit version detection** ([R-7](01-requirements.md)) — which of 2020 to 2027 are on this
   PC. Detect from the machine, never from a guess.
4. Implement **waiting** while Revit is running ([R-38a](01-requirements.md)) — keep checking, name the
   version still open, and do not proceed until it is genuinely closed. Waiting, not a one-shot refusal:
   the user closes Revit and the install carries on without being started again.
5. Per-product results ([R-19](01-requirements.md)): one product failing does not stop the rest.
5a. **Wait, then replace cleanly** ([R-38](01-requirements.md)). Detect an existing install, back it up,
   **delete its folder completely, verify it is gone, then copy**. Nothing is renamed and no `.old`
   folder is ever created — the owner ruled on 2026-09-21 that a best-effort sweep piles up copies until
   nobody can tell which one Revit is loading.
5b. **If the delete does not fully succeed, STOP** ([R-38b](01-requirements.md)) and change nothing
   further. A clean refusal is recoverable; a half-removed install is not.
6. Every path through [`HeronPaths`](../../../../platform/Heron.Core/HeronPaths.cs). **Nothing else
   builds a Heron path** — that is the rule in [`platform/README.md`](../../../../platform/README.md).

### Done when

- Running the engine with `heron` and `heron-doc` installs both into a chosen Revit version, and both
  tabs appear on restart.
- Running it with Revit **open** waits and names which Revit. Close it, and the install **continues on
  its own** without being started again.
- After a replace, the Addins folder holds **one copy of the product and no `.old` anything**
  ([R-38c](01-requirements.md)). Count the folders before and after — this is the check that catches a
  rename sneaking back in.
- A deliberately broken product in the list fails **alone**, and the report says which and why.
- Installing over an existing version **replaces** it: a file that version 1 shipped and version 2 does
  not is **gone** from the Addins folder afterwards. Plant one and check; this is the step that fails
  silently if it is written as a copy.
- `%APPDATA%\Heron` is **byte-for-byte unchanged** across an install ([R-20](01-requirements.md)).
  Compare before and after; do not assume.

### Cannot prove

Anything about the window, and anything about downloading — the files are local at this stage.

---

## Stage 4 — The window

**Status: NOT STARTED**

### Do

1. One window, laid out as [`01-requirements.md §3`](01-requirements.md) draws it.
2. **The product list is drawn from the manifest.** If the list is written in the window's code, the
   stage has failed its purpose even if it looks correct.
3. Show each product's real state — Installed / Not installed / Update available ([R-2](01-requirements.md)).
3a. **The `Heron` tab opens into two ticks**, driven by `partOf` in the manifest and not by code
   ([R-33](01-requirements.md)). Every other tab is one tick ([R-36](01-requirements.md)).
3b. **No Update or Repair button.** Install replaces whatever is there ([R-23a](01-requirements.md)), and
   the window says so plainly on a product already installed rather than leaving the user to guess
   whether they are about to duplicate it.
4. Show detected Revit versions as tick boxes ([R-8](01-requirements.md)).
5. Grey out a product that does not support a ticked version, **with the reason on it**
   ([R-10](01-requirements.md)).
6. The "Close Revit first" line is visible **before** the user presses Install, not after it fails.
7. Wording per [`docs/14`](../../../14-golden-rules.md) — plain English, and every failure says what to
   do next.

### Done when

- A screen recording: tick two products, two Revit versions, press Install, and both tabs appear in both
  Revits after restart.
- Adding a product to the manifest makes it appear in the window **with the installer not rebuilt**.
  This is [R-3](01-requirements.md) and it is the single most important test in this stage.
- Ticking **All tools without the AI Bridge connector** installs the tools and no bridge, and the
  `Heron` tab appears with the tools panel and no AI Bridge panel ([R-34](01-requirements.md)).
- Pressing Install on an already-installed product **replaces** it and the window says it did, rather
  than reporting a fresh install that did not happen.

### Cannot prove

That it survives a real site laptop — locked-down policies, no internet, odd display scaling. That is
Stage 7.

---

## Stage 5 — Fetch from the GitHub release

**Status: NOT STARTED**

### Do

1. The installer reads the manifest **from a named release**, not from a branch
   ([`docs/07 §1a`](../../../07-installation-and-update.md)).
2. Download each ticked product's asset.
3. **Verify before use** ([R-12](01-requirements.md)) — a published checksum at minimum. A file that
   fails verification **never reaches the Addins folder**.
4. Failure messages that name the cause ([R-14](01-requirements.md)): no internet · blocked by network ·
   release not found · file corrupt. **"Error" is not one of the allowed words.**
5. Nothing downloaded is executed in order to decide what to install ([R-13](01-requirements.md)).

### Done when

- A clean PC with no Heron installed gets `Heron` from a real GitHub release and the tab appears.
- A deliberately corrupted asset is **refused**, the Addins folder is untouched, and the message says
  the file did not verify.
- With networking off, the message says it cannot reach GitHub — not "error".

### Cannot prove

Whether a particular contractor's firewall allows it. That is found on site, and it is why
[Q-PE-5](03-open-questions.md) stays open.

---

## Stage 6 — Routes 1 and 2, onto the same engine

**Status: NOT STARTED**

Stages 3 to 5 build **route C** — the installer with a window. [S7](00-structure.md) says there are
three front doors and one engine, so this is where the other two are hung on it.

**If this stage ends up re-implementing any install rule, it has failed**, however well it works. A rule
that lives in three places is three rules, and two of them go stale.

### Do

1. **Route 2 — the repo handed to the AI** ([R-29](01-requirements.md)). The user downloads the repo and
   gives it to the AI in-product; the AI installs from the files it was given.
2. **Route B downloads nothing** ([R-30](01-requirements.md)). Its files are already in the repo, so it
   must take the local path through the engine rather than the release path. This is the offline install
   that [Q-PE-5](03-open-questions.md) wanted, arriving as a side effect.
3. **Route 1 — natural language** ([R-28](01-requirements.md)). The user says *"install this repo"* and
   the AI installs everything, **then asks which panels they want**. Asking afterwards is the point: a
   conversation can install first and narrow later, which a window cannot.
   **Read [Q-PE-10](03-open-questions.md) before building this one** — whether route 1 may act on any
   repository, or only Heron's own signed release, is unresolved and it is a security question.
4. **Both report** ([R-32](01-requirements.md)) — what was installed, into which Revit versions, what was
   skipped and why. An automatic install that says nothing cannot be checked by the person it happened
   to.
5. Neither route may ask for administrator rights, and neither may touch `%APPDATA%\Heron`
   ([R-16](01-requirements.md), [R-20](01-requirements.md)).

### Done when

- Route 2 installs on a machine **with networking off**, from a fresh clone, and the tabs appear.
- Route 1 installs from a sentence, then **asks which panels** and honours the answer.
- **The engine is one engine:** a rule changed once — the refusal while Revit is open is the cheapest to
  test — changes behaviour in **all three** routes. Break it deliberately and watch all three fail;
  that is the only proof that they share it.
- Each route prints a report naming products, versions, and skips.

### Cannot prove

Whether the cloud plan itself is right. Route A can only install what it is told to install; what it is
told comes from a cloud design that is outside this folder.

---

## Stage 7 — Uninstall, update, and a rollback that has been tested

**Status: NOT STARTED**

### Do

1. **Uninstall**: untick a product, apply, its files go ([R-21](01-requirements.md)).
2. **Uninstall removes the product and nothing else.** `%APPDATA%\Heron` survives
   ([R-22](01-requirements.md)).
3. **Update**: a newer version replaces the old one in place, settings kept ([R-23](01-requirements.md)).
4. **Rollback, actually performed** ([R-24](01-requirements.md)) — `docs/07 §7` rule 5 demands it is
   tested, not merely implemented, and `brain/heron_update.py` refuses a release without a recorded
   `rollback_tested`.
5. Audit line per operation ([R-27](01-requirements.md)) through
   [`HeronAudit`](../../../../platform/Heron.Core/HeronAudit.cs).

### Done when

- Install, uninstall, reinstall, update, roll back — all five run, and the tab state after each is
  recorded.
- After a full uninstall of every product, `%APPDATA%\Heron` still holds the config and the audit log.
- `rollback_tested` is recorded, so `heron_update.py` no longer refuses the release.

### Cannot prove

An upgrade from a version that does not exist yet. Re-test at every release; this is not a one-time
stage.

---

## Stage 8 — Sign it, and ship it

**Status: NOT STARTED**

### Do

1. **Code-sign the installer** ([R-25](01-requirements.md)). Unsigned, it is blocked by SmartScreen and
   by contractor IT — and the user it is for is precisely the one who cannot click past that.
2. Metadata header ([`docs/29`](../../../29-metadata-standard.md)) — the five fields, on the installer
   too ([R-26](01-requirements.md)).
3. Publish as a GitHub release with the manifest and every product asset.
4. **Test on a real locked-down machine**, not a developer PC. A developer PC is the one machine that
   proves nothing about this.
5. Write the install instructions in the README — one documented command or one documented download.
   **Never "paste this link and let the AI run it"**; `docs/07 §1a` refuses that, and so does
   [Golden Rule 19](../../../14-golden-rules.md).

### Done when

- A modeller who has never seen Heron installs it on a locked-down laptop, unaided, and reports the tab
  appearing.
- No administrator prompt appeared at any point.

### Cannot prove

That the products are any good. That is what the fragments' own proofs are for.

## Stage 9 — The Settings panel

**Status: NOT STARTED**

**Last, and deliberately so.** It decides what is *shown*, which is meaningless until something is
reliably *installed*. [S9](00-structure.md).

### Do

1. **[Q-PE-11](03-open-questions.md) is answered — panels yes, tabs no.** `RibbonPanel.Visible` is
   read-write official API and works live. Hiding a tab needs unsupported `AdWindows.dll` and **Heron
   will not do it** ([R-43a, R-43b](01-requirements.md)). One confirmation is still owed on a machine
   with a .NET SDK: that `RibbonPanel.Visible` is present in **all eight** releases —
   `tools/check-api-surface.py` answers it, and the evidence says it long predates 2020.
2. A **Settings panel** on the `Heron` tab, installed whenever any Heron product is
   ([R-42](01-requirements.md)).
3. A window listing every installed **panel**, each with a tick ([R-43](01-requirements.md)) — grouped
   under its tab for reading, but **the tab itself is not tickable** ([R-43a](01-requirements.md)). The
   list is read from **the manifest**, never typed into the window ([R-46](01-requirements.md)).
4. The choice saved under `%APPDATA%\Heron` ([R-44](01-requirements.md)), so it survives every install
   and replace.
5. Each product's ribbon build **reads that choice** and builds only what is on. A hidden panel is
   **still installed** ([R-45](01-requirements.md)) — un-hiding needs no installer and no download.
6. **No restart message is needed** — the change is live ([R-43c](01-requirements.md)). If a panel ever
   fails to hide, the window says so plainly rather than ticking the box and doing nothing visible.
7. The window is a tool window and its code-behind **never touches the Revit API directly** — the
   `ExternalEvent` pattern, per
   [`revit-ribbon-and-windows`](../../../../.claude/skills/revit-ribbon-and-windows/SKILL.md).

### Done when

- Every panel can be hidden and brought back **without restarting Revit**, and a screen recording shows
  it happening live.
- **No tab is ever hidden**, and `grep -ri "AdWindows\|Autodesk.Windows" revit/` finds nothing
  ([R-43b](01-requirements.md)).
- Hiding a panel, closing Revit and reopening it: **still hidden**. The choice is persistent, not a
  session toggle.
- Running the installer again — including a replace — leaves the choices **untouched**
  ([R-44](01-requirements.md)).
- Hiding a panel and then checking the Addins folder: **the files are still there**
  ([R-45](01-requirements.md)).
- Installing a new product while others are hidden: the new one is **visible**
  ([R-47](01-requirements.md)) — and if that turns out to be the wrong default, the row says to reverse
  it rather than to argue about it.

### Cannot prove

Whether the list is the one a modeller wants to see. That is a question for a modeller using it for a
week, not for a test.

---

## 3. Order, and why it is this order

```text
Stage 0  decisions      -> so the next session does not reinvent the shape
Stage 1  manifest       -> so expansion is data, not a rebuild
Stage 2  second tab     -> CHEAPEST POSSIBLE PROOF that the whole plan works
Stage 3  engine         -> install works before anything is drawn
Stage 4  window         -> a face on a working engine
Stage 5  download       -> the network, last of the mechanics
Stage 6  routes 1 and 2 -> the other two front doors, onto the SAME engine
Stage 7  uninstall      -> the way back, before real users arrive
Stage 8  sign and ship  -> the locked-down laptop is the real exam
Stage 9  settings       -> what is SHOWN, once what is INSTALLED is reliable
```

**Stage 2 is the one to do first after the paperwork.** It costs a dummy button and it answers the
question the entire plan rests on: *can two Heron tabs live in one Revit?* Every other stage assumes
yes.

This follows the rule already written in [`docs/27-build-order.md`](../../../27-build-order.md) — the
rails come before the thing that runs on them.

---

## 4. Progress

Update this table as stages complete. **Do not mark a stage done without its evidence recorded.**

| Stage | What | Status | Evidence |
|---|---|---|---|
| 0 | Record the decisions | NOT STARTED | — |
| 1 | Product manifest | NOT STARTED | — |
| 2 | Second tab | NOT STARTED | — |
| 3 | Installer core | NOT STARTED | — |
| 4 | The window | NOT STARTED | — |
| 5 | GitHub download | NOT STARTED | — |
| 6 | Routes 1 and 2 | NOT STARTED | — |
| 7 | Uninstall / update / rollback | NOT STARTED | — |
| 8 | Sign and ship | NOT STARTED | — |
| 9 | The Settings panel | NOT STARTED | — |
