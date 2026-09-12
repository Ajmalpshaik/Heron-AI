# HANDOVER — the session that ran 2026-09-07 into 2026-09-08 (the PROVING track)

> **Archived session note.** This is a record of work that is finished. It was moved out of
> [`HANDOVER.md`](../HANDOVER.md) on 2026-09-12 so that file could go back to being the short
> live entry point it is supposed to be. Nothing here is specification: where it disagrees with
> [DECISIONS.md](../DECISIONS.md), the [Golden Rules](../14-golden-rules.md) or the
> [Constitution](../../HERON_CONSTITUTION.md), **those win**.

---


**16 `PROVEN` at the start of it, 52 at the end.** Everything below was done with Revit 2024 open on the
owner's PC, mostly on `Snowdon Towers Sample HVAC`. **The method is the reusable part and it is at the
bottom of this entry — read that first if you are here to prove more fragments.**

### What was proved, and the one thing that made it fast

| | |
|---|---|
| The activity banner (D-50) | **B6, B7, B8, B9, B10, B11, B13 all pass.** It had never been seen on a screen; it has now, in every colour |
| The write path | **Was structurally broken. Fixed, and the first write Heron has ever made ran through it** |
| 36 fragments | Proved to *run* against a real model — most for the first time |
| 36 more | **Proved outright**, 16 → 52 |
| One new fragment | `FIND_DATES_IN_VIEWS` — written and proved the same night |

### THE FIVE BUGS, because each cost real time and each is the kind that returns

1. **The write path could never have worked.** `revit_apply_move` returned *"Missing or wrong token"*.
   One JSON key, `token`, meant the SESSION token to `BridgeServer.Dispatch` (line 349, checked first)
   and the APPROVAL token to `RevitWrite` (line 103). `body.update(op_args)` let the second overwrite
   the first, so every apply was refused as unauthenticated. **No value satisfied both.** The approval
   is now `approvalToken`, and `op_args` refuses `op`/`token`/`client` outright rather than silently
   winning. Nothing but a live Revit could have found it: both sides compiled, both read a string
   called `token`.
2. **`looks_empty` could never return True.** The executor renders everything as text, so a count of
   zero arrives as the STRING `"0"` — one character, therefore "not empty". **Every negative case was
   flagged, whatever it returned.** A warning that always fires is furniture.
3. **`read-space-loads` threw on the only selection it exists for**, with Revit's own `Not Computed!`.
   Its `noLoad` handler already said *"the space is unbounded and has no volume to load"* and could
   never be reached — the property read threw first. **A guard placed after the thing it guards
   against.**
4. **Both schedule fragments refused the only object a person can select.** They demanded a
   `ViewSchedule`; clicking a schedule on a sheet gives a `ScheduleSheetInstance`. A view cannot be
   selected as an element, so they were correct in isolation and **unusable in practice**.
5. **A vendor namespace inside `brain/`**, from fix 3. `check-structure` refuses it and was right to.
   It greps the file TEXT, so a **comment** naming it fails too.

### Three decisions, and one of them replaced the guessing for good

- **[D-51](../DECISIONS.md)** — a negative case is judged by its COUNTS, not by whether the fragment
  went silent. `findings` is prose; 134 of the 350 provide one.
- **[D-52](../DECISIONS.md)** — a count of what was TURNED DOWN is not a count of what was FOUND.
  `noSystem: 16` means sixteen were examined and none had one, which is **stronger** evidence than
  silence. Amended the same evening to cover work counters (`scanned`, `jointsChecked`).
- **[D-53](../DECISIONS.md)** — a fragment that CANNOT come back empty is proved by **tracking**:
  `count-elements` returned 28, 16, 4, 12, 37 against selections of 28, 16, 4, 12, 37. Eleven fragments
  went through on that.

**FOUR RULE CHANGES IN ONE EVENING, ALL IN THE SAME DIRECTION, AND THAT WAS SAID OUT LOUD AT THE TIME.**
The fifth was refused. What replaced it is the fix that matters: **`role: result | accounting` on a
`provides` entry**, so a fragment DECLARES which outputs are findings and which count what it was
handed. It landed optional, with the naming patterns kept as fallback so that 349 fragments did not
have to be edited at once.

**They were all edited on 2026-09-09, and it is REQUIRED now.** All 1,192 provides declare a role,
read off what each fragment is FOR rather than what its output is called - and that reading
disagreed with the patterns **166 times, in both directions**. `select-unenclosed-rooms` declares
`unplaced` and `unenclosed`, the two faults it exists to find, and `REJECT_NAMES` swallowed both;
`set-mep-slope.inGroup` is bookkeeping no pattern could see, and a non-zero one would have banked a
proof for a run that moved nothing. So `REJECT_PREFIX`, `REJECT_NAMES` and `WORK_COUNTER` were
deleted rather than kept as a fallback that is wrong one time in seven, and `check_contract` now
refuses a provide with no role. `findings` is the one exemption. **Nothing reads a name any more.**

### The method — this is the part to reuse

**TWO CONTRASTING SELECTIONS, NEVER SELECT-THEN-CLEAR.** Clearing the selection does not give an empty
answer: `elements` becomes unbound and the executor refuses, which proves nothing. Select ducts (the
positive for MEP fragments, the negative for room fragments), then spaces (the reverse). **Five
selections — ducts, spaces, walls, sheets, equipment — carried most of the library.** Sheets are the
best negative in it: no length, no volume, no level, no routing.

**THE ARRANGEMENT IS THE WORK; RUNNING TAKES SECONDS.** On a model whose content is LINKED — which
Snowdon Towers is — the thing must be **drawn in the host**, because the executor skips linked
documents by design. Five proofs needed something built: host walls, a ceiling, a curtain wall, a Join
Geometry join (walls merely touching do NOT count), and a reference section (a plain section selects
the *view*, not the marker).

**THREE KINDS OF FRAGMENT, THREE PROOF SHAPES:**

| Kind | Proof | Cost |
|---|---|---|
| Reporters | Two selections, one with the thing and one without | Cheap |
| Structure reporters — `count-elements` | Can never be empty; **tracking** (D-53) | Cheap |
| **Defect-finders — 34 unproven** | The fault must be BUILT on purpose | Slow, one per fragment |

**A DEFECT-FINDER CANNOT BE PROVED ON A CLEAN MODEL.** `check-flow-direction` was rejected four times
before equipment showed `jointsChecked: 57` — it works, there is simply no fault in Snowdon Towers to
find. Its positive needs two connectors both set to `Out`, built in the Family Editor.

**A THROWAWAY FRAGMENT IS THE FASTEST DIAGNOSTIC HERE.** The executor runs any read-only C# handed to
it, so *"what does Revit actually see"* is answerable in about a minute without building anything. It
is what found the date sitting in a text note as `09-09-2026`.

### What is left, honestly

- **Every proof says `second_route: NOT ESTABLISHED`.** All 52. D-30 asks for one *"where one exists"*
  and nobody has settled whether one exists for any of them. **That is the largest open weakness.**
- **`read-space-loads` is fixed and UNPROVEN.** Every space in the model returns `noLoad`; a positive
  needs Areas and Volumes on with the analysis run, or a Design Heating Load typed on one space.
- **`read-schedule-contents` is fixed and UNPROVEN** — it needs `maxRows`, a caller's value.
- **278 of 350 still want a caller's half** — a category, a name, a distance. Unchanged, and still the
  largest unlock.
- **The add-in is STALE** and was before this session; nothing here rebuilt it.
