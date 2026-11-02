# Needs checking — Group LF

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LF - which images and PDF pages a model holds: `LIST_IMAGES` (2026-11-02)

Asked for in the modeller's words: *"what images are loaded in this model"* - Revit's Manage > Manage Images -
beside *"import this PDF page into the current view"*, *"place the site plan image C:\Drawings\site.png on
Level 1"* and *"bring in page 3 of the architect's PDF as an underlay"* ([Group LG](group-lg.md)). No card
listed them: `FIND_PURGEABLE_ELEMENTS` names only the images it finds **unused** and counts the rest without
naming them, and `READ_RENDERED_IMAGES` reads saved renderings and the image files in a folder, not Manage
Images - look-alikes, not duplicates. `READ_RENDERED_IMAGES`' routing table sent *"which images are loaded in
the model"* to `FIND_PURGEABLE_ELEMENTS`, which cannot name a used image; that row now names this card
([row 5b-1387](../fragment-issues/section-5b-rows-401-425.md)).

| What | Where | What it does |
|---|---|---|
| `LIST_IMAGES` (READ) | [`brain/fragments/list-images`](../../brain/fragments/list-images/fragment.yaml) | Lists every **image in Manage Images** - a picture or a PDF page - with its name, file path (and whether it is kept relative), the **PDF page**, whether it is **imported, linked or a rendering saved to the project**, its **pixel size and DPI** and whether its **file was found** (those four 2021 on), and **how many times and in which views** it is placed (`report`, `imageCount`, `placedCount`, `names`). **It never calls an image unused** - "placed 0 times" is what it reads; an image named by an image parameter is in use, and only the purge list's rule says so. A model with none is said in words. Opens no transaction |

**The read half of a pair** - every question about which images there are is answered here, so none reaches
`IMPORT_IMAGE`.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `ImageType` `Path`, `PathType` and `PageNumber`, `ImageInstance`, `GetTypeId`
and `OwnerViewId` are the same 2020 to 2027; `ImageType` `Source`, `Status`, `Resolution`, `WidthInPixels`,
`HeightInPixels` and the enums `ImageTypeSource` and `ImageTypeStatus` are **2021 on**, so a 2020 row says
Revit 2020 does not say imported or linked and gives no pixel size or file status. **Nothing here has run in
Revit.** The card is DRAFT. **Only Revit can settle** what `Path` holds for an image imported rather than
linked, what `Status` a linked image whose file was renamed reports on reopen, and whether a rendering saved
to the project is `Internal` - LF2 and LF3 check them.

**Routing.** See [Group LG](group-lg.md) - both cards were measured together.

**The models - two scratch projects, `images-list` and `images-none`**, never a working model. The job file is
[`tools/jobs/list-images-lf-2026-11-02.yaml`](../../tools/jobs/list-images-lf-2026-11-02.yaml) (`--dry-run`
reads it back and sends nothing). Every line below with `HERON_CLIENT_ID=ajmal-pc`, `--session <process id>`
when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LF1** | *Arrange.* Two projects new from the metric architectural template, saved under `%TEMP%\heron-work`. `images-list`: with Revit's own Insert > Import PDF, page 2 of a three-page `plans.pdf` placed in "Level 1" and on a sheet "A101"; Insert > Import Image, `site.png` placed once in "Level 1"; from 2021 Insert > Link Image, `logo.png`, its placement then deleted. Save, close, rename `logo.png`, reopen. `images-none`: the template untouched | Manage Images on `images-list` shows three rows, `logo.png` not found; `images-none` shows none |
| **LF2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file, both models open | Negative, `images-none`: `imageCount` 0, *No image or PDF page is in this model's Manage Images.*, no row of the other model. Positive, `images-list`: `imageCount` 3, `placedCount` 3, the plans row page 2, imported, placed 2 times in "Level 1" and "A101"; `logo.png` linked, FILE NOT FOUND, placed 0 times - never "unused" |
| **LF3** | *The second route.* Manage > Manage Images on `images-list`; then a rendering saved to the project and `LIST_IMAGES` again; then `FIND_PURGEABLE_ELEMENTS` | The same names, Count column and Link/Import; one more row "saved in the project (a rendering)"; `logo.png` among the purge list's unused image names |
| **LF4** | *Revit 2020.* LF1 without the linked image, then LF2 | `imageCount` 2; every row says Revit 2020 does not say imported or linked, with no pixel size |
