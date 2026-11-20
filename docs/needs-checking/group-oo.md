# Needs checking — Group OO

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-11-19 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group OO - the owner's real specification for the knowledge store: the chunker, the citation and the refusal on a genuine one (2026-11-19)

*Tracked as GitHub issue [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009) since 2026-11-16 (WN-38) - the owner gives the document there; the run and its result are written here.* WN-38 moved here on 2026-11-20 from the work note named below, retired that day (WN-22, issue [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007)).

**Why this group exists.** The owner's ask for one real numbered specification lived only in a work note
(`work-notes/plans/next-steps-2026-09-12.md` A3, tracked as
GitHub issue [#1009](https://github.com/Ajmalpshaik/Heron-AI/issues/1009)) and in
[FOR-THE-OWNER](../FOR-THE-OWNER.md) section 4, so `python tools/owner-queue.py` never showed it - its *ONE
DOCUMENT* bucket files only a row of this register ([FRAGMENT-ISSUES row 5b-1180](../fragment-issues/section-5b-rows-1176-1200.md)).
Every test document so far was written to be easy; one with real clause numbers settles whether the
chunker, the citation path and the refusal survive a genuine one. **Numbered is the only hard condition**,
because clause numbers are what make a citation testable. Nothing in Heron changed with this group, and
nothing here has run.

| ID | Do this | Pass looks like |
|---|---|---|
| **OO1** | *On any PC, no Revit.* Give one real numbered specification document - a company standard if one is numbered, otherwise one QCS section. `python brain/heron_ingest.py <file> --scope company`, then `python brain/heron_ingest.py --boundaries <document id>` with the id it printed. Then in a chat, `heron_standards` with scopes `company` for three things the document requires, each in a clause you can find, and one thing it does not say; and `heron_check` on each answer drafted from them, with the same request and scopes | In the boundaries list every clause number reads whole and no rule is cut from the EXCEPT or *unless* that qualifies it. Each of the three answers names the clause the document gives it, and the clause says it. The fourth is not answered from the document. **A FAIL is worth as much:** write down the clause that was cut or the citation that was wrong, and it becomes a row in FRAGMENT-ISSUES |
