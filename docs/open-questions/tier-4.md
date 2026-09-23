# Open questions — Tier 4

> One section of [the register](../OPEN-QUESTIONS.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## Tier 4 — Strategic

### 🔵 Q-24 — Name and trademark

"Heron" is widely used in software. Worth checking before branding, packaging and an app-store listing exist.

**Answer: the name is Heron AI. It was CHOSEN, not cleared. See [D-37](../DECISIONS.md).**

Ajmal kept the name and declined the offer to check for an existing product first. **So no trademark or
existing-product search has been done** — this question's own concern, that the name is widely used in
software, stands unexamined. Recorded plainly so that a later session reading *"Q-24 answered"* does not
conclude otherwise.

A legitimate choice for a free tool with no branding to defend. The **technical** window to rename stays
open until the repository goes public ([Q-28](answered.md#q-28--when-does-the-repo-go-public--when-licence--safety-files-exist-and-there-is-working-code)):
today it is a mechanical change across 53 code files; afterwards it breaks installed add-ins and user
folder paths. If a check is ever wanted, before publication is the moment it is cheap — and the last
one.

---

### 🔵 Q-25 — Liability

If a Heron-generated change causes a defect in a delivered model, who is responsible? Now a public-product
question, not a personal one. Partly addressed by an explicit disclaimer ([17 §5](../17-open-source-and-distribution.md))
and by the licence choice (Q-27).

**Answer: no warranty, the standard open-source position — and it is already in place twice. See
[D-36](../DECISIONS.md).** Apache 2.0 carries it as licence; [`DISCLAIMER.md`](../../DISCLAIMER.md) already says
it in plain words a modeller will read. Nothing new is written.

Two things are recorded with it. **`DISCLAIMER.md` is load-bearing**: it promises a preview, a single undo
entry, skipped owned elements and no unprompted sync to central — all of which are **unproven today**, so
it must move with the code rather than after it. And this records a choice, **not legal advice**: it holds
for Heron as it is now, free and open source. If Heron is ever sold or supplied as part of a paid service,
reopen it rather than assume it carries.

---

### 🔵 Q-26 — Autodesk App Store requirements

Confirmed as a later goal ([D-07](../DECISIONS.md)). Their review constrains packaging, permissions and
installer behaviour — cheaper to read the requirements before the installer is finalised than after.

**Answer: GitHub now; the App Store door is kept open by not closing it, and nothing is built for it. See
[D-38](../DECISIONS.md).**

**The requirements have NOT been read, and that is the honest half of this answer.** This question asked
for them to be read before the installer is finalised. Writing Autodesk's current packaging, signing and
review rules from memory would repeat the exact failure this repository has already had twice with the
Revit API — a confident answer nobody checked. **Read them from Autodesk, at the time, or not at all**; so
the reading is deferred along with the listing, and it is the **first** task if one is ever attempted, not
the last.

Four things are already true and may or may not help — stated as facts, not as compliance claims: per-user
install with no admin rights (proven), **no network code in the add-in** (verified against the source), a
standard `.addin` manifest, and Apache 2.0. [Q-38](from-master-specification-part-2.md#q-38--what-is-the-exact-install-command-new) is the
live piece and belongs to the GitHub route.

---
