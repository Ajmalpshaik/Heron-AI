# Needs checking — Group OL

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group OL - what wrote a document to disk when `ACTIVATE_DOCUMENT` left it, and the read-back that now says so (2026-11-18)

**Tracked as GitHub issue [#1035](https://github.com/Ajmalpshaik/Heron-AI/issues/1035) since 2026-11-19** - the checks still owed are listed there; tick the group in that issue when they are done. Every result stays in this file.

From [row 5b-1851](../fragment-issues/row-5b-1851.md): on 2026-11-18, on Revit 2020, the document in
front was written to its file within about a second of `ACTIVATE_DOCUMENT` bringing another document
forward, with no save asked - **3 times out of 3 when the document brought forward already had a window,
0 out of 1 when it was opened from its file.** The trace in a cloud container found no Heron code that
saves there. So **OL1 comes first and needs no Revit run at all**: what that day left on disk may already
name what wrote the files.

[`activate-document`](../../brain/fragments/activate-document/fragment.yaml) **version 3, DRAFT** (was
PROVEN at version 2) only adds reads: the document left behind - whether it has unsaved changes, and its
file's time - before the call and after it, answered as `leftBehindWritten` and a finding. It compiles on
every release it claims (`tools/check-fragments-compile.py`, 2026-11-18, the cloud container's .NET
SDK). **It has not run in Revit.** No add-in code changed, so nothing needs rebuilding: the card travels
as source from the checkout, and `revit_show` runs a DRAFT and says so. **Revit 2020 first** - the release
it was seen on; the same rows on any other release are evidence about that release only.

**The models are Heron's own scratch files, never a working model:** `ol-host.rvt` and `ol-family.rfa`
in `%TEMP%\heron-work\ol`. A write of either is the thing being measured, so it costs nothing.

| ID | Do this | Pass looks like |
|---|---|---|
| **OL0** | *Arrange, Revit 2020.* Write down Revit's **Options > General > Save reminder interval** - do not change it. Make a project from `DefaultMetric.rte` with `CREATE_FAMILY_DOCUMENT` - an ADMIN card: the owner's **Admin** switch on in Settings, with Changes; it makes projects too, from an `.rte` - saved as `%TEMP%\heron-work\ol\ol-host.rvt`, and a Generic Model family from its template the same way, saved as `ol-family.rfa` in that folder. Bring each to the front once with `ACTIVATE_DOCUMENT`, so both have a window | Both files on disk, both open in a window. The reminder interval written down |
| ~~**OL1**~~ | The evidence of 2026-11-18 - read, nothing run. (1) Revit 2020's journal for that session, in… — [full row removed 2026-11-24 - in git history](../needs-checking-archive/README.md) | **Write down what each says, word for word.** |
| **OL2** | *Version 3, positive - to a document ALREADY IN A WINDOW (the pattern seen 3 of 3).* `ol-family` in front; draw one reference plane in it by hand and do NOT save; note `ol-family.rfa`'s time in Explorer. `revit_show ACTIVATE_DOCUMENT` with `documentPath` = `ol-host.rvt`'s full path | `activated true`, `activeAfter` `ol-host`, and **`leftBehindWritten` says what Explorer shows**: true with *"'ol-family' WAS WRITTEN TO ITS FILE while Revit switched windows - its file's time moved from ... to ..."* (local times, as Explorer shows them) when the file's time moved and a new `.0001` is beside it; false with *"'ol-family', now behind, still had unsaved changes when Heron looked - Heron saved nothing. The document brought forward was already open in a window..."* when it did not - and then OL6 matters. Either is a pass for the read-back; **write down which** - it is the answer to row 5b-1851 on this release **Seen 2026-11-20 outside the OL rig** (not struck): Revit 2020, `ACTIVATE_DOCUMENT` from *House Scratch 1042* (unsaved: a placed column) to *Column Scratch 1044*, already open in a window - `leftBehindWritten false`, with the row 5b-1851 caution; `House Scratch 1042.rvt` still read 15:07:29 afterwards (and at 15:33). One switch to an open window, nothing written. |
| **OL3** | *Version 3 - to a document NOT OPEN (the case seen 0 of 1).* Close `ol-host` (it is saved). `ol-family` in front, still holding its unsaved reference plane - if OL2 wrote it, draw another and do not save. `revit_show ACTIVATE_DOCUMENT` with `documentPath` = `ol-host.rvt`'s full path | `activated true`, and `leftBehindWritten` agrees with Explorer, as in OL2 - and if not written, the unsaved-changes line WITHOUT the caution, because `ol-host` had no window. Write down which: the field case says not written **Seen 2026-11-20 outside the OL rig** (not struck - not `ol-host`/`ol-family`): Revit 2020, `ACTIVATE_DOCUMENT` from the command line to scratch files not open - *House Scratch 1042* left behind *Tower Scratch 1042* holding a kept camera view, and *Pump Scratch 1030* left *House Scratch 1042* - each `leftBehindWritten false` with *"... now behind, still had unsaved changes when Heron looked - Heron saved nothing"*; the left-behind file's time unchanged (Tower 14:32:42 until the owner's own save at 15:07). |
| **OL4** | *Version 3, negative - a switch that happens, from a document with nothing unsaved.* `ol-host` open in a window and saved; `ol-family` in front, saved just now (Ctrl+S), nothing drawn since; note `ol-family.rfa`'s time. `ACTIVATE_DOCUMENT` with `documentPath` = `ol-host.rvt` | `activated true`, **`leftBehindWritten false`**, no line about `ol-family`, its file's time unchanged. The negative that reaches the new reads: a read-back that always said "written" fails here |
| **OL5** | *The card's old negative - no switch is tried.* `ol-family` in front with an unsaved reference plane; `documentPath=C:\nope\missing.rfa` | `activated false`, `activeAfter` empty, `leftBehindWritten false`, findings only *"NOT activated..."* and *"There is no file at \"C:\nope\missing.rfa\"."* - no line about the document behind, because nothing was switched. The file's time unchanged |
| **OL6** | *After the reply, where no guard is armed.* After OL2 and OL3, leave Revit untouched for two minutes, then look at the left-behind file's time again, and at the journal and the add-in log for those minutes | The time as the reply left it. **A time that moved AFTER a reply that said `leftBehindWritten false`** means the write is Revit's, after Heron's job ended - write down what the journal shows at that second |

**The proof itself is written as version 2's was**, once OL2 and OL4 read as above. `ol-host` open in a
window and saved, `ol-family` in front holding an unsaved reference plane:

```
python mcp/client/heron_bridge_client.py validate activate-document --set "documentPath=<ol-host.rvt's full path>" --negative-set "documentPath=C:\nope\missing.rfa"
```

**The negative of a proof is OL5's missing file, not OL4's switch (row 5b-1968).** A switch that HAPPENS answers `activated true` and `activeAfter` *ol-family* - both results, so a batch reads NEG NOT EMPTY and `accept` refuses it. OL4 is read by eye beside the proof; the missing file is what reads empty (version 2's signed negative).

The positive leaves `ol-family` with its unsaved work for `ol-host`; the negative then leaves `ol-host`,
with nothing unsaved, for `ol-family` - and must say `leftBehindWritten false`. Read
`brain/proof-drafts/activate-document.yaml`, and the findings it quotes - the runner cannot read a
sentence - then sign on the owner's word only:
`python brain/heron_validate.py accept activate-document --by "Ajmal PS"`. The second route is Revit's own title bar, and `REPORT_OPEN_DOCUMENTS` (DRAFT) for the document in front.
