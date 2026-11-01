# Needs checking — Group JV

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group JV - a circuit that already exists, edited: `EDIT_ELECTRICAL_CIRCUIT` (2026-11-01)

Asked for in the modeller's words: *"add these lights to circuit 3"*, *"take this socket off its
circuit"*, *"move these circuits to panel DB-2"*, *"disconnect this circuit from its panel"*. Nothing in
Heron edited a circuit that exists - `CREATE_ELECTRICAL_CIRCUIT` makes a new one and can pick a panel
only for the circuit it has just made, and `brain/heron_retrieve.py` on each sentence offered only
readers, the create, `CREATE_WIRES` and `DISCONNECT_CONNECTORS`.

| What | Where | What it does |
|---|---|---|
| `EDIT_ELECTRICAL_CIRCUIT` (MODIFY) | [`brain/fragments/edit-electrical-circuit`](../../brain/fragments/edit-electrical-circuit/fragment.yaml) | the four buttons of the Electrical Circuits tab on one existing circuit - Add to Circuit, Remove from Circuit, Select Panel, Disconnect Panel - the circuit named by number (and panel), or selected, or the one a selected device is on; every change read back |

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). `AddToCircuit`,
`RemoveFromCircuit`, `SelectPanel` and `DisconnectPanel` were read off the shipped assemblies of all
eight with `tools/api-surface --members`. **Nothing here has run in Revit.** The card is DRAFT.

**Not known from the assemblies, and recorded here rather than guessed** (JV6 to JV10 measure them):
what Revit does to a circuit whose last device is taken off - the fragment refuses that whatever the
answer (JV9); whether `AddToCircuit` takes a device already on another circuit of the same kind, and
whether it then leaves the first (JV8); what circuit number `SelectPanel` gives on the new panel (JV4).

**The model - MEP Night Test in Revit 2020**, the model `REPORT_PANEL_CIRCUITS` was proved on
(2026-10-21: 19 panels, 155 circuits). Every run rolled back. The job file is
[`tools/jobs/edit-electrical-circuit-jv-2026-11-01.yaml`](../../tools/jobs/edit-electrical-circuit-jv-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing; both of its jobs write
`brain/proof-drafts/runs/edit-electrical-circuit.json`, so read the first before running the second).
Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are
connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **JV0** | *Open.* MEP Night Test open and in front in Revit 2020, **nothing selected**, Changes on at the ribbon | The title bar reads *MEP Night Test* |
| **JV1** | *Read the arrangement.* `report-panel-circuits` with `panelName=PP-1B`, then with nothing | Circuit 1 on PP-1B and its device count, and which other panels have a circuit 1. **If they differ from the job file's values, change the job file** - never the card |
| **JV2** | *Disconnect, the D-30 positive.* The job file's first job | `changed` 1; `report` *taken off PP-1B. IT IS ON NO PANEL NOW* |
| **JV3** | *Its negative - THE FIRST CASE TO RUN.* `circuitNumber=1`, no panel | `changed` 0, *NOTHING WAS CHANGED*, every panel with a circuit 1 named; none disconnected |
| **JV4** | *Select Panel, the positive.* The job file's second job, PP-1B to PP-1A | `changed` 1; `report` *moved from PP-1B to PP-1A, where it is circuit N* - **record N** |
| **JV5** | *Its negative.* `panel=PP-1B`, the panel it is on | `changed` 0, *already on PP-1B*; its circuit number unchanged |
| **JV6** | *Remove from Circuit.* By hand: select two of circuit 1's devices on PP-1B in a plan, `validate edit-electrical-circuit --write --set edit=remove` with no circuit named | `changed` 2; `report` *9 device(s) before, 7 after* (or the counts JV1 read) |
| **JV7** | *Add to Circuit.* Each run rolls back, so JV6's two devices are back on the circuit; select instead two devices of the same system type that are on **no** circuit, `--set edit=add --set circuitNumber=1 --set circuitPanel=PP-1B` | `changed` 2, the counts before and after |
| **JV8** | *A device already on another circuit.* One device of a PP-2B circuit selected, `edit=add`, circuit 1 on PP-1B | **Record what Revit does** - refused in its own words, moved, or on both; the findings say which, read back |
| **JV9** | *Every device off - the Article 7 case.* All of circuit 1's devices on PP-1B selected, `edit=remove` | `changed` 0, *NOTHING WAS CHANGED*, the finding saying it would leave the circuit empty; the circuit still there |
| **JV10** | *A selection left over.* One device of a PP-2B circuit selected, `edit=disconnect`, circuit 1 on PP-1B named | `changed` 1, circuit 1 on PP-1B **alone**; the finding saying the selection was not used |
| **JV11** | *Kept, through `try-once`, on the modeller's yes.* Disconnect, as JV2, on a saved copy | Keep: PP-1B's panel schedule without the circuit; then `REPORT_PANEL_CIRCUITS` lists it on no panel - the second route |

**Then sign**, on the owner's word only: `python brain/heron_validate.py accept edit-electrical-circuit
--by "Ajmal PS"` once its positive and negative pass, and `heron-status` set to PROVEN in the same
change. A row that fails is recorded here and the card corrected; nothing in a row changes to make it
pass.

**Routing, measured 2026-11-01 in a cloud container on its `model` backend, re-ranker absent** - the
owner's PC ranks with `model+rerank`, which is **NOT RUN** here. The four sentences above reach this card
by identity. *"remove this light from the circuit"*, *"change the panel of circuit 5 to LP-2"* and *"add
this receptacle to the existing circuit"* reach it ranked; on the same tree without it they reached
`SELECT_BY_ELECTRICAL_CIRCUIT` or `REPORT_PANEL_CIRCUITS`. **Two sentences it took when it was written**, each
measured against the same tree without this card: *"circuit these sockets"*, which
`CREATE_ELECTRICAL_CIRCUIT`'s routing table claims - a coin toss on fusion, pulled by *"take this socket
off its circuit"* - and *"what is on circuit 3"*, a question `SELECT_BY_ELECTRICAL_CIRCUIT`'s table
claims, which reached this write whenever the host did not mark it a question (D-128) - pulled by *"add
these lights to circuit 3"*. The modeller's own sentences on this card were not weakened to buy them
back. ~~**JV12** - declare each on the card that answers it.~~ **DONE 2026-11-01**, [row
5b-1356](../fragment-issues/section-5b-rows-401-425.md): *"circuit these sockets"* declared on
`CREATE_ELECTRICAL_CIRCUIT`, *"what is on circuit 3"* on `SELECT_BY_ELECTRICAL_CIRCUIT`, and this card
named in the routing tables of both and of `REPORT_PANEL_CIRCUITS`. Each sentence now reaches its card
by identity (scratch store, `model` backend). All three cards are PROVEN; the edit is outside `impl/`, so
no fingerprint moved. **On merging main the same day** `CREATE_ELECTRICAL_CIRCUIT` had been merged into `CREATE_MEP_SYSTEM` and retired, so *"circuit these sockets"* and this card's routing row moved to `CREATE_MEP_SYSTEM` (DRAFT); a retired card claims nothing.
