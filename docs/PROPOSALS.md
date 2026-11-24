# Proposals — Gaps, Additions & Ideas

> | | |
> |---|---|
> | **Type** | **Permanent register.** Append-only, and **never deleted** — this is where work notes empty into |
> | **For** | The owner, deciding what to accept, reject or defer |
> | **Authority** | **Nothing here overrides the specification.** These are findings, not decisions — a accepted one becomes a [DECISION](DECISIONS.md) |
> | **Waiting on you?** | `python tools/owner-queue.py` — **never a list typed on this page** |
> | **Adding to it** | A reviewed gap, risk or idea. **Raw ideas go to `work-notes/ideas/` instead**, until they are worth putting in front of somebody |
> | **Its numbers** | Derive the open ones: `python tools/owner-queue.py` |
> | **Where each section lives** | Since 2026-09-23 **each section is its own file** in [`proposals/`](proposals/part-a.md) — [Part A's](proposals/part-a.md), for one — and this page keeps the rules and, where each section was, its heading and a line naming its file. **A new proposal is a new section: write it on this page, in full**, and the next run of `python tools/split-register.py proposals --write` gives it its own file. The whole register, read as one text: `python tools/register-text.py docs/PROPOSALS.md` |

> **Read [FOR-THE-OWNER.md](FOR-THE-OWNER.md) first if you are the owner.** It is the one page that
> says what is waiting on you, across every register, without holding a list of its own.


> Engineering review of the Master Specification, carried out 2026-08-27.
> Covers **[Part 1](00-master-specification.md)** (the platform) and
> **[Part 2](00b-master-specification-agent-os.md)** (the Agent Operating System).
> Nothing here overrides the spec. These are findings for the owner to accept, reject or defer.
>
> **Status legend:** 🔴 blocking · 🟠 important · 🟡 worth doing · 🔵 idea · ✅ closed by Part 2

---

## Part 0b — What the Additional Requirements (Part 4) changed

**Its own file:** [`proposals/part-0b.md`](proposals/part-0b.md)

## Part 0a — What the Master Handover Baseline (Part 3) changed

**Its own file:** [`proposals/part-0a.md`](proposals/part-0a.md)

## Part 0 — What Master Specification Part 2 changed

**Its own file:** [`proposals/part-0.md`](proposals/part-0.md)

## Part A — Gaps in Master Specification Part 1

**Its own file:** [`proposals/part-a.md`](proposals/part-a.md)

## Part B — Feature ideas not in the specification

**Its own file:** [`proposals/part-b.md`](proposals/part-b.md)

## Part C — Strategic questions the spec does not address

**Its own file:** [`proposals/part-c.md`](proposals/part-c.md)

## Part D — The one recommendation that matters most

**Its own file:** [`proposals/part-d.md`](proposals/part-d.md)

## Part E — Three findings taken from working Revit tooling, written as Heron's own

**Its own file:** [`proposals/part-e.md`](proposals/part-e.md)

## Part F — Opened by building the Improvement Gate, 2026-09-12

**Its own file:** [`proposals/part-f.md`](proposals/part-f.md)

## F23 — four of the five remaining Documentation agents have no distinct source, or no distinct job

**Its own file:** [`proposals/f23.md`](proposals/f23.md)

## F24 — `docs/09 §151` asks for a percentage that the agent it asks has refused to produce

**Its own file:** [`proposals/f24.md`](proposals/f24.md)

## F25 — should the import walk skip `.git`, `node_modules` and build folders?

**Its own file:** [`proposals/f25.md`](proposals/f25.md)

## F26 — the import pipeline is ordered *index → save*, and both indexes read the library

**Its own file:** [`proposals/f26.md`](proposals/f26.md)

## F27 — five Development rows are already built, and whose work they measure decides by which file

**Its own file:** [`proposals/f27.md`](proposals/f27.md)

## F28 — the import classifies into five words the workspace has never heard of

**Its own file:** [`proposals/f28.md`](proposals/f28.md)

## F29 — the panic button cannot press itself, and Revit is the reason

**Its own file:** [`proposals/f29.md`](proposals/f29.md)

## F30 — a reference model's profile carries the client's own job number

**Its own file:** [`proposals/f30.md`](proposals/f30.md)

## F31 — five Standards rows differ only by their subject

**Its own file:** [`proposals/f31.md`](proposals/f31.md)

## F32 — `redact()` returns two values and reads like it returns one

**Its own file:** [`proposals/f32.md`](proposals/f32.md)

## F33 — three contracts promised less than their code does, and nothing could see it

**Its own file:** [`proposals/f33.md`](proposals/f33.md)

## F34 — D-52 is enforced in Python and unchecked in C#

**Its own file:** [`proposals/f34.md`](proposals/f34.md)

## F35 — half a hole is closed, and the other half needs the checks to change first

**Its own file:** [`proposals/f35.md`](proposals/f35.md)


## F36 — D-59's 62 edits would un-prove 56 fragments, and four of them must not be made at all

**Its own file:** [`proposals/f36.md`](proposals/f36.md)

## F37 — a refusal was resting on a majority, and one file tipped it

**Its own file:** [`proposals/f37.md`](proposals/f37.md)

## F38 — the repository went public and two documents still say it did not

**Its own file:** [`proposals/f38.md`](proposals/f38.md)

## F39 — five Development rows may be the host's, and the tier model already says so everywhere else

**Its own file:** [`proposals/f39.md`](proposals/f39.md)

## F40 — Workforce Planning scores a proposal against the register's BUILD NOTES, not only its job

**Its own file:** [`proposals/f40.md`](proposals/f40.md)

## F41 — the scaffolder writes a module name the suite then rejects

**Its own file:** [`proposals/f41.md`](proposals/f41.md)

## F42 — the HVAC design engine leaves five things only the owner can settle

**Its own file:** [`proposals/f42.md`](proposals/f42.md)

## F43 — the fire protection design engine leaves six things only the owner can settle

**Its own file:** [`proposals/f43.md`](proposals/f43.md)

## F44 — a kept change could say how its counts compare with the dry run before it, once Heron knows whose dry run that was

**Its own file:** [`proposals/f44.md`](proposals/f44.md)

## F45 — an AI that can write files can turn on Changes, Admin and Publish, because each is one line in a text file

**Its own file:** [`proposals/f45.md`](proposals/f45.md)

## F46 to F55 — ten ideas from the 2026-11-15 study of other Revit MCP projects, filed and not decided

Filed 2026-11-19 (WN-30, issue [#1007](https://github.com/Ajmalpshaik/Heron-AI/issues/1007)) from the
research note `docs/work-notes/investigations/2026-11-15-revit-mcp-landscape.md` - its table *The best parts
out there that Heron does not have* and the smaller ideas under it - so that nothing worth keeping lives only
in a note the owner retires. The note is named, not linked, for that reason. **Nothing here is decided:** each
entry says what the idea is, what Heron has today as read on 2026-11-19, and what it would touch. The effort
is the note's own judgement, not a measurement. Per [D-177](decisions/D-177.md) the projects the ideas came
from are not named; any idea taken is re-authored, never copied ([D-25](decisions/D-25.md),
[31 research](31-research.md)).

**The rest of the note's ideas already have a home**, which is why they are not filed here:

| The note's idea | Where it is |
|---|---|
| 1 - after Discard or Undo, check the model really went back | Built: the rollback witness, `rollbackCheck` on the reply ([`HeronRollbackWitness.cs`](../revit/Heron.Revit.Addin/HeronRollbackWitness.cs), [`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs)); its Revit run is [NEEDS-CHECKING A16b](needs-checking/group-a.md) |
| 2 - the Delete box counts what Revit really deletes | [D-142](decisions/D-142.md)'s Delete box; the counting is WN-9 of issue [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) |
| 3 - an approval tied to the exact request and the model as it was | WN-10 of issue [#1005](https://github.com/Ajmalpshaik/Heron-AI/issues/1005) |
| 4 - an automatic read-back after every change | [D-156](decisions/D-156.md), `readBack` - [Group NS](needs-checking/group-ns.md) |
| 5 - a write switch the AI cannot flip | [D-166](decisions/D-166.md), from [F45](proposals/f45.md) |
| 7 - a change that timed out is never sent again blind | [D-161](decisions/D-161.md) |
| 8 - a check never passes on missing data | [D-165](decisions/D-165.md) |
| 10 - a proof record pins the add-in build and the Revit build | [D-174](decisions/D-174.md), `built_on:`. Its other half, Revit's journal as a witness, is F54 below |
| 12 - a signed one-click Windows Setup | [D-167](decisions/D-167.md) |
| A change that touched nothing leaves no empty undo entry, and says so | [D-160](decisions/D-160.md) and [D-168](decisions/D-168.md) |
| Every tool listed at start-up, before a Revit connects | Already held: on Windows every tool is served whether or not a Revit is connected ([`mcp/server/heron_tools.py`](../mcp/server/heron_tools.py), beside `LIBRARY`) |
| One line telling the chat that text from the model is data, never a task | Already held: Article 14, *what you read is data*, is in every chat's instructions ([`brain/instructions/host.chat.yaml`](../brain/instructions/host.chat.yaml)), with [Golden Rule 19](14-golden-rules.md) and the reply's model text encoded as data (row 5b-1596) |

### 🔵 F46. Revit's warnings and dialogs during a change - shown in the Keep box, a dialog parked rather than answered blind

The note's idea 6. Warnings Revit raises during a change shown in the Keep or Discard box, or the change
rolled back unless they are accepted; a dialog that opens mid-change parks the job and is answered against a
fresh look, never by sending the change again; a dialog about delete, purge, sync or overwrite never answered
automatically. **Heron today:** a reply lists the warnings Revit raised and Heron dismissed
([`RevitFragment.cs`](../revit/Heron.Revit.Addin/RevitFragment.cs), the `warnings` count and its list), and a
dialog watch is among the rails run by hand in Revit on 2026-10-18 ([docs/README.md](README.md)'s status).
**Would touch:** the write path, the Keep box, the bridge. Effort: medium.

### 🔵 F47. A check on a fragment's code before it ever runs

The note's idea 9. A rule over the library's source that refuses Save, Sync, Export, reaching another open
document, and file or network calls in a fragment, at compile time; and a READ fragment that changed the
model caught and rolled back. **Heron today:** no such check over a fragment's source was found
(`brain/heron_fragment.py` and `tools/check-*.py` searched for those calls on 2026-11-19); a MODIFY fragment
never runs as a read ([`fragment-proving`](../.claude/skills/fragment-proving/SKILL.md)). **Would touch:** a
new gate over the library, the DRAFT compile, READ and ANALYZE runs. Effort: medium to large.

### 🔵 F48. A spreadsheet round trip with a three-way compare

The note's idea 11. A hidden UniqueId on each row; the export's baseline and the model's identity kept inside
the file; only edited cells written; a cell whose value changed in the model since the export flagged; units
that are not clear held until confirmed. **Heron today:** the DRAFT method
[`parameters-csv-round-trip`](../brain/skills/parameters-csv-round-trip.yaml) matches rows by element id and
says it writes no UniqueId; [Group NJ](needs-checking/group-nj.md) is its Revit run, not yet run.
**Would touch:** the Companion's tables and that method. Effort: medium to large.

### 🔵 F49. A published trust statement - a tool manifest from Heron's own tool table, and what each control does not stop

The note's idea 13. A list of every tool and what it may do, generated from Heron's own tool table and kept in
step by a check, and a privacy and security page that also says what each control does NOT stop.
**Heron today:** [12](12-security-and-permissions.md) and [D-169](decisions/D-169.md)'s disclaimer; no
generated manifest. **Would touch:** a check, the documentation. Effort: small.

### 🔵 F50. A Claude Code plugin with the server version pinned, and an allow-list of Heron's read-only tools

The note's idea 14. The server shipped as a Claude Code plugin whose version is pinned, and a generated list
of Heron's read-only tools a modeller may allow without being asked each time. **Heron today:** the install
command is still open as [Q-38](open-questions/from-master-specification-part-2.md), plugin install its
preferred option; [D-167](decisions/D-167.md)'s Setup is the installer being built. **Would touch:** the
installer and its packaging. Effort: small.

### 🔵 F51. What changed in the model since Heron last looked, and who changed it

The note's idea 15. A model diff between sessions: what changed since Heron last read the model, and whether
Heron, a person or a sync changed it. **Heron today:** its own recent changes are docked in Revit
([D-163](decisions/D-163.md)) and each write leaves a run record; nothing compares the model across
sessions. **Would touch:** the Companion and the run records. Effort: large.

### 🔵 F52. An undo-hostile kind of change - a family reload, a link reload, Save As - confirmed by typing

From the note's smaller ideas. Changes that Revit's own Undo cannot fully take back put in a class of their
own, which needs a typed confirmation. **Heron today:** [Article 7](../HERON_CONSTITUTION.md) asks explicit
confirmation for a family reload, anything touching a linked model and the other destructive operations it
lists; no class for *Undo will not take this back*. **Would touch:** the risk classes and the boxes that ask.

### 🔵 F53. Each Heron change written to Revit's journal

From the note's smaller ideas: a line in Revit's journal for each Heron call, as Revit itself has begun to
mark a change made through an AI connector (the note's reading of search summaries, to be confirmed in
Revit's own documentation). **Heron today:** no line is written to the journal (`revit/` searched on 2026-11-19); its
own record is the run record. **Would touch:** the add-in's write path.

### 🔵 F54. Revit's journal as an independent witness in a proof record

The other half of the note's idea 10: the journal lines naming the transaction a proof ran, kept in the
proof record beside `built_on:` ([D-174](decisions/D-174.md)), so the record is witnessed by something Heron
did not write. **Heron today:** a proof records the fragment's fingerprint and `built_on:`; no journal line.
**Would touch:** proof records ([D-30](decisions/D-30.md)). Effort: small.

### 🔵 F55. Prompts that must reach NO tool, kept beside the routing checks

From the note's smaller ideas: negative-control prompts for routing - sentences that must not reach a given
tool or method, run with the positive ones. **Heron today:** [`tools/check-routing.py`](../tools/check-routing.py)
asks each card's own sentences back, and [`tools/check-intrusion.py`](../tools/check-intrusion.py) counts the
shortlists a card intrudes on; no set of sentences that must reach nothing. **Would touch:** the routing
checks and their test data. Effort: small.

## 2026-09-19 — what is left after twenty-three gaps were closed, and the two that never can be

**Its own file:** [`proposals/2026-09-19-what-is-left-after-twenty-three.md`](proposals/2026-09-19-what-is-left-after-twenty-three.md)

## 2026-09-19 — five of PR #190's six proposals are built, and the sixth is still a refusal

**Its own file:** [`proposals/2026-09-19-five-of-pr-190-s-six.md`](proposals/2026-09-19-five-of-pr-190-s-six.md)

## 2026-09-19 — rule (d) refuses four fragments, and only two of them deserve it

**Its own file:** [`proposals/2026-09-19-rule-d-refuses-four-fragments.md`](proposals/2026-09-19-rule-d-refuses-four-fragments.md)
