# Needs checking — Group MK

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone. **The
> register's rules, and every group's place in it, are on that page.** A new row for this group goes in
> this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads it back
> into the register for every tool that reads it, so a row here is seen exactly as it was seen there.

## Group MK - Insert > Point Cloud and Manage Images > Reload - link a point cloud, and reload images from their files or Reload From another file: `LINK_FILES` widened, `RELOAD_IMAGES` (2026-11-02)

Asked for as two gaps on Revit's ribbon: *"Link point cloud and link IFC"* (Insert tab) and *"Reload images (Manage
Images > Reload)"*. No card did either: a grep of every fragment's C# that day for `PointCloudType`,
`PointCloudInstance`, `CreateFromIFC` and `ImageType` `Reload`, `ReloadFrom` and `CanReload` found none, and
`brain/heron_retrieve.py` (Revit 2024, a scratch store) sent *"link this point cloud"* to `ADD_REVISION_CLOUD` and
*"reload the images"* to `LOAD_FAMILY`. heron-duplicate-checker's method (`heron_duplicates.look`: 0 already here)
found look-alikes only: `RELOAD_LINKS` reloads or unloads Revit links and never an image, `IMPORT_IMAGE` brings a new
image in, `LIST_IMAGES` reads Manage Images. **Linking a file is `LINK_FILES`' job**, so it is widened (DRAFT on main
and here; version 2) rather than joined by a second card; reloading an image is a write and `LIST_IMAGES` a read, so it
is a new card. **An IFC link is not built** - see below.

| What | Where | What it does |
|---|---|---|
| `LINK_FILES` version 2 (MODIFY) | [`brain/fragments/link-files`](../../brain/fragments/link-files/fragment.yaml) | Version 1 unchanged - Revit and CAD links, one file, several or a folder, origin or shared, the already-linked left out and named. **Added: a point cloud** - a named `.rcp` or `.rcs` makes a point cloud type and one instance **origin to origin**, in one SubTransaction so a refused instance leaves no type behind, read back by id with its file's found status. **A folder still gathers only Revit and CAD files**, so a folder linked before links the same files now. **By shared coordinates a point cloud is named and not linked** ([row 5b-1441](../fragment-issues/section-5b-rows-401-425.md)), and **an `.ifc` file is named and not linked** ([row 5b-1440](../fragment-issues/section-5b-rows-401-425.md)) |
| `RELOAD_IMAGES` (MODIFY) | [`brain/fragments/reload-images`](../../brain/fragments/reload-images/fragment.yaml) | Manage Images' two buttons. **Reload**: the images `names` names (";"-separated, exact) - or, left out, every image - read again from their own files with their own page and resolution; a saved rendering, an image with no file path, and one whose file Revit cannot read (`CanReload`) are **left as they were and named** in `skipped`. **Reload From**: **one** named image from `fromPath` and, for a PDF, `page` (left out, its own), keeping its source, relative or absolute path and DPI; anything but one image named is refused with the names there are. Read back after a regenerate: on 2021 on each image's status and pixel size **before and after**, and one that reads *Failed to load* is not counted; Reload From reads back the file name. **Revit 2020 reloads, but refuses Reload From by name** - its image's DPI cannot be read there |

**What is done and what is not.** Both compile for all eight releases, 2020 to 2027
(`tools/check-fragments-compile.py`, 2026-11-02, a Linux container). Read off the shipped assemblies with
`tools/api-surface --members`: `PointCloudType.Create(Document, string, string)`, `GetPath` and `FoundStatus`, and
`PointCloudInstance.Create(Document, ElementId, Transform)` are the same 2020 to 2027 (`PointCloudFoundStatus` gains
`Invalid` in 2025); `ImageType` `Reload`, `ReloadFrom(ImageTypeOptions)`, `CanReload`, `Path`, `PathType` and
`PageNumber` are 2020 to 2027, while `Source`, `Status`, `Resolution` and the pixel sizes, and the `ImageTypeOptions`
constructor taking an `ImageTypeSource`, are 2021 on. **`RevitLinkType.CreateFromIFC` is present 2020 to 2027 but
links only the Revit file Revit's IFC importer writes from the `.ifc`** - RevitAPI.xml calls it *"one of a series of
steps"*, the rest in the IFC add-in - so a fragment cannot link an IFC, and none pretends to
([row 5b-1440](../fragment-issues/section-5b-rows-401-425.md), OPEN). **Nothing here has run in Revit.** Both cards
are DRAFT. **Only Revit can settle**: the engine name `PointCloudType.Create` wants for a ReCap file (the card passes
the extension, `rcp` or `rcs`, as RevitAPI.xml says - *"the file extension or engine identifier"*); where Revit's own
*Auto - By Shared Coordinates* puts a scan, which decides row 5b-1441; whether `ImageType.Path` holds the full path
after Reload From with a relative path kept; and what `CanReload` says for a linked image whose file was renamed -
MK2 to MK5 check them.

**Routing.** Measured through `heron_retrieve.find` on fresh scratch stores of the tree at `79a709d` and of this change (`model` backend; no Revit filter, 2020 and 2024; with and without the host's question flag), over every declared utterance, identity and routing-table sentence of every live card and 22 probes (5,084 sentences). Every sentence the two cards declare reaches them by identity; *"link this point cloud"* moved from `ADD_REVISION_CLOUD` to `LINK_FILES`, and *"reload the images"* from `LOAD_FAMILY` to `RELOAD_IMAGES`. The first draft took *"what images are loaded in this model"* on the keyword route and *"are the images loaded"* without the flag from `LIST_IMAGES`; both are back with it after rewording the write's purpose and saying in `LIST_IMAGES`' purpose that it reads whether each image is loaded ([row 5b-1442](../fragment-issues/section-5b-rows-401-425.md), FIXED). *"which images need reloading"*, nobody's words, still reaches the write without the flag, and three point cloud and IFC probes reach writes exactly as they did before ([row 5b-1443](../fragment-issues/section-5b-rows-401-425.md), OPEN). `tools/check-routing.py` on a scratch store: no question answered by a write, the same 66 routing-table sentences unreached, two contests fewer; `tests/test_routing_phrases.py` fails exactly its known seven.

**The models.** Two scratch projects on Revit 2024, new from the metric architectural template, saved under
`%TEMP%\heron-work`, never a working model - **`links-mk`**, nothing linked, with a small ReCap project `scan.rcp`
and a copy of any small `.ifc` as `structure.ifc` in `%TEMP%\heron-work\link-mk` (the owner's own test files, never a
client's); and **`images-reload`**, arranged as `RELOAD_IMAGES`' cases say: `site.png` imported and `plans.pdf` page 2
imported, each placed once in "Level 1", `logo.png` linked and placed once; saved and closed, then outside Revit
`site.png` painted green at the same size and `logo.png` renamed. Every write here is ROLLED BACK unless the modeller
keeps it. The job file is
[`tools/jobs/point-cloud-reload-images-mk-2026-11-02.yaml`](../../tools/jobs/point-cloud-reload-images-mk-2026-11-02.yaml)
(`--dry-run` reads it back and sends nothing); its paths are the owner's PC's `%TEMP%`. Every line below with
`HERON_CLIENT_ID=ajmal-pc`, `--session <process id>` when two Revits are connected, and `< /dev/null`.

| # | Run | Look for |
|---|---|---|
| **MK1** | *Arrange.* Make `links-mk` and `images-reload` and their files as above, and save each | Manage Links empty in `links-mk`; Manage Images in `images-reload` listing site.png, the plans page and logo.png, logo.png's file not found and site.png still RED |
| **MK2** | *Point cloud - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's first job, `--write`, rolled back | Negative, `structure.ifc`: linked 0, *"structure.ifc: an IFC file is not linked here - ..."*, Manage Links unchanged. Positive, `scan.rcp`: linked 1, one point cloud in `linkedIds`, *"scan.rcp: linked, origin to origin, a point cloud, its file found."*, Manage Links' Point Clouds tab listing it. **RECORD Revit's refusal word for word if `PointCloudType.Create` rejects the engine name `rcp`** - the card is corrected before any proof |
| **MK3** | *Point cloud, by hand with `validate --write`, rolled back.* `scan.rcp` with `placement shared`; MK2's positive KEPT once with `try-once` in a scratch copy, then run again; the folder `link-mk` with one `.rvt` added; then Insert > Point Cloud, *Auto - By Shared Coordinates*, in a copy whose shared coordinates are moved 10 m east (Manage > Coordinates > Specify Coordinates at Point) | Shared refused by name, nothing linked; the second run `alreadyLinked` scan.rcp, one cloud; the folder links the `.rvt` only - no point cloud, no IFC. **RECORD where Revit's own shared placement puts the scan** (row 5b-1441) |
| **MK4** | *Reload - D-30 positive and negative, THE NEGATIVE FIRST.* The job file's second job, `--write`, rolled back | Negative, `logo.png`: reloaded 0, skipped *"Revit cannot reload it from ... Left as it was."*, and **site.png still RED in Level 1**. Positive, `site.png`: reloaded 1, report *"... reloaded, ... - now Loaded, <w> x <h> px (was ...)"*, the square GREEN |
| **MK5** | *Every image and Reload From, by hand with `validate --write`, rolled back.* `names` left out; `names "site.png"`, `fromPath` site-v2.png; `names "site.png; logo.png"` with the same `fromPath`; `names "no such image"`; Revit 2020 with `fromPath` | Every image: reloaded 2, logo.png skipped. Reload From: reloaded 1, the square BLUE, Manage Images' Path site-v2.png. Two names: refused, *"Reload From takes ONE image ..."*. The unknown name listed with the names there are. 2020: refused by name, nothing reloaded. `names` left out never read as an image called "all", `fromPath` left out never as a file called "none" |
| **MK6** | *The second route.* Insert > Point Cloud, *Auto - Origin to Origin*, and Manage Images > Reload and Reload From, by hand on copies of the same saved files | The same place in a 3D view and the same Point Clouds row; the same colours and Path; one Ctrl+Z takes each run back as one entry |
