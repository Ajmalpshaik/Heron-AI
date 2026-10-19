# Proposals — Part C

> One section of [the register](../PROPOSALS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Part C — Strategic questions the spec does not address

| # | Question | Why it matters now |
|---|---|---|
| **C1** | **Commercial model** — open source, closed, freemium, company-internal? | Determines the licence, whether the community marketplace is even coherent, and whether the repo can stay private. |
| **C2** | **Who is the first real user?** | If it is the owner only, ship the terminal front-end and skip the installer for a year. If it is colleagues, the installer becomes v1 scope. |
| **C3** | **Name / trademark** | "Heron" is widely used in software. Worth a check before branding, packaging and a marketplace exist. |
| **C4** | **Liability** | If a Heron-generated change causes a defect in a delivered model, who is responsible? Needs an answer before anyone outside the author uses it on live work. |
| **C5** | **Relationship to the existing AJ-Tools / PyRevit-Tools / AEB-Tools repos** | Is Heron a rewrite, a wrapper, or their new home? This decides whether the knowledge import system is a v1 feature or a later one. |
| **C6** | **Autodesk App Store distribution** | If it is ever a goal, their review requirements constrain packaging and permissions — cheaper to know now than to retrofit. |

**Answered since - recorded 2026-10-18; the table above is left as it was written.**

| | Answered by |
|---|---|
| **C1** | [D-07](../DECISIONS.md) and [D-08](../DECISIONS.md) - free and open source, on public GitHub, under Apache 2.0 |
| **C2** | [D-07](../DECISIONS.md), which also answers Q-22 - installable by anyone |
| **C3** | [D-37](../DECISIONS.md) - the name is Heron AI, and it says plainly that no trademark check has been done |
| **C4** | [D-36](../DECISIONS.md) - no warranty; the licence is the legal instrument and `DISCLAIMER.md` the plain one |
| **C5** | [Q-23](../OPEN-QUESTIONS.md) - upgrade and absorb - and [D-25](../DECISIONS.md): the existing libraries are studied and re-authored, never imported |
| **C6** | [D-38](../DECISIONS.md) - a GitHub download now; an App Store listing kept possible, and nothing built for it |

---
