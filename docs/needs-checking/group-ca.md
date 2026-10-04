# Needs checking — Group CA

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CA - family parameters switched between type and instance, in the order their formulas allow (2026-10-04)

**The owner's PC, Revit 2024, on an UNSAVED scratch family made for the purpose - never on a saved family
of his.** Asked for on 2026-10-04, when his PPR gate valve would not resize to its pipe because its sizes
are type parameters (row [5b-308](../fragment-issues/section-5b-rows-176-200.md)):

- [`add-family-parameters`](../../brain/fragments/add-family-parameters/fragment.yaml) - `ADD_FAMILY_PARAMETERS`, FRG-PAR-022, version 2 (`switchExisting`)

**What is already measured, and it is NOT a signed proof.** Scratch Family17 (Revit 2024, session 8804,
made with CREATE_FAMILY_DOCUMENT from Metric Generic Model and never saved) was built as the gate valve is,
in small: A = 50 mm (type), B = A * 2 and C = B + 1 mm (type formulas), a Domestic Cold Water pipe connector
sized by A, and a dimension between two planes labelled B. Run with `validate --in Family17 --write`, rolled
back: all to instance switched C, B, A and read back as instance with the connector on A and the label on B
kept; A alone was refused naming B. With the family kept as instance and read by REPORT_FAMILY_PARAMETERS
(formulas unchanged), back to type switched A, B, C; C alone was refused naming C. The family was put back
to type afterwards and read again. Compiles on all eight releases (2026-10-04).

| # | Check | Expected |
|---|---|---|
| **CA1** | The run records in `brain/proof-drafts/runs/` read and signed | `accept --by` on the owner's word; until then DRAFT |
| **CA2** | On a COPY of a family whose nested family has a TYPE parameter linked to a host parameter, that host parameter switched to instance | Refused naming the nested family and its type parameter; nothing switched |
| **CA3** | On a COPY of a saved family with a type catalogue (.txt) beside it, a catalogue column switched to instance | Refused naming the column; nothing switched |
| **CA4** | A reporting parameter switched to type | Revit's own refusal quoted first and the whole call rolled back - **record the words**: whether Revit refuses this is not yet measured |
| **CA5** | The first positive case on Revit 2020 | The same order and read-back as on 2024 |
| **CA6** | The gate valve itself, in the session that owns it, `all` to instance | Either switched with the connectors and the nested handwheel's link kept, or refused naming exactly what blocks it - **never touched from this group's runs** |
| **CA7** | A family holding a parameter really called `All`, switched with parameterNames all | Refused as ambiguous, nothing switched (added after review, 2026-10-04; compiled, not yet run) |
| **CA8** | An Image parameter switched to instance | Refused by name before anything changes - Revit's MakeInstance throws on one (added after review; compiled, not yet run) |
