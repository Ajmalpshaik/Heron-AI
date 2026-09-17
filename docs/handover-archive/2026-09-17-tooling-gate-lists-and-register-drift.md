# The release gate list was decided by YAML indentation, and a lesson written down three times did not reach the next file

**2026-09-17.** The tooling sitting: checkers, suites, CI, register reconciliation. No Revit, and
none needed.

> **Archived session note.** Nothing here is specification. Where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win.** Written as a new file on purpose:
> other sessions were running against the same tree, and a new file cannot conflict. The two
> register findings in §3 are **not** applied to
> [FRAGMENT-ISSUES.md](../FRAGMENT-ISSUES.md) here — they are handed over to be folded in one at
> a time.

---

## 1. `check-gaps` had one UNFINISHED item, and it was a crash that CI cannot see

`python tools/check-gaps.py` on Windows reports exactly one:

```
FAIL  test_buildmatrix.py (0.7s)
```

It is not the suite. `brain/heron_buildmatrix.py` wrote every path it names in a refusal with
`os.path.relpath(path, ROOT)`, and both of the paths it can name — `golden` and `props` — arrive as
**arguments**. On Windows `os.path.relpath` **raises** across drives rather than returning something
useless:

```
ValueError: path is on mount 'C:', start on mount 'D:'
```

and it is called **while wording the refusal**, so the crash replaces the message. The reader gets a
traceback exactly where they were about to be told what to do.

**This is the fourth instance.** `heron_authoring`, `heron_tag` and `heron_context` each carry a
comment about the same defect; `heron_tag`'s records that the first two were byte-identical copies
of one another, bug included, and all three were fixed on **2026-09-15**.
`brain/heron_buildmatrix.py` was written **two days later**, in
[PR #171](https://github.com/Ajmalpshaik/Heron-AI/pull/171), and arrived with it anyway.

**That is the part worth keeping.** The lesson had been learnt, written down three times, and did
not reach the next file — because nothing in the repository can refuse the spelling. In most places
`os.path.relpath` is correct.

**Why CI is green and will stay green.** `tempfile.mkdtemp()` lands on `C:` while a checkout on `D:`
is a different drive. On Linux `/tmp` and the checkout share a mount, so the suite passes whatever
the code says. `tools/check-gaps.py`, run on Windows, is the only thing that named it.

Fixed by using `heron_fragment.repo_relative` — the repository's one answer, which falls back to the
absolute path instead of raising. A fourth private copy of `_where` would have been the actual
mistake.

**The test is written twice, and the second half is the one that matters.** The behavioural check
cannot fail on Linux however broken the module is, and a check that cannot fail is not a check. So
`tests/test_buildmatrix.py` also reads the module's own text and refuses the spelling, with comment
lines stripped first — because the module is now required to carry a comment naming the function it
may no longer call. Same argument as `tests/test_change_reporting.py`, which tests a field name as
text for the same reason.

Proved both ways: the text check answers `False` against the old source and `True` against the
fixed one, on this machine, with no drive involved.

## 2. Two modules read `gates.yml`, disagreed by three, and nothing compared them

`brain/heron_tag.py` decides which gates a **release** must report green.
`brain/heron_qa.py` decides which checks a **change** must report. Both read
`.github/workflows/gates.yml`, with a regex each. Measured before the fix:

| | sees |
|---|---|
| `heron_qa.required()` | **11** |
| `heron_tag.required_gates()` | **8** |

The three missing were `check-routing`, `check-intrusion` and `check-compile` — and the reason is
not a decision anybody made. `heron_tag.RUNS` was anchored on `run:`, so it could only see a step
written as a **one-liner**. Those three are `run: |` blocks: the first two need a `mkdir` for
`HERON_KNOWLEDGE` first, and `check-compile` captures a log and then refuses a `SKIPPED` release.

**So a release could be cut with `check-compile` never reported.** Whether the add-in compiles at
all was not release-blocking, because of how somebody indented YAML.

`required_gates`' own docstring promises the opposite — *"a fifth gate added to CI becomes required
here with no edit"* — and `tests/test_tag.py`'s comment from earlier the same day says the same
thing about the four checkers that had just been wired. The promise was real. The regex did not
keep it.

Fixed by anchoring on `python` rather than on `run:`. The looser match errs in the safe direction
and that is deliberate: an **invented** gate refuses a release, a **missed** one ships an unchecked
artefact.

Three things now hold it:

* `tests/test_tag.py` still pins the list, so a gate **leaving** CI is a test failure rather than a
  quietly easier release.
* It now also **compares the two readers** instead of pinning two lists. Pinning is what let this
  drift: two lists typed in two files agree until one of them is edited.
* `.github/workflows/gates.yml` carries a header saying, at the place people edit it, that the file
  is read by code and that adding a gate makes it block a release.

**That header invented a phantom gate in its own first draft.** It spelled the invocation out in a
sentence; both readers picked it up and `required_gates` returned twelve, `check-something` leading
the list. The new comparison check in `tests/test_tag.py` is what caught it. It is reworded, and it
now says so — the same trap `heron-ship` already records against `check-structure`, where a comment
counts.

### What is still true and was not changed

`heron_qa.required()` has the same looseness — it always did — and it is now shared rather than
divergent. A sentence in `gates.yml` that looks like an invocation becomes a required check for
**both** gates. That is the safe direction, and the agreement check surfaces it immediately.

## 3. Two rows say OPEN, and a later row has closed each of them

`tools/open-defects.py` prints **35** open ids and says in its own header that the one thing it
cannot see is a row closed by a later row in prose. Reading them, two are:

### Row 111 — closed by row 117, and by the code

Row 111: *"`revit_change` has never reported what a write did, because it reads a key nothing
emits."* Status cell: **OPEN**, with *"The fix is `verdict` instead of `answer`, plus rendering
`provides` … It needs a test that FAILS on the old key."*

All three exist:

* `mcp/server/heron_mcp_server.py` reads `reply.get("verdict")` and renders `provides`, with the
  history in a comment naming row 111.
* `tests/test_change_reporting.py` exists, tests the field name as text, and **passes**.
* **Row 117 says so itself**: *"IT IS ONLY VISIBLE BECAUSE ROW 111 SHIPPED THE SAME DAY."* Row 117
  is a genuinely new and separate defect — `WithVerdict` writing "the model was CHANGED" from
  `applied`, which is the request rather than the outcome — and it is correctly open.

### Row 57 — closed by row 64, in both of its clauses

Row 57: *"`check-vertical-clearance` cannot be proved on this model."* Status cell: **Open — needs
content, and the content needs a decision. One of the seven stale proofs, so it is owed a re-run
regardless.**

Row 64 is headed **"[ROW 57] IS SOLVED, AND THE ANSWER WAS A BOX RATHER THAN A CATEGORY"**, carries
the measured pair — `requiredGap 2000 mm → tooClose 1, clashing 0`; `requiredGap 1 mm →
tooClose 0, clashing 0` — and its own status says **"All seven stale proofs closed."** Both clauses
of row 57's status are contradicted by name.

### A third shape, offered rather than asserted

Rows **109**, **113** and **116** are three OPEN rows tracking one fact: a question resolving to a
write. 109 found one sentence by hand; 113 measured 14 in 45; 116 found the cause and took 14 to 7.
116 carries the live state. 109 and 113 are superseded rather than open — which is the
*two-marks-one-fact* failure row 10's own status names against `E11`–`E15`. **Not claimed here**,
because "superseded" is a judgement about someone else's row.

## 4. What this sitting did not do

* **Nothing was applied to the registers.** §3 is handover, not an edit. Other sessions were live
  in the same tree.
* **`heron_authoring._where` and `heron_tag._where` are still two identical copies**, and
  `heron_fragment.repo_relative` is a third answer with slightly different wording — it returns
  `../../tmp/x.json` where `_where` returns the absolute path. One of the three should own it.
  `heron_buildmatrix` deliberately made no fourth copy and took `repo_relative` as it is.
* **Nothing here proves anything against a model.** Every check in this sitting is text or a
  comparison. [D-30](../DECISIONS.md) is untouched.
* **No gate was widened and no known-failure list was edited.** The list in `gates.yml`'s `tests`
  job is exactly as it was.

## 5. Re-derive every number above

```bash
python tools/open-defects.py                       # the ids, not the count
python tools/check-gaps.py; code=$?; echo $code    # on its OWN line
python tests/test_buildmatrix.py
python tests/test_tag.py
python tests/test_change_gate.py
python tests/test_change_reporting.py
```

And the divergence in §2, which is now zero:

```bash
python -c "import sys,os; sys.path.insert(0,'brain'); import heron_tag,heron_qa; print(heron_tag.required_gates()); print([os.path.basename(p)[:-3] for p in heron_qa.required()])"
```
