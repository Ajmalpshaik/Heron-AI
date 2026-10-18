# Disclaimer

**Read this before using Heron AI on any project model.**

---

## Heron AI modifies live Revit models

Heron AI is software that reads and writes Autodesk Revit models. It can select, move, modify, create
and delete model elements. It can change parameters across thousands of elements in a single operation.

Used carelessly, it can damage a model you are about to deliver.

## Before you use it

- **Work on a copy, or make sure the model is backed up.** Every time.
- **Never use it for the first time on a live deliverable.** Try it on a test model.
- **Verify every change before issuing.** Heron shows you what it did — check it.
- **Turn on Ask to Keep if you want to see a change before it stays.** It is on Revit's ribbon and
  **off by default**: a change asked for in a chat is then kept at once, as one entry in Revit's undo
  list. With it on, Revit selects what changed and asks Keep or Discard, and nothing stays until you
  choose Keep ([D-126](docs/DECISIONS.md), [D-138](docs/DECISIONS.md)). A move still shows its preview
  first, and a never-proved tool's one run is always asked.
- **Know how to undo.** Every Heron change is a single entry in Revit's undo stack **for one
  document**, by design ([Golden Rule 16](docs/14-golden-rules.md)). Revit's own API cannot span two,
  so a job touching two models can never be one Ctrl+Z - nothing in Heron writes to two today, and a
  job that would will say so before it starts.

## On worksharing / central models

- Heron **never** synchronises with central on its own initiative.
- Heron cannot modify elements owned by another user, and will tell you when it skips them.
- Coordinate with your team as you normally would. Heron does not change how worksharing works.

## What Heron sends where

Heron uses AI models to understand what you ask for. **Heron's replies are the conversation**, so what
it says about your model reaches the AI provider by design. That is not a setting, and there is no
switch that turns it off.

**The model file never leaves your machine.** A `.rvt`, an `.rfa`, a family or a project template is
never uploaded — [D-26](docs/DECISIONS.md), and it is the line the owner drew in his own words: not
*nothing may travel*, but *the file may not travel*. Embeddings are computed locally
([D-24](docs/DECISIONS.md)), and a Heron tool answers a question rather than returning the model.

**Everything else about the work travels.** Project names, file names, element counts, sizes,
parameter values, engineering reasoning and generated code go to the provider like anything else you
would type to an assistant. **Identifiers are not redacted first** — [Q-40](docs/OPEN-QUESTIONS.md)
asked whether they should be, and the answer was no.

**There is no local-only mode, and there is no per-project configuration to check.**
[12 §4](docs/12-security-and-permissions.md) set out three positions — cloud only, local only, hybrid
per project — and the decision took **none of them**; it drew the line at the file instead. Every
setting Heron understands is declared in one place, `Defaults` in
[`HeronConfig.cs`](platform/Heron.Core/HeronConfig.cs), and not one of them is about what may leave
the machine. **This section promised a local-only mode until 2026-09-21, and there has never been
one** ([row 5b-53](docs/FRAGMENT-ISSUES.md)).

**So a confidentiality agreement is a decision about the project, not a setting to change.** Ask
before you use Heron on it, not after.

See [docs/12-security-and-permissions.md](docs/12-security-and-permissions.md).

## No warranty

Heron AI is provided **"as is", without warranty of any kind**, express or implied, as set out in the
[Apache License 2.0](LICENSE) under which it is distributed.

The authors and contributors accept **no liability** for:

- defects, errors or omissions in models modified using Heron AI
- data loss or model corruption
- project delays, rework or commercial loss
- any consequence of relying on Heron AI's output without verification

**You remain professionally responsible for the models you produce and issue.** Heron AI is a tool.
Like any tool, its output is your work.

## On standards checking

Where Heron reports compliance with a standard — ISO 19650, QCS, Ashghal requirements, a company
standard — it reports against the source documents it has been given, and cites them.

**It does not certify compliance.** It does not replace a competent person's review, a formal QA/QC
process, or your obligations under the project's information requirements.

## On AI-generated code

Heron can generate new automation. A newly built agent's first run is **watched, not contained**: it
is handed a world whose Revit door and production scopes are shut, every attempt to open one is
written down, and the run is marked so it can never count as evidence toward promotion. Human
approval is still required before anything becomes production capability.

**That is most of the value and none of the guarantee, and saying so is deliberate.** The agent runs
as ordinary Python in the supervising process. One that cooperates is held; one that calls `open()`,
imports the bridge or reaches into globals is not stopped by anything. Building the wall means a
separate process with the ambient capabilities removed, and that is **not built** —
[D-84](docs/DECISIONS.md) chose *keep what exists, build no subprocess, and take the word out*, for
exactly this reason. **This section said *"tested in a sandbox"* until 2026-09-21, which is the
promise that decision withdrew** ([row 5b-52](docs/FRAGMENT-ISSUES.md)).

What it does catch is the honest accident — a new agent reaching for Revit because nobody told it not
to — and that is most of them. Treat newly generated tools with the same scepticism you would give a
script a colleague wrote yesterday.

> **As built (2026-10-18):** a fragment Heron writes on the spot is generated code too, and it is the
> one kind that does reach a model. Since [D-119](docs/DECISIONS.md) a `DRAFT` fragment may run once on
> the saved model in front of you, as one undo step held behind Revit's Keep or Discard box - asked
> every time, whatever Ask to Keep says ([D-138](docs/DECISIONS.md)) - and nothing it did stays unless
> you press Keep. Such runs have been made on scratch copies of Autodesk sample models in Revit 2024
> and 2027 — evidence: [NEEDS-CHECKING Group EM](docs/needs-checking/group-em.md), its 2026-10-18 run.

---

*If any of the above is unacceptable for your project, do not use Heron AI on that project.*
