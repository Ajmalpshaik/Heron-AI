# The first five minutes of the next session

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**Run this before reading anything else. It is computed from disk, so it wins over every sentence
below:**

```bash
python tools/check-gaps.py
```

It sweeps the build order against what is actually on disk, runs every test and every checker, checks
every agent id against the registry, walks the fragment library, the capability registry and the
dependency graph, and reads the register. Then it sorts everything into **UNFINISHED** and **WAITING**,
and its exit code follows only the first.

**As of the twentieth session, 2026-09-02, it reports 0 unfinished and 56 waiting.** Everything waiting
needs a machine, a dependency or a conversation: **48 a real Revit**, plus the unproven fragments as one
further item, **Windows** (`A4`, `A6`, `A8`), **1 the .NET SDK** (`A9` — the eight fragments no compiler
has read), **1 a network that can reach the weights host** (`A7`), **1 the owner** (`R1b`), and **1 an
optional dependency this machine does not have** — `test_mcp_serves.py` reporting honestly that it was
skipped, rather than being counted as a pass.

> Those figures were **read off the tool, not carried forward**, and the sentence they replace shows why
> that matters: it said *55 waiting* and then listed parts summing to 54, and it still counted **3 owner
> items** after `R1` and `R2` had been struck off in the same session. A total and its own breakdown
> disagreeing is the cheapest possible drift to catch and it survived anyway. Re-derive rather than
> edit the digits.

**`A8` is mostly done, and doing it found the worst defect in this repository's history — read this
one first.** It was written down as *needing Windows*. It did not: the blocker was a missing **pip
package**, and the MCP SDK is pure Python. Installing it took ten minutes and produced two things.

**A real SDK now serves all ten tools** — names, descriptions and argument schemas — and the three brain
tools answer through its own dispatch with both refusals intact. That is the half `test_brain_reachable.py`
could only ever read *as text*, and it is now [`tests/test_mcp_serves.py`](../../tests/test_mcp_serves.py).

**And installing it revealed that Heron's MCP server would not start at all on a fresh machine.**
`pip install --user mcp` — the exact line [`tools/HeronRevit.ps1`](../../tools/HeronRevit.ps1) hands the user —
now resolves to SDK **2.x**, which **deleted `mcp.server.fastmcp`**: `FastMCP` was renamed `MCPServer`.
The server's import was written against 1.x, so it raised `ImportError` before registering a single tool.
**Every Heron tool absent from the host, on any machine installing today, with no Revit and no Windows
involved.** Nothing in the repository could see it, because nothing here had ever imported the SDK — the
entire suite reads that file as text, and text cannot fail an import.

The fix is the import and nothing else: the class is looked up newest-first, because 2.x's `MCPServer`
takes the same `@server.tool()` decorator and the same `run()`. **Proven on both SDK majors installed
side by side**, and validated the way this repository requires — the old line was put back and watched
to fail. `heron_version` now reports which SDK is serving, since *"Heron stopped working"* and *"the SDK
moved underneath it"* look identical from the user's side.

**What genuinely remains of `A8` needs the PC:** a real host over stdio — that Claude Code connects,
renders the docstrings and picks a tool from them. In-process dispatch is a strong signal ahead of that,
not a substitute for it.

**If the tool and this file ever disagree, believe the tool.** It is computed from disk; this file is
typed. That is not a hypothetical — for most of 2026-08-29 this tool reported `UNFINISHED - nothing`
while the entire Phase 2 brain sat unreachable, because it checked that each step's module and test
existed and never asked whether anything called them. The check that catches it was added the same day.

### Then, in order

| | What | Where it happens |
|---|---|---|
| **1** | ~~`R1`~~ **DONE 2026-08-29 — all 21 read back, all 21 confirmed, nothing moved.** What is left of the review is **`R1b`**: show him the trust model working with his own fragments in it, because [D-14](../DECISIONS.md) stays *Proposed* until he has seen it | Needs a screen |
| **2** | **`B1` to `B4`** — open Revit 2020, look for the **Heron AI** tab, press **Heron**, then `ping` and `count` | Needs Revit |
| **3** | **`C3`** — with `write.enabled` still **false**, ask for a move and watch it be **refused, by name**. Prove the gate before testing the write, or a passing move proves nothing | Needs Revit |
| **4** | **`D1`–`D3`** — *"move the ducts up 200 mm"*, say yes, then **MEASURE ONE**. The single most important line in the whole register | Needs Revit |
| **5** | **`D5`** — **one** Ctrl+Z puts it all back. Two means Golden Rule 16 is broken | Needs Revit |

Everything else in [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) follows in the order written there. It is in
dependency order on purpose: nothing in group D can be attempted before group A passes.

### And the ones that need no Revit at all

- **`A7`** — the trained embedding backend has **never run**, and 2026-08-31 sharpened *why* on a second
  and different container. `pip install model2vec` **works** — PyPI is reachable and the package is
  fine. What fails is `StaticModel.from_pretrained("minishlab/potion-base-8M")`, with
  `ProxyError: 403 Forbidden`. **So the precondition is not "a working network", it is reaching
  huggingface.co**, and the register row now names the host instead of saying "a model host" — a
  sentence anyone with a working network would reasonably read as already satisfied. Until it runs,
  search finds words and not meaning, and [`tests/test_embed.py`](../../tests/test_embed.py) says so in
  measured numbers. **One thing it did prove**: with the package actually installed, the fallback ran
  against a *failed download* rather than a missing import — a branch that had never once executed —
  and it degraded to `lexical` and said so on its first line.
- **`A4` and `A6`** — both need Windows but not Revit. `A4` is the Windows named pipe itself; `A6` is
  thirty seconds confirming the SDK probe reads Windows correctly.
- **`A8` was on this list and nobody knew it**, filed under *needs Windows* when what it needed was
  `pip install mcp`. That is the fourth time something here turned out to be waiting on somebody trying
  it rather than on a machine. **The count is now four, and the standing advice stands: assume the
  fifth is out there.** The one that cost the most was believing an environment-specific wall was a
  property of the project.

### The one thing that was buildable here — now built

**The brain was wired to nothing, and now it is wired.** Eight modules, seven fragments and ten skills
were on disk, tested and passing, with **no MCP tool reaching any of them.** The host talks to Heron only
through the tools in [`mcp/server/heron_tools.py`](../../mcp/server/heron_tools.py), and every one of them went
straight to the bridge. That left Phase 2's third definition-of-done clause open for **no external reason
at all**, which is why it was worth doing on a machine with no Revit.

**What was added**, all of it read-only and none of it touching a model:

| | |
|---|---|
| [`mcp/server/heron_brain.py`](../../mcp/server/heron_brain.py) | The one seam between the MCP side and `brain/`. Opens the store, rebuilds it if the machine is fresh, indexes if stale, and hands back rows. **It holds no knowledge of its own** |
| `heron_capabilities` | What Heron knows how to do: ten jobs, which have every part provided, and the seven capabilities nothing provides |
| `heron_resolve` | Who can do one capability, at what risk, on which releases — **asked for by capability, never by fragment id** |
| `heron_lookup` | The user's own sentence resolved to a **capability**, with the provider underneath as evidence rather than as the answer |
| [`tests/test_brain_reachable.py`](../../tests/test_brain_reachable.py) | Step 12's acceptance test re-run through the seam: add a better provider and the call site is the same line; delete the original and it still answers |

**Two things it deliberately does not do, and every answer says both out loud:**

- **Resolving is not running.** A fragment carries C# in `impl/`, the bridge speaks a fixed set of
  operations, and none of them compiles one — [D-28](../DECISIONS.md)'s in-process Roslyn is unbuilt.
  So the host can now learn *what would do the job* and still cannot have it done. A tool that let that
  be inferred would be worse than no tool, because a plan built on it fails at the last step.
- **Nothing underneath is proven.** Every skill and every fragment is still `DRAFT`.

**The version filter reaches the host as a wall, not a preference.** The release comes from the bound
Revit session, read **without ever asking and without claiming a lease** — looking must never be the act
of claiming, which is the lesson `revit_health` learned about the lease one commit after building it.
With no Revit connected the tools still answer and say the filter did not run.

**The original finding came from running `tools/check-metadata.py` and following what it said**, hours
after `check-gaps.py` had reported everything clean. `check-gaps` now has a check for it — and that check
is an **import** check, so treat its green accordingly: it can see that the seam exists, not that a host
ever called through it. That is `A8`.

### The library, and the question that was put to the owner

**This section used to say seven capabilities were wanted by the skills and provided by nothing, and to
ask before building them.** It was right to ask. He answered, and the answer changed the shape of the
work: build the fragments now, re-authored from his earlier library, and check every one of them in Revit
later — *"checking in revit we will do after because that is a big work... mark as not verified and when
pc came we will check."*

**So the library is 146 fragments and every skill has every capability provided.** The seven were written
on 2026-08-29; twenty-five more followed on 2026-08-30, and seven more on 2026-08-31 — those last chosen
by asking the brain the owner's own sentences and reading what came back, rather than by working through
the earlier library in order. `python brain/heron_skill.py` shows no gaps.

**And his second instruction is the one that must not be quietly undone.** None of them inherits the
earlier library's proven status, however well proven it is there:

> *"Even in the aj ai proven fragment dont mark in heron this is proven because we will check each and
> everyone again in heron ai so mark it as a not proven in heron."*

That is [D-44](../DECISIONS.md), and it is **enforced rather than remembered** —
[`brain/heron_fragment.py`](../../brain/heron_fragment.py) refuses a status above `DRAFT` whose proof does not
match the implementation in front of it. The gate had existed and nothing stood on it: a fragment
declaring `PROVEN` on another model's proof passed every check in this repository, which was measured by
writing one.

**What that buys, and what it does not.** All one hundred and two fragments compile on all eight releases and
none of them will fail at the PC for a reason a compiler could have found — which on 2026-08-31 stopped
being a figure of speech, when the gate caught a tag accessor that Revit 2027 has removed. Not one has met a model. The debt did
not go away — it got **counted**, which is the whole point of `check-gaps` keeping *unfinished* and
*waiting* in two lists that must never be one.

---
