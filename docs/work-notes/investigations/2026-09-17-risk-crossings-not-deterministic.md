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
**Established 2026-10-17 - see the last section:** another checkout's `index()` rebuilding
`identities` from cards that did not declare the sentence, reproduced on the 2026-09 code.
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

## 2026-10-17 - which write made the identity miss, reproduced

**The question this note left open is answered, by making it happen again.** The miss is
**another checkout's `heron_search.index()` writing the same store** - on the 2026-09 code it
rebuilt `identities` from its own cards on every open, so a tree that did not declare the sentence
replaced the table between this tree's rebuild and its read. Row 116 carries the finding; this is
the output.

### The probe

One question asked in a loop through `heron_brain.lookup` of a named tree, on a scratch
`HERON_KNOWLEDGE`, with `HERON_AUDIT` pointed at a throwaway folder:

```python
import os, sys, collections
tree, question, n = sys.argv[1], sys.argv[2], int(sys.argv[3])
sys.path.insert(0, os.path.join(tree, "mcp", "server"))
sys.path.insert(0, os.path.join(tree, "brain"))
import heron_brain as brain
seen = collections.Counter()
for i in range(n):
    got = brain.lookup(question, revit="2020")
    seen[(got.get("route"), got.get("capability"))] += 1
for k, v in seen.most_common():
    print(os.path.basename(tree), question[:30], v, k)
```

### On the 2026-09 code - `5669e7e5` (#165, which declared the sentence) and its parent

`git log -S "how many pipes are there" -- brain/fragments` names exactly one commit, `5669e7e5`,
2026-09-17 01:51 - the morning of the four runs above. Any checkout taken before it lacked the
declaration.

```
wt-new how many pipes are there 1 ('identity', 'COUNT_ELEMENTS')        # build
wt-new how many pipes are there 20 ('identity', 'COUNT_ELEMENTS')       # alone, 20 asks
# both at once - the parent asks "what size is this duct" 60 times beside it
wt-new how many pipes are there 58 ('hybrid', 'CAP_OPEN_PIPE_ENDS')
wt-new how many pipes are there 2 ('hybrid', 'COUNT_ELEMENTS')
wt-old what size is this duct 58 ('hybrid', 'AUTO_SIZE_MEP')
wt-old what size is this duct 2 ('hybrid', 'SELECT_TOUCHING')
# alone again
wt-new how many pipes are there 5 ('identity', 'COUNT_ELEMENTS')
```

`CAP_OPEN_PIPE_ENDS` is the very answer run 2 printed. Asked one after the other, the tree recovers
at once - each open rebuilt from its own cards - so the miss needs the other tree **at the same
time**, which is what a second live session is.

### On today's code - `5ded99d` (#544, which declared *rename this view*) and its parent

```
wt-today-new rename this view 3 ('identity', 'RENAME_ELEMENTS')         # alone
# both at once, 40 each
wt-today-new rename this view 22 ('identity', 'RENAME_ELEMENTS')
wt-today-new rename this view 17 ('hybrid', 'RENAME_ELEMENTS')
wt-today-new rename this view 1 ('hybrid', 'REPORT_VIEW_SETTINGS')
```

**Still 18 misses in 40.** The digest guard of row 136 skips a rebuild only when the cards are the
same; two trees whose cards differ still each rebuild from their own. What keeps the sweep stable is
its private store, the default since 2026-09-29. `--shared`, or two worktrees pointed at one
`HERON_KNOWLEDGE`, can still miss - whether a store named that way should refuse a second tree is
row 131's policy question, still the owner's.

### The md5 that moved on every run - a second, harmless write

With one writer and the answers identical run after run, the store's md5 still changed every time.
Every table compared equal before and after a sweep - FTS5 shadow tables included - so the write
changed no row. Stepping through one open at a time found it:

```
after first open (593, '965b2481')          # (SQLite file change counter, md5)
open_scope()    -> (594, 'e94033ad')        # a commit, nothing changed
lookup how many pipes are there  WROTE (595, 'ac16138a')
lookup what size is this duct    WROTE (596, '5bf4bec6')
```

`heron_scope.Store._create` `INSERT OR REPLACE`d the `scope` and `schema` rows of `meta` and
committed on every open, so every lookup was a commit. It writes a row only when its value differs
now, and the same walk prints no write at all:

```
after first open (591, '160f42a7')
refresh ... EMBED.index_chunks   -  (591, '160f42a7')
lookup how many pipes are there  -  (591, '160f42a7')
lookup what size is this duct    -  (591, '160f42a7')
```

### The sweep after the change

| | lexical | `model` (model2vec 0.10.0, `potion-base-8M`) |
|---|---|---|
| three private runs, `--revit 2024` | byte-identical reports | byte-identical reports |
| store md5 before the run = after | yes, `160f42a7...` every run | yes, `d980de4e...` every run |
| `THE STORE CHANGED WHILE THIS RAN` | never printed | never printed |
| crossings | 4 | 2 |

Three `--shared` runs on one persistent folder ended on `160f42a7...` each time - the md5 of a fresh
private build. Before the change, five private runs already agreed on their answers; what did not
reproduce was the file.

**This note can now retire once row 116 is closed** - the crossings are what keep it open, and they
are the per-sentence judgements the row names.
