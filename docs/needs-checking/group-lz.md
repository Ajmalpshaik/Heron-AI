# Needs checking — Group LZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LZ - Annotate > Keynoting Settings - load the keynote file the modeller names by its full path, or reload the one loaded, absolute or relative, with Revit's load result and the file and entry count read back - and the keynote file named by a read of its own: `LOAD_KEYNOTE_FILE`, `REPORT_KEYNOTE_FILE` (2026-11-02)

Asked for in the modeller's words: *"load the keynote file C:\Standards\Keynotes.txt"*, *"reload the keynote
file"*, *"which keynote file is this project using"*, *"change the keynote file to the office standard on the
server"* and *"set keynote numbering to by sheet"*. No card did it: of every fragment's C# none called
`KeyBasedTreeEntryTable.LoadFrom` or `Reload`, and `REPORT_KEYNOTES` read the loaded table's entries but never
named the file. heron-duplicate-checker's method, run before writing: `brain/heron_merge.py` found no
shared signature with a shared capability; the duplicate check (`0 already here`) and the Matcher (`0
match(es), 0 partial`) found nothing equal. **The read half is a card of its own, `REPORT_KEYNOTE_FILE`.** It
was first written into `REPORT_KEYNOTES`, then DRAFT, as `keynoteFile` and `tableEntries`; the same day #794
proved `REPORT_KEYNOTES` on Revit 2024 against code without them, so the read moved line for line to
`REPORT_KEYNOTE_FILE` (FRG-VIEW-170, READ) and `REPORT_KEYNOTES` keeps main's proved code byte for byte.
**Look-alikes, kept separate:** `REPORT_EXTERNAL_REFERENCES`
(PROVEN) lists the keynote table as one row among every link, import and point cloud, with no entry count, and
widening its code would put its proof out of date; `RELOAD_LINKS` (DRAFT) reloads Revit links on link types;
`LOAD_FAMILY` (PROVEN) loads a family file.

| What | Where | What it does |
|---|---|---|
| `LOAD_KEYNOTE_FILE` (MODIFY, Revit 2020-2027) | [`brain/fragments/load-keynote-file`](../../brain/fragments/load-keynote-file/fragment.yaml) | Loads the keynote file at `filePath` - the full path the modeller typed, read on the Revit PC - into the keynote table; with `filePath` empty, reloads the file already named (Revit's Reload). `pathType` absolute or relative as asked, empty keeps the current one; with no file named, a path type asked for loads the same file again under it. **The path is never guessed:** one that is not whole, or has no file at it, is refused before anything changes; relative for a model never saved is refused. Revit's `ExternalResourceLoadStatus` is reported in words, with the files it could not read, the lines it could not parse, the entries it left out (duplicate key, missing parent) and its failure messages. The file - path as saved, path type, where it resolves, Revit's status - and the entry count are read back before and after into `fileBefore` / `fileAfter` and `entriesBefore` / `entriesAfter` |
| `REPORT_KEYNOTE_FILE` (READ, Revit 2020-2027) | [`brain/fragments/report-keynote-file`](../../brain/fragments/report-keynote-file/fragment.yaml) | `keynoteFile`: the file Keynoting Settings names, as saved, its path type, where it resolves when that differs, and Revit's status (Loaded, NotFound) - empty, and said, when none is named; and `tableEntries`, the entries in the loaded table, counted. `REPORT_KEYNOTES` (PROVEN, #794) still answers the tags and the values the table does not hold |

**What is done and what is not.** Both compile on every release 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member they call was read off the
reference assemblies of all eight releases with `tools/api-surface --members` - the lists are in each card's
`compatibility-note`. **Revit requires an open transaction for the load:** `LoadFrom` and `Reload` throw
`ModificationOutsideTransactionException` without one (RevitAPI.xml, 2020 to 2027), so Heron's transaction group,
which stopped Reload Latest ([row 5b-1423](../fragment-issues/section-5b-rows-401-425.md)), is no obstacle here.
**Nothing here has run in Revit.** Both cards are DRAFT. **Only Revit can settle:** whether
`Element.GetExternalFileReference` answers for the keynote table, or only the resource reference's display path
does (LZ2); which status `Reload` returns for a file that has not changed - Success or ResourceAlreadyCurrent
(LZ4); whether a load the transaction group later discards puts the old file back (LZ5). **Not done:** the
numbering method (By keynote / By sheet) - no Revit API member reaches it on any release, [row
5b-1420](../fragment-issues/section-5b-rows-401-425.md), OPEN; a keynote file on a keynote server other than
Revit's own (`CreateLocalResource` makes a local reference only); *"the office standard on the server"* names
no path, and the host asks for it rather than guessing.

**Routing.** `heron_retrieve.find` on fresh scratch stores of the tree at `72055b3` and of this change, over the
five sentences and every live card's utterance and routing row with *keynote*, *load*, *reload*, *file*,
*numbering*, *path type*, *relative*, *absolute*, *office standard* or *server* in it (129), on Revit 2024, 2027
and none named, with and without the host's question flag. The three writes reach `LOAD_KEYNOTE_FILE` by
identity without the flag (before: `REPORT_KEYNOTES`, and `LOAD_FAMILY` for *"reload the keynote file"*);
*"which keynote file is this project using"* reached `REPORT_KEYNOTES` by identity with and without it, and
since the read moved reaches `REPORT_KEYNOTE_FILE` - with and without the flag, never `LOAD_KEYNOTE_FILE`
(`heron_retrieve.find` on a scratch store of the split, 2026-11-02, Revit 2024, 2027 and none named). One
sentence the new words moved off the card a routing table names is declared on that card, card text only -
[row 5b-1421](../fragment-issues/section-5b-rows-401-425.md). *"Set keynote numbering to by sheet"* still reaches
`REPORT_KEYNOTES`, after the move too; nothing can do it (row 5b-1420).

**The model** - a scratch project from Revit's default architectural template, saved under `%TEMP%\heron-work`,
never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job file is
[`tools/jobs/keynote-file-lz-2026-11-02.yaml`](../../tools/jobs/keynote-file-lz-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LZ1** | *Arrange.* `keynote-lz`: a new project from the default architectural template; Annotate > Keynoting Settings shows Revit's default keynote file, Absolute - write its path and its entry count (View) down. One keynote tag on Level 1. `keynote-lz-b`: the same template, saved beside it, with `keynotes-b.txt` loaded by hand in Keynoting Settings, Absolute. In `%TEMP%\heron-work`, beside the saved model: `keynotes-b.txt`, four lines of key, text and parent separated by tabs; `keynotes-bad.txt`, two lines with no tabs and one key written twice. `keynotes-missing.txt` must not exist. **Since 2026-11-15:** also File > New > Family > Generic Model (metric), left unsaved, so its title is `Family1` - LZ2's negative. Type the full paths into the job file in place of `YOU` | Keynoting Settings shows the default file; the three paths typed whole; `keynote-lz`, `keynote-lz-b` and `Family1` open in the one Revit |
| **LZ2** | *The read, D-30 positive and negative, THE NEGATIVE FIRST.* The job file's `report-keynote-file` job | **Negative since 2026-11-15, `Family1`** ([row 5b-1737](../fragment-issues/row-5b-1737.md)): keynoteFile empty, tableEntries 0, *"... is a family - the keynote file is a project setting, so nothing was read"*. Then by hand, `validate --in keynote-lz-b report-keynote-file`: keynoteFile names keynotes-b.txt and **not** the default file, tableEntries 4 - a second check, never the empty half. Positive, `keynote-lz`: keynoteFile the default file's path, *"path type Absolute"*, *"Revit: Loaded"*; tableEntries the count written down. **If keynoteFile is only a display path with no path type, that is Revit's answer for `GetExternalFileReference` - record it** |
| **LZ3** | *D-30 positive and negative, THE NEGATIVE FIRST.* The job file's `load-keynote-file` job, `--write`, rolled back - judged on `loadedFile` since version 2 ([row 5b-1647](../fragment-issues/row-5b-1647.md)) | Negative, keynotes-missing.txt: **loadedFile empty**, refused *"There is no file at ... that this PC can see"*, fileAfter equal to fileBefore, entriesAfter equal to entriesBefore. Positive, keynotes-b.txt: loadedFile and fileAfter *"...keynotes-b.txt (Absolute), Revit: Loaded"*, loadResult *"loaded"*, entriesAfter 4. Then once by hand with the path typed in forward slashes: *"loaded"* and **no** *"not the path typed"* line ([row 5b-1738](../fragment-issues/row-5b-1738.md)) |
| **LZ4** | *The reload and the path type, each rolled back.* `filePath` empty, `pathType` empty, on the default file; then `filePath` keynotes-b.txt, `pathType` relative; then `filePath` empty, `pathType` relative | First: *"reloaded"* or *"already current - Revit changed nothing"* - record which - with fileAfter and entriesAfter as before. Second: fileAfter *"keynotes-b.txt (Relative)"*, entriesAfter 4. Third: the default file again, *"(Relative)"* - or Revit's refusal, if the default file is on another drive from the model |
| **LZ5** | *The refusals and Revit's own result.* `filePath` `Keynotes.txt` (no folder); `pathType` sideways; `filePath` keynotes-bad.txt; then any load DISCARDED in Revit's Keep or Discard box | *"not a full path"* and the path types named, nothing changed; keynotes-bad.txt: the unreadable lines and the duplicate key named in words, entriesAfter what Revit kept; after Discard, Keynoting Settings shows the file from before |
| **LZ6** | *Kept once, and the second route.* Through `try-once` on the modeller's yes: keynotes-b.txt loaded; then Annotate > Keynoting Settings by hand | ONE entry in Revit's undo list; the dialog shows keynotes-b.txt, its path type, and View lists the entries counted in entriesAfter |

### The sitting - Revit 2024, then Revit 2020 (re-arranged 2026-11-15, field report #854)

**Why now.** Sruthina's drawing set on Revit 2024 (`villa 2`, [Ajmalpshaik/Heron-AI#854](https://github.com/Ajmalpshaik/Heron-AI/issues/854)
item 1, [#853](https://github.com/Ajmalpshaik/Heron-AI/issues/853) D1) placed 47 keynote tags and two keynote legends,
and every one prints blank until a project keynote file is loaded - `LOAD_KEYNOTE_FILE` was refused as DRAFT. **What
has run so far:** version 1 on Revit 2020, 2026-11-14, `P1102 MEP`, rolled back - the US Metric file loaded (3840
entries) and a path with no file was refused with the table untouched, but its result was filled both times
([row 5b-1647](../fragment-issues/row-5b-1647.md)). **What changed, 2026-11-15, in a cloud session with no Revit:**
`LOAD_KEYNOTE_FILE` version 2 (result `loadedFile`, empty on any refusal or failed load; full-path compare, row
5b-1738) and `REPORT_KEYNOTE_FILE` version 2 (a family answered, never read - the negative, row 5b-1737). Both still
DRAFT and never proven, so no proof went stale. Both compile for Revit 2020 to 2027 (`tools/check-fragments-compile.py`,
2026-11-15). `tests/test_keynote_file_cards.py` holds both repairs and fails on version 1. **Nothing here has run in
Revit since.**

**One sitting, in this order.** The job file is the same one, re-arranged: `report-keynote-file` and `load-keynote-file`
are pinned to `keynote-lz`, and the read's negative is `Family1`.

| Step | Revit | Run | Rows |
|---|---|---|---|
| 1 | 2024 | Arrange LZ1 in Revit 2024 from its default architectural template (metric). Write down the default keynote file's path, path type and entry count | LZ1 |
| 2 | 2024 | `python tools/batch-prove.py tools/jobs/keynote-file-lz-2026-11-02.yaml --dry-run`, then the same with `--session <2024 pid>`. Both jobs should read `PASS`. The positive's path type is the one step 1 wrote down - on Revit 2020 the default file read `Content`, not Absolute ([row 5b-1647](../fragment-issues/row-5b-1647.md)) | LZ2, LZ3 |
| 3 | 2024 | The hand checks: `keynote-lz-b` named by the read; the forward-slash load; then LZ4 and LZ5, each rolled back, and LZ5's Discard | LZ2, LZ3, LZ4, LZ5 |
| 4 | 2024 | Review both drafts (`python brain/heron_validate.py review <fragment>`) and sign only on the owner's word - `accept` under his own name | - |
| 5 | 2024 | LZ6 - through `try-once`, on his yes, `keynotes-b.txt` kept once; then Keynoting Settings by hand, the second route | LZ6 |
| 6 | 2020 | **LZ7.** Only once both are PROVEN on 2024: arrange LZ1 again in Revit 2020 (its template, its default file), then `python tools/batch-prove.py tools/jobs/keynote-file-lz-2026-11-02.yaml --release 2020 --session <2020 pid>` and `accept <fragment> --release 2020` ([D-131](../decisions/D-131.md)) | LZ7 |

| # | Run | Look for |
|---|---|---|
| **LZ7** | *Revit 2020, the release entry.* Step 6 above, after LZ2 and LZ3 are signed on 2024 | Both jobs `PASS` on 2020 with the same readings as LZ2 and LZ3; `accept --release 2020` writes one `proof.releases` entry each. A DRAFT here reads `NOT PROVEN` - prove on 2024 first |

**Not for the sitting:** Sruthina's own model and keynote file. The scratch projects are the proof; once
`LOAD_KEYNOTE_FILE` is PROVEN, loading her project keynote file into `villa 2` is an ordinary chat request, kept or
discarded by her.

