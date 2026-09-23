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

---
