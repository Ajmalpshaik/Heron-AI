# The proof path add-in agents never had, and the six that used it

**2026-09-18.** The REVIT ENGINEERING session, continued past midnight from the 17th. Two Revits open
throughout, both the owner's idea, and that arrangement is what made any of this possible in one
sitting.

**Before today no add-in agent could be proven at all.** Not "had not been" — *could not be*. All 26
`.cs` files in `revit/Heron.Revit.Addin/` carry `Heron-Status: DRAFT`, including agents built weeks
earlier and run against real models many times, because the entire proving apparatus is fragment
shaped: `batch-prove` reads `brain/fragments/*/fragment.yaml`, and `heron_validate`, the drafts in
`brain/proof-drafts/` and `check-signatures.py` are all per-fragment. Ten agents were proved by hand on
the 17th and **nothing in the repository recorded it, because there was nowhere to record it.**

`tools/prove-agent.py` is the nowhere. **Six proofs are now signed.**

---

## 1. What it is, and what it copied rather than invented

Five commands: `sessions`, `track`, `review`, `accept`, `check`. Three guarantees taken straight from
the fragment path, because they are load-bearing and were paid for once already:

| | |
|---|---|
| **The machine never signs** | `by:` is written EMPTY. A draft reaching a proof without a person's name fails loudly rather than counting as proven quietly |
| **The prover cannot reach what it proves** | `write_draft` refuses any path under `revit/`. "It never promotes an agent" is a property of the code's reach, not of its good intentions — the fragment side gets the same guarantee from folder layout |
| **`gaps:` is read first** | what the run could NOT establish. A draft with gaps is a job half done, not a proof with footnotes |

**Tracking replaces the negative case, and that is forced rather than chosen.** [D-30](../DECISIONS.md)
wants a positive case and a negative one. A `list_*` agent describes whatever document it is handed, so
**no input makes it correctly return nothing** — there is no negative case to arrange.
[D-53](../DECISIONS.md) tracking is the substitute the `fragment-proving` skill already names for this
shape: the answer must FOLLOW the input. An agent falling back to a cached `Document`, the active view
or the whole session would report the same numbers whatever model was in front of it.

So `track` takes two sessions and **refuses if both have the same model open** — two readings of one
model is one case run twice, and it proves nothing about whether the answer moved.

**Proofs live in `brain/agent-proofs/<ID>.yaml`, beside `brain/agents/<ID>.yaml`.** A fragment carries
its proof inside its own yaml; a `.cs` file cannot. The pairing is deliberate: **the contract is the
promise and the proof is the evidence**, two files with one key, neither pretending to be the other.

**It lives in `tools/` because `brain` may not import `mcp`** ([D-48](../DECISIONS.md)), and
`batch-prove.py` set that precedent rather than this inventing a new arrangement.

---

## 2. Three defects it found in itself, all by being run

None was visible by reading it.

**It described its own ignorance as the model's.** `(? elements)` against a reply carrying
`count = 3513` — it looked for `placedElements` and `count_elements` returns `count`. Now it asks for
each key in turn, and asks `count_elements` separately so the model is named with its size whatever
operation ran. A proof against the wrong file is worse than no proof, and the size is part of saying
which file.

**It called thin evidence clean.** The levels draft read as a pass on **1 number moved and 5 held the
same** — both models happen to hold 2 levels and 0 grids. An agent ignoring the model entirely would
have produced five of those six rows. It now names the ratio and refuses to imply strength it has not
got. **No threshold was invented**: how much movement is enough depends on which two models somebody
chose, which is a fact about the arrangement rather than about the agent.

**The lease refused it nine times in a row**, and that was the lease working. It identifies a CHAT
([D-22](../DECISIONS.md)); a tool under its own id is a second chat, so every request to a Revit the
chat was already using came back `session_in_use`. `--client-id` now exists, opt-in and documented
both ways — sharing an id makes the tool the same chat, and also means a proof run can land in the
middle of somebody's conversation. The operator decides knowing both.

---

## 3. What was signed, and what was refused

**Signed as Ajmal PS, on his instruction, after he verified the room count in Revit himself:**

| agent | what moved between the two models |
|---|---|
| `RM-028` rooms | rooms 0 → 7, spaces 0 → 2 |
| `EXP-018` export | rooms 0 → 7, blank sheets 1 → 0 |
| `FAM-012` families | families 251 → 252, placed instances moved |
| `VIE-013` views | views 24 → 26, templates 16 → 18 |
| `SCH-026` schedules | schedules 3 → 4 |
| `WRK-014` worksets | worksets 2 → 0 |

**Four were NOT signed, and the refusals are the point:**

`IMP-019` imports — **nothing moved.** Neither model holds a CAD link, so the two runs are identical,
which is exactly what an agent ignoring the model would produce. The owner linked a DWG on the 17th;
Revit was closed for a deploy and the unsaved link went with it.

`LVL-027`, `SHT-029`, `DIM-031` — **thin.** They work; the two models are too alike to show it. Signing
these to clear a list would be worth less than leaving them, because a signature stops anybody looking
again.

**One real project model fixes three of the four at once.**

---

## 4. Two agent bugs fixed the same sitting, both found by running them

**`revit_worksets` hid 95% of the document.** It read `Workset1 181` and `Shared Levels and Grids 2`
against 3,513 elements — 183 named, 3,330 missing, and nothing said where they went. **The first
diagnosis was wrong**: the obvious reading is "elements on no workset", and `onNoWorkset` is ZERO on
that model. Every element HAS a workset. The collector asks for `UserWorkset` — the right list to show —
while every view, family symbol and settings element sits on a VIEW, FAMILY or STANDARD workset, counted
into the tally and matching no row. Now 181 + 2 + 3,330 + 0 = 3,513. **A list whose parts do not add up
teaches the reader to distrust the parts.**

**`revit_imports` named a file nobody could use** — `location <Not Shared>`. Correct verdict, useless
label: `ImportInstance.Name` says where the thing sits, and the filename lives on the `CADLinkType`,
which is what Revit's own Manage Links shows.

---

## 5. What the next session needs

1. **A model with a SAVED CAD link** for `IMP-019` — ideally one linked and one imported, so the
   distinction the agent exists to draw is exercised.
2. **One real project model** as the second session, which makes `LVL-027`, `SHT-029` and `DIM-031`
   strong in one pass.
3. **`--client-id` is not optional in practice.** Pass the id the chat uses, and clear a stale lease
   with `HERON_CLIENT_ID=<id> python mcp/client/heron_bridge_client.py release`.
4. **Nothing here promotes an agent.** All 26 `.cs` files still say `DRAFT`, and moving one is a
   separate deliberate act that belongs to the owner. The proofs record evidence; they do not confer
   status.
