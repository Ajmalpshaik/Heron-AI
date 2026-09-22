# Prompts for the rest of the plugin-extension work

**Written 2026-09-21**, the day the installer first installed three Revit releases in one press.

> ## A, B and C are DONE — 2026-09-22, merged as [#253](https://github.com/Ajmalpshaik/Heron-AI/pull/253)
>
> All three shipped, and the owner then asked for a fourth thing that none of them had: **one download
> carrying the plugin AND the brain, working offline afterwards, with the internet used only to ask
> whether a newer version exists.** That is `R-51` to `R-56`, and it is built too.
>
> **PROMPT D below is what is left**, and it is not building — it is **proving**. Nothing in any of
> this has ever run against a real Revit. Every claim so far is *"compiles, and the suites pass on
> Linux"*, which the Constitution does not accept as proof.
>
> **Run D on the owner's Windows PC when he has time.** Nothing else is waiting on it, and nothing
> else should be built on top of it until it has been run.

Three prompts, each **self-contained** — paste one into a fresh session that knows nothing about this
work. They are written to be handed out one at a time, not all at once: see **Order and collisions**
below before starting two.

**Every prompt says WHERE to run it.** The Claude cloud is **Linux**; Ajmal's PC is **Windows with
Revit 2020, 2024 and 2027**. Everything except the parts that touch Revit builds and tests in the
cloud, and CI proves it — the C# compiles for all eight releases on `ubuntu-latest` on every push.

---

## Order and collisions

**A, then B and C.** All three touch `platform/Heron.Installer/` and the installer window, so two
running at once will collide in the same files.

| | owns | can start |
|---|---|---|
| **A** — safety + honest window | `tools/deploy-addin.ps1`, `InstallEngine.cs`, `InstallerWindow.cs` | now |
| **B** — Stage 7, uninstall | the installer engine's remove path, the window's apply | after A merges |
| **C** — Stage 5, download package | `.github/workflows/`, packaging, the engine's fetch path | after A merges |

**B and C can run together after A**, if B is told it owns the *remove* path and C the *fetch* path,
and neither reformats shared files.

---

## The rules every one of these inherits

Copy this block into each prompt. It is the discipline that produced the results in
`docs/NEEDS-CHECKING.md`, and it is not optional.

> - **Ajmal is a BIM modeller, not a coder.** Explain in plain language, no developer jargon.
> - **Close Revit before every deploy command.** Every one of them refuses while it is open — that
>   refusal is itself a test.
> - **Take the screenshots the rows ask for** and save them in the repo under `docs/proof/` with the
>   row id in the filename.
> - **Never edit a test or a gate to make something pass.** A red result is an answer, not a problem
>   to remove.
> - **Never claim something passed without saying what you actually ran and what you saw.** Keep
>   **PASS / FAIL / NOT RUN / NEEDS REAL REVIT** separate and never merge them.
> - **If something fails, diagnose it, tell Ajmal in plain language what broke and why, propose the
>   smallest fix, and wait for his yes before changing code.**
> - **Stop and tell him the moment one fails.**
> - Write every result back into `docs/NEEDS-CHECKING.md` (PASS/FAIL, date, what was seen), update the
>   stage in `docs/work-notes/plans/plugin-extension/02-implementation.md`, and **only then may the
>   word PROVEN be used**.
> - Run the ten gates `.github/workflows/gates.yml` decides on, commit on a new branch, open a PR.
> - Ask before using his screen — he uses that PC.

## The four traps this work has already fallen into

Copy this block too. Each one cost real time.

> 1. **The installer window deploys `Release`. The dev flow builds `Debug`.**
>    `DeployScriptDeployer` (`platform/Heron.Installer/WindowsAdapters.cs`) hardcodes `Release` and
>    always has. Build `-c Release` when the question is "does the installer work" — otherwise nine
>    correct builds sit in a folder the window never opens, and it reports *"was not installed …
>    dotnet build … -c Release"*, which reads like your change failed. **This cost a full round-trip
>    on 2026-09-21 after row `AB5` had already written it down.**
> 2. **Each Revit release has its OWN build folder** since PR #242 —
>    `bin\x64\<Configuration>\<RevitVersion>\`, set by `OutputPath` in `Directory.Build.props`. Build
>    every release you need; they no longer overwrite each other. Four tests still pass
>    `-p:OutputPath=` by hand to dodge the old collision — harmless leftovers, not a missing fix.
> 3. **Verify at the binary level, never by the success line.** Read `TargetFrameworkAttribute` out of
>    the deployed DLL — 2020 must be `net472`, 2024 `net48`, 2027 `.NETCoreApp v10.0`. The window
>    saying "installed" is not the same as the right file being there.
> 4. **`RevitAccelerator.exe` in the task list is NOT Revit.** It is an Autodesk background service and
>    does not block a deploy. Look for `Revit.exe`.

---

# PROMPT A — Close the wrong-year hole, and stop the window offering what it cannot install

**Where: the Claude cloud (Linux) for the code. Ajmal's Windows PC for the proof.**

**Linux needs this first, or every brain call dies:**

```bash
export HERON_KNOWLEDGE="$HOME/heron-kb" && mkdir -p "$HERON_KNOWLEDGE"
```

You are a senior Revit add-in engineer working on Heron AI. Read `docs/NEEDS-CHECKING.md` — the last
section, dated 2026-09-21, titled *"THE INSTALLER CAN NOW INSTALL EVERY REVIT IN ONE PRESS"*. It
records two things found but deliberately not fixed. Fix them.

**[paste the rules block and the four traps block here]**

## What is already true, so you do not redo it

PR #242 gave every Revit release its own build folder. On 2026-09-21 Ajmal ticked Revit 2020, 2024 and
2027, pressed Install **once**, and all three installed with the correct runtime — verified by reading
the deployed assemblies. **That part works and is proven. Do not touch it.**

## The two defects

**1. The deploy script substitutes a different release's build.**

In `tools/deploy-addin.ps1`, when no build exists for the release it was asked about, it falls back to
*any* build it can find. The comment says the runtime guard makes that safe. **It does not.** The guard
reads the **runtime**, not the **year** — Revit 2021, 2022, 2023 and 2024 all build `net48`, so a stale
`net48` leftover deploys into any of them and reports success. **This is the same class of defect row
`A12` recorded.** On 2026-09-21 it did not bite only because the leftover happened to be .NET 10.

*Proposed smallest fix, already agreed in principle:* **refuse when no build exists for the release
asked for**, rather than substituting. Say which release, and print the exact `dotnet build` command.
The fallback buys nothing now that every release has its own folder.

**2. The window offers a Revit version it has no build for.**

A release with no build on disk can be ticked, and you only discover it **after** pressing Install.
Ajmal hit this five times in one evening. The window already knows how to grey a row and print the
reason underneath — row `AB2` proves it does this for unfinished products, and **R-10** requires the
reason to be visible, not only in a tooltip. Do the same for a release with no build.

**Be careful here:** the window must not *build* anything, and must not become slow. Decide what it can
cheaply know. If telling before the press turns out to be impossible without a build, **say so and stop
— do not guess**.

## Also correct, while you are in these files

Two comments point at `Directory.Build.targets`, which does not exist — the fix landed in
`Directory.Build.props` after the `.targets` attempt was measured as a silent no-op. In
`tools/deploy-addin.ps1` (above the build discovery) and `tools/check-api-surface.py`. The same
`deploy-addin.ps1` block also still claims *"THE BUILD OUTPUT IS SHARED BETWEEN ALL EIGHT RELEASES"*,
which stopped being true in the commit that rewrote the lines directly above it.

## Prove it, and prove the test can FAIL

`.claude/skills/heron-ship/SKILL.md` §2a: **a fix is not proved until its test has been seen to fail.**
Run the new checks against the code as it stood. If none go red, the check is not testing the fix. A
check that raises instead of failing loses every other failure in its section — ask with `getattr`
before you call.

## Done when

- A deploy asked for a release with no build **refuses by name** and prints the command to fix it.
- The window **greys a release it cannot install** and says why underneath, before Install is pressed.
- Ajmal sees it on his PC: tick a Revit with no build, and it is greyed with a reason.
- The stale comments are gone.
- All ten gates pass; results written into `docs/NEEDS-CHECKING.md`; screenshots in `docs/proof/`.

---

# PROMPT B — Stage 7: uninstall, and a rollback that has actually been performed

**Where: the Claude cloud (Linux) for the code. Ajmal's Windows PC for every proof — this stage is
almost entirely about what happens on a real disk.**

```bash
export HERON_KNOWLEDGE="$HOME/heron-kb" && mkdir -p "$HERON_KNOWLEDGE"
```

You are a senior Revit add-in engineer working on Heron AI. Build **Stage 7** as
`docs/work-notes/plans/plugin-extension/02-implementation.md` specifies it. Read that stage in full
before writing anything — this prompt does not replace it.

**[paste the rules block and the four traps block here]**

## Why this matters now

**There is no way to remove Heron from the installer window today.** Ajmal asked for one directly on
2026-09-21 — *"same like installer do we need to make a uninstaller ??"*. To remove it now he has to
ask a session or delete folders by hand. Stage 7 is the way back, and the plan says it comes **before
real users arrive**.

## What the plan asks for

1. **Uninstall**: untick a product, apply, its files go (**R-21**).
2. **Uninstall removes the product and nothing else** — `%APPDATA%\Heron` survives (**R-22**).
3. **Update**: a newer version replaces the old in place, settings kept (**R-23**).
4. **Rollback, actually performed** (**R-24**) — `docs/07 §7` rule 5 demands it is *tested*, not merely
   implemented, and `brain/heron_update.py` refuses a release with no recorded `rollback_tested`.
5. An audit line per operation (**R-27**) through `platform/Heron.Core/HeronAudit.cs`.

## What is already proven, so you build on it rather than redoing it

- **Rollback works and has been seen to work** — row `AA8`, proved with planted marker files, backup
  path changed and verified. Read that row before designing the uninstall path.
- **The window reads state from disk every time** — row `AB4`. Delete a product's folder by hand and
  the row changes on reopen. Uninstall must keep that true.
- **A product needs BOTH its folder and its `.addin` manifest** to count as installed. Removing one and
  leaving the other is the failure mode `AB4` was written to catch.
- **The engine waits for Revit rather than failing** — row `AB5`. Uninstall must do the same: Revit
  holds the assembly open, so it cannot be deleted while Revit runs.

## Done when

- Install, uninstall, reinstall, update, roll back: **all five run on Ajmal's PC**, and the ribbon state
  after each is recorded with a screenshot.
- After uninstalling **every** product, `%APPDATA%\Heron` still holds the config and the audit log.
- `rollback_tested` is recorded, so `heron_update.py` stops refusing the release.
- Ajmal can remove Heron from all three of his Revits **from the window**, with no help.

## What you cannot prove, and must say so

An upgrade from a version that does not exist yet. The plan says this is **not a one-time stage** —
re-test at every release.

---

# PROMPT C — Stage 5: make Heron installable by someone who is not Ajmal

**Where: the Claude cloud (Linux) for everything — including the GitHub Actions workflow, which is
where the package should be built. Ajmal's Windows PC for the one proof that matters: a real download
on a machine with nothing installed.**

```bash
export HERON_KNOWLEDGE="$HOME/heron-kb" && mkdir -p "$HERON_KNOWLEDGE"
```

You are a senior Revit add-in engineer working on Heron AI. Build **Stage 5** as
`docs/work-notes/plans/plugin-extension/02-implementation.md` specifies it, and read
`docs/07-installation-and-update.md` §1a first.

**[paste the rules block and the four traps block here]**

## Start by understanding why this is the biggest gap

**Today, nobody but Ajmal can install Heron.** Row `AA9` **FAILED**: a downloaded Heron will not
install, because Windows refuses to run a script that came from the internet under the default
`RemoteSigned` execution policy, and `Unblock-File` cannot reach itself. Decision **D-96** answers this
— *a downloaded Heron is installed by the installer, and only by the installer* — and makes the
installer's `-ExecutionPolicy Bypass` **load-bearing**.

**But there is no packaged installer to download.** So D-96 names a door that does not exist yet, and
`AA9` stays failed until this stage ships. **Read D-96 in `docs/DECISIONS.md` and the AA9 section of
`docs/NEEDS-CHECKING.md` before designing anything.**

Note also: **signing was considered and refused** — yearly cost, and it expires silently. Do not
re-propose it as the fix here. Stage 8 is where that argument lives.

## What the plan asks for

1. The installer reads the manifest **from a named release**, not from a branch.
2. Download each ticked product's asset.
3. **Verify before use** (**R-12**) — a published checksum at minimum. A file that fails verification
   **never reaches the Addins folder**.
4. Failure messages that name the cause (**R-14**): no internet · blocked by network · release not
   found · file corrupt. **"Error" is not one of the allowed words.**
5. **Nothing downloaded is executed in order to decide what to install** (**R-13**).

## The thing Ajmal actually asked for, in his words

A download that gives **a clean two-file zip** — the installer and what it needs — not a folder a person
has to dig through. He said on 2026-09-21, looking at the build output path, that it was *"not
somewhere anybody should have to dig to find an installer"*. Build the package to be opened by a
modeller, not by a developer.

**It must carry all eight Revit releases**, 2020 through 2027 — not the three Ajmal happens to have.
`tools/check-compile.py` already builds every one of them in CI, so the builds exist; the packaging is
what is missing. A user with Revit 2022 must get a working 2022 build.

## Done when

- A **clean PC with no Heron installed** gets `Heron` from a real GitHub release and the tab appears.
- A **deliberately corrupted asset is refused**, the Addins folder is untouched, and the message says
  the file did not verify.
- **With networking off**, the message says it cannot reach GitHub — not "error".
- **`AA9` can be re-run and pass**, or its failure is replaced by a recorded reason it no longer
  applies. Do not quietly drop the row.

## What you cannot prove, and must say so

Whether a particular contractor's firewall allows it. That is found on site, and it is why `Q-PE-5`
stays open.

---

# PROMPT D — Prove it on a real Revit, or find out it does not work

**Where:** Ajmal's **Windows PC**, with Revit 2020, 2024 and 2027. **Not** the cloud — every row here
needs a real Revit, which is the entire point of them.

**When:** any time. Nothing is blocked waiting for it, and nothing should be built on top of the
installer until it has been run.

## Why this prompt exists

The installer is merged and every gate is green. **That proves the code is self-consistent and nothing
else.** `D-30` is explicit: a proof is a recorded run against a **named real model**, and there has not
been one. The Heron tab has never appeared on a machine because of this code. The checksum refusal has
never refused a real file.

**The two rows below are worth more than the other nine put together**, and if either fails the rest of
the run is pointless:

| | |
|---|---|
| **`AE3`** | Revit closed, install, open Revit 2024. **If no Heron tab appears, nothing else matters.** |
| **`AF4`** | Change one character in `checksums.txt` and install. **If it installs anyway, the whole file-verification story is wrong** — and that story is what makes route 2 safe to hand to somebody on a USB stick. |

## Paste this into a fresh session on the Windows PC

```
ROLE
Act as a senior Revit add-in engineer, QA checker, and evidence auditor working
on the Heron AI repository. I am not a coder - I am a BIM modeller. Explain
everything in plain language, and never assume I will read code to understand
what happened.

CONTEXT
Heron's installer was built and merged, but NOTHING has ever run against a real
Revit. Every claim so far is "compiles and passes tests on Linux", which is not
proof. This machine is my Windows PC with Revit 2020, 2024 and 2027 installed.

The tests to run are already written down as rows in docs/NEEDS-CHECKING.md:
AE1 to AE6 and AF1 to AF5. Read those rows first - they say exactly what to do
and exactly what counts as proof. Do not invent your own test steps.

FIRST, BUILD THE FILES
Run:  python tools\build-release-assets.py
This makes a `dist` folder. The AF rows need it.

GOAL
Run AE1 to AE6 and AF1 to AF5 on this real machine, record honestly what
happened, and write the results back into docs/NEEDS-CHECKING.md.

MAIN TASK
1. Read the AE and AF rows in docs/NEEDS-CHECKING.md before running anything.
2. CLOSE REVIT before every install command. If a command refuses because Revit
   is open, that refusal is itself a test result - record it, do not work
   around it.
3. Run the rows in this order: AE1, AE3, AE2, AE4, AE5, AE6, AF1, AF2, AF3,
   AF4, AF5.
4. After each row, write down the exact command, the exact output, and the exit
   code. Not a summary - the real text.
5. Take a screenshot where the row asks for one. Save into docs\proof\ with the
   row id in the filename, for example AE3-heron-tab.png.
6. Write each result into docs/NEEDS-CHECKING.md with the row id, the date, and
   what was actually seen.

THE TWO ROWS THAT MATTER MOST - STOP IF EITHER FAILS
- AE3: Revit closed, install, then open Revit 2024. The Heron tab must appear.
  If there is no tab, STOP. Do not run the rest. Report what happened.
- AF4: change one character inside checksums.txt, then install from that folder.
  It MUST refuse. If it installs anyway, STOP IMMEDIATELY and report it - that
  would mean the whole file-verification story is wrong.

FOUR STATES, NEVER MIXED
Record every row as exactly one of:
- PASS          - it was run and it did what the row says
- FAIL          - it was run and it did not
- NOT RUN       - and say why it could not be run
- NEEDS RELEASE - it needs a published GitHub release, which does not exist yet

"It compiled" is not PASS. "No error appeared" is not PASS. Only the thing the
row asks for is PASS.

SAFETY RULES
- Do not guess. Do not assume missing information.
- NEVER edit a test, a gate, or a row to make something pass. A red result is
  an answer, not a problem to remove.
- Do not change any source code to fix a failure without telling me first and
  waiting for my yes.
- Do not touch %APPDATA%\Heron - that is my own data.
- If something is unclear, mark it NEEDS_REVIEW rather than deciding for me.

VERIFICATION RULES
- Never claim a row passed without showing the command you ran and the output
  you saw.
- Check the Revit tab with your own eyes or a screenshot, not from the
  installer's success message.
- If a row cannot be run, say NOT RUN and why. Do not skip it silently.
- Do not write the word PROVEN anywhere until I have confirmed the result.

WHAT NOT TO CHANGE
- Do not change the test rows themselves.
- Do not change installer source code without my permission.
- Do not publish a GitHub release. That is my decision.
- Do not delete anything from docs\proof\.

OUTPUT FORMAT
Give me a simple table:

| Row | PASS / FAIL / NOT RUN | What I actually saw |

Then, for every FAIL, a short plain-language explanation of what went wrong and
the smallest fix you would suggest - but do not apply it yet.

FINAL REPORT REQUIRED
1. What you understood
2. What you ran
3. Which rows passed
4. Which rows failed, and what you saw
5. Which rows could not be run, and why
6. What you wrote into docs/NEEDS-CHECKING.md
7. Screenshots saved, by filename
8. Anything that needs my decision
9. What you did NOT change
10. Next recommended step

IMPORTANT RULES
- Close Revit before every deploy command.
- Report failures the moment they happen. Do not save them for the end.
- Tell me in plain BIM-modeller language, not developer language.
- One honest FAIL is worth more to me than ten passes you were not sure about.
```

## What this prompt deliberately does NOT do

- **It does not publish a release.** That is the owner's decision, and `AC1` to `AC5` and `AF6` wait on
  it. Until one exists those rows are **NOT RUN**, which is not a failure and not a pass.
- **It does not fix anything.** A failure is reported and diagnosed; the fix is a separate decision, so
  that a bad fix cannot quietly become the reason a row passed.
- **It does not touch Stage 8 or Stage 9.** Signing is parked at the owner's request, and `R-52`'s
  *"ask where to keep it"* is the window's job.

## Done when

- Every row from `AE1` to `AE6` and `AF1` to `AF5` carries **PASS**, **FAIL** or **NOT RUN with a
  reason** in [`docs/NEEDS-CHECKING.md`](../../../NEEDS-CHECKING.md) — **none left blank**.
- The screenshots the rows ask for are in `docs/proof/` with the row id in each filename.
- **The word PROVEN appears only where the owner has confirmed it**, and nowhere else.
