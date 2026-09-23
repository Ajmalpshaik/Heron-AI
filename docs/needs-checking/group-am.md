# Needs checking — Group AM

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-24 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group AM - `report-connectors` in a chat: every connector in full, its system, and what a reply did not send (2026-09-23)

**The fragment went back from `PROVEN` to `DRAFT`** because its code changed: every connector line now carries its duct or pipe system type after the domain - `DomainHvac SupplyAir` - and `connectorSummary` carries the whole answer as one string, because through a chat its three dictionaries arrived as counts and the connectors never arrived at all ([FRAGMENT-ISSUES 5b-195](../fragment-issues/section-5b-rows-176-200.md)). The old proof - Snowdon Towers Sample HVAC, 2026-09-07 - is kept as the record of what ran and has to be replaced on a named model ([D-30](../DECISIONS.md)). A chat reply also names, on one `NOT SENT:` line, every dictionary that held something it did not send. Written 2026-09-23 on the owner's PC without driving Revit: **nothing below has run.**

| # | Check | Expected |
|---|---|---|
| **AM1** | The re-proof, in Project1 on Revit 2024, from this change's branch or after it merges: `python tools/batch-prove.py tools/jobs/report-connectors-project1-2026-09-23.yaml --dry-run`, then the same without `--dry-run` - ducts in the floor plan `1 - Mech` against walls in the same plan. Then read the draft, sign it with `python brain/heron_validate.py accept report-connectors --by "Ajmal PS"`, and set `heron-status: PROVEN` | `PASS`. The positive: an entry in `connectorFacts` for each duct, and `connectorSummary` naming each one - or, past 50 connectors, whole ducts and a last part saying how many were left out. Each line opens `DomainHvac` and a system word, and **the word on an OPEN duct end is the first reading of one anywhere**: the API calls an unconnected connector's system type undefined, so `UndefinedSystemType` there is Revit's answer and not a fault. The negative: `connectorSummary` empty, every dictionary `0 entry(ies)`, every wall in `noConnectors`. `NEG NOT EMPTY` means the summary said something when it had nothing to say |
| **AM2** | The chat, once this is merged and the Claude app restarted - the reply's new line is Python, which only a restart reloads, while the fragment is read afresh on every call. Select one air terminal or one piece of mechanical equipment and ask what connectors it has | `revit_read REPORT_CONNECTORS` prints `connectorSummary` whole, each connector opening with its domain and system word - `DomainHvac SupplyAir`, `DomainHvac ReturnAir` - and each matches the connector's System Classification in Revit. Under the list, one line: `NOT SENT: what is in connections, connectorFacts and openConnectors. Only how many entries each holds reached this reply.` No `NOT SENT` line means the server is still running the old code - compare its start time with the file's |
