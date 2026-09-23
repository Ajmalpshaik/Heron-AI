# Fragment issues — section 1

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 1. SUSPECT — these upset Revit, and why is not known

Both failed **cleanly** — `transaction.Commit()` did not return `Committed`, the write path rolled back,
and the element count afterwards was unchanged. Revit then raised its own dialog:

> *"A serious error has occurred. It is strongly recommended that you use Save As to save your work in
> a new file before continuing."*

The model was not damaged and nothing was saved. **Neither has been run since.** They are first on the
list to look at together.

| Fragment | Values used | What came back |
|---|---|---|
| `create-mep-system-type` | `copyFromName=Supply Air`, `newName=HERON TEST SYS`, `abbreviation=HTS` | `operation_failed` — "ran but Revit did not accept the change" |
| `assign-scope-box-to-view` | `views=Model Linking`, `scopeBoxName=Grids` | `operation_failed` — same |

**How to look at them safely:** one at a time, on a freshly opened model, with nothing else run in that
session — so that if Revit complains again it is unambiguous which one did it. Run READ-only first
(`fragment <name>` with no `--write`) to see how far the C# gets before the transaction matters.

---
