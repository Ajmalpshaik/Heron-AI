# Session note — A CLICK IN REVIT ANSWERS THE CHAT, FIVE EVERYDAY JOBS GET A METHOD, AND A CLOUD CHAT NOBODY SET UP GETS A LIBRARY

> **Archived session note** from 2026-11-11. It was moved out of [`HANDOVER.md`](../HANDOVER.md) on
> 2026-11-11 by [`tools/archive-handover.py`](../../tools/archive-handover.py), so that file could go back to
> being the short live entry point it is supposed to be. **Its words are unchanged; only its links were
> re-pointed.** Nothing here is specification: where it disagrees with [DECISIONS.md](../DECISIONS.md),
> the [Golden Rules](../14-golden-rules.md) or the [Constitution](../../HERON_CONSTITUTION.md), **those
> win**. A note records what was true on its own day.

---

### 2026-11-11 — A CLICK IN REVIT ANSWERS THE CHAT, FIVE EVERYDAY JOBS GET A METHOD, AND A CLOUD CHAT NOBODY SET UP GETS A LIBRARY

Written to the rule in [section 11](../HANDOVER.md#11-handover--2026-09-12): what is NEW, a MISTAKE worth not repeating,
what is still TO DO. Everything here was built in a cloud container; **nothing in it has run in Revit.**
The fixes of the day are in `git log --first-parent d77439ad..` on the branch and in their rows, not here.

**NEW.**

- **`PICK_ON_SCREEN`**, DRAFT: Revit waits for ONE click in the view in front and hands the point back in
  millimetres, so *where* no longer has to be typed as three numbers. It changes nothing in the model
  ([row 5b-1507](../fragment-issues/section-5b-rows-401-425.md), [Group NH](../needs-checking/group-nh.md)).
- **Five methods, all DRAFT:** `issue-drawing-package`, `health-audit-cleanup`, `project-setup`,
  `views-per-level`, `room-setup`. Every capability they name was already in the library; the order was
  missing ([row 5b-1518](../fragment-issues/section-5b-rows-401-425.md)).
- **Off Windows, with no store named, the library-only server makes one of its own** - so `heron_lookup`
  and `heron_method` answer in a cloud chat nobody set up. [D-153](../decisions/D-153.md), **Proposed**
  ([row 5b-1508](../fragment-issues/section-5b-rows-401-425.md)).

**MISTAKES worth not repeating.**

- **A method named a route as proven that its card refuses.** `room-setup` said `PLACE_ROOMS` numbers and
  names rooms; the card's names and numbers are for areas, and with rooms the call is refused. A review
  caught it. Check each step of a method against the card's own refusals in its code, not against its name.
- **A sketch plane chosen by reasoning.** `DIVIDE_PARTS` version 2 first took *"the largest flat face of
  the first part"*, with no evidence. RevitAPI.xml on all eight releases, and two public add-ins, already
  gave the answer - a horizontal plane. Read the API's own text and public code before reasoning a choice.
- **An utterance that took a sentence from placement.** *"put it here"* on `PICK_ON_SCREEN` moved that
  sentence off `SHOW_ELEMENTS` and `PLACE_FAMILY_INSTANCES`, and the chat was shown no placement card.
  Measure a new utterance main against branch (`score-routing --sentences`) before it goes on a card.
- **Looking for a folder before making it.** Two servers started together both saw none; the second's
  error was swallowed and it served its whole life with no store. Make it with `exist_ok` and no look
  first. `heron_scope.Store` still looks first ([row 5b-1520](../fragment-issues/section-5b-rows-401-425.md)).
- **An unset shell variable in a redirect.** A log path built on a variable that was empty wrote the gate
  logs to the root of the filesystem - `/gate.check-intrusion.lexical.log` and two more were there on
  2026-11-11. Use `${VAR:?}` in a path, and keep rough work in the scratch folder the session names.

**TO DO.**

- **On the owner's PC, Revit open:** [Group NH](../needs-checking/group-nh.md), with somebody at Revit to
  click; [Group NI](../needs-checking/group-ni.md) - NI7 waits on
  [row 5b-1510](../fragment-issues/section-5b-rows-401-425.md)'s repair;
  [Group DQ](../needs-checking/group-dq.md) DQ3a to DQ3d and [Group DR](../needs-checking/group-dr.md) DR6a and
  DR10a to DR10c, the proofs of `DIVIDE_PARTS` version 2, `SET_RAILING_TYPE` version 2 and
  `COPY_ELEMENTS_BETWEEN_DOCUMENTS` version 4. The job files are in `tools/jobs/`, dated 2026-11-11.
- **On the PC's own backend:** `python tests/test_routing_phrases.py` with the re-ranker. Rows 5b-1513 and
  5b-1514 were measured on a proxy of it, **not on it**, and close or stand on that run; rows 5b-1512 and
  5b-1517 are the same suite with no re-ranker.
- **The owner's to decide:** [D-153](../decisions/D-153.md), accept or overturn; [Q-86](../open-questions/tier-2.md) -
  how a card says it is waiting for a person, and whether a face can be clicked.
- **One cloud chat with no `HERON_KNOWLEDGE`,** on a branch carrying D-153's change: ask `heron_lookup`
  once (row 5b-1508, seen from a test host only).
- **Rows recorded and not repaired:** `python tools/open-defects.py` lists them - 5b-1505 to 5b-1507,
  5b-1509 to 5b-1515, 5b-1517, 5b-1518 and 5b-1520 on 2026-11-11; 5b-1507 is `PICK_ON_SCREEN`'s own row,
  open until Group NH is run. **It does not list 5b-1519**, whose
  state begins *RECORDED, NOT FIXED* and not *OPEN*; that row is unrepaired too.
- **Five rows from the documentation pass of the same day,** 5b-1529 to 5b-1533: a structural floor,
  insulation on fittings and a structural plan are not built; a slanted column is in
  `PLACE_LINE_BASED_FAMILY`'s code, on no card's words and never run; and Article 9 of the Constitution
  still puts *Ask to Keep* on the ribbon, which is the owner's to amend (D-150).
- **No group holds a run of the five methods.** Row 5b-1518 says what each still needs from a real Revit.
