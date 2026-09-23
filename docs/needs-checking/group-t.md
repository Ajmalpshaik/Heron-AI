# Needs checking — Group T

> One group of [the register](../NEEDS-CHECKING.md), in its own file since 2026-09-23 so that it can be read
> alone. **The register's rules, and every group's place in it, are on that page.** A new row
> for this group goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py)
> reads it back into the register for every tool that reads the register, so a row here is seen
> exactly as it was seen there. Written by
> [`tools/split-needs-checking.py`](../../tools/split-needs-checking.py).

## Group T — what `prove-agent.py track` cannot see, found 2026-09-19

*Lettered T because Group H was already taken by the lease (H1-H10). Written as H first, which put two H1 rows in one register and made `check-gaps` count both - fixed the same day.*

Added while proving the four owed add-in agents against the two live Revit 2024 sessions
(`20472` *Project1 work_ajmal.al*, 3,529 elements; `36908` *test projject*, 3,594 elements).

Five agents were proved and signed that day — SHT-029, DIM-031, GRP-033, PHS-032, SYS-030. Two
could not be, and **neither failure is about the agent**. Both are about the tool that judges it.
They are recorded here because a reader coming back to `IMP-019` and `LVL-027` will otherwise
re-run them, get the same verdict, and conclude the agents are broken.

### T1 — `compare()` reads only top-level numbers, so a list that moves is invisible

[`tools/prove-agent.py`](../../tools/prove-agent.py) `compare()` skips any value that is not an `int`
or `float` at the top level of the reply. Its docstring gives the reason, and the reason is sound
*for a string*: `"A string that differs is usually the document name, which is the input rather
than the answer"`. It does not extend to a **list of things found in the model**, which is the
answer and nothing else.

**`IMP-019` is the case.** `track` reported `moved: NOT ESTABLISHED` — every comparable number
identical, `importedCount: 0, linkedCadCount: 1, revitLinks: 0`. The raw replies, read by hand the
same minute:

| | session 20472 | session 36908 |
|---|---|---|
| `linkedCad[0].name` | `box.dwg` | `Project1 - Section - Section 1.dwg` |
| `document` | `Project1 work_ajmal.al` | `test projject` |

**The agent reads the model.** It names a different DWG in each, correctly. The count is 1 in both
because each model happens to hold exactly one CAD link, and a count of one against a count of one
is the only thing `compare()` was allowed to look at.

**`LVL-027` is the same shape, one level down.** `track` found one number moving — `onNoLevel:
3513 -> 3558`, which is really the element total wearing a different hat. Inside `levels[]`, which
`compare()` never opens:

| | session 20472 | session 36908 |
|---|---|---|
| Level 1 | **8** elements | **29** elements |
| Level 2 | **8** elements | **7** elements |

Identical names, identical elevations (0 and 4000), different contents. The per-level counts are
the agent's actual answer and they move in both directions at once — which is *harder* to fake
than a single total, not easier.

**So the standing advice to "add or rename a level in one model" would work, and it treats the
symptom.** `levelCount 2 -> 3` is a top-level number, so `compare()` would see it. But the evidence
that the agent reads the model is already in hand; what is missing is a tool that can record it.

### T2 — `track` cannot pass an argument, so an agent that needs one cannot be tracked at all

`track` takes `--operation`, `--first`, `--second`, `--client-id`. There is no way to send an
operation argument, and it sends the bare op. Three operations refuse without one:

| Operation | Agent | Bare-op result |
|---|---|---|
| `read_parameters` | `HERON-REVIT-PAR-011` | `no_category` |
| `select_by_category` | `HERON-REVIT-CAT-009`, `HERON-REVIT-SEL-008` | `operation_failed` |

**`PAR-011` was then driven by hand with `category=Pipes`, and it is one of the strongest readers
measured all day:**

| | session 20472 | session 36908 |
|---|---|---|
| `elements` | 0 | **2** |
| `distinctParameters` | 0 | **99** |
| `listed` | 0 | **99** |
| `typesRead` | 0 | **1** |

Every number moves. It cannot be signed, because the tool that writes drafts has no way to make
that call.

### What would settle T1 and T2

Both are one change to `tools/prove-agent.py`, and **neither has been made** — the file was not
this session's to edit:

| | |
|---|---|
| ~~**T1**~~ | DONE 2026-09-19. — [full row](../needs-checking-archive/group-t.md#row-t1) |
| ~~**T2**~~ | DONE 2026-09-19. — [full row](../needs-checking-archive/group-t.md#row-t2) |

### T3 — the rest of the twenty-three are mostly not trackable, and that is correct

Of the 23 add-in agents with no proof, the file headers show they are **not 23 separate readers**.
Seven share `RevitWrite.cs` (`TSA-006`, `TRN-005`, `WRN-016`, `CTX-007`, `ELE-010`, `KRN-EVD-014`,
`KRN-HUM-018`) and are the machinery *inside* a write — transactions, warning capture, context,
evidence, human wording. Four share `Commands.cs` (`CON-001`, `HLT-025`, `UI-022`, `OPS-STP-007`).
Two share `HeronApplication.cs` (`RIB-023`, `VER-002`). `CMP-021` compiles C#. `APP-003` is the
dispatcher every operation already passes through.

**None of those has a read operation to send**, so `track` cannot reach them and no amount of
model would help. They need a different kind of proof — one that observes them doing their job
during somebody else's operation — and that does not exist yet. Recorded rather than left looking
unproved by neglect.

`LNK-015` *is* reachable (`list_links`) and *is* thin: both models hold **zero Revit links**, so
only `hostElements` moves. Note this is not the reason sometimes given — the linked **CAD** files
belong to `IMP-019`, not to `LNK-015`. Proving `LNK-015` needs a model with a real Revit link in
it.

### T1 and T2 were fixed the same day, and T3 stands

**Read this rather than the two rows above**, which say the changes were not made. They were, an hour
later and on the owner's say-so. `tools/prove-agent.py` now does both:

- **`compare()` descends one level into a list** — its length, then each item's own numbers *and
  names*, first `LIST_ITEMS = 5` items.
- **`track --arg KEY=VALUE`**, repeatable, passed through to `op_args`. The **same** arguments go to
  both models, deliberately: varying them would let a different answer come from a different input
  rather than a different model, which is the one thing tracking exists to rule out.

Four agents were then proved and signed that could not be before:

| Agent | What moved, once the tool could see it |
|---|---|
| `IMP-019` | `linkedCad[0].name: box.dwg -> Project1 - Section - Section 1.dwg` |
| `LVL-027` | `levels[0].elements: 8 -> 29`, `levels[1].elements: 8 -> 7`, `onNoLevel: 3513 -> 3558` |
| `PAR-011` | `distinctParameters: 95 -> 90`, `listed: 95 -> 90`, `parameters[]: 95 -> 90` (`--arg category=Ducts`) |
| `SEL-008` | `selected: 18 -> 8` (`--arg category=Ducts`) |

**The guard that mattered was checked first:** `compare(A, A)` — the same reply against itself —
still returns nothing moved. A change that widens what counts as movement has to be shown not to
manufacture it.

**Ducts were chosen over pipes on purpose.** `category=Pipes` gave `0 -> 2` elements and `0 -> 99`
parameters, and a zero is the weaker half of a pair: an agent that failed to read would also return
zero. `Ducts` gives **18 against 8** and **95 against 90** — both sides non-zero and different,
which no fallback and no constant can produce.

#### One honest side effect: the thin-tracking flag now over-fires on list-heavy replies

`TRACKING IS THIN` compares how many things moved against how many held. Descending into lists adds
every matching item field to the *held* side, and on a parameter report those are **Revit's own
parameter names, identical by nature**. `PAR-011` reads *3 moved, 44 held* and is flagged thin,
while its real argument — 95 distinct parameters against 90, both non-zero — is strong.

**No threshold was invented to paper over it.** The gap text already says the reader judges, and
that is what happened here. Recorded so the next person reading `PAR-011`'s proof knows the flag is
inflated rather than the evidence weak.

#### `CAT-009` is genuinely not trackable, and that is the correct answer

`select_by_category` returns `categories: 1` in **both** models, and it should. `CAT-009` maps a BIM
word to a Revit category — *"Ducts"* to `OST_DuctCurves` — and that answer does not depend on which
model is open, because it is a lookup rather than a reading. Tracking cannot prove it and proving it
by tracking would mean nothing. It needs a different kind of test: a table of words against the
categories they must resolve to.

`DOC-004` is the same shape from the other end. It chooses *which document*, and `document` is
excluded from `compare()` as the input. Every one of the fifteen signed proofs names the right model,
which is real evidence that it works — but it is evidence sitting in fifteen other agents' files,
not a proof of its own.

---
