# Session note — `heron_mcp_server.py` IS NO LONGER STALE, AND THE DIFF WAS 26 LINES

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — `heron_mcp_server.py` IS NO LONGER STALE, AND THE DIFF WAS 26 LINES

**A STALE mark is not a re-read of the file. It is a re-read of what MOVED**, and the cheapest way to
find that is the blob the mark was taken at:

```bash
git cat-file -p <blob from docs/REVIEW-LEDGER.tsv> > /tmp/old.py && diff -u /tmp/old.py <the file>
```

For this 3,516-line file the answer was **26 lines** — the `_values_array` repair of 2026-09-22, where
a semicolon stopped starting a new value. Read word by word and **sound**.

**Checked rather than taken on trust:** no other semicolon split exists on the value path anywhere in
`mcp/` or `brain/`; the C# side's `OneOverride` comment is the one it now agrees with; and
`tests/test_values_crossing.py` holds the rule in nine checks that all pass — a value carrying
semicolons stays **one** value, a newline still separates two, and the name ends at the **first**
equals sign.

#### One thing checked and dismissed, and the add-in is why

A supplied value whose name matches no declared need is never read — `supplied` is a dictionary the
needs are looked up *from*. That looks like a silent drop on a **write** path, which would be the worst
kind. **It is not.**

- `DescribeSupplied` renders **every** supplied `name=value` back as `ranWith`, so a caller sees what
  they actually sent.
- `BindNeeds` refuses a declared need nobody supplied, by name, with `needs_unbound` — *"Running
  anyway would report 0 results, which reads as 'there was nothing to find' rather than 'nobody was
  asked'."*

So a typo in a value name means the **real** need goes unsupplied and the run **refuses**. It cannot
report success on a write that used a default the caller never chose.
