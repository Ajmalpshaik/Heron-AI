# HANDOVER — 2026-09-08 (the fragment-review track): ten new fragments, none proved

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**READ THIS FIRST IF YOU ARE SITTING DOWN AT THE PC.** Ten fragments were added today and **not one of
them has been near a real model.** They compile on all eight releases, which proves only that the API
surface agrees. Everything below is what to test, and roughly in what order.

### What was added, and what each one needs from you

| Capability | Fragment | Risk | The one thing to check |
|---|---|---|---|
| `REPORT_OPEN_DOCUMENTS` | `report-open-documents` | READ | Two projects open with the SAME title. Both must be listed with their paths, and the collision said out loud |
| `SWITCH_ACTIVE_PROJECT` | `switch-active-project` | EXECUTE | It only REQUESTS the switch. Revit performs it after the operation ends, so check the tab bar afterwards - and never chain a write onto it assuming it landed |
| `OPEN_VIEW` | `open-view` | EXECUTE | Ask it for a VIEW TEMPLATE by name. It must refuse, not fail obscurely |
| `CLOSE_VIEW_TABS` | `close-view-tabs` | EXECUTE | Ask it to close EVERY open tab. It must keep one - the ACTIVE one - because closing the last view closes the project |
| `SELECT_BY_CONNECTOR_SIZE` | `select-by-connector-size` | READ | An exact 230 × 230 selection, then hover a match and read the size off the connector. This is the fragment most exposed to a units error |
| `PLACE_FAMILY_ON_FACE` | `place-family-on-face` | MODIFY | Place on a ceiling, then MOVE THE CEILING. If the instances stay behind they were never hosted, whatever it reported |
| `CREATE_FLEX_DUCT` | `create-flex-duct` | MODIFY | A route over the length limit must create NOTHING. Then TRACE_CONNECTIVITY: the diffuser must still read as OPEN, because this makes flex and does not join it |
| `CREATE_ELECTRICAL_CIRCUIT` | `create-electrical-circuit` | MODIFY | **Run it TWICE on the same devices.** What Revit does then is the one thing that was deliberately not guessed at |
| `PROPOSE_MEP_OPENINGS` | `propose-mep-openings` | READ | A duct crossing a wall at 45°. The through-thickness must read LONGER than the wall - that is what the solid intersection buys over a bounding box |
| `DISCONNECT_CONNECTORS` | `disconnect-connectors` | MODIFY | Count the elements before and after. The count must be IDENTICAL - it disconnects and deletes nothing |

### What else changed today

- **`HANDOVER.md` and `NEEDS-CHECKING.md` moved into `docs/`.** The root now holds entry-point files
  only. Every link was repointed, including the one place that READS the register rather than linking
  it (`tools/check-gaps.py`).
- **Two real defects fixed in existing fragments.** `check-model-standards` treated a pattern with no
  wildcard as a PREFIX, so the rule `Supply Diffuser` accepted `Supply Diffuser OLD` - every exact rule
  in every project was silently a prefix rule. `import-parameter-values` read the file line by line
  before parsing it as CSV, so an Alt+Enter cell became three rows.
- **Stale documentation corrected.** `brain/README.md` claimed thirty-two fragments all `DRAFT` and
  that the executor was not built. `CONTRIBUTING.md` said there was no implementation yet.

### The thing worth carrying forward

**Four mistakes were made today and reading the code caught none of them.** The compiler refused
`ElectricalSystem.Create` with the wrong collection type; `heron_graph.py --orphans` found a fragment
nothing could ever reach; and a deliberate second pass over the finished diff found one fragment
answering an empty list to a question nobody asked, and another doing two hundred times the geometry
work it needed. The tools earned their keep, and so did re-reading the diff after believing it was
done.

---
