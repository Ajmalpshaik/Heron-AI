# Proposals — Part B

> One section of [the register](../PROPOSALS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Part B — Feature ideas not in the specification

### 🔵 B1. Watch-and-learn skill capture

§34 learns from **repeated utterances**. Far more powerful: learn from what the user **does in Revit**.

The user performs a workflow manually once. Heron observes the document changes and offers: *"You selected 47 ducts by category and moved them up 200 mm. Want me to remember that as a skill?"*

This is how a BIM modeller naturally teaches a tool — by doing the job, not by describing it. It converts every user into a fragment author without any of them writing a fragment, and it directly serves Rule 1.

Technically feasible via Revit's `DocumentChanged` event. Non-trivial, high payoff.

### 🔵 B2. "What did Heron change?" report

A panel listing every change Heron made in this session — element counts, parameters touched, undo entries — with per-item revert.

Cheap (the audit log already holds it) and it is the first thing a BIM coordinator will ask for.

### 🔵 B3. Panic button — undo everything Heron did today

One control that rolls back the session's Heron transaction groups. Its existence changes how willing people are to try the tool at all.

### 🔵 B4. Explain mode for junior modellers

*"Explain what you just did."* → in BIM terms, not code: which categories, which filter, which parameter, and why.

Turns Heron from a black box into a training tool. For a company running junior modellers, that is a distinct commercial argument.

### 🔵 B5. Overnight batch mode

Run standards checks, model health reports and clash summaries across many models overnight, when Revit is free and nobody is competing for the machine.

Fits the Background Scheduler already specified, and it is the highest-value use of a licence that is otherwise idle for 14 hours a day.

### 🔵 B6. Visible cost meter

*"This session: 12 requests, 3 new fragments, $0.40."*

Users trust systems whose cost they can see. It also makes the T1/T2/T3 discipline self-enforcing — an expensive path becomes visible immediately.

### 🔵 B7. Team knowledge sharing before public community

§35 goes from personal knowledge straight to a GitHub PR into a public community. Most of the value lands earlier: **a company-internal shared brain**, on a network share or a private repository.

Same lifecycle, no confidentiality problem, and it is what an employer would actually pay for.

### 🔵 B8. Model health baseline

Record model statistics over time — element counts, warnings, file size, purgeable items — so Heron can say *"warnings have gone from 40 to 380 since last month."*

Trivial to collect, and it is the report BIM managers already produce by hand.

### 🔵 B9. Degraded / offline mode

State plainly what still works with no internet: proven fragments, cached skills, health checks, local retrieval. A tool that stops entirely when the connection drops will not be trusted on site.

---
