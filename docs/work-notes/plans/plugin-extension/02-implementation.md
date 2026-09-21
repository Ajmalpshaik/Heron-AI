# Heron Plugin Extension — the implementation note

> **Type:** Operational work note. **Not specification.** Where a sentence here disagrees with the
> [Constitution](../../../../HERON_CONSTITUTION.md), the [Golden Rules](../../../14-golden-rules.md) or
> [DECISIONS.md](../../../DECISIONS.md), **those win and this note is out of date.**
> **Status:** **Active.** Stage 0 and Stage 1 are DONE. **Stages 2, 3 and 4 were RUN on the owner's PC
> on 2026-09-21 and are PROVEN** — Revit 2024.3 and Revit 2020.2.9, fourteen rows passed, one failed and
> was fixed the same day, and two are owed (`AA9` needs a browser download, `AB6` is blocked until
> `heron-tools` is a real product). Stages 5 to 9 are NOT STARTED. Opened 2026-09-20.
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

**Status: DONE — 2026-09-21. [D-87 to D-95](../../../DECISIONS.md), all nine.**

Nine structural decisions were taken in conversation on 2026-09-20 and 2026-09-21 and lived **only in
[`00-structure.md`](00-structure.md)**. A decision that lives in a work note disappears when the work
note is deleted. They are now in [DECISIONS.md](../../../DECISIONS.md), which is append-only and never
deleted.

**Nine were written, not six.** The list below says *"S1 to S6"* because it was written on 2026-09-20,
before S7, S8 and S9 existed. Ajmal asked on 2026-09-21 for all nine. Six would have left the three
newest decisions — including the Settings panel, which the whole of Stage 9 rests on — in a folder that
gets deleted at closure.

**S7 was split, and that is recorded rather than smoothed over.** It carries two claims: *the product
list is a manifest* (structural, nothing later works without it → **D-93**) and *there are three front
doors onto one engine* (Stage 6, not built, not designed in detail, and carrying an unresolved security
question in [Q-PE-10](03-open-questions.md)). The second is **not recorded yet**. Writing an unbuilt
route into the permanent register as a settled decision is the exact move rule 2 of
[this folder](README.md) forbids.

### Do

1. Write S1 to S6 from [`00-structure.md`](00-structure.md) into
   [DECISIONS.md](../../../DECISIONS.md) as new numbered decisions, in the house format —
   Context, Decision, Consequences.
2. The next free number is **after D-86**. Check it; do not assume.
3. Cross-link them back here.

### Done when

`grep -c '^## D-' docs/DECISIONS.md` has risen by the number written, and each new decision names the
owner instruction and its date.

**Evidence, 2026-09-21:**

```text
grep -c '^## D-' docs/DECISIONS.md     88  before  ->  97  after      (+9)
highest decision before                D-86  (derived, not assumed:
    grep -oE '^#+ *D-[0-9]+' docs/DECISIONS.md | grep -oE '[0-9]+' | sort -n | tail -1)
written                                D-87 .. D-95
python tools/generate-decision-summary.py   added 9 missing row(s)
python tools/check-docs.py                  BROKEN LOCAL LINKS: 0
                                            D-87..D-95 all resolve
```

Each of the nine names the owner instruction it came from and its date, and each is cross-linked both
ways — the decision points back at its `S` section, and [`00-structure.md §3`](00-structure.md) carries
a table pointing forward at the decision.

### Cannot prove

Nothing technical. This is bookkeeping — but it is the bookkeeping that stops the next AI session
inventing a different structure.

---

## Stage 1 — The product manifest

**Status: DONE — 2026-09-21.** [`platform/heron-products.json`](../../../../platform/heron-products.json),
checked by [`tools/check-products.py`](../../../../tools/check-products.py), which has been **made to
fail on purpose and did**.

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

**Evidence, 2026-09-21. Both results, as the row above demands.**

```text
python tools/check-products.py                          EXIT 0
    Products:  5  (2 shipped, 2 proving, 1 planned)
    Releases built:  2020..2027   (brain/heron_dotnet.py, DERIVED not typed)

python tools/check-products.py --file <copy with heron-doc given
                                       the AI Bridge's GUID>     EXIT 1
    DUPLICATE addInId: heron-bridge and heron-doc both claim
    7A1F4C62-9D3E-4B18-8E52-1C0A6F2D5B41. Revit keys add-ins by this
    GUID, so installing both is a load failure and neither tab appears

python tests/test_products_manifest.py                  EXIT 0, 36 checks
    16 different faults, each broken on a copy, each refused BY NAME
```

**The format is JSON, and the reason is evidence rather than taste.** The installer and the Settings
panel both have to read this file on Windows, inside Revit, on all eight releases. Heron already parses
JSON there with no package at all — [`revit/Heron.Bridge/Json.cs`](../../../../revit/Heron.Bridge/Json.cs),
hand-written precisely so nothing is loaded into Revit's assembly context. YAML would need a NuGet
package on every one of the eight targets. The five metadata fields are carried as **keys in the
document**, the way a `fragment.yaml` carries them, because JSON has no comments.

**Three things were decided here that the plan did not name, and each is reversible:**

| | Decided | Why |
|---|---|---|
| **A `state` field** | `SHIPPED` · `PROVING` · `PLANNED` | Without it the manifest either lies — offering products whose files do not exist — or is useless to Stage 4, which needs the `Heron` tab's two ticks to be real rows. **`PROVING` was added because the checker asked for it**: it refused a `PLANNED` row once Stage 2's files appeared, and a third state was the honest answer |
| **GUIDs allocated now** for `heron-tools`, `heron-doc`, `heron-mep` | in the manifest, not later | A GUID must be unique for ever and a duplicate is fatal. Allocating them from one file is what stops two sessions generating the same one |
| **Heading-ness is derived**, never a field | an entry is a heading exactly when another entry names it in `partOf` | D-93 says the same field drives the indentation and nothing is special-cased. A `kind` field would be a second thing to keep in step |

**`requires` is empty everywhere, on purpose.** [Q-PE-2](03-open-questions.md) — whether Doc and MEP
work without the AI connector — has not been ruled on. An empty list says *not decided*; a filled one
would be a guess wearing a decision's clothes.

**What Stage 3 inherits as a known gap.** The checker can verify that a product agrees with the
`.addin` it ships, because that file is in the repository. It **cannot** check a release asset, because
there is no release. That half of *"refuses a missing asset"* arrives with Stage 5.

### Cannot prove

That Revit accepts the GUIDs. Only a real Revit does that — Stage 2.

---

## Stage 2 — Prove the shape with a second tab

**Status: PROVEN — 2026-09-21. DONE, except `AA9`.**

> **RUN ON THE OWNER'S PC, 2026-09-21. The question this stage was built to ask has an answer, and the
> answer is yes.** Revit 2024.3 and Revit 2020.2.9: **two tabs appeared, `Heron` and `Heron Doc`, and
> only one of them read `Heron`** — so `CreateRibbonTab` found the existing tab instead of making a
> second one, and **R-35 holds**. The `Heron` tab carried an `AI Bridge` panel and a `Tools` panel
> together, built by two separate add-ins.
>
> **`AA2`, the case written down as the one expected to break first, did not break.** With the AI Bridge
> removed, the tools piece **created the `Heron` tab on its own**. So `CreateRibbonPanel` can reach a tab
> another add-in made, **S3 stands as designed**, and **R-34 is deliverable**: the tools and the AI
> connector stay separate products.
>
> `AA1` to `AA8` PASS. **`AA9` is NOT RUN** — it needs the repository downloaded as a zip through a
> browser, which is what puts the Windows mark on the files, so `Unblock-File` in `deploy-addin.ps1`
> **still has never run**. Rows and evidence: [NEEDS-CHECKING](../../../NEEDS-CHECKING.md), Group AA.
> Screenshots: [`docs/proof/`](../../../proof/).

### What exists now

| | |
|---|---|
| [`revit/Heron.Doc/`](../../../../revit/Heron.Doc/) | the SECOND tab — `Heron Doc`, one panel, one button that shows a message |
| [`revit/Heron.Tools/`](../../../../revit/Heron.Tools/) | the SECOND PIECE of the FIRST tab — a `Tools` panel on the tab named `Heron` |
| [`tools/deploy-addin.ps1`](../../../../tools/deploy-addin.ps1) | puts them into Revit and takes them out again — `-Product heron-doc`, `-Product heron-tools` |
| [`tests/test_ribbon_tab_sharing.py`](../../../../tests/test_ribbon_tab_sharing.py) | the half a machine with no Revit can answer |

**Both are throwaways and both say so in their own headers.** They are `PROVING` in the product
manifest — the state meaning *the files exist and no user may be offered them* — and the whole of both
projects is deleted when the real Heron Doc and Heron tools are built.

**How these get onto a machine changed on 2026-09-21, in the middle of the stage.** When Stage 2 was
built, `tools/deploy-addin.ps1` named `Heron.Revit.Addin` in every path and could deploy nothing else,
so the stage was given a small throwaway script of its own rather than widening a proven one. Stage 3
then had to widen it anyway — [R-31](01-requirements.md) allows exactly one engine, and an engine that
can install one product is not one. The throwaway had become a **second copy of the deploy rule**, which
is how one of two copies goes stale, so **it was deleted** and Stage 2 now deploys with the real script:

```powershell
.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-doc
.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-tools
```

**That makes this stage's proof worth more, not less.** The Revit session that answers AA1 to AA6 now
exercises the script that ships, rather than one written to be thrown away. What it costs is that
`deploy-addin.ps1` has **changed since its last proof and has not been run** — see the four states
below, and rows `AA7` to `AA9` in [NEEDS-CHECKING](../../../NEEDS-CHECKING.md).

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

### Where it actually stands — the four states, kept apart

| | |
|---|---|
| **PASS** | Compiles. `tools/check-compile.py` — **all 9 projects on all 8 releases, 2020 to 2027, 0 warnings**, with the two new ones included. That is the API agreeing and **nothing more** |
| **PASS** | `tests/test_ribbon_tab_sharing.py` — 26 checks. The two Heron pieces name the same tab character for character, **neither assumes it loaded first**, the panels have different names, Heron Doc is its own tab, three manifests carry three different GUIDs, and nothing under `revit/` touches `Autodesk.Windows` |
| **NEEDS REAL REVIT** | **(a)** two Heron tabs side by side · **(b)** all three combinations — AI Bridge only, tools only, both — as ONE `Heron` tab · **(c)** deleting the second product's two files takes its tab and leaves the first untouched |
| **NOT RUN** | `tools/deploy-addin.ps1` has not executed since it was changed. It is PowerShell for Windows and this container is Linux, so not one line of it has run here. **Its rollback proof of 2026-09-19 is STALE for the current file** |
| **PASS** | `tests/test_deploy_script.py` — the text checks a Linux machine can make on it: `-Product` defaults to `heron-bridge` and **that default resolves to the four values that used to be hardcoded**, no product name is left in the logic, and all six guards are still there |

**Tools-only is still the case expected to break first**, and nothing done here changes that. What has
been done is remove every way it could break *silently*: the tab strings are compared by a test rather
than by care, and both pieces catch `Autodesk.Revit.Exceptions.ArgumentException` around
`CreateRibbonTab` — checked by its **full** name, because `System.ArgumentException` is a different type
and catching that one would catch nothing Revit throws.

### How to finish it — on the machine with Revit on it

**Close Revit before each command.** Every one of them refuses while it is open and names the release
it found; that refusal is itself row `AA6`.

```powershell
dotnet build revit\Heron.Doc\Heron.Doc.csproj      -p:RevitVersion=2024
dotnet build revit\Heron.Tools\Heron.Tools.csproj  -p:RevitVersion=2024

# (b) BOTH - one Heron tab with two panels, plus a second Heron Doc tab
.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-doc
.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-tools
#    start Revit, SCREENSHOT

# (b) TOOLS ONLY - the case that breaks first
.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-bridge -Remove
#    start Revit, SCREENSHOT: a Heron tab WITH NO AI Bridge panel

# (b) AI BRIDGE ONLY - what exists today
.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-tools  -Remove
.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-bridge
#    start Revit, SCREENSHOT

# (c) the uninstall story, before any uninstaller exists
.\tools\deploy-addin.ps1 -RevitVersion 2024 -Product heron-doc -Remove
#    start Revit: Heron Doc gone, Heron tab and its three buttons untouched
```

**Three screenshots, one per combination**, recorded in [NEEDS-CHECKING](../../../NEEDS-CHECKING.md).
**The failure to watch for is two tabs both called `Heron`.** On screen that looks almost right.

### Cannot prove

That a real tool works. The button does nothing on purpose — this stage is about the **shape**, and
mixing a real tool into it would leave both unproven when it failed.

> **If this stage fails, stop.** Everything after it assumes separate add-ins can coexist. Find out here,
> at the cost of one dummy button, not after the window is built.
>
> **It has not been found out yet.** Stage 3 must not begin until the three screenshots exist.

---

## Stage 3 — The installer core, with no window at all

**Status: PROVEN — 2026-09-21. DONE.**

> **RUN ON THE OWNER'S PC, 2026-09-21. The engine did something, not just decided something.** All three
> pieces that had never run one line have now run: `PowerShellRevitEnvironment` found Revit 2020, 2024
> and 2027 on the PC and named the open one by process id; `InstalledProductsOnDisk` read the Addins
> folder and changed its answer when a folder was deleted by hand; and **`DeployScriptDeployer` actually
> installed the AI Bridge for Revit 2024**, which Revit then loaded — tab, panel, and the bridge
> answering `ping`.
>
> It also refused correctly: with Revit open it changed **nothing** and said which process was holding
> it, then carried on by itself once Revit closed. Rows and evidence:
> [NEEDS-CHECKING](../../../NEEDS-CHECKING.md), Group AB.
>
> **It was also built before Stage 2 was proved.** The plan says Stage 3 must not begin until `AA1`,
> `AA2` and `AA3` have passed; the owner chose to build it anyway on 2026-09-21. That is recorded in
> [NEEDS-CHECKING Group AA](../../../NEEDS-CHECKING.md) with what it costs if `AA2` fails — the engine
> survives, the *shape* does not.

**Separate the engine from the face.** A window on top of a broken engine is two problems that look
like one.

### The shape it took — C# decides, PowerShell acts

Everything that has to be **got right** is in C#, where it can be run against a fake Revit and a fake
deployer on any machine. Everything that has to **touch Windows** is one thin adapter per job, and each
one shells out to the script that already owns that rule rather than writing a second copy of it.

| | |
|---|---|
| [`platform/Heron.Installer/ProductManifest.cs`](../../../../platform/Heron.Installer/ProductManifest.cs) | reads `platform/heron-products.json`. Nothing about a product is written in code — [R-3](01-requirements.md) |
| [`InstallPlan.cs`](../../../../platform/Heron.Installer/InstallPlan.cs) | what will be installed where, and **what is skipped with the reason** — [R-10](01-requirements.md). Pure: no files, no Revit, no clock |
| [`IRevitEnvironment.cs`](../../../../platform/Heron.Installer/IRevitEnvironment.cs) | the two questions about Windows, as an interface, so a test can answer them |
| [`InstallEngine.cs`](../../../../platform/Heron.Installer/InstallEngine.cs) | plan → **wait for Revit** → deploy each → report per product per release |
| [`WindowsAdapters.cs`](../../../../platform/Heron.Installer/WindowsAdapters.cs) | **the only two places this reaches Windows.** One dot-sources [`tools/HeronRevit.ps1`](../../../../tools/HeronRevit.ps1); the other runs [`tools/deploy-addin.ps1`](../../../../tools/deploy-addin.ps1). **Neither has ever run** |
| [`tests/Heron.Installer.TestHost/`](../../../../tests/Heron.Installer.TestHost/) | 38 checks against a fake Revit and a fake deployer |

**`tools/deploy-addin.ps1` was generalised rather than copied.** It deployed exactly one add-in until
2026-09-21 — every path in it said `Heron.Revit.Addin`. [R-31](01-requirements.md) allows one engine, so
the four facts that differ per product now come from the product list. Three other things were found
missing while doing it and were fixed in the same file, each one a requirement that was already written
down and simply not implemented:

| | |
|---|---|
| [R-37](01-requirements.md) | the **download mark** is cleared on the deployed files. A DLL that arrived through a browser makes Revit refuse the add-in with a message naming nothing useful |
| [R-38](01-requirements.md) | it **replaces**, rather than copying over. The previous install's folder is deleted and verified gone before anything is copied, so a file the old version shipped and the new one dropped cannot go on being loaded |
| [R-38b](01-requirements.md) | a delete that does not finish **stops**, having copied nothing. The backup is taken first, so `-Rollback` always has somewhere to go |

**Nothing is renamed aside and no `.old` folder is ever made** — [R-38c](01-requirements.md), and it is
checked by a test rather than remembered.

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

### Where it actually stands — the four states, kept apart

**Not one of the six lines above has been answered.** Every one of them is about what happens on a
machine with a Revit on it, and none of them can be asked here. What *can* be asked was, and it is a
different question: does the engine **decide** correctly before it acts?

| | |
|---|---|
| **PASS** | `tests/test_installer_engine.py` — **38 checks** on a fake Revit and a fake deployer. A heading installs nothing of its own and names its pieces; a `PROVING` product is never offered; a release the product does not support is skipped **with the reason**; a release on the PC that was not ticked is reported rather than passed over; it **waits** while Revit is open and carries on by itself; a Revit whose release cannot be read blocks everything rather than guessing; reaching the wait ceiling changes **nothing**; one product failing does not stop the others |
| **PASS** | `tests/test_deploy_script.py` — the text checks a Linux machine can make on the generalised deploy script: `-Product` defaults to `heron-bridge` and **that default resolves to the four values that used to be hardcoded**, no product name is left in the logic, the replace is a delete-then-copy with no rename, and all six older guards are still there |
| **PASS** | `tools/check-package.py` — widened on 2026-09-21 from one manifest to **every product's**: each one still carries the line the deploy script rewrites, and the script refuses a rewrite that would match nothing |
| **NOT RUN** | **Both Windows adapters.** `PowerShellRevitEnvironment` and `DeployScriptDeployer` have never executed a line. So has `deploy-addin.ps1` since it was changed — its rollback proof of 2026-09-19 is **STALE for the current file** |
| **NEEDS REAL REVIT** | Every line under *Done when* above, and rows `AA7` to `AA9` in [NEEDS-CHECKING](../../../NEEDS-CHECKING.md) |

**Item 6 — every path through `HeronPaths` — is satisfied by the engine building no Heron path at all.**
It does not reference [`Heron.Core`](../../../../platform/Heron.Core/HeronPaths.cs) and does not need
to: the only paths it makes are to the two scripts it runs, inside the repository. **The two Heron paths
in this stage are both built in PowerShell** — the Addins folder and
`%LOCALAPPDATA%\Heron\install-backup\` — and a `.ps1` cannot call a C# class.
[`platform/README.md`](../../../../platform/README.md) rule 3 says `HeronPaths` is the only thing that
builds a Heron path, so **that rule and `tools/deploy-addin.ps1` disagree, and have since 2026-09-19.**
Recorded, not resolved: it predates this stage, and the fix is a decision about how PowerShell asks for
a path rather than a line to change here.

**Why the waiting is worth its own line.** Revit holds every assembly it has loaded, so a delete fails
while it is open. AJ Tools renames the folder aside and sweeps later; the owner refused that, because
the copies pile up until nobody can tell which one Revit is loading. This waits instead — and it
**never closes Revit**, because an open Revit has a model in it and that model very likely has unsaved
work. There is no `-Force` and no "it looked idle".

### Cannot prove

Anything about the window, and anything about downloading — the files are local at this stage.

**And anything at all about installing.** No file was written, no Revit was looked for, no PowerShell
ran. A green test run here says the engine would make the right decisions; it says nothing whatever
about whether the copy works.

---

## Stage 4 — The window

**Status: PROVEN — 2026-09-21. DONE, except `AB6`.**

> **RUN ON THE OWNER'S PC, 2026-09-21. The window has been seen, and it installed something.** It drew,
> it read, it listed the three releases on this PC, and **`AB3` — the row the whole design rests on —
> passed**: a product added to `platform/heron-products.json` by hand appeared in the window **with no
> rebuild**, the running exe predating the edit by a minute. **R-3 holds: adding a product next year is
> a line in a file.**
>
> **`AB1` FAILED and was fixed the same day.** The window is a fixed 620 wide and cannot be resized, and
> the longest description lost its last word — *"the three buttons that exist"* instead of *"...exist
> today."* — with **no ellipsis**, so the truncated line still read as a finished sentence, and neither
> the tooltip nor dragging the window could recover it. The tick box now carries a wrapping `TextBlock`
> rather than a bare string, which is what every other block in that file already did. Widening the
> window was rejected as a fix: it clips again, just as silently, the first time a longer product is
> added — and that is the one thing R-3 promises will only ever be a line in a file.
>
> **`AB6` is NOT RUN and is blocked rather than failed.** It asks for the tools to be installed without
> the bridge, and the only tools product that exists is the Stage 2 throwaway at `PROVING`, which the
> manifest forbids an installer to offer. The window obeys — the row is greyed and would not tick. **The
> manifest was deliberately not edited to force it.** Rows and evidence:
> [NEEDS-CHECKING](../../../NEEDS-CHECKING.md), Group AB. Screenshots: [`docs/proof/`](../../../proof/).

### What exists now

| | |
|---|---|
| [`platform/Heron.Installer/InstallerScreen.cs`](../../../../platform/Heron.Installer/InstallerScreen.cs) | **everything the window shows, decided with no window.** Which rows, which may be ticked, what a tick installs, what is greyed and what the grey says |
| [`platform/Heron.Installer.App/`](../../../../platform/Heron.Installer.App/) | the WPF window — `HeronInstaller.exe`. It draws what it is handed and **decides nothing** |
| [`tests/test_installer_window.py`](../../../../tests/test_installer_window.py) | the checks a machine with no Windows can make on a window |

**The split is the point.** A window cannot be run here, so a rule written inside one cannot be checked
by anything. Every rule lives in `InstallerScreen`, where the test host runs it against a fake Revit and
a fake disk — **79 checks**, up from 38 — and the window is left with layout. `tests/test_installer_window.py`
is what holds that line: it fails if the window starts asking a product anything, if a product is named
in it, or if an Update or Repair button appears.

**`HeronInstaller.exe`, not `Heron.Installer.exe`.** The library beside it is already called
`Heron.Installer`, and two files a dot apart is a thing somebody eventually double-clicks the wrong one
of.

### What Stage 4 changed underneath itself

**The window needed the Revit `Addins` folder**, to answer whether a product is already installed
([R-2](01-requirements.md)). It built the path in C# and
[`tools/check-structure.py`](../../../../tools/check-structure.py) **refused the edit** — only
`HeronPaths` may resolve a special folder. Referencing `HeronPaths` was then tried and undone:
[`Heron.Core`](../../../../platform/Heron.Core/HeronPaths.cs) follows the Revit release and the
installer is release-independent, so the reference would pin the installer to whichever release happened
to be building.

So the path got **one owner**: `Get-RevitAddinsFolder` in
[`tools/HeronRevit.ps1`](../../../../tools/HeronRevit.ps1). `deploy-addin.ps1` had been spelling it
itself and now asks; the installer asks through `IRevitEnvironment`. **One copy where there were nearly
three** — and the rule it still disagrees with is [Q-58](../../../OPEN-QUESTIONS.md), which is recorded
rather than closed.

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

### Where it actually stands — the four states, kept apart

| | |
|---|---|
| **PASS** | Compiles. `tools/check-compile.py` — **all 13 projects on all 8 releases, 0 warnings**, the window included |
| **PASS** | `tests/test_installer_engine.py` — **79 checks** against a fake Revit and a fake disk. Every row comes from the product list; the tab opens into two ticks; either piece on its own installs; a product that cannot be installed here is greyed **with the reason**; an installed one stays tickable and says Install replaces it; a greyed row installs nothing **even if its tick arrives set** |
| **PASS** | `tests/test_installer_window.py` — the window holds no rules, names no product, has **no Update and no Repair button**, prints the grey reason rather than hiding it in a tooltip, builds the *Close Revit first* line **above** the Install button, and waits off the window's thread |
| **NOT RUN** | **Nothing has been drawn.** And `InstalledProductsOnDisk`, the third Windows adapter, has never executed a line either |
| **NEEDS REAL REVIT** | Every line under *Done when* above, and `AB1` to `AB7` in [NEEDS-CHECKING](../../../NEEDS-CHECKING.md) |

**The most important one is not a screenshot.** *Done when* asks that adding a product to the manifest
makes it appear in the window **with the installer not rebuilt**. That is [R-3](01-requirements.md), it
is the whole reason the design is shaped this way, and it is `AB3`.

### Cannot prove

That it survives a real site laptop — locked-down policies, no internet, odd display scaling. That is
Stage 7.

**And anything at all about how it looks.** No window has been opened. Text on a screen can be too
small, cut off, or wrapped into nonsense at a scaling factor nobody tested, and none of that shows up in
a compile.

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

Stages 3 to 5 build **route 3** — the installer with a window. [S7](00-structure.md) says there are
three front doors and one engine, so this is where the other two are hung on it.

**If this stage ends up re-implementing any install rule, it has failed**, however well it works. A rule
that lives in three places is three rules, and two of them go stale.

### Do

1. **Route 2 — the repo handed to the AI** ([R-29](01-requirements.md)). The user downloads the repo and
   gives it to the AI in-product; the AI installs from the files it was given.
2. **Route 2 downloads nothing** ([R-30](01-requirements.md)). Its files are already in the repo, so it
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

Whether the AI asks the right question. Route 1 installs everything and then asks which panels are
wanted; whether that question is the right one to ask is a matter for a modeller using it, not a test.

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
| 0 | Record the decisions | **DONE** 2026-09-21 | [D-87 to D-95](../../../DECISIONS.md); `grep -c '^## D-'` 88 -> 97; `check-docs` 0 broken links |
| 1 | Product manifest | **DONE** 2026-09-21 | `platform/heron-products.json`; `check-products.py` PASSES on it and **FAILS exit 1** on a duplicated `addInId`; `tests/test_products_manifest.py` 36 checks, 16 faults each refused by name |
| 2 | Second tab | **BUILT AND UNPROVEN** 2026-09-21 | Compiles on all 8 releases, 0 warnings; `tests/test_ribbon_tab_sharing.py` 26 checks pass. **NEEDS REAL REVIT** - no ribbon has been seen, no screenshot exists |
| 3 | Installer core | **BUILT AND UNPROVEN** 2026-09-21 | `platform/Heron.Installer/` builds; `tests/test_installer_engine.py` 38 checks pass against a fake Revit; `deploy-addin.ps1` generalised to every product and `tests/test_deploy_script.py` passes. **NOT RUN** - both Windows adapters and the deploy script itself have never executed |
| 4 | The window | **BUILT AND UNPROVEN** 2026-09-21 | `platform/Heron.Installer.App/` compiles on all 8 releases, 0 warnings; `tests/test_installer_engine.py` 79 checks and `tests/test_installer_window.py` pass. **NOT RUN** - no window has been drawn |
| 5 | GitHub download | NOT STARTED | — |
| 6 | Routes 1 and 2 | NOT STARTED | — |
| 7 | Uninstall / update / rollback | NOT STARTED | — |
| 8 | Sign and ship | NOT STARTED | — |
| 9 | The Settings panel | NOT STARTED | — |
