# Needs checking — Group S

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group S — groups and assemblies, and the edit that lands in twelve places (needs Revit)

`HERON-REVIT-GRP-033` — [`revit/Heron.Revit.Addin/RevitGroups.cs`](../../revit/Heron.Revit.Addin/RevitGroups.cs).
Compiles on 2020–2027, 0 errors, 2026-09-16. **Tracked against two models 2026-09-19 and signed** ([`brain/agent-proofs/HERON-REVIT-GRP-033.yaml`](../../brain/agent-proofs/HERON-REVIT-GRP-033.yaml)) - so it is no longer NEVER RUN. That proof is D-53 tracking and it is NOT these rows: the cases below are still unchecked, and tracking says the answer follows the model, not that any one of them is right. Read-only — nothing is grouped, ungrouped
or edited, and no transaction is opened.

Use a model that actually uses groups: one group type placed several times, ideally one nested group,
and if you have one, an assembly. **A model with no groups proves almost nothing here.**

**This group closes a guess that is already in shipped code.** `E10` above is proved: a move of a group
member returns cleanly and shifts nothing, no exception and no warning, so `RevitWrite` compares
positions either side and then reports *"almost certainly inside a group"*. That hedge is there because
nothing could check beforehand. These rows are whether it can now.

| ID | Do this | Pass looks like |
|---|---|---|
| **S1** | `revit_groups` with no category, on a model you know | Every group type in the Project Browser is listed, with the same names, and `placements` matches how many you can count. A group Revit shows and Heron does not is the finding |
| **S2** | Compare `members` against what you see when you edit the group | The same number. It is counted off ONE instance — if it disagrees with the browser, either a member id is resolving to nothing or instances of one type do not hold the same content, and the second would be news |
| **S3** | Check a group definition that is in the browser but placed nowhere | `placements` reads 0 and `members` is **absent, not zero**. There is no instance to count off, and a zero there would mean "empty group" rather than "unknown" |
| **S4** | **The row this agent exists for.** Put two ducts in a group, place that group 12 times, then ask `revit_groups("ducts")` | `mostPlacements` reads 12 and the ducts are listed with their group type. **Then do E10's move and see the two answers agree** — this one before, RevitWrite's after |
| **S5** | Ask for a category with no grouped elements in it | It says so in words: an edit reaches exactly one place. An empty list and *"nothing here is grouped"* must not read the same |
| **S6** | Pin one duct, group another, and ask | They come back as **two different rows with different reasons** — `pinned` true on one, `inAGroup` true on the other. A group member is NOT pinned, and merging the two would send somebody to unpin something that needs ungrouping |
| **S7** | Nest a group inside another group and put a duct in the inner one | `nested` is true and `chain` has two entries, innermost first, each with its own `placements`. **Then check the arithmetic by hand** — how many places does editing that duct really reach? This is the row that settles whether the counts multiply, which the code deliberately refuses to assume |
| **S8** | A detail group and a model group in one model | `kind` tells them apart, using Revit's own category names. A detail group lives in one view and a model group does not |
| **S9** | An assembly with elements in it | It appears under `assemblies` with a member count. **Then edit a member and check whether another assembly of the same type changed** — this is the question the code refuses to answer and this row is where it gets answered |
| **S10** | A category of several thousand elements across a few group types | It answers in a few seconds. The placement count is cached per group type; if it hangs, the cache is not being hit |
| **S11** | Pin to one project, switch Revit to another, ask again | Refused, nothing read. Approving an edit against the wrong model's placement count is how twelve of the wrong rooms change |

**Why this group exists at all:** editing one element inside a group edits it everywhere that group is
placed. That is what groups are *for* — and it is a bad surprise when nobody told you the element was in
one. Revit raises no error, so the only defence today is knowing your own model.

**What it deliberately does not do:** multiply the nested counts, or explain assemblies. Whether an inner
group's placement count already includes the copies carried inside its parent is a question about Revit,
not about this code, and `S7` is what settles it. Whether an edit inside one assembly travels to another
of the same type is `S9`. Both would have been easy to guess at and a wrong number here reads exactly
like a right one — [D-52](../DECISIONS.md).

---
