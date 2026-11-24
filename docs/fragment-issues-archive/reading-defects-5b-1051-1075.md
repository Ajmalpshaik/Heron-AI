# FRAGMENT-ISSUES archive — section 5b, rows 1051 to 1075

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
> Rows 1051 to 1075 of section 5b belong in this file. A number in that band with no entry below
> was never moved — it is still in the register, in full. Written by
> [`tools/archive-fragment-issues.py`](../../tools/archive-fragment-issues.py).

---

### Row 5b-1060

*Moved from the register on 2026-11-17.*

**Defect.** **THE MOVE STILL TELLS TWO MODELS APART BY THE KEY EVERY PROJECT FROM ONE TEMPLATE SHARES, SO CLICKING INTO ANOTHER SUCH MODEL BEFORE SAYING YES IS REFUSED FOR THE WRONG REASON, NAMING NEITHER MODEL.** Found 2026-10-17 running [E3](../needs-checking/group-e.md) on the owner's laptop, Revit 2024.3, add-in `0.1.0+76006767` ([the run](../needs-checking/group-e.md#the-2026-10-17-run-of-e1-e10-g1-g2-and-g4)). A preview in *Heron D3 move test* (3 ducts), then `activate-document` on *Rails-S dynamo sample* (a copy of Revit 2024's `DynamoSample_2024.rvt`), then the approval: *"The model changed since that preview. It described 3 ducts, and 3 of them are gone. Nothing was changed. Ask again to see the current picture."* Both models' replies carried `projectKey` `8764c510-57b7-44c3-bddf-266d86c26380-0000c160` - the default template's, the key [5b-324](../fragment-issues/section-5b-rows-301-325.md) found shared - and different `creationGuid`s (`cd4afd1d-...`, `add3fb0c-...`). `RevitWrite.DocumentKey` in [`RevitWrite.cs`](../../revit/Heron.Revit.Addin/RevitWrite.cs) returns `ProjectInformation.UniqueId`, so `ExecuteMove`'s Golden Rule 20 check finds the two equal, never reaches `document_not_in_front` or `document_closed`, and only the element-id re-count refuses - with words about the model changing. With a model of another key in front (*Rails-F Snowdon Facades*, `7cacd49c-...-00012b83`) the same steps gave the designed words: *"That preview was made for Heron D3 move test, but Rails-F Snowdon Facades is in front now. Heron D3 move test is still open - click back to it and ask again."* `DocumentKey`'s own comment says the key is shared and that changing it is *"a write-path change owed its own proof in Revit"*; this is that proof's failing half, measured. The same key decides `IsStillOpen`, so E4's *closed* and E5's link filter need models of different keys too. **Read in the source, NOT run:** between two copies of one file - the same key AND the same element ids - the re-count would match, and the move would land in the copy in front; `Document.CreationGUID` would not separate those either (5b-324 measured seven copies of one model carrying one), a path would. **NOT checked:** E4 and E5 themselves, not run that day

**State.** **MOVED TO ISSUE [#933](https://github.com/Ajmalpshaik/Heron-AI/issues/933) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** The refusal is safe - nothing moved either time, re-read - but it names neither model, and a modeller cannot tell it from a real change. Owed: a key that separates two models from one template, as [D-113](../decisions/D-113.md) gave the knowledge stores, and a decision about two copies of one file; then E3 again on both pairs

---

### Row 5b-1061

*Moved from the register on 2026-11-17.*

**Defect.** **ONE MOVE WRITES TWO AUDIT LINES UNDER TWO WORKFLOW IDS, SO THE LINE THAT SAYS WHICH DUCTS MOVED CANNOT BE JOINED TO THE ONE THAT SAYS WHICH REVIT RAN IT AND HOW LONG IT TOOK.** Found 2026-10-17 running [G4](../needs-checking/group-g.md) ([the run](../needs-checking/group-e.md#the-2026-10-17-run-of-e1-e10-g1-g2-and-g4)). In `%APPDATA%\Heron\audit\audit-202610.jsonl` the move at 15:30:35 on *Heron D3 move test* wrote `wf-20261017-123035-0060-4e4f` (`move_elements`, with `elements`, `documentId`, `undoEntry` and the rest) and, a millisecond later, `wf-20261017-123035-0059-ca43` (`move_elements`, `session 2024/8168`, `ms 666`). Every move that day did the same, refused ones too: `-0075-a4ea` beside `-0074-194c`, `-0115-f43d` beside `-0114-5244`. `RevitDispatcher.Dispatch` mints the job's id and writes its line from it; `RevitOperations` calls `RevitWrite.Run(app, request, op)` without it, and `ExecuteMove` mints a second - `var workflow = HeronIdentity.NewWorkflowId();` - for its own record and for `Failed`. `HeronIdentity`'s docstring calls the workflow id the key that ties one sentence to every element touched (Golden Rule 10, [5b-6](../fragment-issues/section-5b-rows-001-025.md)). The detailed entry is complete on its own, which is what G4 asked; the join is what is lost. **NOT checked:** whether any other operation that writes its own audit record mints its own id the same way

**State.** **MOVED TO ISSUE [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** Owed: hand the job's id to `RevitWrite.Run` and record under it - an edit to `RevitWrite.cs`, which stales every golden move case resting on it

---

### Row 5b-1062

*Moved from the register on 2026-11-17.*

**Defect.** **WHEN REVIT REFUSES A MOVE, HERON SAYS ONLY *"Revit did not accept the change, so nothing was moved"* - NOT WHAT REVIT OBJECTED TO, AND NOT THAT THE MOVE WAS ROLLED BACK.** Found 2026-10-17 running [G1](../needs-checking/group-g.md) ([the run](../needs-checking/group-e.md#the-2026-10-17-run-of-e1-e10-g1-g2-and-g4)): the 935 duct fittings of *Heron D3 move test*, every one joined to pinned ducts, asked up 200 mm. After 11.4 s that one sentence was the whole answer, in the chat and in the audit's `reason`, with `warnings` 0; the model was untouched, re-read. `RevitWrite.CollectWarnings.PreprocessFailures` returns `ProceedWithRollBack` at the first failure of Error severity or above without keeping its words, and the branch for a commit that did not commit answers the fixed sentence. A fragment write has quoted Revit since package C3 - [E21](../needs-checking/group-e.md): *"Revit would not let it through: "Constraints are not satisfied" (3 elements) ... Revit confirmed the rollback"* - and the move kept its own preprocessor. So a modeller is pointed at nothing in particular, and G1's *the message says it was rolled back* is met in substance only. **NOT checked:** what Revit's message was - nothing recorded it

**State.** **MOVED TO ISSUE [#934](https://github.com/Ajmalpshaik/Heron-AI/issues/934) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** Owed: the move's preprocessor keeping the first failure's words and element count, as the fragment path does, and G1 run again

---

### Row 5b-1065

*Moved from the register on 2026-11-17.*

**Defect.** **THE INSTALLER PUTS THE LOCK'S VERSIONS IN THE REAL USER FOLDER, WHILE `heron-ready`, IN THE SAME RUN, SAYS ANY VERSION OTHER THAN CLAUDE'S COPY'S LEAVES *"two copies of one package mixed in Claude's view"* - AND ON THE OWNER'S LAPTOP IT DID, AND HERON STILL STARTED.** Measured 2026-10-17 ([EP1 and EP5](../needs-checking/group-ep.md)). The Store Claude's copy held `mcp` 2.3.0, `cryptography` 50.0.2 and `pydantic` 2.14.0 ([row 417](../fragment-issues/section-5b-rows-401-425.md)); the installer, run outside Claude, put `requirements-lock.txt`'s `mcp` 2.2.0, `cryptography` 50.0.1 and `pydantic` 2.13.5 in the real `%APPDATA%\Python\Python312\site-packages`, and heron-ready's section 2a in that very run printed *"install from your own terminal, pinned to the versions in Claude's copy"* with `mcp==2.3.0` in its pip line. Read from Claude's shell afterwards: `mcp-2.2.0.dist-info` and `mcp-2.3.0.dist-info` are both seen, and `importlib.metadata.version("mcp")` answers **2.2.0**; every file under `mcp`, `cryptography`, `pydantic` and `pydantic_core` that exists in both places reads byte for byte as the COPY's (249, 190, 212 and 8 files), and three files only the real folder has show through - `pydantic\_internal\_internal_dataclass.py`, its `.pyc`, and `pydantic_core\_pydantic_core.pyi`. So Claude runs the copy's code with a few of the lock's files laid in, under the lock's version number. `heron-ready --check` from Claude's shell said READY, and `tests/test_mcp_serves.py` PASSED there in 51 s. **NOT checked:** a pair whose stray file is imported; what pip run from inside does to such a pair.

**State.** **MOVED TO ISSUE [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - two sources disagree, and Ajmal PS resolves it.** [WHAT-TO-INSTALL §1b](../WHAT-TO-INSTALL.md) point 2 and heron-ready's section 2a say *install the copy's versions*; the installer installs the lock's, as [Q-63](../OPEN-QUESTIONS.md)'s settled install half says. Neither is changed here. Which set a Store-Claude PC should end up with is the open half of row 417 and Q-63. **2026-10-18 - NOT settled by [D-135](../decisions/D-135.md)**, which moved Heron's DATA and not its packages. While the copy holds packages Claude installed, they shadow the real folder's file by file whatever Heron does with its data, so which set a Store-Claude PC should end with is the same choice it was. Reasoned, not tried: once the copy holds no Heron packages - a Reset of the app, or the owner removing them - the installer's lock versions are the only ones, and the advice to install the copy's versions (WHAT-TO-INSTALL §1b point 2, heron-ready section 2a) is the one to retire. Still Ajmal PS's.

---

### Row 5b-1066

*Moved from the register on 2026-11-17.*

**Defect.** **FROM INSIDE THE STORE CLAUDE, A WRITE INTO A FOLDER THAT ALREADY EXISTS ON THE REAL DISK LANDS ON THE REAL DISK - NOT IN THE APP'S COPY - SO THE INSTALLERS' REFUSAL SAYS SOMETHING THIS LAPTOP DOES NOT DO.** Measured 2026-10-17 on the owner's laptop, Store Claude `AnthropicPBC.Claude_2.31226.1.0_x64__fnn82j28hfe8t`: a folder made under `%APPDATA%\Heron` from the owner's own terminal; from Claude's shell, a line appended to a file in it and a NEW file made in it; read back from the terminal, both were on the real disk, and the copy's `LocalCache\Roaming\Heron` held no such folder. The refusal ([row 495](../fragment-issues/section-5b-rows-401-425.md)) says *"an add-in installed from here would land where Revit never looks"*, and [Q-63](../OPEN-QUESTIONS.md) with rows 416 to 419 describes every write under AppData as sent to the copy. What row 416 measured on 2026-10-10 were writes whose folders did not yet exist outside - the packages, the store. On this laptop every `Addins\<release>` folder exists, so an install from inside would plausibly have reached Revit after all. **NOT checked:** a folder that exists in BOTH places, such as `Python\Python312\site-packages` now; a new folder made from inside; and the add-in install itself from inside - the refusal stopped it, as it should.

**State.** **MOVED TO ISSUE [#900](https://github.com/Ajmalpshaik/Heron-AI/issues/900) 2026-11-16** - still open, tracked there; this row is kept whole. Was: **OPEN - recorded, not fixed.** The refusal itself stays: from inside, where a file lands depends on which folders exist where, which nobody installing can see. Its SENTENCE, and Q-63's description of the copy, are the question - for Ajmal PS, or the next session on the installer. **MEASURED 2026-10-17 at 23:53 - the case this row left NOT checked ([D-135](../decisions/D-135.md)):** in a folder that exists in BOTH places, a new file, a new folder, an edit to a file both hold and a write-then-rename all land in the COPY - the rename even over a file only the real disk held, leaving a new file in the copy that hides the real one; only an append to a file the real disk alone held reached it. A new FOLDER made from inside a real-only folder reached the real disk. So the refusal's sentence is true wherever the copy holds the folder, and which folders it holds depends on what was installed from where - the refusal stays; its wording is still the installer's session's.
