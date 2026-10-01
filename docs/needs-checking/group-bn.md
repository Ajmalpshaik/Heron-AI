# Needs checking — Group BN

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-01 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group BN - a family built from a picture: the `family-creation` skill widened to every form, the locks, a round size and voids that cut (2026-10-01)

**The owner's PC, in a chat with Heron connected, Revit 2024 first, then 2020.** The third of the three
changes the owner asked for on 2026-10-01 - *"maybe I will give only an image"*. No new tool: the
[`family-creation`](../../brain/skills/family-creation.yaml) skill now lists every form tool from
[Group BL](../needs-checking/group-bl.md) and every tool from [Group BM](../needs-checking/group-bm.md), in the
order a family has to be built, and opens with a step zero - **read the thing first**: what it is, its
category, the forms it is made of, every size, which sizes vary - said back as one list, with every size
ASKED for, because a picture gives shape and proportion and never size (D-33).

**What is already known, and it is NOT a proof.** The skill is well-formed (`python brain/heron_skill.py`),
every capability it names exists in the library, and its new sentences route to capabilities it declares
(`python tools/check-skill-routing.py`, 2026-10-01). **Nothing about following it has been watched.** Like
every skill it is `DRAFT`; a skill is proved by a real chat following it to a family that flexes.

**The arrangement:** a fresh family from the template each row names, open in front, Heron connected and
Changes on; the picture or sketch given in the chat. Each family is thrown away after its row.

| # | Check | Expected |
|---|---|---|
| **BN1** | A PHOTO of a square ceiling diffuser with a round neck, and nothing else - no sizes. *"Make a Revit family of this"* on the metric Generic Model template | The host says back what it sees - Air Terminals; a square face, a box; a round neck, an extrusion on the box's top - and **ASKS for every size and name in one list before building anything.** A host that builds with sizes it scaled off the photo FAILS this row, whatever it builds |
| **BN2** | BN1 again, answering the list: face 595 x 595 x 50, neck 250 diameter x 100, type "600 x 250", Supply Air | Built in the skill's order: category, parameters, values, formula if any, planes, labels, the box, the neck (CREATE_FAMILY_EXTRUSION), the neck's top LOCKED to its plane, the neck's diameter LABELLED, the connector, then REPORT_FAMILY_FORMS and FLEX_FAMILY at 795 x 795 with a 300 neck. **Record every place it strays from the order** - the evidence for whether a skill's order should be written as steps a host must follow, rather than prose it reads |
| **BN3** | A hand SKETCH with sizes on it: a pipe support plate - an L, 200 x 150, 10 thick, with two 14 mm holes | An L-shaped CREATE_FAMILY_EXTRUSION, two VOID extrusions for the holes, COMBINE_FAMILY_FORMS cutting them, its volume dropping by the two bores; flexed at another plate size if the modeller made one a parameter |
| **BN4** | A picture of something TURNED - a valve handwheel, or a cone reducer - with its sizes given | A CREATE_FAMILY_REVOLUTION from a half-section, not a stack of extrusions; LABEL_FAMILY_RADIUS where a round size is a parameter, and BM3's answer read again in the flex |
| **BN5** | BN2 on **Revit 2020** | The same family, the same flex |

**Not in this group:** materials, reference lines and angles, nested families - the skill's own NOT YET
line says so.
