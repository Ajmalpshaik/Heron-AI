# FRAGMENT-ISSUES archive — section 5b, rows 876 to 900

> **Finished rows, moved out of the live register.** These are rows from section 5b of
> [`FRAGMENT-ISSUES.md`](../FRAGMENT-ISSUES.md) — Heron's own defects found by reading the repository — whose state said they were
> finished when they were moved. Each still has its line in the register: the same number, a
> one-line title, the opening of its state, and a link to its full text here.
>
> **Nothing below was rewritten** except its links, which were re-pointed one folder deeper and
> each checked to reach the same file it reached before. Where a row disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win** — a row records what was true on
> the day it was written.
>
> **Nothing new is written here.** A new defect goes in the register, and a row here that turns
> out not to be finished is reopened **there**, on its own line.
>
> Rows 876 to 900 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-880

*Moved from the register on 2026-11-17.*

**Defect.** **`LIST_REVISIONS` THREW ON A FAMILY DOCUMENT - *"ccda is not a project document. Parameter name: ccda"* - AND, BECAUSE A PROJECT ALWAYS KEEPS AT LEAST ONE REVISION, IT HAD NO NEGATIVE CASE THE D-51 RULES COULD PASS.** Found 2026-10-17 re-proving [`list-revisions`](../../brain/fragments/list-revisions/fragment.yaml) on Revit 2020 (session 51568) for NEEDS-CHECKING Group BA: `revit_read capability=LIST_REVISIONS` with Ajmal PS's family `TRG_GEN_CutOut_Rectangular_HVAC` in front answered *"'list-revisions' threw while running: ccda is not a project document."* - Revit's words about an internal argument, from `RevisionSettings.GetRevisionSettings`, which only a project has. The same day showed why it mattered for the proof: Revit refuses to delete a project's last revision ([`DELETE_REVISION`](../../brain/fragments/delete-revision/fragment.yaml)), so `elements` and `revisionTable` - both `role: result` - are never empty in any project, and the 2026-09-13 proof's negative (a template revision ON NO SHEETS) is `NEG NOT EMPTY` under `looks_empty` as it reads today. A family, the one document with no revisions, was the only negative left, and it threw.

**State.** **FIXED 2026-10-17 - RUN in Revit 2020, SIGNED by Ajmal PS and PROVEN the same day.** `if (doc.IsFamilyDocument)` at the top of the fragment: a family answers *"'<title>' is a family, and a family holds no revisions - Sheet Issues/Revisions belongs to a project. Bring a project to the front."* with every result empty; a project runs exactly as before. `tools/check-fragments-compile.py`: every fragment compiles on 2020 to 2027. Run with [`tools/jobs/list-revisions-project1-2026-10-17.yaml`](../../tools/jobs/list-revisions-project1-2026-10-17.yaml): `PASS` - positive on the scratch "Project1" (six revisions made by hand, the 2020 numbering route reading Numeric and Alphanumeric A to Z), negative on the family (`elements` 0, `revisionTable` empty, the finding above). The purpose and utterances are unchanged, so no routing moved. Not covered: a revision on a sheet on Revit 2020 - no open project had one.

---

### Row 5b-900

*Moved from the register on 2026-11-17.*

**Defect.** **`heron_validate.py` STILL WROTE FIVE PATHS WITH A RAW `os.path.relpath`, SO `accept --release` AND `accept --second-route` CRASHED ON WINDOWS WHILE FORMATTING THEIR "NO DRAFT" REFUSAL WHEN THE RUN RECORD SAT ON ANOTHER DRIVE - [ROW 5b-152](../fragment-issues/section-5b-rows-151-175.md)'S TRAP, IN CODE WRITTEN AFTER ITS AUDIT.** Found 2026-10-17 on the owner's PC (repository on D:, temp folder on C:) merging main into PR #584: `tests/test_release_proofs.py` (D-131, #582) exited 1 with *"test_a_release_entry_is_accepted could not run: ValueError: path is on mount 'C:', start on mount 'D:'"* - the suite's drafts folder is a temp folder, and the refusal's own sentence raised. Linux CI has one mount and cannot see it. The five calls: the two `no draft` refusals, the `draft` record-location refusal (twice) and `Drafted ...`.

**State.** **FIXED 2026-10-17**: all five go through `heron_fragment.repo_relative(path, ROOT)`, heron_relpath's rule, which names a path whole when it has no relative form. `tests/test_release_proofs.py` section 10 FORCES the two-drive condition, as `tests/test_relpath.py` does, so it fails on every machine before the fix (both refusals raised, and a source check found a raw `os.path.relpath`) and passes after; the whole suite passes on the owner's PC

---
