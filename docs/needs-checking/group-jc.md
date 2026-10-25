# Needs checking — Group JC

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-10-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads it, so a row here is seen
> exactly as it was seen there.

## Group JC - Cast Shadows ticked once and copied to other views through a view template: `COPY_GRAPHIC_DISPLAY_OPTIONS` version 1 (2026-10-24)

A new card, [`copy-graphic-display-options`](../../brain/fragments/copy-graphic-display-options/fragment.yaml),
written in a cloud session - the dialog and, on the owner's word the same day (*"cover all kind of
settings, even the sun settings, everything"*), the view's sun settings too - for [row 5b-1336](../fragment-issues/section-5b-rows-401-425.md): the owner asked for a
small shadow in a plan, and `SET_GRAPHIC_DISPLAY_OPTIONS` could set the shadow intensity but not tick
Cast Shadows.

**What Revit's API can and cannot do here.** Read off `RevitAPI.xml` of 2020 to 2027 on 2026-10-24
(the `Nice3point.Revit.Api.RevitAPI` packages the build uses): **no release has a member for Cast Shadows,
Show Ambient Shadows, Ambient Light or Photographic Exposure**, and 2025, 2026 and 2027 carry no new
display member at all. `BuiltInParameter.GRAPHIC_DISPLAY_OPTIONS_SHADOWS` and its siblings are the dialog's
buttons, not values. What every release does have is the view template route: a template carries the whole
Graphic Display Options dialog as one setting, `GRAPHIC_DISPLAY_OPTIONS`, and
`View.ApplyViewTemplateParameters` copies what a template includes onto a view once, with no link kept.
An Autodesk forum thread asks the same question and has no API answer
([Revit API forum, "Disable view shadows casting"](https://forums.autodesk.com/t5/revit-api-forum/disable-view-shadows-casting/m-p/9814744));
the include list is `SetNonControlledTemplateParameterIds`
([Autodesk developer blog, "View Template Include Setting"](https://blog.autodesk.io/view-template-include-setting/)).

**What is done and what is not.** The card compiles for every release 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-10-24, in the cloud) and its sentences reach it on this
container's backend. **Nothing has met a Revit.** The route rests on one thing no document says outright -
that `ApplyViewTemplateParameters` carries the Cast Shadows tick - and JC1 is the run that finds out.
Whether a view template carries a view's **Sun Setting** is not in the API reference either, so the card
also copies the source view's `SunAndShadowSettings` member by member - the same members `SET_SUN_SETTINGS`
(PROVEN) writes - and reads each back. `revit_change` refuses it until JC8 is signed.

**The rig (JC0).** The owner's Project1 house (Revit 2020, DefaultMetric), **saved** as
*Heron JC Shadows.rvt* in `%TEMP%\heron-work\group-jc` - a run needs a saved model. In it: *Level 1* with
Cast Shadows ticked by hand, shadow intensity 25 and its sun set by hand to a Still study at 10:00 on 21 June; *Level 1 - Colour 2* with Cast Shadows off and no view
template; *Level 2* given a view template *Heron JC Held* that includes Graphic Display Options.
[`tools/jobs/graphic-display-copy-jc-2026-10-24.yaml`](../../tools/jobs/graphic-display-copy-jc-2026-10-24.yaml)
runs JC1's rolled-back half and JC3.

| # | Run | Look for |
|---|---|---|
| **JC0** | The rig, by hand | The three views as above; nothing selected |
| **JC1** | *The copy.* `sourceView=Level 1`, `targetViews=Level 1 - Colour 2` - once through `try-once`, on the owner's yes, Keep | `viewsCopied` 1, `sunCopied` 1, `readBack` shadow 25 on the target; **the plan shows the shadows**, its Sun Settings read Still, 21 June 10:00 and its Graphic Display Options has Cast Shadows ticked; the View Templates dialog lists no new template; one Ctrl+Z takes it back |
| **JC2** | *Negative, by hand, FIRST.* `sourceView=Level 1 - Colour 2` (shadows off), `targetViews=Level 1` | `Level 1` shows **no** shadow afterwards - the copy carries OFF, and nothing the source did not hold is switched on |
| **JC3** | *Negative.* `sourceView=Level 1`, `targetViews=Level 2` | `viewsCopied` 0, refused *"... every one is held by its own view template ..."*, `blockedByTemplate` naming *Level 2* and *Heron JC Held*, NOTHING WAS CHANGED |
| **JC4** | *Across view types.* `sourceView=Level 1`, `targetViews` a 3D view and an elevation | Copied, or a finding *"Revit would not copy onto ..."* naming each - **not known** which |
| **JC5** | *A template as source.* `sourceView=Heron JC Held`, `targetViews=Level 1 - Colour 2` | Copied; *Heron JC Held*'s Include ticks read the same afterwards as before |
| **JC5a** | *The sun left alone.* JC1 with `sunSettings=keep`, and once onto a view whose sun was never touched | `sunCopied` 0 and the target's Sun Settings unchanged; and whether two views share one sun - **not known** |
| **JC6** | *DefaultMetric's own templates, by eye.* View > View Templates > Manage, each shipped template's Graphic Display Options | Whether any has Cast Shadows on - **not known from the cloud**; if one has, it is a ready source for JC5 |
| **JC7** | *Routing, on the PC's backend.* "turn on cast shadows in Level 1" with no source named | Whether it should reach this card (which needs a view to copy from) or `SET_GRAPHIC_DISPLAY_OPTIONS` (which says to tick it by hand); on this container's backend it reached this card |
| **JC8** | The signature, on the owner's word only, after JC0 to JC5a | `python brain/heron_validate.py accept copy-graphic-display-options --by "Ajmal PS"` |

A row that fails stays open; nothing in a row changes to make it pass.
