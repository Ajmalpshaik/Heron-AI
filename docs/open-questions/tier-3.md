# Open questions — Tier 3

> One section of [the register](../OPEN-QUESTIONS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Tier 3 — Needed soon

### ✅ Q-53 — What checks the licence of knowledge Heron imports, before Heron's users redistribute it? → **A gate that reads the files** *(asked and answered 2026-09-09)*

Found by reading [`K-Dense-AI/scientific-agent-skills`](https://github.com/K-Dense-AI/scientific-agent-skills)
at file level ([31 research](../31-research.md)), and it is not hypothetical — it is a live
example of the failure.

**Its README says the project is MIT and that you may *"modify, distribute, and use freely."* Four of
its 163 skills carry `© 2025 Anthropic, PBC. All rights reserved.`** A fifth is MIT under a different
copyright holder. Nothing on the landing page says so; only listing the licence files does. **Their own
skill scanner checks security and never looks at a licence.**

**Heron is walking into the same position.** [17](../17-open-source-and-distribution.md) publishes Heron
under Apache 2.0 ([D-08](../DECISIONS.md)); [09](../09-skills-and-fragments.md) plans **community packages**,
which [Golden Rule 19](../14-golden-rules.md) already names as a source Heron reads. So Heron will import
knowledge somebody else wrote, and Heron's users will redistribute what Heron ships.

**At the time this was asked, none of Heron's tools mentioned a licence.** `check-metadata.py` checks headers,
`check-structure.py` checks boundaries, `check-docs.py` checks claims. Nothing checks what an imported
package permits.

**Three shapes:**

1. **A declared field.** A package manifest names its licence, and `check-metadata.py` refuses one that
   does not — the same shape as the header rule that already works. Cheapest, and it trusts the
   declaration.
2. **A gate with an allow-list.** Only licences compatible with Apache 2.0 may be imported, checked by
   a tool. Stronger, and it needs a list somebody maintains.
3. **Nothing, and say so.** Imports are the user's responsibility, stated plainly in
   [17](../17-open-source-and-distribution.md) rather than left unsaid.

**What is not in doubt:** [Golden Rule 12](../14-golden-rules.md) already stops Heron publishing private
knowledge automatically. Nothing yet stops Heron *carrying* somebody else's knowledge under a licence
its users cannot honour, and the difference between those two is the whole question.

**Answer: A GATE, AND ITS ONE RULE IS READ THE FILES — 2026-09-09. See
[D-66](../DECISIONS.md).** [`tools/check-licence.py`](../../tools/check-licence.py), exits 1 on a finding.

**The rule comes straight from the failure.** Nobody lied: the top-level claim and the file-level truth
were different, and only listing the files showed it. So the tool walks every file of every fragment and
skill, finds the markers **in the content**, and compares them with what the unit declared. It keeps
**unmarked** apart from **clean** — *"no evidence of a problem"* and *"evidence of no problem"* are
different findings, and merging them is the tool that project already has.

**Today: 370 units, all clean, exit 0.** Which on its own proves nothing — so
[`tests/test_licence_check.py`](../../tests/test_licence_check.py) rebuilds the exact failure and checks
each marker is caught: a reservation of rights nested under a folder declaring `source: OFFICIAL`, an
incompatible name, a compatible one, and Heron's own copyright. **A licence checker that has only ever
seen a clean library is a plausible zero ([D-52](../DECISIONS.md)) wearing a gate's clothes.**

**It also locks a false positive.** The first pattern matched `rule.GetCriterion(c) as
PrimarySizeCriterion` and reported that fragment as somebody else's work. `(c)` now counts only next to
a year — **a licence tool that cries wolf is a tool somebody turns off**, and this one would have been
turned off over a cast.


---

### ✅ Q-52 — Should the composition graph become a third retrieval stream? → **No. Measured.** *(asked and answered 2026-09-09)*

Found by reading [`rohitg00/agentmemory`](https://github.com/rohitg00/agentmemory) at file level
([31 research](../31-research.md)). It fuses **three** streams — BM25, vector and a graph —
by weighted RRF at `RRF_K = 60`. [`heron_retrieve.py`](../../brain/heron_retrieve.py) fuses **two**, at the
same constant, chosen independently. [`heron_graph.py`](../../brain/heron_graph.py) exists and is not one
of them; its only production caller is [`check-gaps.py`](../../tools/check-gaps.py).

**The obvious repair is probably wrong, and that is why this is a question.** agentmemory's graph stream
expands *entities found in the query* — a recall widener, closer to Heron's keyword route than to
anything else. Heron's graph answers a different question: `composes_into` / `composes_from`, derived
from the contracts, *A provides what B needs*.

**That is a composition graph, not an entity graph.** Fusing it into retrieval mixes *"which fragment
answers this request"* with *"which fragment goes next to that one"*, and a strong helper could outrank
the fragment that actually answers — `set-selection` beating the filter whose output it selects.

**Three shapes:**

1. **Leave it.** Retrieval answers one question and the graph answers another, and keeping them apart
   is why neither is confused today.
2. **A third fused stream**, weighted low, so composition settles near-ties and cannot overturn a
   clearly better match — which is exactly the reasoning already written into
   [`heron_retrieve.py`](../../brain/heron_retrieve.py)'s status nudge.
3. **A second answer, not a fused one:** the best match is returned, and *what composes with it* is
   returned beside it, labelled. No ranking is touched and the modeller gets the next step named.

**This one can be measured rather than argued.** [`measure-routes.py`](../../tools/measure-routes.py) gives
the before, and [D-39](../DECISIONS.md) already requires a disagreement to be analysed rather than
counted.

**AND SOMEBODY HAS MEASURED IT — added 2026-09-09 from
[`garrytan/gbrain`](https://github.com/garrytan/gbrain)** ([31 research](../31-research.md)),
which the owner named the same day. It reports **+31.4 points P@5 from the graph stream** over its own
graph-disabled variant and over vector-only RAG, on a 240-page corpus.

**It also changes which comparison this question should have been making.** The caution above is about
`agentmemory`'s graph, which widens recall by expanding *entities in the query*. **gbrain's edges are
derived from references already in the content, deterministically, with no model call** — which is
[D-40](../DECISIONS.md) word for word, and what [`heron_graph.py`](../../brain/heron_graph.py) already does
from the contracts. **The closer analogue is the one reporting the large lift.**

**What does not transfer is the number.** Their corpus is prose about people and companies, where an
edge is a cross-reference between documents. Heron's is fragments, where an edge means *this one's
output fits that one's input*. **Take the direction as evidence and the magnitude as nothing.** Shape 2
or 3 above is now the more likely answer, and it is still a measurement to run rather than a decision to
take on somebody else's corpus.

**Answer — measured on 2026-09-09 with [`tools/measure-graph.py`](../../tools/measure-graph.py). It is
NO for shape 2, and the measurement does not touch shape 3.**

The answer key is each fragment's own `semantic-identity` — 360 questions whose right answer is known
because nobody wrote it to make retrieval look good. Four query shapes, three degraded on purpose,
because a graph can only help where the answer is **not** already first.

| shape | P@1 today | P@1 with the graph | |
|---|---|---|---|
| `exact` | **95.6%** | 94.4% | −1.1 |
| `no-first` | **93.1%** | 92.2% | −0.8 |
| `content` | **90.4%** | 89.0% | −1.4 |
| `half` | **70.8%** | 69.2% | −1.7 |

**Six settings were tried and all six lost** — weights 0.05, 0.10 and 0.30 against 1, 3 and 5 seeds.
The gentlest costs 1.1 points of P@1; the strongest costs 14. **P@5 never improved at any setting**, so
the graph did not even widen recall, which is the one thing it was supposed to be good at.

**And the reason is in the graph, not in the fusion:**

> neighbours per fragment: **median 50, worst 230**, and none at all for 68 of them.

**gbrain's graph is sparse and Heron's is dense.** A page mentions three people; a fragment providing
`IList<Element>` composes with every fragment that needs one — most of the library. So *"the neighbours
of the best hit"* is not a signal here, it is a large slice of the library added as competitors. **That
is what does not transfer, and it is a property of the corpus rather than of the idea.**

**Two honest limits on this number.** The vector route ran on the `lexical` backend, not the trained
one — which makes the baseline *weaker*, so a stronger baseline can only make the graph's job harder,
and the conclusion is robust in the direction that matters. And the answer key is easy: a real modeller
never types a fragment's declared sentence. **Both point the same way.** A re-run on the owner's PC now
costs about fifteen seconds.

**Shape 3 is untouched and still open as an idea** — returning *what composes with the best match*,
labelled, beside the answer rather than fused into the ranking. Nothing measured here bears on it,
because it changes no ranking. If it is ever wanted it is a separate question, not this one.

---

### ✅ Q-51 — What guards the path from retrieval to context, on the day Heron indexes text it did not write? → **Stamp it. Provenance, not scanning** *(asked 2026-09-09, answered 2026-09-20)*

Found by reading [`ruvnet/ruflo`](https://github.com/ruvnet/ruflo) at file level
([31 research](../31-research.md)). Its `agentdb-retrieval-guard.ts` scans every chunk
coming back from vector search **before** it is assembled into a prompt, and reports 93–100% undefended
attack success against poisoned memory entries.

**Heron already enforces [Golden Rule 19](../14-golden-rules.md) where it counts, and that half is done.**
Risk comes from the operation registry inside the add-in, never from the request — no text arriving on
the pipe can raise Heron's permission level.

**The half not yet faced.** Rule 19 governs what text may *authorise*; it says nothing about the text
itself travelling upward. Heron makes no model calls ([D-01](../DECISIONS.md), [D-58](../DECISIONS.md)), so
Heron cannot be injected — **Heron is the carrier**. [`heron_context.py`](../../brain/heron_context.py)
assembles parts, stamps each with `source`, and hands them to the host.

Today that is safe for a reason with an expiry date: **every source is Heron's own** — the request, the
fragment library, its tests, the API surface. The exception is already written into the part list:

```python
STANDARD = "standard"        # the clauses cited - source does not exist yet
```

**That source is exactly what the RAG work creates** — project standards, specifications, family
descriptions, imported packages. Retrieval becoming useful and Heron carrying text it did not write are
the same event, so the guard belongs in the design of the index rather than bolted on after it.

**Three shapes, and they are not equivalent:**

1. **Mark, do not scan.** Every part already carries `source`; a part from an indexed document is
   labelled untrusted and the host decides. Cheapest, honest, and it puts the judgement where
   [D-01](../DECISIONS.md) already puts the model.
2. **Scan and flag.** Heron screens retrieved text and annotates what looks like an instruction. Needs a
   pattern library Heron would have to own and keep current.
3. **Scan and refuse.** Strict mode. A false positive silently drops the clause a modeller needed, which
   is the plausible-zero failure this repository legislates against.

**One detail worth keeping whatever is chosen**, because it is not obvious: ruflo **flags oversized
chunks rather than truncating them** — truncating lets an attacker pad a payload past the scanner's own
window.

**What is not in doubt:** a retrieved clause is data. The question is only whether Heron says so, and to
whom.

**NOT NOW — and there is now a tripwire that says when. Owner's call, 2026-09-09.**

**The guard stays unbuilt, deliberately.** Every part Heron carries today comes from a source Heron
wrote: the caller's own request, the fragment library, its test cases, the executor's import list. A
scanner written against no corpus is a scanner written against a guess — and the same day supplied the
evidence rather than the opinion, when [`Q-52`](../OPEN-QUESTIONS.md)'s +31.4 from somebody else's corpus
lost at all six settings against Heron's own.

**What was built instead is [`tests/test_carried_sources.py`](../../tests/test_carried_sources.py)** — a
tripwire, not a guard. It asserts that **every part, on every path, at every depth, has a source that is
one of Heron's own labels or a file inside this repository.** 2,160 parts checked; all of them Heron's.

**It fires the day that stops being true**, and its failure message says so in as many words: *this is
probably not a bug, it is Q-51 becoming live — decide the guard now, with the index rather than after
it.* It also watches the two places the change would show first: the `STANDARDS` path still refusing for
want of a clause store, and `heron_context.py` still carrying the countdown comment
`STANDARD = "standard"  # the clauses cited - source does not exist yet`.

**Checked against what it will actually meet**, rather than assumed: a QCS clause reference, an imported
community package, a specification PDF on the user's disk, and text read out of a Revit model are all
classified **foreign**; the request, the capability, a fragment and the import list are all classified
**Heron's**. Eight of eight. **A tripwire that cannot trip is decoration.**

**It reads no part's body and looks for no pattern in one.** It cannot tell a safe clause from a hostile
one and does not pretend to. It answers one question — *is Heron still only carrying its own words?* —
which is the question whose answer changing is what makes this one urgent.

**So this question stays open on purpose**, and it is no longer something anybody has to remember.

**ANSWERED 2026-09-20 by the owner: shape 1, mark and do not scan.**

Every part carrying text Heron did not write is **stamped with its source**, and Heron **attributes
rather than asserts** - *"the supplier's datasheet says yes"*, never a bare *"yes"*. **No chunk scanner
is built.**

**The owner was given the drawing-office form of it and chose on that:** a stamp is
**FOR INFORMATION ONLY - NOT FOR CONSTRUCTION**. You still read the sheet; you know what it is and
whose it is. The worked example was a supplier datasheet whose own small print claims QCS approval,
asked back later as fact.

**Half of this already exists** - `heron_context.py` stamps each part with `source` - so what is owed
is the ATTRIBUTION half: the rule that a part stamped as somebody else's may not be spoken in Heron's
own voice. Shapes 2 and 3 are refused, not deferred.

---

### ✅ Q-54 — Is the cloud-embedding opt-in worth building, or is it a switch nobody will turn on? → **Build it, and split by CONTENT TYPE rather than by scope** *(asked 2026-09-10, answered 2026-09-20)*

**Carried in as `Q-D` from `docs/work-notes/plans/rag/03-working-note.md` §5 when that note was
retired.** It was the note's only home and a work note is deleted at the end of its life, so it comes
here rather than going with it.

[D-24](../DECISIONS.md) already says **local by default, cloud opt-in per scope** — and
[Q-11](tier-2.md#-q-11--local-or-cloud-embeddings) is where that was settled. **This is the sharper question
that survived it:** there is **no opt-in path and no per-scope setting**, so the choice D-24 describes
cannot currently be made at all. That is `R-30`.

Given [D-26](../DECISIONS.md) allows project content in the cloud, the opt-in may be worth building — or
it may be a switch with no user.

*Cheapest honest answer if unsure:* leave it unbuilt and **record that decision**, rather than building
a switch nobody turns on. An unbuilt setting that is written down is cheaper than a built one that is
not used, and either is better than a specification promising a choice the code cannot offer.

**ANSWERED 2026-09-20 by the owner - and the answer changes the SHAPE of the question.**

[D-24](../DECISIONS.md) frames the opt-in **per scope**. The owner's rule is **per content type**, and he
asked for the earlier position to be changed outright: *"in our previous decision also we need to
change - it was never give anything cloud."*

| | Cloud |
|---|---|
| **Documents** - PDFs, specs, standards, **and client project documents** | **May go.** Asked specifically about client tender documents and consultant drawing sets and he said yes, all of them |
| **Revit models, families, model data** | **Never leave the PC** |

**Two things he asked while deciding, answered at the time and recorded because the answers bound the
decision:**

**"More context means better answers and no hallucination - am I right?"** Half right, and the
counter-example is in this repository: [row 114](../FRAGMENT-ISSUES.md) is Heron telling him **twice**
there was no condensate drain in a model holding **63** such elements, with full model access. More
RELEVANT context reduces hallucination. It does not remove it, and more is not automatically better -
which is why retrieval takes the top ~20 and re-ranks rather than sending everything.

**"It will not go to GitHub - am I right?"** **Correct, and verified against `.gitignore` rather than
asserted.** Three layers: the store lives in `%APPDATA%\Heron\knowledge`, outside the repository;
the databases are ignored; and the SOURCE documents are blocked by extension - `*.pdf`, `*.docx`,
`*.xlsx`. The file's own comment says why it is permanent: *"A fork propagates and GitHub caches, so
this one is permanent if it happens once."*

**What he accepted as the cost:** everything in means the superseded revision goes in too, and Heron
will answer from it confidently. He was told this plainly before answering.

---

### ✅ Q-55 — May an INGEST read another scope? → **Yes — and it must carry the PRACTICE, not only the count** *(asked 2026-09-11, answered 2026-09-20)*

**Carried in as `Q-E` from `docs/work-notes/plans/rag/03-working-note.md` §5 when that note was
retired.**

[docs/20 §4](../20-knowledge-trust-and-conflict.md) asks for conflict detection **at write time** as well
as read time, with a good reason: resolving a conflict once, at ingest, is far cheaper than resolving
it on every query, and it stops the knowledge base accumulating contradictions in the first place.

**It cannot be built without answering a contractual question first.** At read time the crossing is
already authorised — a host asked one question of two named scopes, and `librarian()` opens each on its
own. **At write time nothing has been asked of anybody.** Finding a conflict at ingest means reading
ANOTHER scope while writing into this one, and [D-33](../DECISIONS.md) makes cross-scope a **contractual**
matter rather than a technical one.

*Worth saying out loud:* the read-time version already crosses, and deliberately keeps that crossing to
**a number and a clause number, never a clause**. A write-time version could hold to the same limit.
Whether it may happen at all is still the owner's call.

**ANSWERED 2026-09-20 by the owner - and he asked for MORE than this question offered.**

The question offered the read-time limit: **a number and a clause number, never a clause.** The owner
declined that ceiling. He wants Heron to **remind him with the actual prior practice, by project**:

> *"In Project A you used 700mm ceiling void. Do you want the same here?"*

So an INGEST **may** read another scope, and the crossing **carries the practice**, not only the count.
His words: *"it need to remember to me - in project one we did like this, so do you need to do like
that. AI need to tell me and remind me."*

**One reservation, put to him at the time and recorded rather than resolved:** this is safe because it
is HIM - he worked on both projects and already knows both, so Heron is reminding him of his own work.
**The day a second person uses Heron**, someone could be shown a client's practice having never worked
for that client. **Revisit on the first multi-user install**, not before.

---

### ✅ Q-56 — What actually contains a new agent while it runs? → **Nothing more than today — and the word "sandbox" goes** *(asked 2026-09-14, answered 2026-09-20)*

`HERON-AHR-SBX-016`'s row asks it to run a newly built agent "in isolation — never against a live
model, never able to write production knowledge". What is built runs the agent as ordinary Python **in
the supervising process**, and every rule it enforces goes through the `world` object it hands over.

**An agent that cooperates is held. An agent that does not is not held at all** — it can call `open()`,
import the bridge, reach into globals, or delete files, and nothing in the module is in its way. The
review that found this put it plainly: only agents that voluntarily route access through `world.write()`
or `world.revit()` are restricted.

That is most of the value and none of the guarantee. The accident — a new agent reaching for Revit
because nobody told it not to — is contained, recorded and reported, and that is what a first run
usually produces. A *hostile* or badly broken agent is not contained, and the word "sandbox" implies it
is.

**The question is which of these Heron is buying**, because they are different pieces of work:

| | |
|---|---|
| **Watch, and record what it tried** | what exists. Cheap, useful, honest if it says so |
| **Contain it** | a separate process with its ambient capabilities removed — no filesystem it was not given, no network, no imports it was not handed — and a real timeout, since the same subprocess is also the only way to stop a runaway agent |

**Why it is not just built:** an OS-isolated runner is a component in its own right, it has to work on
Windows where the modeller is, and half-doing it inside this module would leave something that *looks*
finished. [Golden Rule 11](../14-golden-rules.md) — untested code never touches a live model — is the rail
that matters most here, and it holds today because `write.enabled` is `false` and nothing generated has
ever run near a model.

*Cheapest honest answer if unsure:* say so in the module and the register row — **"watched, not
contained"** — and leave the subprocess until an agent is actually being generated by
`HERON-AHR-BLD-004`. The gap is written at the top of `brain/heron_sandbox.py` either way.

**ANSWERED 2026-09-20 by the owner: the cheapest honest answer, which this question itself proposed.**

**Watch, and record what it tried.** What exists stays; **no subprocess isolation is built.** It holds
only agents that cooperate, and that is exactly the honest accident a first run usually produces.

**And the word goes.** *"Sandbox"* must be replaced with **"watched, not contained"** in the module and
in the `HERON-AHR-SBX-016` register row. The owner chose the option whose whole point is that the name
stops promising what the code does not do - so the rename is the deliverable here, not a tidy-up.

---

### ✅ Q-50 — Should a preview select the elements it is about to change? → **Yes, both sets, capped at 500** *(asked and answered 2026-09-09)*

Found by reading [`affaan-m/ECC`](https://github.com/affaan-m/ECC) at file level
([31 research](../31-research.md)). Its `plan-canvas` skill opens a plan **in the human's
browser** so they can point at the element they mean instead of describing it, and its own reason is
that *"move this, change that"* is easier pointed at than typed.

**For a modeller the browser is the wrong canvas and the right one is already open.** Heron's preview
today is a sentence — *"would move 34 ducts up 200 mm in Tower-A, skipping 6."* Everything needed to
show it instead is already in memory: [`RevitWrite.cs`](../../revit/Heron.Revit.Addin/RevitWrite.cs) holds
`preview.Ids` and `preview.Skipped` when the preview is offered, and
[`set-selection`](../../brain/fragments/set-selection/) already exists.

**The second half is the better half.** Selecting the *skipped* set shows what Heron decided not to
touch — which is `Q-46` answered by showing rather than by wording, and no sentence carries it as well
as thirty highlighted ducts.

**Why it is a question and not a fix.** Three reasons, and each is a real objection:

1. It changes a **confirmation path**, and [Golden Rule 9](../14-golden-rules.md) says that path is gated
   in code, not improved on initiative.
2. Changing the selection **destroys the selection the modeller had**, which on a busy day is the thing
   they spent two minutes building. It may need to be offered rather than done.
3. A preview that highlights 4,000 elements is not a preview, it is a mess. There is a count above which
   showing is worse than telling, and nobody has said what it is.

**A second half arrived the same day**, from the index at
[31 research](../31-research.md): the **"approve with changes"** pattern — *"modifying
tool input before execution… the reference design for safe-by-default harnesses that don't simply block
or permit."*

**Heron's approval is binary on purpose**, and that stays: the token is minted for one preview, a
non-matching token is refused, and the set is re-counted against the live model before anything moves.
**But binary is about the token, not about the conversation.** A modeller who says *"yes, but 150 not
200"* starts the whole request again today. The Heron-shaped version is not executing something
modified — it is **taking the correction and producing a new preview at once**, guarantee intact. That
is a better question than "should the preview select things", and it may be the same answer.

**Answer: YES — 2026-09-09. See [D-60](../DECISIONS.md).** The owner: *"yes I think it's good."* Asked
where showing stops being better than telling, he set the number himself: **500**.

**The three objections are answered one at a time, because each was real.**

**1. [Golden Rule 9](../14-golden-rules.md) is not touched, and that is a fact about what changes rather
than a promise.** The confirmation path is the token: minted for one preview, refused when it does not
match, and the set re-counted against the live model before anything moves. **Selecting elements writes
nothing and mints nothing.** It changes what the modeller can see while deciding, which is the one thing
Rule 9 was never guarding. A preview that highlights and a preview that does not must accept and refuse
exactly the same tokens, and that is testable rather than asserted.

**2. The modeller's own selection is saved first and put back.** Nobody said this; it is the safe
default and it is recorded here as such so it can be overruled. A selection built over two minutes on a
busy federated model is work, and destroying it to show a preview trades one annoyance for a worse one.
So the current selection is captured before the preview highlights anything and restored when the
preview is declined or expires. **Accepting is the one case where it is not restored** — after a change
lands, the elements that changed are what the modeller wants selected.

**3. 500, and above it Heron tells instead of shows.** Roughly one busy MEP level. Above the cap the
preview stays the sentence it is today and says why: *"4,120 elements — too many to highlight."*
**The cap is a setting with 500 as its default**, not a constant, because the number that is a mess on a
laptop on site is not the number that is a mess on a workstation.

**Both sets are selected, and the skipped one is the half that earns this.** *"Would move 34 ducts up
200 mm in Tower-A, skipping 6"* — the 6 are the sentence nobody reads carefully. Thirty highlighted
ducts and six in a second colour is `Q-46` answered by showing instead of by wording. The cap applies to
each set on its own: a job that skips 4 out of 4,120 can still show the 4.

**The "approve with changes" half stays open, and stays out of this decision.** Taking *"yes, but 150
not 200"* and producing a new preview at once is a better question than this one and it is a different
change — the guarantee holds only because the token is minted fresh, so it is a new preview, never a
modified execution. It is not part of D-60 and it is not answered here.


---

### ✅ Q-49 — Should Heron's gates run automatically, or stay checks a person remembers? → **YES, as a hook in a skill** *(asked and answered 2026-09-09)*

Found by reading [`affaan-m/ECC`](https://github.com/affaan-m/ECC) at file level
([31 research](../31-research.md)). Its `hooks/hooks.json` registers **PreToolUse** hooks
that run before a tool does and can refuse it. **Heron has no `.claude/settings.json` and no hooks of
any kind.**

Heron holds the rules — `check-structure.py` refuses `Autodesk.Revit` outside `revit/`,
`check-metadata.py` refuses a file without a header, `check-docs.py` refuses a broken link — but every
one of them runs **only when a person types it**. This repository has already paid for that once: four
binding rules were described as proposals in `README.md` for eight days
([32 §4](../32-master-architecture-reconciliation.md)) because nothing checked the sentence on the day it
went stale.

**What is not in doubt:** the rules are right and they are cheap. What is in doubt is whether the
enforcement belongs to Heron at all — hooks are a **host** mechanism, and [D-01](../DECISIONS.md) gives the
host orchestration. A `.claude/settings.json` in this repository is a convenience for whoever develops
Heron; it is not part of what a modeller installs, and it must never be confused with one.

**Three shapes:**

1. **Nothing.** The gates are run before a push, and the discipline holds because the person is careful.
   It has held for 34 commits.
2. **A pre-commit hook**, which is git's and belongs to the repository rather than to any agent.
3. **A `.claude/settings.json` PreToolUse hook**, which is the host's and only helps whoever uses that
   host.
4. **The hook declared in a skill's own frontmatter** — found on 2026-09-09 in
   [`garrytan/gstack`](https://github.com/garrytan/gstack) ([31 research](../31-research.md)),
   whose `careful`, `freeze` and `guard` skills each carry their `PreToolUse` entry inline. **The guard
   installs with the capability**, so the two cannot drift apart, and
   [`.claude/skills/`](../../.claude/skills/) already exists here. This is the best of the four.

**If the answer is yes, three traps come with it**, each of which silently turns a hook into
decoration. All three are quoted from `freeze/bin/check-freeze.sh`, which learned them the hard way:

- **The decision must be nested under `hookSpecificOutput`.** *"Claude Code ignores a top-level
  `permissionDecision`, which silently no-ops the block."*
- **A hook that dies is read as permission.** *"Any unexpected non-zero death… would otherwise exit with
  no decision JSON, which Claude Code treats as non-blocking — the edit proceeds."* Their fix is an
  `EXIT` trap that emits a deny.
- **Polarity is a decision.** *"freeze is a DENY-tier hook, so an unreadable payload DENIES (fail
  closed)… **a boundary that fails open is not a boundary**."* Their `careful` is ask-tier and fails the
  other way, deliberately.

**Answer — the owner, 2026-09-09: *"yes add the hooks"*.** Shape 4, which was the recommendation.

**Built as [`.claude/skills/heron-guard/`](../../.claude/skills/heron-guard/SKILL.md)** — one hook, one
rule: the Revit vendor namespace outside `revit/` is **refused at the moment the edit is proposed**,
rather than when somebody remembers the sweep.

**One rule and not five, deliberately.** Not metadata headers, not links, not counts. **A hook with
false positives is a hook somebody turns off**, and then the boundary is gone along with the noise.
`check-structure.py` still runs the full sweep and still owns everything else it checks.

**All three traps honoured, and each is asserted in
[`tests/test_heron_guard.py`](../../tests/test_heron_guard.py):** the decision is nested under
`hookSpecificOutput`; **a crash denies** rather than being read as permission; the polarity is deny-tier
and fails closed. **Plus a fourth thing gstack also ships and the question did not name** — `HERON_GUARD=off`,
because a fail-closed hook that cannot be turned off is one bad edit from a repository nobody can work
in.

**Two adaptations rather than a copy** ([D-25](../DECISIONS.md)): it is **Python, not bash**, because Heron
is developed on Windows where a bash hook would simply not run; and the pattern is **built from parts**,
because `check-structure.py` greps file text and would otherwise fail the hook for containing the string
it exists to forbid — which is exactly how a docstring in `heron_context.py` broke the same rule the
same day.

**And it is for developing Heron. It is not part of what a modeller installs** — hooks are the host's
mechanism, [D-01](../DECISIONS.md) gives the host orchestration, and nothing in it reaches a model, a
fragment, or a user.

**2026-09-23 — the wiring moved from shape 4 to shape 3, and the answer stands.** A hook declared in a
skill's frontmatter is registered only when the skill is invoked, so the guard ran only in sessions that
had loaded `heron-guard` — proven on 2026-09-22, when the script refused a forbidden edit piped into it
by hand and the same edit made through the editor in a normal session went through. It is wired from a
committed [`.claude/settings.json`](../../.claude/settings.json) now, which every session reads, and the
skill declares no hook so it runs once. *Yes, the gates run automatically* is unchanged; only where the
hook is declared did.

---

### ✅ Q-48 — Should a fragment that READS look inside loaded links? → **Yes, when the modeller asks for it** *(asked and answered 2026-09-09)*

[`tools/check-revit-gate.py`](../../tools/check-revit-gate.py) asked question 8 of the fourteen — *are
linked documents handled correctly?* — of all 360 fragments. **62 of them collect from the host document,
declare a reading risk, and say nothing about links anywhere.**

**This is the one finding on that list that is about what a modeller sees rather than about code.** In
Qatar MEP work a model is federated as a matter of course: the architecture is a link, the structure is
a link, and frequently the MEP a coordinator is checking is a link too. A fragment that collects only
the host returns a **confident smaller number**, and nothing in the answer says a link was skipped.
[`HANDOVER.md`](../HANDOVER.md) has already hit this from the other side while proving — some cases have to
be **drawn in the host** because the model's content is linked.

**The 45 fragments that WRITE are correctly excluded and that is an API fact, not a judgement.** A
linked element belongs to another document and cannot be changed through this one; you would have to
open the linked file. So a writer collecting the host only is not under-reaching in the way a reader is.

**Why it is a question and not a fix.** Three answers, and they are genuinely different products:

1. **Per fragment.** Each declares whether it reads links. Honest, and it is 62 edits plus a rule for
   every fragment written afterwards.
2. **A platform rule.** Reading spans loaded links by default, and a fragment opts out. One decision,
   but it changes what **every existing recorded proof measured** — a count taken on the host is not the
   count the same fragment would return afterwards, so [D-30](../DECISIONS.md) fingerprints would be
   answering a different question than the one they were signed for.
3. **The caller chooses**, as an input the way [D-54](../DECISIONS.md) made a view an input. Most flexible,
   and it adds a need to 62 contracts.

**What is NOT in doubt:** an answer that silently omits linked elements is the plausible-zero failure
this repository already legislates against — the same shape as `FILTER_ELEMENTS_BY_CATEGORY` reporting
`unresolvedLevel` so a broken lookup cannot read as a clean count. Whatever is decided, **the answer has
to say whether links were included.**

`python tools/check-revit-gate.py --list links` names the 62.

**Independent evidence arrived on 2026-09-09**, from
[`tirth8205/code-review-graph`](https://github.com/tirth8205/code-review-graph) read at file level
([31 research](../31-research.md)). Its `uncertainty.py` exists because *"a bare
`result_count: 0` is ambiguous — it can mean **this graph cannot see that relationship**."* Same failure,
different industry, and they reached it with no knowledge of Heron. A fragment that collects the host
and stays silent about links is that sentence exactly.

**Answer: THE MODELLER DECIDES, JOB BY JOB — 2026-09-09. See [D-59](../DECISIONS.md).** Reading 3,
in the owner's own words: *"modeller will decide and he know if need to check from linked model or this
model, they know."*

**That rejects reading 2 for the reason the question already gave.** A platform rule that spans links by
default would change what every recorded proof measured — a count taken on the host is not the count the
same fragment returns afterwards — so every [D-30](../DECISIONS.md) fingerprint would be answering a
question it was never signed for. Nothing is re-proved to add a feature.

**And it rejects reading 1, which is the less obvious half.** Per-fragment declaration is honest but it
takes the choice away from the person who has the model open. The same fragment is wanted both ways on
different days: *how many ducts on Level 2* is a host question when the modeller is checking their own
work and a federated one when they are checking a coordination issue. A fragment that decided once
cannot serve both, and the modeller would have to know which of two fragments to ask for — which is
exactly the knowledge [09](../09-skills-and-fragments.md) exists to stop them needing.

**So it arrives as an input, the way [D-54](../DECISIONS.md) made a view an input.** Two fields, and the
second is not optional:

| Field | Where | What it is |
|---|---|---|
| `includeLinks` | `needs`, `source: request` | The modeller's call. **Absent means host only** — the behaviour every existing proof measured |
| `linksSearched` | `provides` | How many linked documents were actually read. **0 with `includeLinks` set means "you asked, none are loaded"**, which is [D-52](../DECISIONS.md)'s rule again |

**The second field is the part that answers the question's own warning.** *"Whatever is decided, the
answer has to say whether links were included."* A boolean echo of the input would not say it — it would
repeat what was asked for, not what happened. A count says both.


---

### ✅ Q-47 — There are two capability-gap paths and only one of them can ever fire → **Wire it up, for the missing case only** *(asked and answered 2026-09-09)*

[`tools/check-reachable.py`](../../tools/check-reachable.py) found it, and the shape is `Q-43`'s exactly:
**`heron_capability.want()` is called from two tests and from no production code.**

It is the only writer of the `capabilities_wanted` table. Its own docstring says what that costs:

> This is what turns *"we have no fragment for that"* from a **silence** into a **finding**.

So the table is always empty, and `heron_capability.gaps(store)` can only ever return what its caller
passes in `required=`.

**The live path is a different one, and it works.** `heron_brain.catalogue()` computes gaps from the
**skills' own declared `missing` capabilities**, never from `capabilities_wanted`. That is what
`heron_capabilities` reports and what `tests/test_brain_reachable.py` checks, and it is sound —
this entry originally suspected that test of passing for the wrong reason, and it does not.

**So the question is which path is the real one.** Two mechanisms answer *"what does somebody need
that nobody provides"*, one derived from skills and one stored on request. [D-40](../DECISIONS.md) is the
rule they are measured against: *an edge is derived before it is stored; store one only when it cannot
be computed from an artifact on demand.* A skill's requirement is computable and is computed. A
**request** for something no skill declares is not computable from any artifact — which is the case
`want()` exists for, and the one that never happens because nothing calls it.

Three ways out, and they are genuinely different:

1. **Wire `want()` up** — a lookup that resolves to nothing records what was asked for. That makes the
   stored half real, and makes `heron_gaps`'s *"things Heron could not do"* answerable from wants as
   well as from failures.
2. **Delete it** — accept that the derived path is the whole answer and stop carrying a table nothing
   fills. Cheapest, and it loses the case above.
3. **Leave it** — and then it should be written down as deliberate, the way the Workflow Engine's
   is in [`HANDOVER.md`](../HANDOVER.md), so the next tool that finds it does not raise it again.

**Answer: WIRE IT UP, AND ONLY FOR THE CASE THAT IS GENUINELY MISSING — 2026-09-09. See
[D-63](../DECISIONS.md).** Reading 1, at `heron_brain.resolve()`.

**[D-40](../DECISIONS.md) points at it rather than away.** A skill's requirement is computable and is
computed. A request for a capability **no artifact declares** cannot be derived by any pass over the
library, because it is not in the library — only somebody asking reveals it. That is the case `want()`
exists for.

**The distinction that makes this one line and not a judgement call was already in that function:**

| What `resolve()` found | Recorded? |
|---|---|
| A provider exists, this release is not on its list | **No** |
| Nobody provides it at all | **Yes** |

**The first row is the important half.** A version wall is not a missing capability. Recording it would
put something on the gap report **that already exists**, and Agent HR would commission a fragment to
build it twice — precisely the failure `heron_gaps.py` was corrected for, where the loudest error in the
trail is the executor behaving correctly.

**What is recorded is Heron's own capability name, never the user's sentence.** And a failed *lookup* is
still not recorded: it has no capability name, only words, and putting a sentence in that column would
make it a different table wearing the same name.


---

### ✅ Q-44 — Should the brain record its own answers in the audit trail? → **Yes, in its own file** *(asked and answered 2026-09-09)*

**The trail only knows what reached Revit.** `HeronAudit` is C# in the add-in, so a request answered
entirely by the brain — `heron_resolve`, `heron_lookup`, `heron_capabilities` — leaves **no record at
all**. [`tools/measure-routes.py`](../../tools/measure-routes.py) can therefore report the STRUCTURAL route
share (the library asked its own phrasings) and never the LIVE one (what people actually typed), which
is the only half that says anything about real use.

[19 §7](../19-context-and-cost.md) asks for exactly this and names where it should go:

> written into the same audit log rather than a separate telemetry system — one append-only record,
> many readers.

**Why it is a question.** That file is written by C# in the add-in and would then also be written by
Python in the MCP server — **two processes appending to one file that is deliberately never pruned**
([`HeronAudit`](../../platform/Heron.Core/HeronAudit.cs)). Interleaved writes, a partial line, and a
reader that must survive both are a design, not a patch. The alternatives are a second file the readers
join (two homes for one fact) or nothing (and the LIVE half stays unmeasurable for ever).

[Golden Rule 14](../14-golden-rules.md) — *every important autonomous operation must be auditable* — points
at doing it. Deciding **which fragment answers a request** is not a small operation.

**Answer: YES, AND THE ANSWER WAS ALREADY IN THE READER — 2026-09-09. See [D-62](../DECISIONS.md).**

**One file per writer, in one directory, merged at read time.** `heron_gaps.read()` globs the audit
directory for `audit-*.jsonl`, skips a bad line rather than dying on it, and **sorts every entry by
`at`**. It has always merged files and does not care how many there are or who wrote them.
`audit-brain-YYYYMM.jsonl` matches that glob, so **nothing downstream changed** — not the reader, not
[`heron-backup.py`](../../tools/heron-backup.py), not `heron_validate.py`, and not one line of C#.

**That is not the "second file the readers join" the question warned about.** There is one home, the
directory. Two files are how two processes write into it with no shared lock, which is the problem that
made this a question rather than a patch.

**What is never written there: the user's sentence.** The live route share needs the route and whether
it resolved. It does not need the words, and this file is append-only and never pruned
([12 §5](../12-security-and-permissions.md)). The wording lives in the utterance cache instead — a local store,
a different lifetime, and one a person can delete without losing the trail.

**A refusal is recorded as `ok: false` with its reason**, because a trail holding only the successes
makes a capability nobody provides look like one nobody asked for — and that is exactly the line the
Capability Gap Agent reads.

**It cannot break a request.** `_audit()` returns a no-op if the module will not import, and `record()`
returns False rather than raising when there is nowhere to write — which on Linux with no `APPDATA` is
every call. A logger that can break the request it was only supposed to describe has the priority
backwards.

**It does not claim `HERON-MCP-LOG-010`.** That row says *keyed by Workflow ID*, and every line goes out
with an empty `workflow`. [D-58](../DECISIONS.md)'s precedent, in the same words.


---

### ✅ Q-45 — Do the Model Router and Fallback belong in Heron at all? → **The degraded rule stays, the routing goes** *(asked and answered 2026-09-09)*

[19 §3](../19-context-and-cost.md) and [19 §4](../19-context-and-cost.md) specify a Model Router (task →
model class) and a Fallback (provider unreachable → route elsewhere, and **mark the result degraded**).
Neither exists.

[D-58](../DECISIONS.md) has just established that **Heron makes no model calls** — [D-01](../DECISIONS.md)
puts every one of them in the host, and `heron_embed` runs locally with no tokens and no cost. On that
reasoning a router here would route nothing.

**But not all of it is the host's.** *"Mark the result degraded, so it never counts as evidence toward
promotion"* is a **trust** rule, and trust is Heron's ([24](../24-trust-model.md), [D-30](../DECISIONS.md)).
`HERON-KRN-MAV-017` in [28](../28-agent-registry.md) carries exactly that clause.

So the question is a split, not a yes or no: **does Heron keep the degraded-result rule and hand the
routing to the host, or do both sections retire?** Answering it decides whether two specified
components get built or struck — and leaving it open is how a specification quietly accumulates
sections nobody will ever implement.

**Answer: THE SPLIT — 2026-09-09. See [D-65](../DECISIONS.md).** Routing goes to the host, the
degraded-result rule stays with Heron.

**The kept clause is not a leftover; it is the only one of the three that was ever Heron's.** Choosing a
model class and re-routing round a dead provider are operational choices about a resource Heron does not
hold. *"Never counts as evidence toward promotion"* is a statement about **what may be believed**, and
belief is what [24](../24-trust-model.md) and [D-30](../DECISIONS.md) govern. `HERON-KRN-MAV-017` keeps that
clause and loses the rest.

**Why a degraded run cannot promote anything.** [D-30](../DECISIONS.md) needs a positive case, a negative
case, a named model and a staleness fingerprint. A result produced after a provider fell over is missing
the one thing a proof is for — the guarantee that what ran is what was meant to run. Promoting on it
would make the lifecycle decorative, which is the same sentence [`heron_search.py`](../../brain/heron_search.py)
uses about running a DRAFT fragment off an exact word match.

**[19 §3–§4](../19-context-and-cost.md) are marked as the host's rather than struck.** A reader who finds
a section missing re-specifies it. A specification that quietly loses a section teaches nothing; one
that says who owns it teaches the boundary — [D-58](../DECISIONS.md)'s lesson, on a second row.


---

### ✅ Q-46 — Is "reports nothing it turned down" a defect class or a design spread? → **A rule, fired only on the empty answer** *(asked and answered 2026-09-09)*

[`tools/check-revit-gate.py`](../../tools/check-revit-gate.py) asks question 14 of the fourteen — *is
rollback/error reporting clear?* — and **143 of 360 fragments name nothing they refused, skipped or
could not resolve.**

[D-52](../DECISIONS.md) is the rule it is measured against: *a count of what was turned down is not a count
of what was found.* And the library's own best fragments treat it as load-bearing —
`FILTER_ELEMENTS_BY_CATEGORY` reports `unresolvedLevel` precisely so a broken level lookup reads as
*"12 found, 12 with no level"* instead of as a plausible zero.

**143 was too many to be a defect list and too many to dismiss**, so the rule was looked for in the
library's own practice rather than reasoned about — and it is there, clearly:

| | reports a refusal | silent |
|---|---|---|
| a fragment that **WRITES** | **172** | 30 |
| a fragment that **READS** | 45 | **113** |

**85% against 28%.** The norm exists and it is not uniform: naming what you refused is already what a
writing fragment does. Reading is where the silence lives, and reading is where the plausible zero does
too.

**So the question was narrowed to the shape [D-52](../DECISIONS.md) is actually about — a fragment that
GOES LOOKING and can DROP something on the way — and it is now 59.** One that counts a list it was
handed cannot skip anything, however silent it is.

**A worked example from those 59, and it is not hypothetical.** `filter-elements-by-type` returns
`found: 0` when its exemplar has no type, and **nothing in the answer separates that from "there are
none of this type."** That is exactly why `FILTER_ELEMENTS_BY_CATEGORY` reports `unresolvedLevel` — so
a broken lookup reads as *"12 found, 12 with no level"* rather than as a plausible zero. One fragment in
the library already solved this; 59 have not.

**What is left to decide is the rule, not the list:** must every fragment that collects and drops name
what it dropped, and does that become a validator rule the way the contract's shape is
([`heron_fragment.py`](../../brain/heron_fragment.py)), or guidance? A validator rule means 59 edits and
every future fragment held to it.

`python tools/check-revit-gate.py --list reporting` names them.

**A cheaper answer arrived on 2026-09-09**, from
[`tirth8205/code-review-graph`](https://github.com/tirth8205/code-review-graph) read at file level
([31 research](../31-research.md)). The reason to hesitate here is cost — every fragment
naming what it turned down makes every answer longer. **They measured it the other way round:**

> One short sentence on the empty case is therefore a **token saving, not a cost**: it replaces a
> multi-thousand-token fallback search with roughly thirty tokens of honesty.

And three choices make that hold, all three available here:

1. **Attach the marker only when the result is empty** — so *"every response that carries results stays
   byte-identical to before."* Nothing already working gets longer.
2. **Hard-cap it.** Honesty with a ceiling cannot grow into a paragraph.
3. **Keep the blind-spot list as data, not conditionals**, and delete an entry when the gap closes —
   which is [D-54](../DECISIONS.md) built into the structure instead of relied on as a discipline.

**So the choice above may not be "59 edits or guidance".** A third shape exists: one rule that fires
only on the empty case, capped, applied once.

**Answer: A RULE, AND THE THIRD SHAPE — 2026-09-09. See [D-64](../DECISIONS.md).** Not 59 hand
edits and not guidance.

**The dropped-count is a declared output**, and *that one exists* is the rule — never that a particular
word does. `unresolvedLevel`, `skipped`, `refused` all satisfy it, because what was dropped varies by
fragment. A rule about the word would be a naming convention wearing a correctness rule's clothes.

**The marker rides only on the empty answer**, so every answer that carries results stays byte-identical
to today. Nothing that already works gets longer, which is what made the cost objection dissolve rather
than be traded off. **Capped**, so honesty cannot grow into a paragraph.

**It is checked today and validated later, and that order is deliberate.**
[`check-revit-gate.py`](../../tools/check-revit-gate.py)'s question 14 now reads the **contract** as well
as the code — declare a dropped-count in `provides` and it passes even where the code reads silent. The
59 are a checked worklist and every fragment written afterwards meets the same question on the same run.
Putting it in [`heron_fragment.validate()`](../../brain/heron_fragment.py) today would make 59 fragments
invalid and fail every gate in the repository on a library that is not broken. **`validate()` is where
it moves once the 59 are cleared, and moving it is how the worklist is declared finished.**


---

### ✅ Q-43 — What may be written into the utterance cache, and by what? → **Only a run that came back** *(asked and answered 2026-09-09)*

**Found by building, not by reading.** [`tools/measure-routes.py`](../../tools/measure-routes.py) parses
the tree for real calls to `heron_search.remember()` — the only function that writes the utterance
cache — and finds exactly one, in [`tests/test_search.py`](../../tests/test_search.py). **No production
code calls it.** Not `ask()`, not `find()`, not any MCP tool.

So route 2 can never fire for a real user. The cache is built, tested, indexed, and permanently empty.

That matters because of where the specification puts it. [19 §5](../19-context-and-cost.md) makes it
**step 1** of the pipeline that must run before any model is invoked, and [19 §6](../19-context-and-cost.md)
calls it *"the one that pays for itself faster than any of the others"*.

**Why this is a question and not a patch.** The obvious wiring — have `find()` call `remember()` with
whatever it just returned — is the dangerous one:

| | |
|---|---|
| A keyword answer is a **candidate**, not a decision | `heron_search.py` says so in its own words: *"a candidate, never a decision"* |
| Caching it makes the guess **permanent** | the next identical wording returns by route 2 and **never searches at all** |
| The wrong answer then looks like the fast answer | which is the confident-wrong-retrieval failure [05 §4](../05-heron-brain.md) built the keyword layer to avoid |

**So the real question is what counts as confirmed.** Candidates, none of them obviously right:

1. **The fragment actually ran and returned a result.** Strongest evidence, and it only exists after
   execution — so nothing is cached for a request that was merely resolved.
2. **The user accepted the candidate** — but [D-01](../DECISIONS.md) puts that conversation in the host,
   which does not call back into the brain today.
3. **The identity route resolved it** — safe, and worthless: those already answer in one lookup, so
   caching them saves nothing.

**Also unanswered: what invalidates it.** [19 §6](../19-context-and-cost.md) names `SkillCreated`,
`FragmentApproved` and project change. `recall()` already deletes a row whose fragment has vanished,
which is the *deleted* case only — a fragment that is **edited** keeps its id and its stale cache entry.

**Answer: A COMPLETED RUN, AND NOTHING ELSE — 2026-09-09. See [D-61](../DECISIONS.md).** Candidate 1,
and the other two fail on their own terms rather than on preference: **the user accepted it** is a rule
nothing can evaluate, because [D-01](../DECISIONS.md) puts that conversation in the host and the host does
not call back; **the identity route resolved it** is safe and worthless, because those already answer in
one lookup.

**The decision is in the signature, not in this paragraph.** `remember(store, text, fragment_id,
evidence)`, and **`evidence` has no default**. A default would make the safe call and the dangerous call
look identical at the call site, and the dangerous one is what somebody reaches for while wiring this up
in a hurry. Anything but `RAN` raises `NotEvidence`. Six values were tried in
[`tests/test_search.py`](../../tests/test_search.py) — `keywords`, `hybrid`, `identity`, `cache`, `None`,
`""` — and none of them wrote a row.

**Staleness is answered with [D-30](../DECISIONS.md)'s own hash rather than a second mechanism.** The row
carries the fragment's `fingerprint()` from the moment it was written, and `index()` calls
`forget_stale()`. Re-indexing is when Heron re-reads the files, so re-indexing is when a row written
against the old bytes dies. A cached wording and a recorded proof go out of date for the same reason and
must not be able to disagree about when. The check is deliberately **not** in `recall()`: route 2 is the
fast route, and hashing implementation files on it would spend the saving it exists to make.

**The cache is still empty, and the reason changed.** The evidence only exists after a run, a run
happens in the add-in, and **no workflow id crosses the seam into the brain** — so nothing can join a
wording to a run that succeeded. [`measure-routes.py`](../../tools/measure-routes.py) was corrected in the
same change per [D-54](../DECISIONS.md): it used to say *"where remember() should be called from is a real
decision"*, and that decision is now made. **One seam remains and it is named.**


---

### 🟡 Q-16 — Which existing repositories are imported first?

`AJ-Tools`, `PyRevit-Tools`, `AEB-Tools` — which are in scope for the first knowledge import, and roughly
how many tools/fragments do they hold?

Note: some are private and may contain client-specific work. Anything imported must be reviewed before it
can reach a public repository.

→ [10 §5](../10-memory-and-knowledge.md)

**Answer: all of them, as reference — and none of them is imported. See [D-25](../DECISIONS.md).**
Ajmal, 2026-08-28: *"use them as a reference only ... In our Heron AI, it should be written completely
from scratch ... study each and every line, word by word, and create it as a new file."*

This answers a different question than the one asked, and the difference matters: there is **no import
pipeline to build**, and the duplicate detection an import would have needed largely goes with it. The
question of *which first* dissolves — reading is cheap and carries no risk; only re-authoring costs
anything, and that is decided one capability at a time.

The confidentiality note above is resolved by the same decision rather than by review: nothing is copied,
so no client-specific content can arrive by being carried across.

**Answer:**

---

### 🟡 Q-17 — Interface language

English only, or does Heron need to understand instructions in other languages used on site?

**Answer: Heron's own wording is English; understanding the user is not Heron's job at all. See
[D-34](../DECISIONS.md).**

Two questions here, not one. Asked the first, Ajmal chose **English only for now** — and declined the
*"built ready for Arabic"* option, so no translation scaffolding is written either. The cost of adding it
later is a pass over every user-facing message, and that is the accepted price rather than a hidden one.

**The second half answers itself.** Heron never interprets language: that happens in the host before
Heron is called ([D-01](../DECISIONS.md)). A request in Arabic, in mixed Arabic and English, or dictated
roughly, already works. So Heron builds no phrase list and no parser for near-misses — that would be a
worse copy of something the host already does, needing maintenance forever.

A **site word that means a Revit word** is a third thing and is not a language problem: it is knowledge,
it belongs in the knowledge store, and Phase 2 owns it. Meanwhile an unfamiliar term is a **question**,
never a quiet reinterpretation — [D-33](../DECISIONS.md).

**Measured 2026-11-26, and nothing was built.** A list of the modeller's own short forms was tried, read
before the search. It added each recorded long form after his words: the owner's *VCD*, *AHU* and *FCU*
from his approved site words. On `lexical` it lost. His 79 fell from 34 first to 30, and the questions
handed a change rose from 7 to 10. A long form names the equipment, so it pulls the cards named after
that equipment above the generic action card. The numbers, the variant tried and the follow-ups not
tried are in [`brain/retrieval-history.md`](../../brain/retrieval-history.md), section *2026-11-26 —
short forms added to a request before the search*. This leaves D-34 as it stands: a site word is still
knowledge for the modeller's own store, and how such knowledge should reach the search is still open.

---

### 🟡 Q-18 — Can community packages contain executable code?

Installing a package means running third-party code inside Revit, inside the user's project. Signing,
source allowlist, version pinning — or declarative skills/fragments only, no executables?

Now a real security question rather than a hypothetical one, since anyone can publish.

→ [06 §10](../06-heron-platform.md)

**Answer: yes, code is allowed — and an unapproved fragment is REFUSED, not warned about. See
[D-35](../DECISIONS.md).** Ajmal chose *"yes, but only after review and approval"*.

Under [D-28](../DECISIONS.md) a fragment is C# compiled and run inside Revit, so a shared fragment is
executable code by construction — this was never a hypothetical.

**The decision is written around the way that choice fails, not around the way it works.** A gate that
depends on somebody remembering to look decays: submissions outpace reading, a backlog forms, and
*approved* quietly comes to mean *nobody objected*. That is the option he rejected, reached by drift. So
there is **no warning dialog** — a warning hands the decision to the person least able to judge it and
most likely to click through. Unapproved does not run.

**Approval and proof are the same gate**, which is what makes the reviewer's job finite: the record
required is the one [D-30](../DECISIONS.md) already demands, and a submission without a negative case is
returned rather than reviewed.

**One thing must be built now:** the fragment format carries an approval record from its first version.
Retrofitting provenance into a format already in use touches every file.

---

### 🟡 Q-20 — What is the v1 definition of done?

*Recommendation:* "select all ducts" and "move them 200 mm up" working end-to-end, on one Revit version,
with undo, audit log and a preview — and nothing else.

→ [ROADMAP.md](../ROADMAP.md)

**Answer: v1 ships with both — it answers questions AND can change the model. See
[D-32](../DECISIONS.md).** Asked which job he wanted first, Ajmal chose **answering questions about the
model**. Asked whether v1 must also change things, he answered **"it must change things too"**.

Those are not in conflict: **first-to-use and finished are different things.** Answering questions is the
daily work and the half already proven, so it is what gets used first — but a Heron that cannot change
anything is a report tool, not the product.

So the recommendation above stands after all, widened: v1 is select-and-move *plus* the questions. Writing
stays **off by default** ([D-19](../DECISIONS.md)), which is about the setting a user turns on, not about
whether the capability ships.

**Recorded as read-only first and reversed within the hour** — see D-32, where the reversal is kept in
view rather than tidied away.

---

### ✅ Q-59 — Are `EXPORT_MODEL_TO_IFC` and `EXPORT_VIEWS_TO_DWG` PUBLISH, so that they need the Publish switch? → **Both PUBLISH** *(found 2026-09-19, asked and answered 2026-09-28)*

**Two fragments write a file out of the model and declare `risk: MODIFY`:**
[`export-model-to-ifc`](../../brain/fragments/export-model-to-ifc/fragment.yaml) and
[`export-views-to-dwg`](../../brain/fragments/export-views-to-dwg/fragment.yaml). Every other export in the
library declares `PUBLISH`.

**Since [D-106](../DECISIONS.md) that difference decides which switch guards them.** A `PUBLISH` fragment
needs the owner's Publish switch as well as Changes; a `MODIFY` one needs Changes alone. So with Changes on,
a chat can export IFC or DWG with Publish off - and a batch walking the library alphabetically can reach
both, which is the one thing the batch guard exists to prevent for `export-*`.
[`tests/test_admin_publish.py`](../../tests/test_admin_publish.py) names the two, so a third cannot join
them unnoticed.

**Found in [FRAGMENT-ISSUES section 6](../fragment-issues/section-6.md) on 2026-09-19**, which said *"one of
the two is mis-declared"* - comparing `export-model-to-ifc` (`MODIFY`) with `export-model-to-nwc`
(`PUBLISH`), neither of which changes the model - and that the question was owed here. It was never
written until now. [Row 5b-254](../FRAGMENT-ISSUES.md) records it again.

**Both sides.** `MODIFY` reads as *changes the model*, and neither fragment does. `PUBLISH` is what
[docs/12 §1](../12-security-and-permissions.md) calls *leaves the machine - export*, and it is what the
Publish switch now covers. Nothing written anywhere argues for `MODIFY`. **Recommendation: declare both
`PUBLISH`** - one line in each card, and both are DRAFT, so no proof moves. Not done in D-106's change,
because which level an operation carries is the owner's decision, not a tidy-up. **Ajmal resolves it.**

**Answer: BOTH `PUBLISH` — 2026-09-28. See [D-107](../DECISIONS.md).** The owner: *"make both exports
PUBLISH for Q-59"*. One line in each card, and both were DRAFT, so no proof moved. Every export in the
library now needs the Publish switch as well as Changes, no batch reaches either of them, and
[`tests/test_admin_publish.py`](../../tests/test_admin_publish.py) names no exception any more.

---

### ✅ Q-92 — When a proof was taken on another Heron add-in build or another Revit build, should a change from that fragment still be kept, or refused until it is proved again? → **Kept, and said - shape 1** *(found 2026-11-16, asked and answered 2026-11-16)*

**What happened.** A proof now records which add-in and which Revit build it was taken on - `built_on:`,
beside the fingerprint ([`heron_fragment.py`](../../brain/heron_fragment.py) `BUILT_ON_FIELDS`). When
either moves, the proof is **stale for that build**: `heron_validate.py draft` says so, and `revit_read`,
`revit_show` and `revit_change` add one line under the answer. It came from idea #10 of the research note on
[Ajmalpshaik/Heron-AI#877](https://github.com/Ajmalpshaik/Heron-AI/pull/877).

**What it does not do.** [D-117](../decisions/D-117.md) refuses to keep a change from a fragment whose proof
no longer describes its CODE - the fingerprint, read from the card alone. A proof on another build is
**not** refused there: it is a flag. Every proof signed before 2026-11-16 records no build and is NOT
RECORDED, so today nothing changes for any of them.

**The question, in a modeller's words: after Heron's add-in is updated, should a tool proved on the old
add-in still change the model from a chat?**

1. **Yes - say it, do not refuse,** as it now does. **Cost:** a change is kept on a build nobody proved it
   on; the line under the answer is all that says so.
2. **No - refuse a kept change until it is proved again on the new build.** **Cost:** every add-in
   update stops every proven change that recorded a build until each is re-proved - a new build hashes
   differently even when nothing it runs moved.
3. **Refuse only when Revit's build moved, flag when only the add-in did.** **Cost:** two rules to
   explain, and an add-in change that does matter is only flagged.

**Nothing is recommended.** Shape 1 is what is built. Shape 2 is what D-30's *"a proof can go stale"* reads
as at its strictest; how often the add-in is rebuilt decides what it would cost.

**Answer: shape 1 - said under the answer, never refused. See [D-174](../DECISIONS.md).** Ajmal PS,
2026-11-16, in conversation: *"Keep it a flag, accept Q-92 shape 1"*. D-117's refusal stays on the
fingerprint alone.

---

### ✅ Q-93 — Checking a folder of models overnight: in the modeller's Revit one model at a time, or a separate Revit for each model? → **Shape 1, in the one Revit Heron is connected to; opening read-only is a read** *(new, 2026-11-17, answered 2026-11-19)*

**Where it comes from.** Item 7 of the build order in [research note 51](../31-research.md),
which the owner asked to be built one by one: *"many models overnight, read only - one report at the end"*.
Checking each item against Heron before building it found two things the build cannot decide alone.

**What Heron has today.** A QA checklist runs on the model in front ([D-152](../decisions/D-152.md)). The
only tool that opens other files in the background is `UPGRADE_FAMILY_FILES` - PROVEN, and **PUBLISH**,
because it saves what it opens. A fragment that runs too long is stopped by its run clock
([D-129](../decisions/D-129.md)), and an open of a large model is one long Revit call no check can
interrupt. Every document a job opens is watched by the run guard.

**The question, in a modeller's words: when Heron checks every model in a folder overnight, where should
it open them?**

1. **In your Revit, one model at a time** - opened in the background, detached, no worksets opened, read,
   closed without saving; the chat sends one model per call and writes one report at the end.
   **Cost:** your Revit is busy for the whole run; a dialog a model raises on opening stops that model;
   one very large model can outlast the run clock. Smaller to build: the background open is a mechanism
   Heron already has proven, and every safety rule stays as it is.
2. **A separate Revit for each model**, started by a small supervisor program that keeps going after a
   crash and resumes after a restart. **Cost:** a large build - a program that starts Revit with nobody at
   the keyboard, answers or refuses its dialogs, and kills it if it hangs; a Revit licence in use per
   model while it runs; and an unsigned Heron add-in makes Revit ask *Load or not* at every start, which
   stops an unattended run until signing is settled ([Q-77](tier-2.md)).
3. **Not built yet** - run the checklist on each model by hand, as today.

**And under shapes 1 and 2, one more:** opening another project file changes no model and saves nothing.
Is that a **read** - allowed with Changes off - or does it need Publish, as the family upgrade does?

**Recommended: shape 1, with opening read-only counted as a read** - the smaller build, on a mechanism
already proven, nothing saved anywhere. The owner decides; nothing is built until then.

**Tracked as GitHub issue [#1038](https://github.com/Ajmalpshaik/Heron-AI/issues/1038) since 2026-11-19** - the owner answers there.

**Held for a discussion, 2026-11-17.** The owner, in conversation: *"7 i have detaild explanation this we
will do it later we need to discus abouth this"*. He has a fuller plan of his own; this question waits for
it, and nothing is built or proposed for item 7 until that discussion.

**Answer: shape 1, and opening a model read-only is a read. See [D-181](../DECISIONS.md).** Ajmal PS,
2026-11-19, in conversation, written on [#1038](https://github.com/Ajmalpshaik/Heron-AI/issues/1038):

1. **Where the models open: in the one Revit the modeller connected Heron to, one model at a time** - his
   words: *"in one Revit because I have to connect the Heron on that Revit ... which Revit I given to AI, in
   that Revit"*. Never a second Revit started for it.
2. **Opening a model read-only, reading it and closing it without saving counts as a read**, so it is allowed
   with Changes off. Publish is not needed, because nothing is saved anywhere.

**Nothing is built by this answer.** The overnight folder check - research note 51's item 7 - is a later build
job of its own, started only when the owner asks for it. These two answers are what that build starts from;
whether his fuller plan of 2026-11-17 adds to them is his to say when it starts.

---

### ✅ Q-94 — Heron's usage counter: does it keep the question you typed, or only a count against the clause it answered from? → **Only the clause and a count** *(found 2026-09-12, carried here 2026-11-20, answered 2026-11-20)*

**Where it comes from.** [D-70](../decisions/D-70.md) settled, on the owner's yes of 2026-09-12, that Heron
keeps a **counter** of how often each standard clause answers a question - a clause id and a count, per
scope, deletable, never sent - so that ranking can learn which clauses are used. Its own section *What this
does NOT settle* says the rest: *"Keeping the sentences a person typed is a materially different thing from
keeping a tally against a clause number ... storing the question text needs its own answer."* The question
was written down in the work note `work-notes/plans/next-steps-2026-09-12.md` (B1) and never put to the
owner; it moved here when that note was retired on 2026-11-20 (WN-22, issue
[#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007)), because a question must never live only in a
work note. Nothing is built either way: the counter itself is R-16 and R-25 of
[`rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md), tracked as WN-12 of
[#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006).

**The question, in a modeller's words: when you ask Heron about a standard, should it remember only that
clause 5.3 was used once more, or also the words you typed?**

1. **Only the clause and a count** - what D-70 already covers. **Cost:** Heron can learn which clauses are
   used, but never which questions led to them, so a wrong answer cannot be traced back to how it was asked.
2. **The clause, the count and the question text,** on this PC only, deleted with the counter, never sent.
   **Cost:** the sentences you type - which can name a project, a client or a room - are kept on disk.

**Nothing is recommended.** Shape 1 is what D-70 covers; shape 2 is new, and it is the half with the privacy
consequence. Whoever builds R-25 stores the clause id and the count until this is answered.

**Answer:** shape 1 - only the clause and its counts; the question's words are never kept. The owner's
words, 2026-11-20, answering question 1 on pull request [#1096](https://github.com/Ajmalpshaik/Heron-AI/pull/1096):
*"yes to all four on the usage counter, build it"* - the fourth of the four, as the pull request put it, was
*"Your question's words are not stored - only the clause and the counts"*. Recorded as [D-191](../decisions/D-191.md) item 4 and built in
pull request [#1135](https://github.com/Ajmalpshaik/Heron-AI/pull/1135). The answer was written here on
2026-11-21, when this question was found still reading open.

**Tracked as GitHub issue [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009) since 2026-11-16 (WN-39)** - the owner answers there.

---

### 🔵 Q-95 — The cloud index for documents: which service makes it, and does the switch stay off until you turn it on? *(asked 2026-11-20 on pull request #1096, carried here 2026-11-21)*

**Tracked as GitHub issue [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009) since 2026-11-21** - the owner answers there.

**Where it comes from.** Q-54, above in this file,
was answered *"build it, and split by content type"*, and [D-82](../decisions/D-82.md) records the line:
documents may go to the cloud, Revit models, families and model data never leave the PC. That is R-30 of
[`rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md), tracked as WN-13 of issue
[#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006). **It is not built, on purpose**: no decision
names the service a document would be sent to, and none says in so many words that the switch is off until
you turn it on. Both were asked on pull request
[#1096](https://github.com/Ajmalpshaik/Heron-AI/pull/1096) (its question 2), which merged unanswered, so
the question comes here rather than living only in a merged pull request.

**The question, in a modeller's words: when Heron may send a document's text to the cloud to index it, which
service does that - and is it switched off until you switch it on yourself?**

1. **Which service.** It needs an account and a key on your PC, and it is your choice - Heron does not pick
   one for you. Nothing is sent anywhere until it is named.
2. **Off until you switch it on**, and the switch says in plain words *"your document text is sent to
   <service> to be indexed - your Revit models never are."* **Recommended: yes** - it is what D-24 kept
   (local is the default) and what D-82 keeps.

**Nothing is built until both are answered.** Until then every document is indexed on this PC only, as
today.

---

### 🔵 Q-96 — Adding a document to Heron's knowledge from the chat, in one step: build it? *(asked 2026-11-20 on pull request #1096, carried here 2026-11-21)*

**Tracked as GitHub issue [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009) since 2026-11-21** - the owner answers there.

**Where it comes from.** R-90 of [`rag/01-requirements.md`](../work-notes/plans/rag/01-requirements.md)
says adding a document is **one obvious action**: you never type a path, name a database or choose a
folder. Half of it is built - Heron decides where the store is, and tells you in your words where a
document went (*"for the whole company - on this PC"*, R-89). **The other half is not:** adding a document
is still a typed command with a file path in it, `python brain/heron_ingest.py <file> --scope company`,
and no Heron tool lets the chat do it. That is WN-20 of issue
[#1006](https://github.com/Ajmalpshaik/Heron-AI/issues/1006). It was asked on pull request
[#1096](https://github.com/Ajmalpshaik/Heron-AI/pull/1096) (its question 3), which merged unanswered.

**The question, in a modeller's words: should you be able to say *"add this PDF for the whole company"* in
the chat, and Heron adds it - instead of typing a command?**

**Recommended: yes.** The tool would read only the one file you name, write only Heron's own knowledge store
on this PC, send nothing anywhere and touch nothing in Revit; it would say first, in your words, where the
document goes and that your file is left untouched (R-91), exactly as the typed command does now. **Cost:**
one more tool the chat can call, and it reads a file from your PC - only the one you name, never a search of
your folders.

**Nothing is built until this is answered.** The typed command stays the way in.

---

### 🔵 Q-97 — When one tool has two proving runs waiting, which one should its proof be signed on? *(asked 2026-11-21, row 5b-1978)*

**Tracked as GitHub issue [#1254](https://github.com/Ajmalpshaik/Heron-AI/issues/1254) since 2026-11-21** - the owner answers there.

**Where it comes from.** Heron's batch proving keeps **one** run record and **one** proof draft per tool,
and a signed proof describes one positive-and-negative pair. On 2026-11-21 three tools were found with more
than one run waiting for them - [row 5b-1978](../fragment-issues/row-5b-1978.md) has the files and lines,
read from the repository, nothing run in Revit:

1. **Make sheets** (`CREATE_SHEETS`). Group OZ owes the *sheets from a table* run, written when the tool
   was version 4; Group PE owes the *placeholder sheets* run of version 5, which is the tool as it now
   stands. Whichever is run second replaces the first, and a proof drafted from OZ's run would describe
   code that has changed since.
2. **Extend a roof to meet another roof** (`EXTEND_ROOF_TO_ROOF`). Three job files prove it - Revit 2024
   and Revit 2020 among them.
3. **Line style and family reference planes** (`CREATE_LINE_STYLE`, `SET_FAMILY_PLANE_REFERENCE`, Group OK).
   Each already has an unsigned proof draft on your PC from a run you KEPT on *Column Scratch 1044* on
   2026-11-20. Running the new OK job files replaces those drafts.

**The question, in a modeller's words: for each of these, which run do you want the proof signed on?**

- **Make sheets.** **Recommended:** version 5's placeholder run (PE3) is the proof, because it is the tool
  as it stands; the table run (OZ) is run afterwards as a check of its own and written down in Group OZ,
  not signed.
- **Roof to roof.** **Recommended:** the first proof on the release Group OY names, and each other release
  run with `--release` (which keeps its own record, `extend-roof-to-roof@2020`), never two runs on the same
  release back to back without a draft read between them.
- **Group OK.** **Recommended:** review and sign the drafts already on your PC first
  (`python brain/heron_validate.py review <tool>`); run the new OK job files only if those drafts are gone.

**Nothing is changed until this is answered.** Until then each of these is run one at a time, with the draft
read and signed or set aside before the next run of the same tool.

**Answer:** the run Revit actually checked, for the tool as it stands - run whatever Revit check a tool still needs,
read its draft, then sign it PROVEN, one run of a tool at a time. The owner's words, 2026-11-26, in conversation:
*"if its need to chek the revit chek that and sing make it proven"*. Recorded as [D-204](../decisions/D-204.md) item 2.

---

### 🔵 Q-98 — How should two tools that can never come back empty be proved - Refresh view and Report level elevations? *(asked 2026-11-23, row 5b-1969)*

**Tracked as GitHub issue [#1305](https://github.com/Ajmalpshaik/Heron-AI/issues/1305) since 2026-11-23** - the owner answers there.

**Where it comes from.** D-30 asks every proof for a second leg that comes back empty. Two tools have none
that can be arranged, and nothing to vary either, so D-53 tracking cannot follow them
([row 5b-1969](../fragment-issues/row-5b-1969.md), read from the files, nothing run in Revit):

1. **Refresh view** (`REFRESH_VIEW`). It takes no input, and its one prescribed negative, *a run with no
   screen*, cannot be arranged - the add-in always gives a card a screen.
2. **Report level elevations** (`REPORT_LEVEL_ELEVATIONS`). It always writes a line, even *"this model has no
   levels"*, and its only input is the *includeLinks* switch, so there is nothing for tracking to follow.

**The question, in a modeller's words: for these two, is a by-hand reading beside the proof enough to sign
on - you watch the tag change on screen for Refresh view, and read the storeys line against Manage > Levels
for Report level elevations - or do you want them left unproved until a check can be built?**

- **Recommended:** a by-hand reading is enough for both, written down on the proof with the Revit release and
  the model, because the effect of each is something you can see and no run record could show more.

**Nothing is changed until this is answered.** Until then neither is put in a batch job.

**Answer:** a by-hand reading is enough for both, written on the proof with the Revit release, the model and the
date. The owner's words, 2026-11-26, in conversation: *"Check by hand is enough"*. Recorded as
[D-204](../decisions/D-204.md) item 1.

---

### 🔵 Q-99 — Heron's search has no "nothing here covers that" cut-off, and the measurement it was waiting for can no longer be made. Measure it on the built-in search, change what is measured, or drop it? *(asked 2026-11-26, after D-203)*

**Tracked as GitHub issue [#1474](https://github.com/Ajmalpshaik/Heron-AI/issues/1474) since 2026-11-26** - the owner answers there.

**Where it comes from.** Two requirements of the knowledge search,
[R-56 and R-58](../work-notes/plans/rag/01-requirements.md), want a **confidence floor**: a candidate
with no real claim on the question is dropped rather than ranked last (R-56), and a question nothing in
Heron covers is refused by name rather than answered with a confident shortlist (R-58). R-57, and the third
of R-59's three *nothings*, stand on them. [R-60](../work-notes/plans/rag/01-requirements.md) says the floor
comes from a measurement and from nothing else, and [R-55](../work-notes/plans/rag/01-requirements.md) forbids
moving a threshold to make a result look better. All of them were **blocked on measuring that floor on the
trained model search** - [NEEDS-CHECKING row A15](../needs-checking/group-a.md), carried in as `W-8`.
[D-203](../decisions/D-203.md) removed the trained model on 2026-11-26, so that measurement **cannot be made
any more**: Heron's one search is the built-in one. Read from the files, nothing run:

1. **On the built-in search the questions overlap.** A15 records that at 360 fragments it did not separate a
   BIM question from a question about cats - twelve questions, every column overlapping, *"how do I bake
   sourdough bread"* holding the widest winning gap - and that on the four questions written down it still
   overlaps. Its reasons are structural: the fused score keeps order and drops strength, and the built-in
   search's own description says it is not meaning.
2. **On the trained model the four were apart** (2026-09-24), but A15 already said that was not a floor: two
   questions with no BIM content are an observation, and six of your 79 - jobs nothing in Heron does - scored
   higher than either, so a floor on nearness would have caught a question about cats and not a BIM job
   Heron cannot do.
3. **Until this is answered the search has no floor** - [`brain/heron_retrieve.py`](../../brain/heron_retrieve.py)
   and [`brain/heron_conflict.py`](../../brain/heron_conflict.py) say so where they rank - and A15, R-56 and
   R-58 stay open on a run that no PC can make.

**The question, in a modeller's words: when a modeller asks Heron something it knows nothing about, should
Heron still try to say *"nothing here covers that"* - and if so, measured how?**

- **Measure it on the built-in search.** Write the questions with no BIM content in a file first, ask them
  beside your 79, and record every column (A15's own closing condition). A15's figures say it will most
  likely overlap; R-60 counts that fail as an answer - the floor cannot be had this way.
- **Change what is measured.** Keep R-56 and R-58, but measure something other than nearness on the one
  search - for one, how much of the question the winning card's own words cover - and write the new
  measurement into R-60 before it is run.
- **Drop it.** R-56, R-57, R-58 and R-59's third *nothing* are closed as not built by your decision, A15 is
  closed with them, and Heron keeps what it does today: it ranks, says which search ran, and never refuses
  a question for having no match.
- **Recommended, from what is recorded:** the first, because it is the one A15 already names and needs no
  network - and if it overlaps, as A15's figures say it will, the second or the third is chosen then, with the
  figures in front of you.

**Nothing is changed until this is answered.** Until then A15 stays open, R-56 to R-58 stay blocked, and the
search ranks without a floor, as today. **Not checked:** no question was asked of the search for this entry;
every figure above is A15's, from [`brain/retrieval-history.md`](../../brain/retrieval-history.md).

---

### 🔵 Q-100 — Every Heron install carries your PC's name, "ajmal-pc", as its client id. Change it to a neutral one, such as "heron-pc"? *(asked 2026-11-27, in the plugin-directory follow-up)*

**Tracked as GitHub issue [#1488](https://github.com/Ajmalpshaik/Heron-AI/issues/1488) since 2026-11-27** - the owner answers there.

**Where it comes from.** [`.mcp.json`](../../.mcp.json) sets `HERON_CLIENT_ID` to `"ajmal-pc"` - your PC's
name. It is the [D-22](../DECISIONS.md) lease identity, *one person, one id*. Heron's Setup copies it onto
every modeller's PC: [`platform/Heron.Installer/ClaudeCodeEntry.cs`](../../platform/Heron.Installer/ClaudeCodeEntry.cs)
reads it with `ClientIdIn(...)` (the call that reads `.mcp.json`, near line 257) and `Add` writes it into the
user-scope Claude Code entry (near line 131). So every install sends Revit your PC's name. It does no harm -
the lease is held per Revit, on one PC - but it is your name in somebody else's install, and the
plugin-directory check of 2026-11-26 asked for that value to be neutral. Read from the files, nothing run:

1. **The documented proof commands give the old value.** The job files in [`tools/jobs/`](../../tools/jobs/README.md),
   their README and most NEEDS-CHECKING group files give their commands as `HERON_CLIENT_ID=ajmal-pc python ...`
   (`git grep -l "HERON_CLIENT_ID=ajmal-pc" | wc -l` counts the files; `git grep -l "ajmal-pc"` lists every
   file that names it). With a different value in `.mcp.json`, those commands would be refused the lease while
   your chat holds it (D-22).
2. **Your own install keeps the old value until Setup runs again.** The user-scope entry already on your PC says
   `"ajmal-pc"`; with a new value in the Heron folder, a chat opened in the Heron folder and a chat opened anywhere
   else would send two ids and race for the lease - the exact thing `ClientIdIn`'s own comment says it is there
   to prevent.
3. **Two tests read the value.** [`tests/test_model_hold.py`](../../tests/test_model_hold.py) checks the literal
   `"ajmal-pc"` in `.mcp.json` (near line 569) and sets it itself (near line 194);
   [`tests/test_commands.py`](../../tests/test_commands.py) compares the lease's person with `.mcp.json`'s value
   (near lines 305 to 311).

**The question, in a modeller's words: should every Heron install go on sending your PC's name, or a neutral
name that is the same for everybody?**

- **Change it, in one pull request of its own.** `.mcp.json`, the documented commands, the tests' literal,
  [`.codex/config.toml`](../../.codex/config.toml) and, since 2026-11-28, the Claude Code plugin's
  [`platform/Heron.Plugin/.mcp.json`](../../platform/Heron.Plugin/.mcp.json) (both carry the same value -
  `tests/test_config_and_health.py` fails while one differs, and `tools/build-plugin.py` refuses to build) change together, and then
  Setup is run again on your PC - a PC check, owed after it merges.
- **Leave it.** It stays `"ajmal-pc"`; nothing changes, and every install goes on carrying your PC's name.
- **Recommended:** change it, to a value such as `"heron-pc"`, in its own pull request.

**Nothing is changed until this is answered.** It was not changed in the follow-up that found it, because a
change of the value alone would break the documented commands and race your own install. **Not checked:** no
command was run with another value; nothing was run in Revit or on your PC; whether the comments that quote
`"ajmal-pc"` - in `mcp/client/heron_bridge_client.py`, `revit/Heron.Revit.Addin/HeronKeepGrant.cs` and
[D-141](../decisions/D-141.md) - should change with it was not looked at.

---

### 🟡 Q-101 — Articles 9 and 12c reach every chat word for word, in developer words, and leave the chat's rules almost no room. Amend them into Revit words, let a chat be given its own wording of them, or leave them? *(asked 2026-11-28, row 5b-2080)*

**Tracked as GitHub issue [#1499](https://github.com/Ajmalpshaik/Heron-AI/issues/1499) since 2026-11-28** - the owner answers there.

**Where it comes from.** Every chat that reaches Heron is told `host.chat` before its first tool call -
[`brain/instructions/host.chat.yaml`](../../brain/instructions/host.chat.yaml), with Articles 7, 8, 9, 10, 12,
12a, 12b, 12c, 14 and 27 assembled **word for word** from [`HERON_CONSTITUTION.md`](../../HERON_CONSTITUTION.md);
only their citations are left out (`citations: leave-out`). A copy or a rewording is refused on purpose, in two
places: the copy rule in [`brain/heron_instructions.py`](../../brain/heron_instructions.py) (`validate`, near
line 376) refuses an instruction whose own text shares any eight words in a row with any Article
(`COPY_WINDOW`), and [`tests/test_outcome_line.py`](../../tests/test_outcome_line.py) (near line 2708) checks
that every word of every Article a chat is given arrives, in order. Both rest on the Constitution's own
*Enforcement* section (near line 246): the Articles are assembled from that file *"so there is one source and
no copies to drift"*. Found 2026-11-28 while fixing [row 5b-2080](../fragment-issues/row-5b-2080.md) (issue
[#1497](https://github.com/Ajmalpshaik/Heron-AI/issues/1497)), by a second session reading the whole assembled
text with you. Read from the files; nothing run in Revit.

Three things a modeller's chat is given read in developer words, not Revit words:

1. **Article 9 is about a quarter of the whole**, and it speaks of a `TransactionGroup`, a `PRODUCTION`
   fragment, *"D-126, superseding D-99's exception"*, D-138 and *"a DRAFT's one run"*. In Revit words it says:
   Revit selects what changed and asks Keep or Discard; nothing is kept until Keep; Discard, Esc or closing the
   box takes it back; with *Ask to Keep* off the change is kept at once, as one entry in the undo list. Those
   words are an illustration, not a proposed text - a shorter wording still has to carry every part of the
   Article: the preview's *what will change, how many, what is skipped*, the box's third link that keeps the
   rest of one job unasked, and the publish and DRAFT runs that are still asked.
2. **Article 9's *"the owner's switch for that question"*** reads, to any modeller who is not Heron's owner,
   as somebody else's switch. It is the modeller's own *Ask to Keep*, in Heron's Settings window
   ([D-150](../decisions/D-150.md)). Article 9 also still says *"on the ribbon"* - that is
   [row 5b-1533](../fragment-issues/section-5b-rows-1526-1550.md), open in issue
   [#915](https://github.com/Ajmalpshaik/Heron-AI/issues/915), and yours to amend either way.
3. **Article 12c's *"`ExternalEvent` boundary"*** is a code word a modeller's chat does not need. And the line
   that introduces the Articles, *"Rules you may not violate, from HERON_CONSTITUTION.md:"* (`compose` in
   `brain/heron_instructions.py`, near line 296), names a file in Heron's own folder, which the same
   instructions tell a chat never to read for a Revit job.

**Why it matters now.** Claude Code gives a chat only the first 4,096 characters of a server's instructions
(row 5b-2080). After that row's fix, `python mcp/server/heron_tools.py --sizes` gives `host.chat` as 3,785
characters, and its budget in [`tests/test_tool_sizes.py`](../../tests/test_tool_sizes.py) is the cap less a
300-character margin (`HOST_INSTRUCTIONS_CAP`, `HOST_INSTRUCTIONS_MARGIN`, near lines 91 and 99) - 3,796, so
11 characters are left. The margin is not spare room: off Windows the server puts its no-Revit line in front
(298 characters, row 5b-2080), and [`tests/test_mcp_stdio.py`](../../tests/test_mcp_stdio.py) holds that form to
the cap itself. The ten Articles are 3,041 of the 3,785 and Article 9 alone is 1,005 - measured 2026-11-28 with
`compose("host.chat")` and `_without_citations` in `brain/heron_instructions.py`. So an amendment that makes
any of these Articles longer by more than a few words, or one more sentence of house rules for a chat, fails
that budget on its pull request. Row 5b-1533's own amendment - a few words, with D-150 named - would most
likely not fit either; that was not tried. Shorter Articles in Revit words would read better to a modeller
**and** give room back.

**The question, in a modeller's words: should the rules every chat is given be written in Revit words - and if
so, by changing the Constitution's own words, or by letting a chat be given its own wording of them?**

- **(a) Amend Articles 9 and 12c in `HERON_CONSTITUTION.md`** into words a modeller reads - an amendment, yours
  alone, recorded as a decision with its reasoning (the Constitution's *Amendment* section). The copy rule and
  the word-for-word test stay as they are. **Cost:** Heron's own change agent is given Articles 9 and 12c from
  the same file ([`brain/instructions/agent.modify.yaml`](../../brain/instructions/agent.modify.yaml)), so it is
  given the new words too; and `host.chat`'s own cases (near the end of `host.chat.yaml`, *"Article 9 arrives
  with how revit_change meets it"* and the one after it) look for *"D-126"*, *"the owner's switch for that"*,
  *"D-138"*, *"third link"* and *"one run are still asked"*, so they change with it.
- **(b) A decision that a chat may be given its own wording of an Article**, held to the Article by a test of
  its meaning - which the copy rule and the word-for-word test refuse today. The Constitution's Articles stay as
  written. **Cost:** two wordings of one rule, which is what the *Enforcement* sentence above gives as the
  reason Articles are assembled, so that sentence may need amending too; and a test of meaning has to be
  designed, because what exists checks words.
- **(c) Leave them as they are.** Nothing changes: every chat goes on being given Articles 9 and 12c in
  developer words, and its rules keep the room they have.

No option is recommended here: the session that found this was asked to record the choices, not to pick one.

**Nothing is changed until this is answered.** Until then the Articles are given word for word, as today, and
a change that takes `host.chat` past its budget fails `tests/test_tool_sizes.py`. **Not checked:** no rewording
was written or measured, so how much room (a) or (b) would give back is not known; nothing was run in Revit or
in a real chat for this entry, and whether a modeller's chat actually misreads *"the owner's switch"* was not
tried; the 3,785 and the 11 are the branch that fixes row 5b-2080, measured before it merged - on `main` at
`addd1539`, `host.chat` is still 4,635.

---

### 🟡 Q-102 — Heron's family method colours every family's reference planes by your own standard: outline and centre planes green, all the rest gray. Keep that for every modeller, or make it an option? *(asked 2026-11-28, from the family method rebuild)*

**Tracked as GitHub issue [#1516](https://github.com/Ajmalpshaik/Heron-AI/issues/1516) since 2026-11-28** - the owner answers there.

**Where it comes from.** [`brain/skills/family-creation.yaml`](../../brain/skills/family-creation.yaml), the method `heron_method` serves for building a family, carries *"THE OWNER'S STANDING RULE FOR PLANE COLOURS (2026-11-17), every family"* (near line 196 on main `820745f3`): the outline planes (Left, Right, Front, Back, Top, Bottom) and both centre planes stay on *Reference Planes* itself, Revit's default green; every other plane goes on a gray subcategory of it, *Secondary References*, 128,128,128, made once with `CREATE_LINE_STYLE` and applied with `otherPlanesSubcategory` in `SET_FAMILY_PLANE_REFERENCE`, and again after any later step that makes a plane. It ends *"Only where the modeller says otherwise for a family, ask; never drop the rule silently."* The cards carry the same rule in their header comments - [`set-family-plane-reference`](../../brain/fragments/set-family-plane-reference/fragment.yaml) version 2 (*"The owner's standing rule, 2026-11-17: in every family ..."*) and [`create-line-style`](../../brain/fragments/create-line-style/fragment.yaml). It is your office standard, given on 2026-11-17 in your own words, and it applies to every family Heron builds for **every** modeller who uses it.

**Why it is asked now.** On 2026-11-28 you decided Heron gets no glossary of one site's words, because *"this is not for me only, this is for everyone"* (recorded in [row 5b-2076](../fragment-issues/row-5b-2076.md)). This is the same ground: a rule of yours, built into the method everybody gets. [D-27](../decisions/D-27.md) says Heron's defaults are *"defaults, not law"*, because *"one person's preferences cannot be the product's"*, and [Q-22](answered.md) answered *Everyone* for who Heron is for. The method's rebuild on your own family standard (pull request #1503, open as a draft) keeps this rule word for word, marked as yours, **while your decision is pending**. Found by the session recording the leftovers of 2026-11-28; read from the method and the cards, and nothing run in Revit.

**The question, in a modeller's words: when Heron builds a family for someone else, should its reference planes come out in your green-and-gray colours without being asked?**

- **(a) Keep it for everyone.** Every family Heron builds has the gray *Secondary References* subcategory and your colour split, as today; a modeller whose office wants something else says so, and the method asks. **Cost:** a modeller who has never heard of the rule gets a subcategory and a colour they did not choose, in every family.
- **(b) Make it an option**, on for you and a stated default for the rest - the default itself (on or off) is part of what you decide. **Cost:** somewhere has to hold the choice for each modeller and the method has to read it before it colours the first plane; where Heron keeps such choices was not looked at, and the cards would still hold your rule as their own wording.
- **(c) Something else** - for example, the rule stays but Heron tells the modeller once, at the start, that it is applied.

No option is recommended here: the session that found this was asked to record the choices, not to pick one.

**Nothing is changed until this is answered.** Until then the method keeps your rule for every family, as today. **Not checked:** how other modellers' offices colour reference planes; whether any other method (for example the mass-family one) carries the rule - a search of `brain/skills/` for *Secondary References* found only `family-creation.yaml`; which of #1503's stages would change under each option.

---
