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
- **Use the preview.** Heron will tell you what an operation will affect before it runs. Read it.
- **Know how to undo.** Every Heron change is a single entry in Revit's undo stack, by design.

## On worksharing / central models

- Heron **never** synchronises with central on its own initiative.
- Heron cannot modify elements owned by another user, and will tell you when it skips them.
- Coordinate with your team as you normally would. Heron does not change how worksharing works.

## What Heron sends where

Heron uses AI models to understand what you ask for. Depending on how it is configured, some of what
you ask — and some model context — may be sent to an AI provider for processing.

**If you work under a confidentiality agreement, check your project's configuration before use.**
Heron supports a local-only mode for restricted projects. If you are unsure whether your project
permits cloud AI processing, ask before you use it, not after.

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

Heron can generate new automation. Generated code is tested in a sandbox and reviewed before it is
allowed near a live model, and requires human approval before it becomes production capability.

That process reduces risk. It does not eliminate it. Treat newly generated tools with the same
scepticism you would give a script a colleague wrote yesterday.

---

*If any of the above is unacceptable for your project, do not use Heron AI on that project.*
