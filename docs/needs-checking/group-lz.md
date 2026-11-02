# Needs checking — Group LZ

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LZ - Annotate > Keynoting Settings - load the keynote file the modeller names by its full path, or reload the one loaded, absolute or relative, with Revit's load result and the file and entry count read back - and the keynote file named by the keynote read: `LOAD_KEYNOTE_FILE`, `REPORT_KEYNOTES` (2026-11-02)

Asked for in the modeller's words: *"load the keynote file C:\Standards\Keynotes.txt"*, *"reload the keynote
file"*, *"which keynote file is this project using"*, *"change the keynote file to the office standard on the
server"* and *"set keynote numbering to by sheet"*. No card did it: of every fragment's C# none called
`KeyBasedTreeEntryTable.LoadFrom` or `Reload`, and `REPORT_KEYNOTES` (DRAFT) read the loaded table's entries
but never named the file. heron-duplicate-checker's method, run before writing: `brain/heron_merge.py` found no
shared signature with a shared capability; the duplicate check (`0 already here`) and the Matcher (`0
match(es), 0 partial`) found nothing equal. **Widened, not written twice:** the read half went into
`REPORT_KEYNOTES`, a DRAFT, so no proof went stale. **Look-alikes, kept separate:** `REPORT_EXTERNAL_REFERENCES`
(PROVEN) lists the keynote table as one row among every link, import and point cloud, with no entry count, and
widening its code would put its proof out of date; `RELOAD_LINKS` (DRAFT) reloads Revit links on link types;
`LOAD_FAMILY` (PROVEN) loads a family file.

| What | Where | What it does |
|---|---|---|
| `LOAD_KEYNOTE_FILE` (MODIFY, Revit 2020-2027) | [`brain/fragments/load-keynote-file`](../../brain/fragments/load-keynote-file/fragment.yaml) | Loads the keynote file at `filePath` - the full path the modeller typed, read on the Revit PC - into the keynote table; with `filePath` empty, reloads the file already named (Revit's Reload). `pathType` absolute or relative as asked, empty keeps the current one; with no file named, a path type asked for loads the same file again under it. **The path is never guessed:** one that is not whole, or has no file at it, is refused before anything changes; relative for a model never saved is refused. Revit's `ExternalResourceLoadStatus` is reported in words, with the files it could not read, the lines it could not parse, the entries it left out (duplicate key, missing parent) and its failure messages. The file - path as saved, path type, where it resolves, Revit's status - and the entry count are read back before and after into `fileBefore` / `fileAfter` and `entriesBefore` / `entriesAfter` |
| `REPORT_KEYNOTES` (READ, Revit 2020-2027), widened | [`brain/fragments/report-keynotes`](../../brain/fragments/report-keynotes/fragment.yaml) | As before - the keynote tags and the keynote values the table does not hold - and now `keynoteFile`: the file Keynoting Settings names, as saved, its path type, where it resolves when that differs, and Revit's status (Loaded, NotFound); and `tableEntries`, the entries in the loaded table, counted |

**What is done and what is not.** Both compile on every release 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Every Revit member they call was read off the
reference assemblies of all eight releases with `tools/api-surface --members` - the lists are in each card's
`compatibility-note`. **Revit requires an open transaction for the load:** `LoadFrom` and `Reload` throw
`ModificationOutsideTransactionException` without one (RevitAPI.xml, 2020 to 2027), so Heron's transaction group,
which stopped Reload Latest ([row 5b-1418](../fragment-issues/section-5b-rows-401-425.md)), is no obstacle here.
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
*"which keynote file is this project using"* reaches `REPORT_KEYNOTES` by identity with and without it. One
sentence the new words moved off the card a routing table names is declared on that card, card text only -
[row 5b-1421](../fragment-issues/section-5b-rows-401-425.md). *"Set keynote numbering to by sheet"* still reaches
`REPORT_KEYNOTES`; nothing can do it (row 5b-1420).

**The model** - a scratch project from Revit's default architectural template, saved under `%TEMP%\heron-work`,
never a working model. Every write here is ROLLED BACK unless the modeller keeps it. The job file is
[`tools/jobs/keynote-file-lz-2026-11-02.yaml`](../../tools/jobs/keynote-file-lz-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session
<process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LZ1** | *Arrange.* A new project from the default architectural template; Annotate > Keynoting Settings shows Revit's default keynote file, Absolute - write its path and its entry count (View) down. One keynote tag on Level 1. In `%TEMP%\heron-work`, beside the saved model: `keynotes-b.txt`, four lines of key, text and parent separated by tabs; `keynotes-bad.txt`, two lines with no tabs and one key written twice. `keynotes-missing.txt` must not exist. Type the full paths into the job file in place of `YOU` | Keynoting Settings shows the default file; the three paths typed whole |
| **LZ2** | *The read.* The job file's `report-keynotes` job | Positive, Level 1: keynoteFile the default file's path, *"path type Absolute"*, *"Revit: Loaded"*; tableEntries the count written down; tagCount 1. Negative, `{3D}`: tagCount 0, keynoteFile and tableEntries the same. **If keynoteFile is only a display path with no path type, that is Revit's answer for `GetExternalFileReference` - record it** |
| **LZ3** | *D-30 positive and negative, THE NEGATIVE FIRST.* The job file's `load-keynote-file` job, `--write`, rolled back | Negative, keynotes-missing.txt: refused *"There is no file at ... that this PC can see"*, fileAfter equal to fileBefore, entriesAfter equal to entriesBefore. Positive, keynotes-b.txt: loadResult *"loaded"*, fileAfter *"...keynotes-b.txt (Absolute), Revit: Loaded"*, entriesAfter 4 |
| **LZ4** | *The reload and the path type, each rolled back.* `filePath` empty, `pathType` empty, on the default file; then `filePath` keynotes-b.txt, `pathType` relative; then `filePath` empty, `pathType` relative | First: *"reloaded"* or *"already current - Revit changed nothing"* - record which - with fileAfter and entriesAfter as before. Second: fileAfter *"keynotes-b.txt (Relative)"*, entriesAfter 4. Third: the default file again, *"(Relative)"* - or Revit's refusal, if the default file is on another drive from the model |
| **LZ5** | *The refusals and Revit's own result.* `filePath` `Keynotes.txt` (no folder); `pathType` sideways; `filePath` keynotes-bad.txt; then any load DISCARDED in Revit's Keep or Discard box | *"not a full path"* and the path types named, nothing changed; keynotes-bad.txt: the unreadable lines and the duplicate key named in words, entriesAfter what Revit kept; after Discard, Keynoting Settings shows the file from before |
| **LZ6** | *Kept once, and the second route.* Through `try-once` on the modeller's yes: keynotes-b.txt loaded; then Annotate > Keynoting Settings by hand | ONE entry in Revit's undo list; the dialog shows keynotes-b.txt, its path type, and View lists the entries counted in entriesAfter |
