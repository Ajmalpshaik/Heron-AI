# Plain questions that reach a write - the owner's call, and D-101's second half

**2026-10-17. Completed the same day - both ruled and applied.** Ajmal answered *"do it the best only"*:
the advice below, both halves. **The 23 sentences are declared** on their cards, each with a dated comment,
and the three answers his word changed are `corrections` in
[`tests/data/plain-questions.yaml`](../../../tests/data/plain-questions.yaml). **D-101's half is
[D-128](../../decisions/D-128.md)**, built behind `heron_lookup`'s `question` flag, after the host's own
calls were measured first (D-128 carries the figures). Rows
[5b-477 and 5b-478](../../fragment-issues/section-5b-rows-401-425.md) say what changed. **Retires once the
pull request that carries it has merged** - its durable half is in the cards, the rows, D-128 and
[`brain/retrieval-history.md`](../../../brain/retrieval-history.md); repair the two links to this file
(the yaml's comment and D-128's Status line) when it goes.

> Nothing here is specification. Every figure is a run on `main` `37ccfbd0`, 569 fragments, no Revit
> filter, each run on its own scratch store through `heron_brain.lookup` - the function `heron_lookup`
> calls. **lexical** is the built-in backend; **model** is model2vec 0.10.0 with
> `minishlab/potion-base-8M`, from a venv outside the repository. No re-ranker. Nothing met Revit.

## 1. The 24 questions where "a PROVEN read answers it, so declaring there is the owner's call"

From rows 477 and 478, numbered as in [`tests/data/plain-questions.yaml`](../../../tests/data/plain-questions.yaml).
**Today** is what answers on each backend now; **w** means a tool that changes the model.
**Declare on** is the card the sentence would go on, word for word, as one more line in its
`utterances:` - which makes it an exact phrase, answered without ranking. Every card named is PROVEN
and READ; an utterance is not part of a proof's fingerprint, so no proof moves.

| # | Question | Today: lexical / model | Declare on | Advice |
|---|---|---|---|---|
| 5 | what size is this pipe | REPORT_PIPE_SEGMENTS / **w** AUTO_SIZE_PIPE | READ_ELEMENT_PARAMETERS | Declare - it already owns *what size is this duct* |
| 6 | what is the flow in this duct | MEASURE_MEP_VELOCITY / **w** AUTO_SIZE_MEP | READ_ELEMENT_PARAMETERS | Declare here, not on MEASURE_MEP_VELOCITY: that gives a velocity, and needs a limit |
| 7 | which system is this duct on | **w** CREATE_MEP_SYSTEM / **w** SET_MEP_SYSTEM_TYPE | READ_MEP_SYSTEM | Declare |
| 15 | how many rooms are on level 1 | FILTER_ELEMENTS_BY_CATEGORY / **w** SET_ROOM_LIMITS | FILTER_ELEMENTS_BY_CATEGORY | Declare on the filter - **the row names REPORT_ROOM_SPACE_DATA**, but that refuses an empty selection, and row 401 put *how many rooms are there* on the filter for that reason |
| 19 | what is the view template of this view | **w** REMOVE_VIEW_TEMPLATE / **w** same | REPORT_VIEW_TEMPLATE_CONTROL | Declare |
| 34 | which elements are on the wrong workset | **w** SET_ELEMENT_WORKSET / **w** same | - | **Do not declare.** SELECT_BY_WORKSET lists one named workset; which element is *wrong* is a judgement no tool makes |
| 35 | what is the insulation on this pipe | **w** SET_MEP_INSULATION / **w** same | READ_ELEMENT_PARAMETERS | Declare - it already owns *what is the insulation thickness* |
| 38 | how many air terminals are there | COUNT_ELEMENTS / **w** CONNECT_AIR_TERMINALS | COUNT_ELEMENTS | Declare - beside *how many pipes are there* |
| 39 | which air terminals have no flow | **w** SET_AIR_TERMINAL_FLOW / **w** same | REPORT_CONNECTOR_LOADS | Declare |
| 41 | which pipes are not in a system | **w** SET_MEP_SYSTEM_TYPE / **w** same | READ_MEP_SYSTEM | Declare - it names every element on no system |
| 47 | which doors have no tag | **w** TAG_ELEMENTS_IN_VIEW / **w** same | FIND_UNTAGGED_ELEMENTS | Declare - beside *which ducts have no tag* |
| 48 | how many walls are there | FILTER_ELEMENTS_BY_CATEGORY / **w** PLACE_WALL_SWEEPS | COUNT_ELEMENTS | Declare |
| 49 | what type is this wall | REPORT_CURTAIN_WALL_TYPE / **w** SET_CURTAIN_WALL_MULLIONS | DESCRIBE_ELEMENTS | Declare on DESCRIBE_ELEMENTS - **the row names READ_ELEMENT_PARAMETERS**, but this card reads family and type from the element's type, and owns *what family and type is this* |
| 62 | what is the sheet number of this sheet | **w** DUPLICATE_SHEETS / **w** CREATE_SHEET | READ_ELEMENT_PARAMETERS | Declare here for *this* sheet; the row also offers LIST_SHEETS, which lists every sheet |
| 63 | who drew this sheet | **w** CREATE_PIPE / **w** CREATE_PIPE | READ_ELEMENT_PARAMETERS | Declare - *Drawn By* |
| 68 | is this schedule filtered | REPORT_SCHEDULE_DEFINITION / **w** SET_SCHEDULE_FILTERS | REPORT_SCHEDULE_DEFINITION | Declare |
| 73 | what category is this family | **w** SET_FAMILY_CATEGORY / **w** same | DESCRIBE_ELEMENTS | Declare - it reads a placed element, so one instance must be selected |
| 83 | what is the name of this room | FILTER_ELEMENTS_IN_ROOM / **w** SYNC_SPACES_WITH_LINKED_ROOMS | READ_ELEMENT_PARAMETERS | Declare |
| 84 | which rooms are on level 2 | **w** PLACE_ROOMS / **w** SET_ROOM_LIMITS | FILTER_ELEMENTS_BY_CATEGORY | Declare on the filter - same disagreement as #15 |
| 89 | what is the voltage of this outlet | SELECT_VISIBLE_IN_VIEW / **w** SET_FAMILY_CONNECTOR_ROLES | READ_ELEMENT_PARAMETERS | Declare |
| 94 | what is the system classification of this pipe | **w** CREATE_MEP_SYSTEM_TYPE / **w** SET_MEP_SYSTEM_TYPE | READ_MEP_SYSTEM | Declare |
| 104 | which walls are room bounding | **w** SET_LINK_ROOM_BOUNDING / **w** same | READ_ELEMENT_PARAMETERS | Declare - *Room Bounding* |
| 105 | what is the height of this wall | **w** CREATE_WALL / **w** CREATE_WALL | READ_ELEMENT_PARAMETERS | Declare - *Unconnected Height* |
| 106 | what is the top constraint of this wall | **w** SET_WALL_CONSTRAINTS / **w** same | READ_ELEMENT_PARAMETERS | Declare - beside *what is the base constraint of these walls* |

**What declaring the 23 marked "Declare" did, measured** on a scratch copy of `main` with only those 23
lines added:

| | lexical | model |
|---|---|---|
| plain questions answered by a write, of 120 | 27 -> **13** | 32 -> **9** |
| plain questions whose named read came first, of 41 | 20 -> 37 | 17 -> 37 |
| his 79 (`score-routing`): handed a change | 9 -> 9 | 11 -> **9** |
| his 79: a question moved down | 1 - #2 GROUP_AND_COUNT 4th -> 5th | 1 - #76 SELECT_BY_FAMILY 1st -> 2nd |
| his 79: a question moved up | none | 2 - #19 COUNT_ELEMENTS 5th -> 1st, #43 not found -> 4th |
| any answer anywhere riskier than before | **none** | **none** |

Every sentence asked was checked: the 120, his 79, the 79 of `check-risk-crossings.py` and the 238 of
`material-questions.tsv`. Each declaration was then measured alone, to say which card moved what: both
falls are read to read - #2 by the two COUNT_ELEMENTS lines, #76 (*Find the VCDs.* now
FIND_LIBRARY_FAMILIES) by the three READ_MEP_SYSTEM lines. None of the 23 was declared anywhere before,
so none becomes ambiguous.

**What stays after them:** 13 / 9 plain questions still reach a write - the eight rows say no read
answers, and others no row named. Those need a new read or D-101, not a declaration.

## 2. D-101's second half - should a QUESTION ever be offered a write at all?

[D-101](../../decisions/D-101.md) proposed: *when the host resolves a request that is a QUESTION, the
candidate set should exclude fragments above READ.* It waits on a reliable test of what a question is.
Measured by adding a risk ceiling to the structured filter in a scratch copy (`heron_retrieve.eligible`,
the step that already drops a fragment for the wrong Revit), and asking every set with and without it.
Two ceilings: **READ** (D-101's words) and **the write line** (keeps EXECUTE view changes such as
isolate, drops MODIFY and above).

| Option | Who decides "question" | His 79, handed a change (lexical / model) | His 79, 1st place | Right answers lost from the shortlist | The 120 plain |
|---|---|---|---|---|---|
| **A. As today** - warn, and declare reads one by one | nobody | 9 / 11 | 18 / 17 | 0 | 27 / 32 reach a write |
| **B. The host says so**, Heron drops writes - ceiling at READ | the host (D-01 kept) | 2 / 2 | 18 / 17 | 1 / 0 | 0 by construction |
| **B. The host says so** - ceiling at the write line | the host | **0 / 0** | 18 / 18 | 1 / 0 | 0 by construction |
| **C. Heron reads the first word** (what, how, is, can...) or a `?` | Heron (against D-01) | 6 / 5 | 16-17 / 15-16 | 4-5 / 3 | all 120 flagged |

**B's figures are a ceiling, not a forecast.** They assume a host that is never wrong about which of
his requests wants no change - the answer key's own risk stood in for it. How often the real host would
be right was not measured here, and is the first thing to measure before relying on it.

**Why C loses right answers - his own words.** It flags 23 of his 79, and 6 of those want a change or a
view change: *"How many diffusers do I need in this room?"* and *"How many sprinklers do I need?"*
(D-86's own example of a question only a write can answer), *"Do the grayout for MEP in the active
view."*, *"How do I isolate the VCD dampers in this model?"*, and two instructions that open *"When
you..."*. It misses 14 of his read requests that are not phrased as questions - *"Check the Description
of the duct accessories."*, *"Show me all the disconnected items."*, *"Find the VCDs."* On the library's
own declared sentences, the evidence D-101 names: 13 of 1,948 on write cards look like questions, and
779 of 1,331 on read cards.

**What no option does:** none answers *"nothing here reads that."* With the filter on, every plain
question still got some read - for *how big is this family file* or *what export settings are there*,
a read that does not answer it. Harmless, and not the honest refusal D-101 asked for.

**Advice: B, ceiling at the write line**, written as a new decision superseding D-101's second half:
the host passes "this is a question" to `heron_lookup`, and Heron drops candidates at or above the write
line for that call only. It keeps D-01 (the host classifies, Heron never does) and D-65 (routing is the
host's), and it measured best. **Before it is built, the host's own calls on his 79 should be measured**
against the answer key - that is the reliable test D-101 was waiting for, and B's figures are only as
good as it is.

## What this does not establish

- **The 120 are a session's sentences**, written before any card was read; his 79 are the ones that
  decide. One encoder for every `model` figure.
- **Nothing met Revit.** A declared sentence routes to a read; that the read answers the question on a
  real model is each card's own proof, not this measurement.
- **Option B was measured as a filter patched into a scratch copy**, not as built code. Nothing in the
  repository filters by risk.
