# Needs checking — Group PH

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group PH - the skill runner's first runs: a job method's plan run whole, both legs, judged and signed by a person (2026-11-21)

**Tracked as GitHub issue [#1196](https://github.com/Ajmalpshaik/Heron-AI/issues/1196) since 2026-11-21** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

**Why:** [FRAGMENT-ISSUES row 141](../fragment-issues/section-5-rows-126-150.md) - nothing could run a skill's
whole plan, so no skill had ever been proved. Built 2026-11-21 in a cloud container with no Revit:
`heron_bridge_client.py plan` runs every step of a plan in order on one lease and one chain (reads as
`run_fragment_read`, changes as `run_fragment_write` with no `apply`, so rolled back; a PUBLISH or ADMIN step
refused; each later step's `expect: previous` sent as `expectChain`; stopped at a failed step);
[`tools/run-skill.py`](../../tools/run-skill.py) runs the positive and the negative leg;
[`brain/heron_skill_proof.py`](../../brain/heron_skill_proof.py) judges the two run records into a draft, and a
person signs a PASS into [`brain/skill-proofs/`](../../brain/skill-proofs/README.md). `tests/test_skill_runner.py`
holds all of it through a fake Revit. **Nothing here has run in Revit** - and three things only Revit can say
decide whether the judging is right.

**The model is a SAVED scratch copy, never a working project.** Every change in a plan run is rolled back.
PowerShell in the Heron folder, `--session <process id>` when two Revits are connected.

| ID | Do this | Pass looks like |
|---|---|---|
| **PH1** | *The first skill proof.* A scratch copy with ducts on one level and none on another. Fill in [`tools/jobs/skills/count-elements-plan.yaml`](../../tools/jobs/skills/count-elements-plan.yaml) (the model, a level with a few ducts, a level with none); `python tools/run-skill.py tools/jobs/skills/count-elements-plan.yaml --dry-run`, then without `--dry-run` | Both legs run, each step `ok`; count-elements' note *"<- elements from filter-elements-by-category (N)"* in the positive; the draft **PASS** with no gaps. **Record** the positive count against the Project Browser or a schedule of ducts on that level. Then sign only if it is right: `python brain/heron_skill_proof.py accept count-elements --by "Ajmal PS"` |
| **PH2** | *Zero from an empty filter* - read off PH1's negative leg | **Record** what count-elements answered when the filter found nothing: `count 0` (the plan's negative is what the judge reads) or `needs_unbound` / a count of the selection. Anything but `count 0` means the negative cannot be arranged this way, and the judge reports NEGATIVE DID NOT RUN or NEGATIVE NOT EMPTY - say which |
| **PH3** | *The expectation refuses a stale chain.* Run PH1's positive leg, then run only count-elements with `expect: previous` after a different read (e.g. a plan of `list-levels` then `count-elements`, the second expecting the first) | The second step refused - `chain_mismatch` or the add-in's own word - with nothing counted, and the run record's step `ok: false`; the plan stops. **Record** the add-in's words. A count here means `expectChain` did not guard the step and row 148's hazard is open again |
| **PH4** | *A change in the middle of a plan.* A MODIFY skill whose write is not its last step, if one is found; otherwise a two-step job of a MODIFY step then a read of what it changed (write the job by hand, `setup` on neither) | **Record** whether the step after a rolled-back change saw the change: the change runs inside its own group and is rolled back before the next step, so the next step should read the model as it was. If it did see it, a plan's later reads are judging a model nobody keeps. Either way the model is as it was after the run - **Ctrl+Z should have nothing of Heron's to take back** |
