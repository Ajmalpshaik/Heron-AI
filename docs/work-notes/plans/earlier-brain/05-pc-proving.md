# 05 — Back on the PC: what needs a real Revit

> **Type:** Operational work note. **Not specification.** Part of [the earlier-brain plan](README.md).
> **Status:** Waiting for the cloud packages to merge. **Owner:** Ajmal PS.

---

The cloud builds, compiles for all eight releases and runs every test. **It cannot prove that anything does
the right thing in Revit** — that needs a real, named model, a positive case **and** a negative case, and a
fingerprint of the code that was proved ([D-30](../../../DECISIONS.md)). This page is that half. It can
start package by package, as each one merges.

## 1. Before any proof — every time something has merged

1. **Pull `main`** into the checkout Revit's Heron runs from.
2. **Anything under `revit/` changed?** Build and deploy it on **every** installed release, not only the
   one open — `tools/deploy-addin.ps1` guards the .NET trap where a newer build lands in an older Revit's
   folder. Then restart Revit.
3. **Anything under `mcp/` or `brain/` changed?** Restart the Claude app. The ribbon button reloads neither
   half — two restarts, not one.
4. Set `HERON_CLIENT_ID=ajmal-pc` on every command-line call, as `.mcp.json` does.
5. **Say which model** before every proof, and if the model lacks what the proof needs, **build it with a
   fragment** rather than waiting for a model that has it.

## 2. The owner's data goes into his own stores — once

| From [03](03-owner-data.md) | Into | Needs |
|---|---|---|
| §A — the 52 site words | the Keyword Agent's store, in his scope, recorded as *Ajmal PS* with the date | C5 merged |
| §C — the grayout standard | company scope, as a standard `heron_standards` can quote | C5 merged |
| §D — the practice values | company scope, as a standard | C5 merged |
| His **licensed NFPA edition** | company scope — **only if he has it**; the sprinkler method refuses to guess limits | before C10's proofs |

## 3. The proofs, package by package

| Package | Prove this | On |
|---|---|---|
| PR #299 | With Changes **ON**, ask a chat to sync with central. **It must be refused, and nothing sent.** Then an ordinary change still works | any model — the refusal happens before Revit is called |
| C1 | A new session on Windows prints the status line; a new session refuses an edit that would put Revit code outside `revit/` | no model needed |
| C2 | With Changes **OFF**, a read question answered through the new read-only door; the rules visible in a fresh chat; if D1 was *preview*, a preview and then an apply, with the count re-checked | a named model |
| C3 | A fragment that raises a Revit **warning** — dismissed and counted; one that raises an **error** — rolled back, **nothing deleted**; an element that has changed in central is skipped with the reason | a **detached copy** first ([Golden Rule 18](../../../14-golden-rules.md)); a workshared central for the last one |
| C4 | Each row whose **code** changed: `connect-air-terminals` reporting a terminal that moved, and reporting nothing when none did; the sizing fragments' open-connector count before and after; every other re-proof C4 listed | named models, built for the case |
| C4, and the owner's own usage ([01 §5](01-findings.md)) | Prove the three jobs he used most that are still DRAFT: `FIND_CLASHES`, `CONNECT_AIR_TERMINALS`, `CENTER_ROOM_TAGS` | named models |
| C5 | A recorded word looked up, and an unrecorded one asked about; grayout run as **one** undo on a view, reporting what stuck | a named model |
| C7 | Its first real use: one missing capability from C9 written, then proved | a named model |
| C8 | A ceiling check that sees an element in a **linked** model and says how many links it read; the central-status column; a placeholder sheet created | a model with a linked architectural model; a workshared central |
| C9 | Terminal layout from a space's airflow, supply and return alternating; the return-airflow mode; duct routing from an FCU to its terminals with the end cap and the 200 mm reducer — each method end to end | named models |
| C10 | Tags placed on the side that reads, then any overlaps resolved; a sprinkler layout checked against his loaded edition | named models; his NFPA edition in company scope |
| C11 | A family created and resized with several values read back | a new family |

## 4. Where a proof is recorded

Follow [`fragment-proving`](../../../../.claude/skills/fragment-proving/SKILL.md): a proof draft per
fragment, `tools/batch-prove.py` for a batch, and the registers' own layout for anything still owed. A
result that cannot be proved yet stays open — **"it ran" is not "it did the right thing"**.
