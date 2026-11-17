# NEEDS-CHECKING archive — Group BD

> **Checks that were done, moved out of the live register.** These are rows of Group BD of
> [`NEEDS-CHECKING.md`](../NEEDS-CHECKING.md) — *`link-family-parameter`: an element already in a family linked to a family parameter, or unlinked, and read back (2026-09-28)* — whose ID was struck through, the register's own
> sign that the check was done. Each still has its line in the register, with a link here. **Its words
> are unchanged; only its links were re-pointed.** Nothing new is written here: a new check goes in the
> register. Written by [`tools/archive-needs-checking.py`](../../tools/archive-needs-checking.py).

---
### Row BD1

*Moved from the register on 2026-11-17.*

**Check.** **By hand, the second route.** In the Family Editor, select the supply duct connector, press the button at the end of Flow's row in Properties, and read which parameter the Associate Family Parameter dialog has selected; then the return connector

**Expected.** **DONE 2026-09-29, by the owner's own hand-setting rather than a second look.** He re-linked both connectors with that dialog on 2026-09-28, before the tool existed - supply to `Actual_Supply_Air_Flow`, return to `Actual_Return_Air_Flow` - and confirmed it in the chat on 2026-09-29; the tool read exactly those two before any proof ran and after every proof. Written into the proof's `second_route`, which also says what it cannot see: a rolled-back change leaves nothing in the dialog to look at. Codex's second review asked for this route before PROVEN

---

### Row BD2

*Moved from the register on 2026-11-17.*

**Check.** The two legs for `link-family-parameter` ([D-30](../DECISIONS.md)) - **signed 2026-09-29.** With the family open in the Family Editor and `HERON_CLIENT_ID=ajmal-pc` set: `python tools/batch-prove.py tools/jobs/link-family-parameter-fcu-2026-09-28.yaml --dry-run`, then without `--dry-run`, then `python brain/heron_validate.py draft link-family-parameter --from brain/proof-drafts/runs/link-family-parameter.json`. Write BD1 into the draft's `second_route`, sign with `python brain/heron_validate.py accept link-family-parameter --by "Ajmal PS"` and set `heron-status: PROVEN`

**Expected.** `PASS` on `changed`: 1 moving the supply connector's Flow to `Designed_Supply_Air_Flow`, 0 and refused by kind for `Unit_Length`; Revit reports the group ROLLED BACK both times, and BD1's dialog still shows `Actual_Supply_Air_Flow` afterwards. **DONE 2026-09-29** - run again after both of Codex's review passes, signed as Ajmal PS on his word, fingerprint `ec38de660815c7c6`. The job file re-runs it any time; a run whose fingerprint differs is about other code

---
