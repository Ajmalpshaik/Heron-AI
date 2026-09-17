# The risk-crossing sweep is not deterministic - the raw runs

**2026-09-17.** The evidence behind [FRAGMENT-ISSUES](../../FRAGMENT-ISSUES.md) row 116's
diagnosis, kept because a conclusion is only re-checkable if the runs it came from are.

> Nothing here is specification. The row carries the finding; this carries the output it was read
> from. Where they disagree, **re-run the tool** - which is the whole point of keeping this.

## What this settles, and what it does not

`tools/check-risk-crossings.py` returned **7, 9, 8 and 8** crossings across four runs on one
machine inside the hour. **Runs 2 and 3 had byte-identical inputs and disagreed.**

Row 116's diagnosis: the sentence that moves is the only one whose right answer comes from the
IDENTITY route rather than from ranking - `how many pipes are there` -> `FRG-ELE-002` =
`COUNT_ELEMENTS` is a declared identity, which is how row 116 fixed it. When that identity misses,
the ranked search answers, and ranking is what answers a question with a write. So the instability
is in the INDEX, not the ranker.

**Not established, and no run below shows it:** which write made the identity miss on run 2.
Comparing the shared store against a copy showed only FTS5 shadow tables differing - `fragments`
372, `identities` 2448, `vectors` 374, `utterances` 0, all equal.

## The four sweeps

| run | questions | store | crossings |
|---|---|---|---|
| 1 | 45 | shared APPDATA | **7** |
| 2 | 46 | shared APPDATA | **9** - the outlier |
| 3 | 46 | shared APPDATA | **8** |
| 4 | 46 | private copy, single writer | **8** |

Run 1 predates adding row 109's sentence to the question list, which is why it asks 45. The seven
names in run 1 appear in all four runs. **The only question that moves is `how many pipes are
there` -> `CAP_OPEN_PIPE_ENDS`, which row 116 lists among the four worst as FIXED.**

### Run 1 - 45 questions, shared store

```
Revit 2020   questions asked: 45

A QUESTION ANSWERED BY SOMETHING THAT WRITES, WITH A READ BEATEN (7):
  what size is this duct                     -> AUTO_SIZE_MEP              MODIFY   beat REPORT_DUCT_WEIGHT
  what is the diameter of this pipe          -> CREATE_PIPE                MODIFY   beat REPORT_PIPE_SEGMENTS
  what workset is this on                    -> CREATE_WORKSET             ADMIN    beat SELECT_BY_WORKSET
  what is missing its mark                   -> REPOINT_VIEW_REFERENCE     MODIFY   beat FIND_UNTAGGED_ELEMENTS
  what views are on this sheet               -> PLACE_VIEWS_ON_SHEET       MODIFY   beat FIND_VIEWS_WITHOUT_TEMPLATE
  what is the insulation thickness           -> SET_MEP_INSULATION         MODIFY   beat CHECK_INSULATION_CLEARANCE
  select all the pipes in this view          -> CAP_OPEN_PIPE_ENDS         MODIFY   beat SELECT_BY_MEP_SYSTEM

  A caller acting on one of these does not get a poor answer to
  its question; it CHANGES THE MODEL in reply to one. heron_lookup
  warns on exactly this shape at the seam a host uses - the
  warning is the floor, not the fix.

REACHED A VIEW CHANGE, OR A WRITE WITH NOTHING SAFE CLOSE (2) - read them:
  isolate all the pipes                      -> ISOLATE_ELEMENTS           EXECUTE
  hide everything except the walls           -> HIDE_ELEMENTS              MODIFY

Exit 0 whatever this finds. A crossing is a judgement a person
makes, the same rule tools/check-routing.py sets - and weakening a
question to buy back a rank is never the answer.
```

### Run 2 - 46 questions, shared store. THE OUTLIER

```
Revit 2020   questions asked: 46

A QUESTION ANSWERED BY SOMETHING THAT WRITES, WITH A READ BEATEN (9):
  what size is this duct                     -> AUTO_SIZE_MEP              MODIFY   beat REPORT_DUCT_WEIGHT
  what is the diameter of this pipe          -> CREATE_PIPE                MODIFY   beat REPORT_PIPE_SEGMENTS
  what workset is this on                    -> CREATE_WORKSET             ADMIN    beat SELECT_BY_WORKSET
  how many pipes are there                   -> CAP_OPEN_PIPE_ENDS         MODIFY   beat REPORT_PIPE_SEGMENTS
  what is missing its mark                   -> REPOINT_VIEW_REFERENCE     MODIFY   beat FIND_UNTAGGED_ELEMENTS
  what views are on this sheet               -> PLACE_VIEWS_ON_SHEET       MODIFY   beat FIND_VIEWS_WITHOUT_TEMPLATE
  what is the insulation thickness           -> SET_MEP_INSULATION         MODIFY   beat CHECK_INSULATION_CLEARANCE
  select all the pipes in this view          -> CAP_OPEN_PIPE_ENDS         MODIFY   beat SELECT_BY_MEP_SYSTEM
  isolate all the pipes in the current view  -> SET_MEP_SLOPE              MODIFY   beat CHECK_FIXTURE_CONNECTIVITY

  A caller acting on one of these does not get a poor answer to
  its question; it CHANGES THE MODEL in reply to one. heron_lookup
  warns on exactly this shape at the seam a host uses - the
  warning is the floor, not the fix.

REACHED A VIEW CHANGE, OR A WRITE WITH NOTHING SAFE CLOSE (2) - read them:
  isolate all the pipes                      -> ISOLATE_ELEMENTS           EXECUTE
  hide everything except the walls           -> HIDE_ELEMENTS              MODIFY

Exit 0 whatever this finds. A crossing is a judgement a person
makes, the same rule tools/check-routing.py sets - and weakening a
question to buy back a rank is never the answer.
```

### Run 3 - 46 questions, shared store, identical input to run 2

```
Revit 2020   questions asked: 46

A QUESTION ANSWERED BY SOMETHING THAT WRITES, WITH A READ BEATEN (8):
  what size is this duct                     -> AUTO_SIZE_MEP              MODIFY   beat REPORT_DUCT_WEIGHT
  what is the diameter of this pipe          -> CREATE_PIPE                MODIFY   beat REPORT_PIPE_SEGMENTS
  what workset is this on                    -> CREATE_WORKSET             ADMIN    beat SELECT_BY_WORKSET
  what is missing its mark                   -> REPOINT_VIEW_REFERENCE     MODIFY   beat FIND_UNTAGGED_ELEMENTS
  what views are on this sheet               -> PLACE_VIEWS_ON_SHEET       MODIFY   beat FIND_VIEWS_WITHOUT_TEMPLATE
  what is the insulation thickness           -> SET_MEP_INSULATION         MODIFY   beat CHECK_INSULATION_CLEARANCE
  select all the pipes in this view          -> CAP_OPEN_PIPE_ENDS         MODIFY   beat SELECT_BY_MEP_SYSTEM
  isolate all the pipes in the current view  -> SET_MEP_SLOPE              MODIFY   beat CHECK_FIXTURE_CONNECTIVITY

  A caller acting on one of these does not get a poor answer to
  its question; it CHANGES THE MODEL in reply to one. heron_lookup
  warns on exactly this shape at the seam a host uses - the
  warning is the floor, not the fix.

REACHED A VIEW CHANGE, OR A WRITE WITH NOTHING SAFE CLOSE (2) - read them:
  isolate all the pipes                      -> ISOLATE_ELEMENTS           EXECUTE
  hide everything except the walls           -> HIDE_ELEMENTS              MODIFY

Exit 0 whatever this finds. A crossing is a judgement a person
makes, the same rule tools/check-routing.py sets - and weakening a
question to buy back a rank is never the answer.
```

### Run 4 - 46 questions, a private copy nothing else on the machine can reach

Hashed either side of the run. **This is the proof that the sweep WRITES the store it reads**,
which its own docstring denied until it was measured:

```
BEFORE 63e333e9a28ef3ae98314f3dd77c11c4
AFTER  9d132778d8f77826c745aa317f75f9db
```

```
Revit 2020   questions asked: 46

A QUESTION ANSWERED BY SOMETHING THAT WRITES, WITH A READ BEATEN (8):
  what size is this duct                     -> AUTO_SIZE_MEP              MODIFY   beat REPORT_DUCT_WEIGHT
  what is the diameter of this pipe          -> CREATE_PIPE                MODIFY   beat REPORT_PIPE_SEGMENTS
  what workset is this on                    -> CREATE_WORKSET             ADMIN    beat SELECT_BY_WORKSET
  what is missing its mark                   -> REPOINT_VIEW_REFERENCE     MODIFY   beat FIND_UNTAGGED_ELEMENTS
  what views are on this sheet               -> PLACE_VIEWS_ON_SHEET       MODIFY   beat FIND_VIEWS_WITHOUT_TEMPLATE
  what is the insulation thickness           -> SET_MEP_INSULATION         MODIFY   beat CHECK_INSULATION_CLEARANCE
  select all the pipes in this view          -> CAP_OPEN_PIPE_ENDS         MODIFY   beat SELECT_BY_MEP_SYSTEM
  isolate all the pipes in the current view  -> SET_MEP_SLOPE              MODIFY   beat CHECK_FIXTURE_CONNECTIVITY

  A caller acting on one of these does not get a poor answer to
  its question; it CHANGES THE MODEL in reply to one. heron_lookup
  warns on exactly this shape at the seam a host uses - the
  warning is the floor, not the fix.

REACHED A VIEW CHANGE, OR A WRITE WITH NOTHING SAFE CLOSE (2) - read them:
  isolate all the pipes                      -> ISOLATE_ELEMENTS           EXECUTE
  hide everything except the walls           -> HIDE_ELEMENTS              MODIFY

Exit 0 whatever this finds. A crossing is a judgement a person
makes, the same rule tools/check-routing.py sets - and weakening a
question to buy back a rank is never the answer.
```

## The probe that cleared the ranker

Five lookups of each question inside ONE process, and the same across three processes. Stable both
ways - which is what moved the suspicion off the ranking and onto the index.

```
label=run1  PYTHONHASHSEED=None  pid=75536

  how many pipes are there                       SAME  ['COUNT_ELEMENTS', 'COUNT_ELEMENTS', 'COUNT_ELEMENTS', 'COUNT_ELEMENTS', 'COUNT_ELEMENTS']
  isolate all the pipes in the current view but  SAME  ['SET_MEP_SLOPE', 'SET_MEP_SLOPE', 'SET_MEP_SLOPE', 'SET_MEP_SLOPE', 'SET_MEP_SLOPE']
```

```python
# probe.py - is brain.lookup deterministic, within a process and across them?
import os, sys
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
QUESTIONS = ["how many pipes are there",
             "isolate all the pipes in the current view but leave out the "
             "condensate drain system"]
import heron_brain as brain
for question in QUESTIONS:
    got = [brain.lookup(question, revit="2020").get("capability") for _ in range(5)]
    print(question[:46], "SAME" if len(set(got)) == 1 else "VARIES", got)
```

A second probe tried to fingerprint the query embedding and **failed with `AttributeError: module
'heron_embed' has no attribute 'embed'`** - the wrong API name. It is recorded because it carried
a result anyway: the winner was `FRG-ELE-002` in all three processes, and the candidate list came
back EMPTY, which is what revealed the answer was arriving by the identity route rather than from
ranking. The bug was in the probe, and the accident was the useful part.

## The sweep after the fingerprint was added

Row 116's named next step, in place - the index printed before the questions, the store re-hashed
after them:

```
Revit 2020   questions asked: 46

THE INDEX THIS RAN AGAINST - compare it before comparing counts:
  store    C:/Users/AJMALA~1/AppData/Local/Temp/claude/D--Ajmal-Aj-Programs-Heron-Ai--claude-worktrees-heron-tooling-checkers-ci-2aa6e5/9465e103-f9c1-4119-a046-3a961be4d712/scratchpad/kb-fp\global.db
  md5      fe11c76690df59418c00ee928c29b8b4
  rows     fragments 372, identities 2448, utterances 0, vectors 374
  Two runs whose numbers differ while THIS block matches are a
  question about the ranker. Two whose numbers differ and whose
  block differs are a question about the index, and that is the
  way round it has been every time so far - see row 116.

A QUESTION ANSWERED BY SOMETHING THAT WRITES, WITH A READ BEATEN (8):
  what size is this duct                     -> AUTO_SIZE_MEP              MODIFY   beat REPORT_DUCT_WEIGHT
  what is the diameter of this pipe          -> CREATE_PIPE                MODIFY   beat REPORT_PIPE_SEGMENTS
  what workset is this on                    -> CREATE_WORKSET             ADMIN    beat SELECT_BY_WORKSET
  what is missing its mark                   -> REPOINT_VIEW_REFERENCE     MODIFY   beat FIND_UNTAGGED_ELEMENTS
  what views are on this sheet               -> PLACE_VIEWS_ON_SHEET       MODIFY   beat FIND_VIEWS_WITHOUT_TEMPLATE
  what is the insulation thickness           -> SET_MEP_INSULATION         MODIFY   beat CHECK_INSULATION_CLEARANCE
  select all the pipes in this view          -> CAP_OPEN_PIPE_ENDS         MODIFY   beat SELECT_BY_MEP_SYSTEM
  isolate all the pipes in the current view  -> SET_MEP_SLOPE              MODIFY   beat CHECK_FIXTURE_CONNECTIVITY

  A caller acting on one of these does not get a poor answer to
  its question; it CHANGES THE MODEL in reply to one. heron_lookup
  warns on exactly this shape at the seam a host uses - the
  warning is the floor, not the fix.

REACHED A VIEW CHANGE, OR A WRITE WITH NOTHING SAFE CLOSE (2) - read them:
  isolate all the pipes                      -> ISOLATE_ELEMENTS           EXECUTE
  hide everything except the walls           -> HIDE_ELEMENTS              MODIFY

THE STORE CHANGED WHILE THIS RAN:
  before   fe11c76690df59418c00ee928c29b8b4
  after    1287a6903accbc889f41cfce78a314f2
  Asking moved the index. That is this tool, another
  session, or both - global.db is ONE file for every
  worktree on the machine. It is why a count from here is a
  sample rather than a measurement.

Exit 0 whatever this finds. A crossing is a judgement a person
makes, the same rule tools/check-routing.py sets - and weakening a
question to buy back a rank is never the answer.
```
