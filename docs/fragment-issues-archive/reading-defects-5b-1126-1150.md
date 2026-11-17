# FRAGMENT-ISSUES archive — section 5b, rows 1126 to 1150

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
> Rows 1126 to 1150 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1126

*Moved from the register on 2026-11-17.*

**Defect.** **[`MOVE_ELEMENTS`](../../brain/fragments/move-elements/fragment.yaml) MOVED A REVIT LINK INSTANCE 5000 MM AND ANSWERED `moved 0`, `unverified 1` - IT READS A MOVE BACK FROM THE ELEMENT'S `Location`, AND A LINK INSTANCE'S PLACE IS ITS TRANSFORM. THE CARD IS PROVEN.** Measured 2026-10-18, Revit 2020 session 30308, scratch Project1: `SELECT_BY_CATEGORY_NAME` *RVT Links* in a 3D view handed in instance 345450 (`HeronLinkSource.rvt : 2`, linked by hand), `offset=5000,0,0`, kept -> `moved 0, attempted 1, unverified 1 [345450]`, no refusal. A scratch read then found the instance's `GetTotalTransform().Origin` at 5000, 0 mm - it had moved. A rolled-back run answered the same. A modeller told *moved 0* moves it again and ends 10 m out.

**State.** **MOVED TO ISSUE [#917](https://github.com/Ajmalpshaik/Heron-AI/issues/917) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN.** Read a `RevitLinkInstance` (and an `ImportInstance`) back by its transform origin before and after, and count it.

---

### Row 5b-1130

*Moved from the register on 2026-11-17.*

**Defect.** **THE HVAC AND FIRE DESIGN CONTRACTS DECLARE THREE REFUSALS THE CODE NEVER PRODUCES BY THOSE NAMES: `HERON-MEP-HVD-001` AND `HERON-MEP-FPD-002` LIST `NEEDS_REQUEST_VALUES`, `UNUSABLE_REQUEST_VALUES` AND `UNKNOWN_CALCULATION`, AND `heron_hvac.run` / `heron_fire.run` ANSWER WITH A `status` OF `missing`, `refused` OR `unknown`.** Found 2026-10-18: `python tools/generate-contract-reference.py` prints six `UNREACHABLE` lines for the two agents (`brain/agents/HERON-MEP-HVD-001.yaml` and `brain/agents/HERON-MEP-FPD-002.yaml`, each under `failures:`). Row 95 is the other direction (refusals produced and never declared); this is a caller written to the contract waiting for three words it is never given, while the code's own three - which read as the same three cases - come back undeclared.

**State.** **MOVED TO ISSUE [#913](https://github.com/Ajmalpshaik/Heron-AI/issues/913) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Either the contracts take the code's words or the code reports the contract's; row 95's open question - what a contract's `version` means when it was written wrong from birth - decides which costs a MAJOR version. The owner's call, with row 95.

---

### Row 5b-1132

*Moved from the register on 2026-11-17.*

**Defect.** **THE "WHICH REVIT?" LIST SHOWS EACH REVIT'S PROCESS NUMBER AND NO PROJECT NAME, AGAINST `mcp/README.md` RULE 4 - *"THE USER NEVER SEES A PROCESS NUMBER. VERSION, PROJECT, AVAILABILITY"* - AND [docs/25](../25-multi-session-and-binding.md) SECTION 2a.** Found 2026-10-18 by the docs review of docs/25. `SessionBinding.describe` in `mcp/server/heron_session.py` writes each line as `"  %d) Revit %s   (session %s)  %s%s"` with the process id as the session, so two Revit 2024s read `1) Revit 2024 (session 63712) FREE` and `2) Revit 2024 (session 24688) FREE`, and the modeller has to know which process holds which project to choose. The choice is by list number, as the rule wants; the project is what is missing.

**State.** **MOVED TO ISSUE [#927](https://github.com/Ajmalpshaik/Heron-AI/issues/927) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** docs/25 section 2a puts the list's changing facts at ask-time (`info` is lease-exempt, as `describe` says); the project name is one of them, and the process number leaves the line. Whether a short session number stays as a tie-breaker is the owner's call.

---

### Row 5b-1133

*Moved from the register on 2026-11-17.*

**Defect.** **`heron_companion` TELLS THE CHAT THE PAGE *"SHOWS THE MODEL, THE VIEW AND THE SELECTION OF THE REVIT THIS CHAT USES, AND CHANGES NOTHING"* - THE SELECTION CARD IS SWITCHED OFF SINCE 2026-10-04, AND THE PAGE'S APPLY, FINALIZE AND PLACE CHANGE THE MODEL.** Found 2026-10-18. The reply and the tool's docstring (*"what is selected, live"*) are in `mcp/server/heron_mcp_server.py`, the `heron_companion` tool; `mcp/companion/static/companion.js` has `SHOW_SELECTION = false` (*"SWITCHED OFF by the owner (Ajmal PS, 2026-10-04)"*, docs/40 section 21.6), and its tables' Apply, the Loads panel's Finalize and the Sprinkler Layout's Place run `revit_change`'s own body (docs/40 section 21.9). The chat repeats the reply, so the modeller is told a page that can change the model is read-only.

**State.** **FIXED 2026-11-11 in the MCP server - the words only, tested in a cloud container (Linux, no Revit); the page was not opened on a PC for it.** The `heron_companion` reply and the tool's description now say what the page is today: which Revit, model and view are in front, what the chat asked Heron to do, and Heron's tables and panels - no selection. What *"changes nothing"* is OPENING the page, and both say it of that alone; each adds that some of the page's buttons change the model, Apply, Finalize and Place among them, under the same Changes switch and the same Keep or Discard question as a change asked for in the chat. No panel is listed by name, so the sentence does not go stale when the page gains one. [`tests/test_companion.py`](../../tests/test_companion.py) reads both out of the server with `ast`: eight checks FAILED on the code as it stood and pass on this. [docs/40 §21.9](../40-heron-companion.md) says so in place of *"describe the page from this section, not from that reply"*.

---

### Row 5b-1134

*Moved from the register on 2026-11-17.*

**Defect.** **FOUR COMMENTS IN CODE STILL DESCRIBE HERON AS IT WAS BEFORE [D-129](../decisions/D-129.md), [D-132](../decisions/D-132.md) AND [D-135](../decisions/D-135.md).** Found 2026-10-18 by the docs sweep. `revit/Heron.Revit.Addin/HeronApplication.cs` (about line 624): *"Nothing can SET the stop any more, so today it is a gate that will never close"* - the bridge's `stop` sets it since D-129, seen in Revit 2024 ([Group EZ](../needs-checking/group-ez.md), EZ3). `tools/measure-brain.py` (about line 127) and `tools/check-routing.py` (about line 327) name the one store at *"%APPDATA%\Heron\knowledge"* - `%USERPROFILE%\.heron\knowledge` since D-135 (`heron_scope.knowledge_dir`). `tools/cloud-setup.sh` (about line 62): a cloud session *"does not start that server (it exits off Windows, row 5b-162)"* - since D-132 it starts and serves the library tools. Comments only; nothing behaves differently because of them.

**State.** **MOVED TO ISSUE [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** Each is a one-line comment edit; the C# one rides the next change that already touches `HeronApplication.cs`, so it costs no extra compile and no proof.

---

### Row 5b-1136

*Moved from the register on 2026-11-17.*

**Defect.** **[NEEDS-CHECKING GROUP EQ](../needs-checking/group-eq.md)'S COMMAND COUNTS A PROOF ON REVIT 2022 THAT DOES NOT EXIST: IT MATCHES `Revit 20xx` ANYWHERE IN A `proof:` BLOCK, AND `create-floor`'S FINDING TEXT SAYS "Revit 2022".** Found 2026-10-18. The command in group-eq.md greps the whole proof block; `brain/fragments/create-floor/fragment.yaml`'s proof model is *Project1 work_ajmal.al, Revit 2024* and its finding reads *"Floor created on 'Level 1' using the Revit 2022 ..."*. So the group's *"one proof on 2022"* (2026-10-17) is the command's, and the reading is 2020 and 2024 only - which is what `heron_matrix.proven_releases` gives (`{'2024': 'proof'}` for create-floor), reading the `model:` line.

**State.** **MOVED TO ISSUE [#937](https://github.com/Ajmalpshaik/Heron-AI/issues/937) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not repaired.** The command reads the `model:` line alone, as `heron_matrix.proof_release` does, and the group's sentence is corrected with it.

---

### Row 5b-1138

*Moved from the register on 2026-11-17.*

**Defect.** **TWO LISTS SAY WHAT A CREDENTIAL LOOKS LIKE AND THEY DISAGREE - THE SETTINGS ONE REFUSES `risk-assessment` AS AN API KEY AND PASSES A BEARER HEADER - AND `heron_commit.stage()` READS A COMMIT'S MESSAGE FOR A CREDENTIAL BUT NEVER THE FILES IT STAGES.** Read 2026-10-18 while building [D-143](../decisions/D-143.md), and measured the same day with `python -I` on branch `claude/secret-scan-gate`. `brain/heron_secrets.py` `PATTERNS` is the redactor's list and, since D-143, the repository gate's. `brain/heron_configuration.py` `SECRET_SHAPED` is a second list of six prefixes, matched as substrings of the lowered value. Asked of the same four values with key `ai-provider`: `risk-assessment` - settings `('sk-', 'an API key prefix')`, redactor `[]`; `desk-top layout` - settings `('sk-', 'an API key prefix')`, redactor `[]`; a `Bearer` header with a 34-character value - settings `None`, redactor `['bearer token']`; `heron_secrets.FAKE_PRIVATE_KEY` - both catch it. So a settings file holding an ordinary word with `sk-` inside it is refused with a sentence that calls it a credential (the same untrue refusal row 5b-117 fixed for key **names**), and one holding a bearer token is accepted. Separately, `stage()` runs the redactor over the message alone (`keeper.redact(said)`): a staged file carrying a token is still answered `may_commit: True`.

**State.** **MOVED TO ISSUE [#936](https://github.com/Ajmalpshaik/Heron-AI/issues/936) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed in D-143's change, on purpose:** each one changes a refusal Heron gives a person (a settings file, a commit), and that is a reviewable change of its own. The repair the evidence points at: `SECRET_SHAPED` asks `heron_secrets.PATTERNS` instead of its own prefixes, and `stage()` reads the staged files' text through the same redactor.

---
