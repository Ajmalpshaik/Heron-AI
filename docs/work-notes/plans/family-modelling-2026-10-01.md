# Family modelling - every form, its constraints, and a family from a picture

> | | |
> |---|---|
> | **Type** | One-time plan, being executed. Retires as described at the bottom |
> | **Asked for** | By the owner, 2026-10-01: *"do we have any capability to create family? ... extrude, revolve, all modelling capability ... the parameters, dimensioning, locking, all kind of thing ... maybe I will give only an image"* |
> | **Owner of this note** | The session building it; after that, whoever proves Groups BL, BM and BN |
> | **Authority** | None of its own. The fragment cards, [NEEDS-CHECKING](../../NEEDS-CHECKING.md) and [FRAGMENT-ISSUES](../../FRAGMENT-ISSUES.md) row 5b-261 are where each fact lives |

## Where it started

Before this plan, the library could build a family only out of **rectangular boxes locked between four
planes** - [`family-creation`](../../../brain/skills/family-creation.yaml) and its nine fragments, all
`DRAFT`, [Group AN](../../needs-checking/group-an.md). No revolve, blend, sweep, swept blend, void, round
shape or free profile existed, and nothing else in the library answered those words (searched
2026-10-01 with `brain/heron_retrieve.py`, and the duplicate and Matcher gates found nothing to start
from).

## The plan - three pull requests, each standing on the one before

| # | What | Group | State |
|---|---|---|---|
| 1 | **The forms.** `create-family-extrusion` (any shape, holes, solid or void), `-revolution`, `-blend`, `-sweep`, `-swept-blend` - FRG-GEO-038 to 042 | [BL](../../needs-checking/group-bl.md) | built, DRAFT, compiles 2020-2027 |
| 2 | **What makes them parametric, and makes a void cut.** Join or cut forms through the family editor's own geometry combination; lock any form's flat faces to named planes; label a round form's radius or diameter; set a form's visibility by detail level and view | BM | next |
| 3 | **A family from a picture or a sketch.** The `family-creation` skill widened to every form, and the order a host follows from an image: category, template, sizes, planes, parameters, forms, locks, labels, flex | BN | after 2 |

**Stacked, not side by side.** Each adds fragments, so each changes the same derived counts and takes the
next NEEDS-CHECKING letters; two opened from `main` at once would conflict on both. The second and third
are opened against the branch before them, and say so.

## What was decided, and where each decision is recorded

- **One tool per form, not one tool with a `kind`** - a request need cannot be left out unless it is a
  `bool`, so a combined tool would make the host type values for inputs its form does not have. Row 5b-261.
- **One shape language** for every form - loops of `circle`, `rect`, `polygon` or corners with
  `arc MX,MY X,Y`, in millimetres, in the work plane's own two MODEL coordinates. Each card's purpose.
- **A void cuts nothing until combined.** The solid-cut utilities refuse an ordinary family document and
  the join utility says it is not for family documents, 2020 to 2027, in Autodesk's own remarks; the
  geometry combination is the family editor's own join and cut. Row 5b-261; built in PR 2.
- **A circle's size may be drivable after all.** The radial and diameter dimension calls exist in a
  FAMILY on every release; the "impossible on 2020 to 2024" measured in
  [16 §Break 2](../../16-version-support-strategy.md) is for projects. Whether a label on a family
  circle drives it is NEEDS REAL REVIT. Row 5b-261; asked in PR 2.

## How it was researched, so it can be done again

The RevitAPI XML documentation ships inside the `Nice3point.Revit.Api.RevitAPI` NuGet package, one per
release, next to the reference assembly `tools/check-api-surface.py` already downloads. Reading every
member's summary, remarks and exceptions across all eight answered every "which release has it" and
most "what does Revit do with it" questions without a Revit. The Revit SDK's own samples
(`GenericModelCreation`, `WindowWizard`) were read for how the calls fit together, and nothing was copied
from them. The Autodesk forum was out of reach from the cloud session.

## Closes when

Every row of Groups BL, BM and BN has been run on the owner's PC - passed, or turned into a defect row -
and the skill's order has been followed once from a picture (BN). At that point the durable half is
already in the cards, the register and row 5b-261; this note is deleted and its row in the
[work-notes index](../README.md) struck.
