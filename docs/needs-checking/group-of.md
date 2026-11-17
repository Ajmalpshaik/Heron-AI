# Needs checking — Group OF

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OF - a proof records which Heron add-in and which Revit build it was taken on, and says so when either moves: `built_on:` (2026-11-16)

**Tracked as GitHub issue [#973](https://github.com/Ajmalpshaik/Heron-AI/issues/973) since 2026-11-16** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

From idea #10 of the research note on [Ajmalpshaik/Heron-AI#877](https://github.com/Ajmalpshaik/Heron-AI/pull/877):
the fingerprint says which fragment C# was proved, and nothing said which add-in ran it or which Revit build
answered. The idea only was taken, re-authored here ([D-25](../DECISIONS.md)).

| What | Where | What it does |
|---|---|---|
| The add-in's address-book entry | [`BridgeIdentity.cs`](../../revit/Heron.Bridge/BridgeIdentity.cs), [`HeronApplication.cs`](../../revit/Heron.Revit.Addin/HeronApplication.cs) | Writes `revitBuild` (Revit's `VersionBuild`) and `addinHash` (first 16 hex of the SHA-256 of `Heron.Revit.Addin.dll`) beside `revitVersion` and `addinVersion`, and names both in the `Heron loaded.` log line. Optional fields: the protocol version stands |
| The run records | [`heron_bridge_client.py`](../../mcp/client/heron_bridge_client.py) `validate` and `try-once`, [`prove-tracking.py`](../../tools/prove-tracking.py) | Write `built_on:` - the four fields - from that entry |
| The proof | [`heron_validate.py`](../../brain/heron_validate.py), [`heron_fragment.py`](../../brain/heron_fragment.py) `build_state` | `draft` copies `built_on` into the proof and says on its `build:` line how it compares with the card; `accept` writes it. A proof without it is NOT RECORDED - never stale, never current, never re-stamped |
| The answer | [`heron_brain.py`](../../mcp/server/heron_brain.py) `proof_build_line`, called by [`heron_mcp_server.py`](../../mcp/server/heron_mcp_server.py) `_build_line` | One line under `revit_read`, `revit_show` and `revit_change` when the proof is stale for the build in front. A flag, never a refusal - [D-174](../decisions/D-174.md) |

**What is done and what is not.** The C# is DRAFT. It compiles for every project on Revit 2020 to 2027
(`tools/check-compile.py`, 2026-11-16, a Linux container) and every Revit member it calls exists in each
(`tools/check-api-surface.py`). `tests/test_proof_build.py` holds the brain, the client and the answer line
without Revit. **Nothing has run in Revit**, so no real address-book entry or run record has been seen with
the two new fields, and no proof records a build yet.

**The model** - any scratch project, never a working model. Every run below is a READ or rolled back.

| # | Run | Look for |
|---|---|---|
| **OF1** | *The add-in says its build.* Rebuild and redeploy the add-in for Revit 2024, start Revit 2024, press Heron to connect | The day's add-in log has `Heron loaded. Revit 2024 build <b>, add-in <v> file <h>` - `<b>` is the build Help > About names, `<h>` is the first 16 characters, lower case, of `Get-FileHash -Algorithm SHA256` on the deployed `Heron.Revit.Addin.dll`. Neither says `(not reported)` or `(not read)` |
| **OF2** | *The run record carries it.* `python mcp/client/heron_bridge_client.py validate --session <pid> <a PROVEN READ fragment>` on the scratch model | `brain/proof-drafts/runs/<fragment>.json` has `built_on` with all four fields, the same values as OF1 |
| **OF3** | *The draft says how it compares.* `python brain/heron_validate.py draft <fragment> --from <that record>` | Its `build:` line says the proof on the card records no build for Revit 2024 - every proof signed before this is NOT RECORDED. Nothing is written to the card |
| **OF4** | *Another release is its own build.* OF1 and OF2 on Revit 2025 | A different `addin_hash` from 2024's - 2025 loads a different DLL - and the 2025 build string |
| **OF5** | *A new add-in build is said.* On a fragment re-proved with OF2's record and signed (`accept`), change anything in the add-in, rebuild, redeploy, restart Revit, ask the same capability through `revit_read` | One line under the answer: proved with Heron add-in ... - *the Heron add-in's file changed since*. The answer itself is unchanged, and nothing is refused |

### Run 2026-11-16, night, Revit 2024 (session 8292) - OF1 to OF3 run with the owner away; OF4 and OF5 not

Revit 2024.3 started at 19:47 with the add-in deployed at 19:35 from main f86a5843; nothing under `revit/`
changed on main since, to 90cdea63, so no rebuild was owed. Rows run from the command line, `HERON_CLIENT_ID`
set, one Revit.

| Row | Result |
|---|---|
| OF1 | **NEEDS REVIEW - log and hash agree; Help > About not read.** `addin-20261116.log`: *"2026-11-16 16:47:48Z  Heron loaded. Revit 2024 build 24.3.60.12, add-in 0.2.0.0 file 80819edd3983a0d5, pid 8292."* `Get-FileHash -Algorithm SHA256` of the deployed `Heron.Revit.Addin.dll` (19:35:03): first 16 `80819edd3983a0d5` - equal. The day's two earlier lines, before the deploy, read *"Heron loaded. Revit 2024, add-in 0.2.0.0, pid ..."* with no build and no file. Revit.exe reads FileVersion 24.3.60.12, ProductVersion 20260709_1515(x64); **which of the two About calls the build is for the owner's eyes** - nobody was at the keyboard |
| OF2 | **PASS** - `validate --session 8292 --in "Heron OC Tags" filter-elements-by-category` (PROVEN READ) on `%TEMP%\heron-work\oc\Heron OC Tags.rvt` in place, the proof's own values (`categories=Walls`, `cutByView=true`, `inViewOnly=Section: OC Section`; negative `categories=Doors`): positive ok, negative ok. The run record's `built_on`: `revit_release 2024`, `revit_build 24.3.60.12`, `addin_version 0.2.0.0`, `addin_hash 80819edd3983a0d5` - all four, the same values as OF1 |
| OF3 | **PASS** - `heron_validate.py draft filter-elements-by-category --from <OF2's record>`: *"build: taken on add-in 0.2.0.0 (file 80819edd3983a0d5), Revit 2024 build 24.3.60.12. The proof on the card records no build for Revit 2024"*. `git diff` on the card: nothing written. The draft is NOT signed - OF5's signature is the owner's |
| OF4 | **NOT RUN - Revit 2025 is not installed** on this PC (2020, 2024, 2026 and 2027 are). The owner's choice: run it on 2027 or 2026 as "not 2025 as written", or leave it |
| OF5 | **NOT RUN - needs the owner:** his signature on OF3's draft (it replaces his 2026-11-16 proof of `filter-elements-by-category`), a throwaway add-in change deployed for Revit 2024 only, and a Revit restart |
