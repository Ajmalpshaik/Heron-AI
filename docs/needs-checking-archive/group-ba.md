# NEEDS-CHECKING archive — Group BA

> **Checks that were done, moved out of the live register.** These are rows of Group BA of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *the Sheet Issues/Revisions dialog from chat: every column, Move, Merge, Delete, Numbering and the arc length (2026-09-28)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row BA10

*Moved from the register on 2026-11-17.*

**Check.** **Revit 2020, the two legs ([D-30](../DECISIONS.md)) for version 3 - the count.** On "Project2", a scratch project, in front - NEVER the job model. Arrange first, kept, by chat: CREATE_REVISION three times, description *HERON PROOF several*, numbering numeric, not issued; LIST_REVISIONS shows exactly three such rows. Then `python tools/batch-prove.py tools/jobs/edit-revision-several-2026-10-10.yaml --dry-run`, then with `--session <the Revit 2020 pid>` and without `--dry-run`

**Expected.** `PASS`. The binding note reads *elements from select-by-parameter-value (3)*. Positive (`confirmCount=3`, issued by M.Sagheer, issued to A.Rahmani, issue): `changed` names all three, apart by " \|\| ", each ending *issued*; `nowIssued` true. Negative (`confirmCount=2`): `refused` names all three and both numbers, `changed` empty. Then LIST_REVISIONS: the three still unissued with issued by and to blank (both legs rolled back) - **Struck 2026-10-18:** run 2026-10-17 on "Project1", Revit 2020 - PASS, *2026-10-17 - run on Revit 2020* below - and signed with BA14: `edit-revision` PROVEN, fingerprint `d3169656228813bf` in its `fragment.yaml`.

---

### Row BA11

*Moved from the register on 2026-11-17.*

**Check.** **Revit 2020, version 3 - the order, and the issued-revision refusal.** On the same scratch project, after BA10's three: CREATE_REVISION three times more, *HERON PROOF several issued*, numbering numeric, ISSUED - they must be the LAST three sequences. Then `tools/jobs/edit-revision-several-issued-2026-10-10.yaml`, dry run first

**Expected.** `PASS`. Positive (`confirmCount=3`, unissue, numbering alphanumeric): `changed` *un-issued; numbering Numeric -> Alphanumeric* on all three, `refused` empty - the request version 2 refused for Seq. 1 first. Negative (issued by, no unissue): `refused` naming all three as ISSUED and *issued by*, none changed - the refusal BA2's job could not arrange. Then LIST_REVISIONS: all three still issued and Numeric - **Struck 2026-10-18:** run 2026-10-17 on "Project1", Revit 2020 - PASS, *2026-10-17 - run on Revit 2020* below - and signed with BA14: `edit-revision` PROVEN, fingerprint `d3169656228813bf` in its `fragment.yaml`.

---
