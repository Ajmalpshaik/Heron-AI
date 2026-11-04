# Needs checking — Group LG

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group LG - a PDF page or a picture imported into a view: `IMPORT_IMAGE` (2026-11-02)

Asked for in the modeller's words: *"import this PDF page into the current view"*, *"place the site plan image
C:\Drawings\site.png on Level 1"* and *"bring in page 3 of the architect's PDF as an underlay"* - Revit's
Insert > Import PDF and Insert > Import Image. No card made an image: a grep of every fragment's C# that day
for `ImageType` and `ImageInstance` found only `FIND_PURGEABLE_ELEMENTS` and `READ_RENDERED_IMAGES`, both
reads. `IMPORT_CAD` imports DWG, DXF and DGN drawings through `Document.Import`, placed origin, centre or
shared, with no page or resolution; `LINK_FILES` links Revit models and CAD drawings; `EXPORT_VIEW_IMAGE` and
`EXPORT_SHEETS_TO_PDF` write files out of views; `SET_VIEW_UNDERLAY` sets a plan's ghosted level -
look-alikes, not duplicates. *"what images are loaded in this model"* is `LIST_IMAGES`' ([Group LF](group-lf.md)).

| What | Where | What it does |
|---|---|---|
| `IMPORT_IMAGE` (MODIFY) | [`brain/fragments/import-image`](../../brain/fragments/import-image/fragment.yaml) | Imports **one page of a PDF** (`page`, from 1) **or a PNG, JPG, BMP or TIFF** into a plan, section, elevation, drafting view, legend or sheet - the view named, or **the view in front** - as an **imported** image (never a link), at `point` "x,y" in **mm** from the internal origin as seen in the view (on a sheet, its origin), `anchor` centre or a corner, at `resolution` DPI or Revit's own. **The path is the modeller's and is never guessed**; it is read on the PC Revit runs on, so a missing file is said in those words at run time. A **3D view, a schedule or a template is refused by name** before anything is loaded; Revit's own refusal (a page the PDF has not got) is passed on, and the image and placement are made in one SubTransaction so nothing is left half made. An image already in Manage Images **from the same file and page is placed again**, not loaded twice (`reusedType`). Read back by id: the image's name, page, pixel size and DPI (2021 on), the view, the placed image's id, width and height in mm, where its centre landed, background or foreground (`imageInstanceId`, `imageTypeId`, `report`, `refused`). Never deletes |

**One of a pair.** `LIST_IMAGES` ([Group LF](group-lf.md)) answers every question about which images there are,
so none reaches this write.

**What is done and what is not.** It compiles for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `ImageType.Create(Document, ImageTypeOptions)`, `ImageInstance.Create(Document,
View, ElementId, ImagePlacementOptions)`, `ImageInstance.IsValidView`, `ImagePlacementOptions(XYZ,
BoxPlacement)` and `ImageTypeOptions` `PageNumber`, `Resolution` and `IsValid` are the same 2020 to 2027.
**The `ImageTypeOptions` constructor is not:** 2020 has `(string, bool)` and no `ImageTypeSource` (that enum is
2021 on); 2022 on has only `(string, bool, ImageTypeSource)` - so 2020 takes the first and every later release
the second with `Import`. **Nothing here has run in Revit.** The card is DRAFT. **Only Revit can settle**
whether `ImagePlacementOptions` takes a point off the view's plane (sections and elevations get the internal
origin projected onto the plane), what `ImageType.Path` holds for an imported image - which decides whether
the same file and page is found again - and what Revit says for a page the PDF has not got - LG3 to LG5 check
them.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree without the two image cards and with them (`model` backend, no re-ranker; no Revit filter, 2020 and 2024 over every declared and claimed sentence, 5,252). The four sentences reach their cards by identity - before, `EXPORT_SHEETS_TO_PDF`, `IMPORT_CAD` and `READ_RENDERED_IMAGES`. *"which images are loaded in the model"*, which `READ_RENDERED_IMAGES`' routing table sent to `FIND_PURGEABLE_ELEMENTS`, now reaches `LIST_IMAGES` and that row names it ([row 5b-1387](../fragment-issues/section-5b-rows-401-425.md), FIXED). Probes nobody declared - *import an image into this view*, *import a pdf*, *put the logo image on the sheet* - reach `IMPORT_IMAGE` (`IMPORT_CAD`, `EXPORT_SHEETS_TO_PDF` or `EXPORT_VIEW_IMAGE` before); *list the images in the model* reaches `LIST_IMAGES`; *link this pdf* still reaches `EXPORT_SHEETS_TO_PDF` (on 2020 `LIST_IMAGES`, `LINK_FILES` before) - no card links a PDF. `tools/check-routing.py`: no question answered by a write; one new contest on the keyword route alone, *"make a pdf of the drawings"*, which `find` answers at `EXPORT_SHEETS_TO_PDF` by identity, written in both cards. **On 2020 and 2021**, where no card makes a PDF, that sentence and *"i need a pdf set"* reach this card, and on 2020 to 2023 `CREATE_TOPOSOLID`'s *"model the ground from these levels"* flipped from one card that cannot do it to another ([row 5b-1388](../fragment-issues/section-5b-rows-401-425.md), OPEN).

**The model - `images-ip`, a scratch project** new from the metric architectural template, saved under
`%TEMP%\heron-work`, never a working model: "Level 1", a "{3D}" view, a sheet "A101", a room schedule, nothing
in Manage Images; `site.png` and a three-page `plans.pdf` in `%TEMP%\heron-work\images`. Every write here is
ROLLED BACK unless the modeller keeps it. The job file is
[`tools/jobs/import-image-lg-2026-11-02.yaml`](../../tools/jobs/import-image-lg-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing); its path is the owner's PC's `%TEMP%`. Every line below with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **LG1** | *Arrange.* Make `images-ip` and the two files as above, and save | Manage Images empty |
| **LG2** | *The D-30 positive, and its negative - THE NEGATIVE FIRST.* The job file, `--write`, rolled back | Negative, `{3D}`: nothing made, refused *"{3D}" is a 3D view, and Revit does not place images there*, Manage Images unchanged. Positive, page 3 into "FloorPlan: Level 1": `imageInstanceId` and `imageTypeId` valid, `reusedType` false, report naming the image, page 3, "Level 1", its size in mm and centre at 0,0 |
| **LG3** | *Point and anchor.* `site.png`, the view left out with "Level 1" in front, `point` 5000,2000, `anchor` bottom left; then on sheet "A101" at 420,297; then in a section | The bottom left corner at 5000,2000 mm (the centre reported half the size further); centred at 420,297 on the sheet; in the section, placed on the section's plane, or Revit's refusal |
| **LG4** | *Never twice, never guessed.* LG2's positive KEPT once with `try-once` in a scratch copy, then run again; then page 9; then `no-such-file.pdf`; then a `.dwg` | `reusedType` true, Manage Images still ONE row for page 3; page 9 refused in Revit's words with nothing left in Manage Images; *There is no file at ...*; *is not a PDF, PNG, JPG, BMP or TIFF* |
| **LG5** | *The second route, and Revit 2020.* Revit's own Insert > Import PDF of page 3 into the same view, side by side; one Ctrl+Z; then LG2 on Revit 2020 | The same page at the same size; one undo takes the image and its Manage Images row away; 2020 imports with the report missing pixel size and DPI |
