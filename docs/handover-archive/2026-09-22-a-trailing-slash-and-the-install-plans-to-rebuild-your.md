# Session note — A TRAILING SLASH AND THE INSTALL PLANS TO REBUILD YOUR PROJECT MEMORY

> **Archived session note** from 2026-09-22. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-23 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-22 — A TRAILING SLASH AND THE INSTALL PLANS TO REBUILD YOUR PROJECT MEMORY

**[Row 5b-119](../FRAGMENT-ISSUES.md), FIXED.** `brain/heron_brain_init.py` read end to end — 264 lines,
2 public functions, **one suite**, nothing outside its own suite calls it.

Its opening line is *"the one install step that can destroy what it finds"*, and it states its
dangerous case three times: *"not a failed initialisation. It is a SUCCESSFUL one, on a machine that
already had stores … an empty knowledge base looks exactly like a fresh install, and the modeller
whose year of project memory it replaced finds out weeks later."*

**Everything turns on one line** — `if path in here`, where `here` is
`set(str(path).strip() for path in existing)`: **the caller's spelling, compared character for
character** against the path `heron_scope` computed.

**Measured on a real store:**

| what the caller hands in | plan |
|---|---|
| the exact path | **0 to create, 1 kept** ✓ |
| the same path with a **trailing separator** | **1 to create, 0 kept** |
| the same path with a redundant **`.`** segment | **1 to create, 0 kept** |

and `os.path.normpath` says both of those name the same file.

**The module states its own limits carefully everywhere else** — its `unjudged` warns that a caller
passing a **stale** list *"gets a plan to overwrite stores this agent was never told about"*. It said
nothing about a list that is current and merely **spelled differently**, which is the easier mistake
and the one a caller cannot see.

Both sides go through one `_same_file()` now — `os.path.normcase(os.path.normpath(...))`. **The
direction is safe by construction**: normalising can only make *more* paths match, and a match means
the store is **kept** rather than created over.

```bash
python tests/test_brain_init.py     # section 1b, 3 red against the module as found
```

**The four checks around them were green BEFORE the fix**, and are what make this a fix rather than a
loosening: the exact path is still recognised, both alternative spellings are asserted by `normpath`
to name the same file *before* being asked for, and a bare filename is still **not** matched.

**`normcase` is there for Windows**, where two spellings differing only in case name one file — and
on POSIX it does nothing. **That half is reasoned rather than measured**, because this container has
no Windows, and it is written down that way rather than claimed.

**A relative path is still not matched, and the answer says so now** instead of leaving it to be
found: resolving one needs a working directory, and choosing which would be this agent guessing where
a caller meant — the thing it exists not to do.

**What is right here is the interesting part**: it does **not** look at the disk. `existing` is handed
in, because *looking and then acting on what it saw* is the shape of the mistake it exists to prevent.
The scope list, the paths, the meanings and the schema version are all `heron_scope`'s own. Two scopes
resolving to one file is refused **before** anything reaches disk. And the same store asked for twice
is one store, not a conflict.
