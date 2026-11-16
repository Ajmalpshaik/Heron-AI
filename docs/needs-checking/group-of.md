# Needs checking — Group OF

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OF - a proof records which Heron add-in and which Revit build it was taken on, and says so when either moves: `built_on:` (2026-11-16)

From idea #10 of the research note on [Ajmalpshaik/Heron-AI#877](https://github.com/Ajmalpshaik/Heron-AI/pull/877):
the fingerprint says which fragment C# was proved, and nothing said which add-in ran it or which Revit build
answered. The idea only was taken, re-authored here ([D-25](../DECISIONS.md)).

| What | Where | What it does |
|---|---|---|
| The add-in's address-book entry | [`BridgeIdentity.cs`](../../revit/Heron.Bridge/BridgeIdentity.cs), [`HeronApplication.cs`](../../revit/Heron.Revit.Addin/HeronApplication.cs) | Writes `revitBuild` (Revit's `VersionBuild`) and `addinHash` (first 16 hex of the SHA-256 of `Heron.Revit.Addin.dll`) beside `revitVersion` and `addinVersion`, and names both in the `Heron loaded.` log line. Optional fields: the protocol version stands |
| The run records | [`heron_bridge_client.py`](../../mcp/client/heron_bridge_client.py) `validate` and `try-once`, [`prove-tracking.py`](../../tools/prove-tracking.py) | Write `built_on:` - the four fields - from that entry |
| The proof | [`heron_validate.py`](../../brain/heron_validate.py), [`heron_fragment.py`](../../brain/heron_fragment.py) `build_state` | `draft` copies `built_on` into the proof and says on its `build:` line how it compares with the card; `accept` writes it. A proof without it is NOT RECORDED - never stale, never current, never re-stamped |
| The answer | [`heron_mcp_server.py`](../../mcp/server/heron_mcp_server.py) `_build_line` | One line under `revit_read`, `revit_show` and `revit_change` when the proof is stale for the build in front. A flag, not a refusal - [Q-92](../open-questions/tier-3.md) |

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
