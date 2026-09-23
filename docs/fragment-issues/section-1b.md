# Fragment issues — section 1b

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 1b. NEEDS A HUMAN AT THE KEYBOARD — Revit opens a dialog Heron cannot answer

| Fragment | What happened |
|---|---|
| `transfer-materials-between-documents` | Copying materials from `Snowdon Towers Sample Architectural` into the scratch model, Revit raised **"Duplicate Types — Material Assets : Steel"** and waited. The run returned `still_running` after 60 s, and the next request was refused with `revit_busy`. The owner pressed OK and Revit carried on |
| `transfer-views-between-documents` | The same, `viewKind=drafting`: **"Duplicate Types — Callout Tag, Drafting View, Section Tag, Viewport"**. Also `still_running` then `revit_busy`. **Found after the row below was written**, which had said only one fragment did this — so the split is 2 against 3, not 1 against 3 |

**THE BRIDGE DID EXACTLY THE RIGHT THING and that half is a pass.** Both messages were the ones
[docs/03](../03-heron-revit.md) asks for — *"Revit started the request but has not finished within 60
seconds. It is still working; do not repeat the request"*, then *"A dialog may be open... Finish what is
open in Revit and ask again"*. No hang, no wrong answer, and it named the cure.

**What cannot be fixed by trying harder:** a transfer that collides with an existing type stops and
waits for a person, every time. Heron cannot dismiss a Revit dialog and must not learn to — the dialog
is Revit asking a question only the modeller can answer, and the two choices produce different models.

**THREE OF THE FIVE ALREADY DO THE RIGHT THING, which settles the argument.** Proved the same
afternoon, against the same two models, with no dialog at all:

| Fragment | Clashes it met | What it did |
|---|---|---|
| `transfer-view-filters-between-documents` | 1 — *"Interior (already here)"* | Copied 54, **reported** the clash |
| `transfer-line-styles-between-documents` | 21 | Created 8, **reported** the 21 |
| `transfer-object-styles-between-documents` | — | Created 73, changed 124, weakened 15, skipped 1 |

So the fix for `transfer-materials-between-documents` **and `transfer-views-between-documents`** is not
a new idea to invent — it is the shape the other three already use: **look for the collision first,
decide it in the fragment, and report it**, rather than starting a paste Revit has to interrupt with a
question. Five fragments, one family, two of them written the other way.

---
