# Fragment issues — section 3f

> One section of [the register](../FRAGMENT-ISSUES.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every section's place in it, are on that page.** What is
> written for this section goes in this file. [`tools/register-text.py`](../../tools/register-text.py)
> reads it back into the register for every tool that reads the register, so it is seen exactly
> as it was seen there. Written by [`tools/split-register.py`](../../tools/split-register.py).

## 3f. PROVED, BUT QUERIED — worth a second look at the sit-down

Marked `PROVEN` and standing when this section was opened, but the owner raised a doubt on the day and
it is recorded rather than argued away. A proof nobody questions is not the same as a proof that
survived being questioned.

| Fragment | Proved on | The doubt |
|---|---|---|
| `set-view-section-box` | 22 elements enclosed in `3D HVAC Layout`; the same call on `FloorPlan: M1` returned `viewRefused true`, `applied false`, 0 enclosed | **The negative may be testing Revit rather than the fragment.** A plan view *cannot* have a section box, so the empty answer is guaranteed by the view type and not by anything the fragment decided. A stronger negative would be a 3D view where the selection has no geometry to enclose — then the fragment has to reach the same conclusion by its own work |

**If the doubt is upheld, the remedy is to re-run it, not to un-prove it by argument** — and D-30's
fingerprint means the record says exactly what was run, so a better arrangement can replace it cleanly.

### SETTLED, and not by argument — 2026-09-09

The doubt was upheld, and the fix in §4 is what settled it: `viewRefused` now means only that the VIEW
refused, so the stronger negative the owner asked for — a 3D view whose selection has nothing to
enclose — is finally distinguishable from the weak one a plan view guarantees.

**`set-view-section-box` is back at `DRAFT`, and the reason matters more than the fact.** It was not
un-proved because the argument won. It was un-proved because the CODE MOVED: fixing the doubt changed
the implementation, the fingerprint stopped matching, and a proof is evidence about the bytes it was
taken against. Set back by the owner on 2026-09-09; the `proof:` block is kept as the record of what
was run.

> Un-proving by argument is what this section refuses. Un-proving because the implementation changed
> under the proof is not an argument at all — it is the fingerprint doing its job, and the only
> correct response to it is to run the fragment again.

---
