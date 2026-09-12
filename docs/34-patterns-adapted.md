<!--
Heron-Agent:  none
Heron-Step:   17
Heron-Status: DRAFT
Heron-Since:  0.1.0
Heron-Layer:  brain
See docs/29-metadata-standard.md
-->

# 34 — The patterns behind the studied repositories, and what each becomes here

**[33](33-external-repository-research.md) asked *what are these projects*. This asks *how do they get
their result*, and answers *what is that worth to a BIM modeller*.**

The difference matters. A matrix row says `agentmemory` fuses three retrieval streams. A **pattern**
says *why fusing beats picking, and what it costs* — and that is the part that survives being moved into
another project. [D-25](DECISIONS.md) is the rule: **studied and re-authored, never imported.** Nothing
here is code from any of them.

**Read [32 §1](32-master-architecture-reconciliation.md) first.** Heron is a **BIM-modeller-facing
platform**, not a developer-assist harness. Most of these projects are the second thing. The pattern
transfers; the shape almost never does.

---

## 1. What came out of it

| | |
|---|---|
| **Patterns extracted** | **20**, from **20** repositories — `gbrain` was added by the owner on 2026-09-09 and is the only one that **measured** a question Heron has open. §2.15 to §2.19 came from a second round on 2026-09-11, against three repositories named by the improvement-gate plans |
| **Adopted and built** | **6** — tiered depth, the cut marker, **the boundary hook** ([§2.9](#29--built--enforce-at-the-moment-of-the-act-not-afterwards)), and the three the improvement gate is made of ([§2.15](#215-built---compare-the-finished-diff-against-what-the-change-said-it-was-for), [§2.16](#216-built---a-file-no-rule-recognises-widens-the-checks-rather-than-passing-quietly), [§2.17](#217-built---check-the-thing-that-gets-delivered-not-the-tree-it-was-built-from)). All tested |
| **Adopted as a rule rather than as code** | **1** — [§2.18](#218-adopted-as-a-rule---mock-only-the-thing-you-genuinely-cannot-have) |
| **Already held** | **7** — Heron had them, and in five cases more strictly |
| **Blocked on an owner decision** | **2** — `Q-50`, `Q-51`, `Q-53`. `Q-49` was answered and built; `Q-52` was answered by **measuring it** |
| **Rejected with a reason** | **3** — one of them **measured and rejected**, not argued ([§2.13](#213--measured-and-rejected--a-third-retrieval-stream-tried-at-six-settings)) |

**The two built together cut a generation packet from 5,737 characters to 372 — and the request crosses
byte for byte at every depth**, which is the half that makes the other half safe.

---

## 2. The patterns, and what each becomes

### 2.1 ✅ BUILT — Tiered depth: carry as much of a part as the task needs

| | |
|---|---|
| **Where from** | [OpenViking](https://github.com/volcengine/OpenViking) — [33 §5.4](33-external-repository-research.md). Idea taken from its README; **no source was read** (AGPLv3) |
| **Their mechanism** | Every entry is processed **on write** into three tiers — L0 abstract (~100 tokens), L1 overview (~2k), L2 the full body — and loaded only as deep as the task requires. Each directory carries its own L0/L1 so relevance is judged before anything is opened |
| **Why it works** | The expensive thing is not *finding* the right item, it is *carrying* it. Deciding relevance needs one sentence; using the item needs all of it. Those are different reads and most systems only have one |
| **What Heron already had** | **Three quarters of it, unnamed.** `semantic-identity` is the abstract, the rest of `fragment.yaml` is the overview, `impl/any/fragment.cs` is the body — and `BUDGET` already loaded different parts per path |
| **What was missing** | **A vocabulary.** `BUDGET` said *which parts*, and had no way to say *how much of one*. So "this path may reach the overview and no further" could not be written down, in a module built so that a budget violation raises |

**Built as** `ABSTRACT` / `OVERVIEW` / `FULL`, with `Context.add()` refusing a part deeper than the
packet's cap. Measured on the generation path:

| depth | packet | neighbour | its cases | API surface |
|---|---|---|---|---|
| `full` | **5,737 ch** | 3,444 | 1,730 | 323 |
| `overview` | **1,774 ch** | 1,111 | 362 | 61 |
| `abstract` | **372 ch** | 70 | 30 | 32 |

**Two Heron-specific choices inside it, and neither is generic:**

- **The cases abstract counts *positive and negative separately*** — because [D-30](DECISIONS.md) makes
  the negative case the load-bearing half of a proof. A single total would hide the one difference worth
  knowing, and a neighbour with no negative case is **said** so, because writing a new fragment modelled
  on one is how a missing negative case spreads. *(Checked across the library while building it: **all
  360 `cases.yaml` files parse, and none lacks a negative case.**)*
- **A part with no shallower form is complete, not deep.** The capability part is derived here from the
  store — a few lines with no fuller version anywhere to be a reduction of. The first version of the cap
  refused it, which was the cap inventing a problem. The distinction is not *how big is this* but **is
  there more of it somewhere**, and only a part read from a file can answer yes.

### 2.2 ✅ BUILT — The marker attached only to the case that could mislead

| | |
|---|---|
| **Where from** | [`code-review-graph`](https://github.com/tirth8205/code-review-graph)'s `uncertainty.py` — [33 §5.6](33-external-repository-research.md) |
| **Their mechanism** | *"A bare `result_count: 0` is ambiguous. It can mean 'the code really has no such relationship', or it can mean **'this graph cannot see that relationship'**."* So a marker is attached **only when the result is empty**, hard-capped in length, and the blind-spot list is **data rather than scattered conditionals** — with entries deleted as gaps close |
| **Why it works** | *"One short sentence on the empty case is a **token saving, not a cost**: it replaces a multi-thousand-token fallback search with roughly thirty tokens of honesty."* And because it fires only on the misleading case, **every response that carries results stays byte-identical** — so the marker means something when it appears |
| **What Heron already had** | The doctrine, as [D-52](DECISIONS.md), and one fragment doing it right: `FILTER_ELEMENTS_BY_CATEGORY` reports `unresolvedLevel` so a broken lookup reads as *"12 found, 12 with no level"* |

**Built as** `Part.cut` and `Context.reduced` — a part that carried all of itself reports **nothing**; a
part that lost something says **how much**. Both the CLI's `report()` and the `heron_context` MCP tool
print a `CUT` line, and both are absent from a full packet.

**The MCP half was missing for one commit, and a security review of the depth change found it** as its
one non-security note: the seam returned `depth` and `cut` from the first day and **the tool's render
loop printed neither**, so a person at the CLI was told what had been left out and the host was told
nothing. **A shorter packet that reads exactly like a complete one is this very pattern's own failure**,
at the surface where it matters most. Fixed and locked in
[`tests/test_brain_reachable.py`](../tests/test_brain_reachable.py) — which is the file that exists
because *complete, tested, and unreachable from a conversation is not what "built" was meant to mean*.

**And the same pattern is what [Q-46](OPEN-QUESTIONS.md) needs**, which is the owner's to answer: the
cheapest version of that question is not 59 fragment edits but **one rule that fires only on the empty
case, capped, applied once.**

### 2.3 ✅ ALREADY HELD, and made stricter by this work — compress what came back, never what was asked

| | |
|---|---|
| **Where from** | [Headroom](https://github.com/headroomlabs-ai/headroom) — [33 §5.14](33-external-repository-research.md) |
| **Their mechanism** | Compresses **tool outputs, logs, files and RAG chunks** — never the user's question. Locally, with nothing sent anywhere. A `ContentRouter` picks a compressor **by content type**: one for JSON, one for source code by AST, one for prose. Reversible: originals cached locally and retrieved on demand |
| **Why it works** | The question is short and load-bearing; the answer is long and redundant. Compressing the wrong one of those saves nothing and destroys everything |
| **Heron's version** | The rule was already written — **in a docstring**, and asserted in a test. §2.1 turned it into a **branch**: `FULL_ONLY = (REQUEST, SITUATION)` means the code path that would shorten a request does not exist. `OST_DuctCurves` is what a shortener takes first, and it is the load-bearing half of a BIM sentence |
| **Not taken** | The compressor itself. Heron has none and should have none until [19 §2](19-context-and-cost.md)'s budgets are agreed. **Their `ContentRouter` names the design for when it arrives**, and Heron already has the hook: a part's `kind` *is* its content type |

### 2.4 ✅ ALREADY HELD, more strictly — deterministic before the model, as an order

**From** [`alibaba/open-code-review`](https://github.com/alibaba/open-code-review), in better words than
this repository had used: *"For review steps that **must not go wrong**, engineering logic — not the
language model — guarantees correctness."* Heron enforces it as pipeline order
([19 §5](19-context-and-cost.md), [02 §6](02-architecture-overview.md)) rather than as an optimisation.
Their fourth deterministic piece states a claim Heron acts on without having written down:
**template-driven rule matching is more stable and predictable than language-driven rule guidance.**

### 2.5 ✅ ALREADY HELD, more strictly — undeclared is refused

**From** [`ruflo`](https://github.com/ruvnet/ruflo)'s `.harness/mcp-policy.json`: `defaultDeny: true`,
audit log on, a dangerous-pattern list. **Heron's is stronger because it is not a file.**
[`RevitOperations.cs`](../revit/Heron.Revit.Addin/RevitOperations.cs)'s guard is a branch every
operation passes: *undeclared is refused* → emergency stop for anything at `MODIFY` or above →
permission level. **A config file can be forgotten by a code path that never consults it.**

### 2.6 ✅ ALREADY HELD, more strictly — evidence before a completion claim

**From** [`superpowers`](https://github.com/obra/superpowers): *"NO COMPLETION CLAIMS WITHOUT FRESH
VERIFICATION EVIDENCE"*, and the row that matters — *a regression test that has only ever passed is not
evidence; the red-green cycle must have been seen.* **[D-30](DECISIONS.md) is that, as a format a tool
checks rather than prose a model is asked to obey.**

### 2.7 ✅ ALREADY HELD — the store must be free to rebuild

**From** [`claude-mem`](https://github.com/thedotmack/claude-mem), as a **counter-example**: its memory is
built by model calls and every observation costs quota, so it **cannot be rebuilt**.
[Golden Rule 11](14-golden-rules.md) makes Heron's store derived and [D-24](DECISIONS.md) requires
re-indexing to be free. **A store that is expensive to rebuild is a store nobody rebuilds, and a store
nobody rebuilds goes stale** — [D-30](DECISIONS.md)'s staleness fingerprint reaching the same conclusion
from the other end.

### 2.8 ✅ ALREADY HELD — the harness refuses to decide when to stop

**From** [`prime-agent`](https://github.com/PrimeIntellect-ai/prime-agent): not a token budget, but
`shouldStopAfterTurn(context) → bool` — a predicate handed to whoever embedded it, with everything
needed to decide. **That is [D-01](DECISIONS.md) written as a type**, and it is why Heron should not grow
a bound of its own.

### 2.9 ✅ BUILT — enforce at the moment of the act, not afterwards

**From** [ECC](https://github.com/affaan-m/ECC) and [gstack](https://github.com/garrytan/gstack).
**[Q-49](OPEN-QUESTIONS.md), asked and answered on 2026-09-09 — the owner said yes.**

Their hooks **block the tool call**; Heron's `check-*.py` held the same rules and ran only when a person
typed them. gstack's shape was the best of the four offered: **the hook declared in the skill's own
frontmatter**, so the guard installs with the capability and the two cannot drift apart.

**Built as [`.claude/skills/heron-guard/`](../.claude/skills/heron-guard/SKILL.md)** — one hook, one
rule: the Revit vendor namespace outside `revit/` is refused **at the moment the edit is proposed**.

**One rule and not five, deliberately.** A hook with false positives is a hook somebody turns off, and
then the boundary is gone along with the noise. The full sweep still owns everything else.

**The three traps, each asserted in [`tests/test_heron_guard.py`](../tests/test_heron_guard.py):**

| trap | what it costs | |
|---|---|---|
| a top-level `permissionDecision` | *"silently no-ops the block"* | the decision is **nested** |
| a hook that crashes | is read as **permission** | **a crash denies**, and says how to recover |
| polarity left to chance | *"a boundary that fails open is not a boundary"* | **deny-tier, fails closed** |

**And a fourth gstack ships that the question had not named:** `HERON_GUARD=off`. A fail-closed hook
that cannot be turned off is one bad edit from a repository nobody can work in, and the person who needs
the hatch is the one whose tooling is already broken.

**Two adaptations rather than a copy** ([D-25](DECISIONS.md)): **Python, not bash** — Heron is developed
on Windows, where a bash hook would not run at all — and the pattern is **built from parts**, because
`check-structure.py` greps file text and would otherwise fail the hook for containing the string it
exists to forbid. That is not hypothetical: a docstring in `heron_context.py` broke that rule the same
day, by quoting it in order to explain it.

**The rule now exists twice, and a test holds the copies together.** `check-structure.py` cannot be
imported — it runs its whole sweep at import — so the test asserts the hook's pattern is character for
character the one the sweep uses, the same answer `tests/test_fragment_imports.py` already gives for the
executor's import list.

**It is for developing Heron and is not part of what a modeller installs.** Hooks are the host's
mechanism ([D-01](DECISIONS.md)); nothing in it reaches a model, a fragment, or a user.

#### And the other 53 gstack skills, read in full — none transfers

All 54 were listed and read at description level, and the three promising ones opened:

| | |
|---|---|
| **`benchmark`** | *"Performance regression detection"* — page load, Core Web Vitals, Lighthouse, bundle size. **Heron has no web page.** But its principle is the one thing [`measure-brain.py`](../tools/measure-brain.py) lacks: a baseline nothing compares against is a number, not a check |
| **`learn`** | *"Review, search, prune and export what has been learned across sessions."* Heron holds this as [DECISIONS.md](DECISIONS.md), [OPEN-QUESTIONS.md](OPEN-QUESTIONS.md) and [D-54](DECISIONS.md) — *a message describing a gap must be corrected when the gap closes* — with `check-docs.py` enforcing the counts. **Already held, and enforced rather than remembered** |
| **`health`** | *"Wraps existing project tools."* That is [`heron_health.py`](../mcp/server/heron_health.py) and the gate set. **Already held** |

**The remaining 50 are a startup founder's workflow** — five iOS skills, four design reviews, seven
browser-automation skills, deploys, ship, retro, office hours, a CEO-mode plan review. Not one belongs
to a BIM modeller, and `ETHOS.md` measures its own value in *"10,000+ usable lines of code per day"*,
which is the developer-harness frame [D-57](DECISIONS.md) rejected outright.

### 2.10 ⏸ OWNER'S CALL — fact-forcing beats confirming, and pointing beats typing

**From** ECC's `gateguard-fact-force.js` — [Q-50](OPEN-QUESTIONS.md). *"Instead of asking 'are you sure?'
(which LLMs always answer 'yes'), this hook demands concrete facts. **The act of investigation creates
awareness that self-evaluation never did.**"* Heron already holds the stronger half — the preview
re-counts against the live model and refuses if the set moved. What it does not hold: **the modeller is
told, not shown.** ECC's `plan-canvas` opens the plan in a browser so the human points at the element;
**Heron's canvas is the model itself**, and `preview.Ids`, `preview.Skipped` and `set-selection` are all
already in memory when it asks.

### 2.11 ⏸ OWNER'S CALL — guard the path from retrieval into context

**From** [`ruflo`](https://github.com/ruvnet/ruflo)'s `agentdb-retrieval-guard.ts` —
[Q-51](OPEN-QUESTIONS.md), and **the most valuable single item the whole programme produced**, because it
lands on work not yet done. Scan retrieved chunks **before** they are assembled into a prompt; **flag
oversized chunks rather than truncating them**, because truncation lets a payload be padded past the
scanner's window. Heron enforces [Golden Rule 19](14-golden-rules.md) where it counts — no text can raise
a permission level — but **Heron is the carrier**, and every source it carries today is its own. The day
the RAG index exists is the day that stops being true, and [`heron_context.py`](../brain/heron_context.py)
says so in its own part list: `STANDARD = "standard"  # the clauses cited - source does not exist yet`.

### 2.12 ⏸ OWNER'S CALL — an inventory of what imported knowledge permits

**From** [`scientific-agent-skills`](https://github.com/K-Dense-AI/scientific-agent-skills) as the
failure and [Headroom](https://github.com/headroomlabs-ai/headroom) as the fix —
[Q-53](OPEN-QUESTIONS.md). One says *"MIT… use freely"* over four skills marked **all rights reserved**,
and its own scanner checks security and never looks at a licence. The other **generates a 330-row
licence inventory as a build artifact**. Heron's dependency inventory would be nearly empty; **the
inventory Heron needs is of its imported knowledge**, which [24 §7](24-trust-model.md) now records as
semi-trusted.

### 2.13 ❌ MEASURED AND REJECTED — a third retrieval stream, tried at six settings

**From** [`agentmemory`](https://github.com/rohitg00/agentmemory) and, decisively,
[`gbrain`](https://github.com/garrytan/gbrain) — [Q-52](OPEN-QUESTIONS.md). Heron fuses two streams.

**The first read said the obvious repair was probably wrong**, because agentmemory's graph widens recall
by expanding *entities in the query* while Heron's is a **composition** graph — *A provides what B
needs* — so fusing could let a strong helper outrank the fragment that actually answers.

**gbrain points the other way, with a number.** Its edges are *"extracted from entity refs with **zero
LLM calls**"* — [D-40](DECISIONS.md) word for word — and it reports **+31.4 points P@5** from the graph
stream over its own graph-disabled variant. **The closer analogue to Heron's graph is the one reporting
the large lift.**

**Take the direction, not the magnitude.** Their corpus is prose about people; Heron's is contracts.
**So it was run** — [`tools/measure-graph.py`](../tools/measure-graph.py), 360 questions whose answer is
each fragment's own declared sentence, across four query shapes.

**All six settings lost.** The gentlest cost 1.1 points of P@1 and the strongest 14; **P@5 never
improved at any of them**, so the graph did not widen recall either — the one thing it was supposed to
be good at.

**The reason is the corpus, and it is the thing that does not transfer:**

> neighbours per fragment: **median 50, worst 230**, none at all for 68 of them.

**gbrain's graph is sparse; Heron's is dense.** A page mentions three people. A fragment providing
`IList<Element>` composes with every fragment that needs one — most of the library. *"The neighbours of
the best hit"* is not a signal here; it is a large slice of the library added as competitors.

**This is the most useful shape a research finding can take.** A number from somebody else's corpus said
*probably yes*. Heron's own corpus said *no*, at every setting, and **named the property that decides
it** — density. Neither could have been reached by reasoning, and the second is only available because
the first was taken seriously enough to test.

### 2.14 ❌ REJECTED — the council, and the swarm

**Anonymised peer review** ([`llm-council`](https://github.com/karpathy/llm-council)) is N model calls
per question, which [19 §5](19-context-and-cost.md) exists to avoid — kept only for a future Shadow Mode,
and **sharpened**: their labels are assigned by position and never shuffled, so a candidate would always
be `Response B` and a position bias would become a systematic bias. **Anonymise *and* shuffle.**

**Swarm coordination and consensus** ([`ruflo`](https://github.com/ruvnet/ruflo)) answers *"which of my
disagreeing replicas is right"*. Heron has **one Revit, one pipe, one queue, one handler**
([D-09](DECISIONS.md)) and **14 MCP tools against their 314**.

### 2.15 BUILT - compare the finished diff against what the change said it was for

| | |
|---|---|
| **Where from** | [awesome-llm-apps](https://github.com/Shubhamsaboo/awesome-llm-apps), Apache-2.0 both at the repository root and in the skill's own front matter. Read at implementation time on 2026-09-11: the skill brief, its diff classifier and its signal reference |
| **Their mechanism** | A stated one-line intent is tokenised, each changed path is tokenised, and a file sharing at least one token is *in scope* while one sharing none is *likely creep*. Signals - a new dependency, a public rename, a config edit, an oversized hunk, a formatting-only file - are attached to the file that raised them, and every item gets **keep, split or justify** |
| **Why it works** | Not the scoring. The scoring is weak and its own reference says so: *"a cheap, deterministic proxy"*. What works is the **discipline of writing the intent down first**, and the refusal to treat a signal as a verdict |
| **What Heron already had** | Nothing. `check-structure.py` asks whether the repository is laid out correctly; nothing asked whether **this change** is |
| **What Heron built instead** | [`tools/check-change.py`](../tools/check-change.py). The classification is **structural, not lexical** - it imports the layering table out of `check-structure.py` and asks whether each file is in a declared part, in a part a declared part **may depend on**, or in neither. Word overlap would call `heron_retrieve.py` unrelated to *"fix retrieval"*; the layering table cannot |

**The `supporting` class is the whole gain, and it is only possible because Heron has the table.** An
intent declaring `mcp` and a diff touching `brain/` is supporting work, because `mcp` may depend on
`brain`. The same intent touching `revit/` is not. That distinction is invisible to any amount of
tokenising and it is exactly the one a reviewer needs.

**Their ten input fields became three.** [29 §4](29-metadata-standard.md)'s test - *would a script fail
the build over this field?* - leaves `intent`, `area` and `risk`. Whether a change needs a real Revit,
which releases it touches and whether a contract moved are all **derived from the diff**, and a derived
fact beats a declared one every time.

---

### 2.16 BUILT - a file no rule recognises widens the checks rather than passing quietly

| | |
|---|---|
| **Where from** | [OpenDesign](https://github.com/nexu-io/open-design), Apache-2.0. Its CI scope classifier and the rule contract that feeds it, read 2026-09-11 |
| **Their mechanism** | A declarative table maps path patterns to *effects* - which test lanes a change requires. A file matching **no** rule sets **every** effect true, and the decision carries a trace naming which rules matched which file and why each escalation happened. The contract is validated before it is used: unknown effects, duplicate ids, uncompilable regexes and reference cycles all fail loudly |
| **Why it works** | The default. Almost every classifier's unmatched case is "ignore"; theirs is "assume the worst", because an unrecognised path is precisely the case a classifier is least able to judge |
| **What Heron already had** | The opposite default in one place - `heron_fragment.load_all()` returns its problems rather than swallowing them ([D-48](DECISIONS.md)) - and nothing like it for a change |
| **What Heron built instead** | An unknown top-level folder is `unrelated` **even when the change declared it**, and an unusable intent exits 2 `BLOCKED` with nothing judged rather than producing a confident report from nonsense |

---

### 2.17 BUILT - check the thing that gets delivered, not the tree it was built from

| | |
|---|---|
| **Where from** | [OpenDesign](https://github.com/nexu-io/open-design), Apache-2.0. Its installed-acceptance step, read 2026-09-11 |
| **Their mechanism** | After a release is published, a separate check reads the **installed** manifest, compares it against the publish receipt, verifies a digest sidecar, drives the lifecycle start to stop, and emits an acceptance record naming exactly what was accepted |
| **Why it works** | Source tests and delivery are different questions, and only the second one reaches a user. Their check would fail on a manifest that no longer binds what was published even though every test passed |
| **What Heron already had** | **Nothing read `Heron.addin` at all** — not one tool in `tools/`, and it is the first file Revit opens |
| **What Heron built instead** | [`tools/check-package.py`](../tools/check-package.py), asking only what is answerable with no Windows, no Revit and no compiler - and **printing what it cannot answer on every run**, because a green run there is not an install |

**Four faults it catches that were invisible to everything else**: an entry class that does not exist or
is not an `IExternalApplication`; an assembly the project does not build; a `<ManifestSettings>` element,
which arrived at Revit 2026 and **crashes 2025 and older** while Heron ships one manifest to all eight
releases; and a manifest the deploy script's literal rewrite no longer matches, where `String.Replace`
does not fail - it silently installs a manifest pointing at the wrong path.

---

### 2.18 ADOPTED AS A RULE - mock only the thing you genuinely cannot have

| | |
|---|---|
| **Where from** | [OpenHands](https://github.com/OpenHands/OpenHands), MIT. Its mock-model end-to-end configuration and the scripted stand-in server behind it, read 2026-09-11. Its predecessor is archived and was **not** opened ([35 §5.1](35-independent-study-notes-open-design-awesome-llm-apps-openhands.md)) |
| **Their mechanism** | The end-to-end run starts the **real published entry point** - the same binary users install - and replaces exactly one thing: the model. The stand-in serves a fixed scripted trajectory whose replies carry recognisable marker tokens, and it can produce the real **failure** modes as well as the happy path |
| **Why it works** | Two properties, and the second is the one usually missing. The mock replaces the part that cannot be made deterministic and **nothing else**, so the test exercises the delivered artefact. And its output is *identifiable as a mock*, so a mocked result cannot be mistaken for a real one |
| **What Heron already had** | The stronger half already, and written down: [13 §3](13-testing-and-quality.md) says levels 1-4 run against a mocked Revit boundary and 5-7 need a real Revit, and [D-30](DECISIONS.md) refuses to call anything proven without a named model, a negative case and a fingerprint |
| **What Heron took** | The rule, stated where it can be acted on: [13 §3a](13-testing-and-quality.md) now carries the offline/real-Revit matrix as a table rather than as a recommendation, and the three suites added by this work each say in their own docstring what they do **not** prove |

**Heron's version of their line is harder, and it stays harder.** They mock a model; Heron would be
mocking *the thing the product exists to change*. A mocked Revit that returned plausible element counts
would produce green suites and no knowledge whatever, which is why nothing here does it.

---

### 2.19 ALREADY HELD, and given a second home - a finding carries the evidence that settles it

| | |
|---|---|
| **Where from** | [OpenDesign](https://github.com/nexu-io/open-design), Apache-2.0. Its artefact lint pass, read 2026-09-11 |
| **Their mechanism** | Deterministic text checks produce structured findings at three severities - must fix, should fix, nice to have. Each finding carries the snippet that raised it, and the set is rendered back to the agent so it can correct itself. Its own header accepts false positives *because* each finding carries its evidence |
| **Why it works** | Severity says what must happen; the snippet lets a reader settle it in one look instead of trusting the tool |
| **What Heron already had** | Both halves, more strictly. `check-revit-gate.py` has four verdicts about **evidence** rather than severity, and `check-routing.py` exits 0 on purpose because *"a tool that failed a build over two fragments both answering to 'grey the background' would teach people to weaken their own utterances to buy a green tick"* |
| **What was added** | The same habit in the new gates: every signal in `check-change.py` names the paths that raised it, and every packaging problem names the file and what Revit would do about it |

---

---

## 3. The pattern behind the patterns

Three of them taught the same lesson from different directions, and it is the one worth keeping:

> **A rule in code beats a rule in prose.**

- ECC ([33 §5.1](33-external-repository-research.md)) holds in **code** — a blocking hook — what Heron
  holds in prose: the gates exist and nothing runs them.
- `superpowers` ([33 §5.8](33-external-repository-research.md)) holds in **prose** — an emphatic
  instruction to a model — what Heron holds in code: a proof format a tool checks.
- gstack ([33 §5.7](33-external-repository-research.md)) supplies the sharpest form of it:
  *"**a boundary that fails open is not a boundary**."*

**§2.1 and §2.3 are that lesson applied to this repository's own work.** *Compression must never touch
the request* was true, tested, and written in a docstring. It is now a branch, and the code path that
would break it does not exist.

**And the gate proved the lesson on this very document's code**, within a minute of it being written:
the first draft of `_surface_tiers` carried a comment **explaining** that the file contains no Revit
vendor namespace — and quoting that namespace in order to explain it. `check-structure.py` failed the
build. **A comment asserting the file did not contain something, which contained it** —
[32 §8](32-master-architecture-reconciliation.md) already records that class twice, and this is the
third.

---

## 4. What none of this is

**None of it has been near Revit.** [D-30](DECISIONS.md) is untouched, no fragment status moves, and
the fragments below `PROVEN` have still never met a model — `python tools/check-gaps.py` for how many,
because that number changes and a typed one goes stale. **That is the critical path and every line above
runs on any machine at any time.**

**Nothing was adopted as code from any of them.** [D-25](DECISIONS.md). The three read in the
2026-09-11 round are Apache-2.0, Apache-2.0 and MIT at the exact paths inspected, and the licences are
recorded because a repository-level assumption is not a provenance check
([35 §3.7](35-independent-study-notes-open-design-awesome-llm-apps-openhands.md)).
