# Session note — THE RE-RANKER RAN FOR THE FIRST TIME, AND IT MAKES PICKING A TOOL WORSE

> **Archived session note** from 2026-09-24. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-09-24 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-09-24 — THE RE-RANKER RAN FOR THE FIRST TIME, AND IT MAKES PICKING A TOOL WORSE

**NEW.** The owner's 79 questions were asked of the trained backend and of a cross-encoder for the
first time — in a cloud session, at 398 fragments, every figure in
[`brain/retrieval-history.md`](../../brain/retrieval-history.md) under 2026-09-24. **The trained backend**
puts more right tools in the top three and barely moves first place, and hands a change to more of the
questions that asked for none. **The re-ranker** takes first place down and sends more of those
questions to a writer still — *"Ping the Revit model."* reached `UNLOAD_LINKS` — and it put the right
clause first on the one document question there is. `A10` is closed; `A15` is measured in part and stays
open. [`WHAT-TO-INSTALL.md`](../WHAT-TO-INSTALL.md) says all of it in plain words, beside everything
else a PC needs for Heron, Python first.

**A MISTAKE WORTH NOT REPEATING.** Both rows waited as *"needs a machine that can reach
huggingface.co"* while the Heron cloud environment had reached it since 2026-09-22
([38](../38-the-cloud-environment.md)), and `tools/check-gaps.py` printed both under *needs a real Revit*
— [row 5b-199](../FRAGMENT-ISSUES.md). The check that settled it was one `curl`. **And the optional
packages went into a separate virtual environment**, so the container's own Python — the one every gate
and suite runs on — never changed under the before-and-after record.

**TO DO — the owner's.** (1) **The re-ranker**: the measurement says not for picking tools; whether it
should read standards documents only is a code change nobody has made. (2) **Its Windows download size
is unmeasured**, and the range it announces is below what Linux fetched — [row 5b-200](../FRAGMENT-ISSUES.md).
(3) **`A15` needs its questions with no BIM content written into a file** before a floor can be
measured; only four of the twelve ever were. (4) **The next experiment for tools, not run:** hand the
re-ranker each fragment's utterances — `_fragment_passage` gives it three short columns, and its own
docstring names this as the fix a measurement would call for.
