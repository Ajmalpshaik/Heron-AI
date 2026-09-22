# Session note — "HERON HAS NO COPY OF ISO 19650" ABOUT A STORE THAT HAS IT

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — "HERON HAS NO COPY OF ISO 19650" ABOUT A STORE THAT HAS IT

**[Row 5b-114](../FRAGMENT-ISSUES.md) and [row 5b-115](../FRAGMENT-ISSUES.md), both FIXED.
[Row 5b-110](../FRAGMENT-ISSUES.md) now has its answer** — its three sites have been read, and they are
not one thing. `brain/heron_iso.py` and `brain/heron_company.py` are both read end to end.

`heron_iso.cite()` gets a shortlist from the Librarian, reopens the store and reads each clause's
words in a second pass. That read was `except Exception: row = None` followed by a `continue` — **so a
clause the store could not read was dropped in silence.**

**Measured end to end** on a real store holding a real ISO 19650 document, with **only that one query**
raising `database is locked`, so the shortlist is identical either way:

| | claimed | clauses | the sentence a reader gets |
|---|---|---|---|
| healthy | **True** | **3** | 3 clauses of ISO 19650 across 1 scope |
| locked | **False** | **0** | **"HERON HAS NO COPY OF ISO 19650 indexed in company."** |

**Nothing anywhere in that answer said a read had failed** — `skipped` was `None`, `route` was still
`documents`, and no `unjudged` line mentioned it. Measured by searching the whole payload for the
words. **That is D-52's plausible zero at its worst**: confident, specific, and acted on by going to
look for a document that is already loaded — **in the module whose headline rule is *"NO SOURCE, NO
CLAIM … there is no path through this agent that writes a sentence about a standard."***

The read is narrowed to `sqlite3.DatabaseError` and **records** on an `unreadable` list carried in the
answer. A genuinely absent or genuinely empty chunk is still skipped, because that is a fact about the
store's contents rather than a fault reading it.

### What row 5b-110 asked for: one defect in three, and the row stays open for a better reason

| site | what it does |
|---|---|
| `brain/heron_iso.py` 207 | **swallowed**, and produced a false sentence → row 5b-114 |
| `brain/heron_company.py` 328 | same handler, **does not swallow** — the `None` goes to an `unreadable` list the answer names |
| `tools/check-skill-routing.py` 203 | already narrowed inside; its outer handler **records** `counts_error` |

**So widening `check-narrow-errors` to every `except Exception` around a `.execute` would flag the two
that are RIGHT.** What separates them is not the handler — it is the four lines after it, whether the
failure is named or becomes a count. That tool reads TEXT on purpose, and *does this handler swallow?*
is not a question a text scan has been shown to answer. **That is the remaining question, and it is a
tooling decision rather than a defect.**

### And row 5b-115 — the grep that found four command lines could not see the fifth

```bash
python brain/heron_iso.py --standard          # IndexError: list index out of range, exit 1
```

**Row 5b-104's scan was a grep for the literal `argv[i + 1]`, and this file reads `argv[at + 1]`.**
One variable name. **Re-run as an AST walk** over `brain/`, `mcp/` and `tools/` — every
`<list>[<name> + 1]` read, whatever the names are — **it finds 27 where the grep found 8.**

**And shape is still not behaviour, so all of the remainder were RUN**: `heron_unit_test` guards its
`--timeout` with `except (IndexError, ValueError)`; `heron_bridge_client`'s `--session` falls through
to a positional and ends at the usage text, exit 2; the nine fixed by 5b-104 and 5b-112 all refuse by
name; and three of the twenty-seven are not command-line reads at all. **The grep found neither the
one that was wrong nor the fact that it was alone.**

**One thing measured and recorded rather than fixed**: `brain/heron_devperf.py --baseline` with no
value refuses at exit 2 and calls it **`unknown option '--baseline'`** — a known option given no
value, so a person goes to check the spelling of a flag that is spelled correctly. It refuses, names
the flag, and neither crashes nor searches; the wording is the whole of it.
