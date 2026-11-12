# Needs checking — Group LD

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LD - which steel connections and connection types a model has: `REPORT_STRUCTURAL_CONNECTIONS` (2026-11-01)

Asked for in the modeller's words: *"what steel connections are in this model"* and *"list the connection
types loaded"* - Revit's Steel > Connection (Structure > Connection on 2020) - beside *"add a base plate
connection to these column bases"*, *"put a shear plate connection between this beam and column"*
([Group LE](group-le.md)) and *"change these connections to Clip angle"*. No card read or placed a structural
connection: a grep of every fragment's C# that day for `StructuralConnection` and `OST_StructConnections`
found nothing. `COPE_FRAMING` cuts a beam end round a column; `PLAN_CONNECTION_ORDER` works out the order to
wire or pipe a set of fittings; "connect" in `CONNECT_OPEN_ENDS`, `CONNECT_AIR_TERMINALS`,
`TRACE_CONNECTIVITY` and `REPORT_CONNECTORS` is a pipe, duct or cable joint - look-alikes, not duplicates.
**Changing a connection's type was found already done:** a connection is an element with a type, and
`CHANGE_ELEMENT_TYPE` (DRAFT) names the type, reads it back and refuses what Revit will not take - so no
second card was written for it, and that card now declares the sentence.

| What | Where | What it does |
|---|---|---|
| `REPORT_STRUCTURAL_CONNECTIONS` (READ) | [`brain/fragments/report-structural-connections`](../../brain/fragments/report-structural-connections/fragment.yaml) | Lists every **connection** with its type (generic, detailed or custom), the members it joins with the **primary** first, its origin in mm, its **approval** and its **code checking status**; and every **connection type loaded** with how many connections use it, and the approval types (`report`, `connectionCount`, `typeCount`, `names`). `show` picks a list; a selection narrows to the connections selected or those joining the members selected, and a member no connection joins is named in `notConnected`. **Plates, bolts, welds and cuts are not read** - only the Advance Steel API reaches them. Opens no transaction |

**The read half of a pair** - every question about connections and connection types is answered here, so none
reaches `PLACE_STRUCTURAL_CONNECTIONS` or `CHANGE_ELEMENT_TYPE`.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-01, a Linux container). Read off the shipped assemblies of all
eight with `tools/api-surface --members`: `StructuralConnectionHandler` (`GetConnectedElementIds`,
`GetOrigin`, `ApprovalTypeId`, `CodeCheckingStatus`, `IsDetailed`, `IsCustom`, `SingleElementEndIndex`),
`StructuralConnectionHandlerType` (`IsGeneric`, `IsDetailed`, `IsCustom`),
`StructuralConnectionApprovalType.GetAllStructuralConnectionApprovalTypes` and
`StructuralConnectionCodeCheckingStatus` are the same 2020-2027. **Nothing here has run in Revit.** The card
is DRAFT. **Only Revit can settle** that the order `GetConnectedElementIds` gives puts the primary first, and
what a template's types report as generic or detailed - LD3 checks both.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `c42bb59` without the two steel connection cards and with them - `model` backend, re-ranker absent, no Revit filter and Revit 2020 and 2024, with and without the host's question flag - over every declared utterance and routing-table sentence of a live card (4,351 sentences, five probes included). *"what steel connections are in this model"* and *"list the connection types loaded"* reach this card with and without the flag (`TRACE_CONNECTIVITY` and `LIST_REBAR_TYPES`, reads, before). *"change these connections to Clip angle"*, declared on `CHANGE_ELEMENT_TYPE`, reaches it without the flag (`FRAME_ELEMENTS_IN_CAMERA` before) and this card with it. **Without the flag no other sentence any card declares or claims moved** but *"include elements in links"*, pulled from one read to `ZOOM_TO_LINKED_ELEMENTS` and declared on `SET_SCHEDULE_OPTIONS`, whose table claims it - closed in the same change, [row 5b-1385](../fragment-issues/section-5b-rows-401-425.md), with the one move that made recorded there. With the flag, 29 other answers moved (of 4,351 sentences on three filters), each read to read, none away from a card that declares or claims the sentence - *"join these two columns"*, *"fix the connections on this run"* and *"check the columns sit on foundations"* among them to this card. Seven paraphrase probes reach this card; *"is this connection approved"* reaches `PLACE_STRUCTURAL_CONNECTIONS` without the flag - nobody's words, so not declared ([row 5b-1386](../fragment-issues/section-5b-rows-401-425.md)). `tools/check-routing.py` on a scratch store: no question answered by a write; *claimed in a routing table and not reached* 70 before, 69 after - that sentence the one gone.

**The model - a scratch project new from Revit's metric STRUCTURAL template in Revit 2020**, saved as
`steel-ld` under `%TEMP%\heron-work` - never a working model. Group LE uses its copy. Nothing below changes
the model. The job file is
[`tools/jobs/report-structural-connections-ld-2026-11-01.yaml`](../../tools/jobs/report-structural-connections-ld-2026-11-01.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`,
`--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LD1** | *Arrange, by hand.* On Level 1: steel columns **C1** and **C2** standing apart, steel beam **B1** framing into C1, structural wall **W1** away from all three. **Save a copy as `steel-le`** for Group LE. Then Steel > Connection: **CN1** between B1 and C1 (a shear plate type, or the template's generic one) and **CN2**, a single-member connection such as a base plate, on C2's base. Save. Write down every type in Connection Settings and each connection's Properties | The arrangement LD2 to LD4 use |
| **LD2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file | Negative, the wall W1: `connectionCount` 0, `notConnected` W1. Positive, B1: `connectionCount` 1 - CN1, its type, "joins 2" naming B1 and C1, origin in mm, approval and code checking status - and not CN2 |
| **LD3** | *The second route.* The card with nothing selected and `show` left out | Both connections, CN2 "joins 1" with the end Revit reports; the primary of CN1 as its Properties show it; every type Connection Settings lists, with its use count, and the approval types |
| **LD4** | *A list nobody has.* `show` "bolts" | Findings name the word as neither connections nor types, and that bolts, plates and welds are not read |
| **LD5** | *Revit 2024 or later.* LD2 and LD3 again on a project made the same way in that release | The same answers |

**Written back 2026-11-12, read in a cloud container (no Revit) - nothing was run here.** Since this group was
written: `CHANGE_ELEMENT_TYPE` reads PROVEN on its card, at version 2 - its `proof` block is signed by Ajmal PS,
2026-11-02, on P1102 MEP (21,446 elements), Revit 2020, session 33624. So where this page calls a card named here
DRAFT, or says nothing has run in Revit, that was true when it was written and is out of date as a statement about the
card. It is a card this group names in passing, not the one its rows prove.
