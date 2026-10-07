# Needs checking — Group CS

> One group of [the register](../NEEDS-CHECKING.md), in its own file so that it can be read alone.
> **The register's rules, and every group's place in it, are on that page.** A new row for this group
> goes in this file. [`tools/needs-checking-register.py`](../../tools/needs-checking-register.py) reads
> it back into the register for every tool that reads the register.

## Group CS - a nested part made in its own window and brought back into the host family (2026-10-07)

The owner, 2026-10-07: *"this tire should be separate family ... it will automatically open ... bring to
that family 1 and it will place"*, and *"I can see the families that nested families that you are creating.
So I can suggest that this is OK or not."* The `family-creation` skill's step 11 now makes such a part in
its own window, stops for the modeller to look at it, and brings it back (a to g). Three tools were widened
for it, each DRAFT:

- `SAVE_DOCUMENT` version 2 - saves ANOTHER open document named by its file, and closes it when it is not the
  window in front ([row 5b-358](../fragment-issues/section-5b-rows-176-200.md): version 1 could never save).
- `ACTIVATE_DOCUMENT` version 2 - "already in front" judged by the window in front, by its file
  ([row 5b-359](../fragment-issues/section-5b-rows-176-200.md)).
- `LOAD_FAMILY` version 3 - a family open with unsaved changes is refused
  ([row 5b-360](../fragment-issues/section-5b-rows-176-200.md)).

**What has run, 2026-10-07, Revit 2024 session 9240, on scratch families only** - `HeronNest_Car`,
`HeronNest_Tyre`, `HeronNest_Hub` in `%TEMP%\heron-work\nest`, never Family1 (the owner's unsaved split AC).
The whole chain, kept: the tyre made from the car (saved, not opened, `openNext`), opened in its own window,
built (a ring and a hub, Work Plane-Based), saved from the car while in front (`closed false`, Revit still
running), the car brought forward (`wasAlready false`), a load of the unsaved tyre refused, the car's own
save refused, the tyre saved and closed from behind, loaded, and four placed at (+-800, +-1300, 0) mm.
Read back: exactly 4 Generic Models in the car, 0.19362 m3 in all - 48.41 L each, the ring's 36.19 L and the
hub's 12.22 L, so the hub added just before the save-and-close went in. Then one `validate` record each, the
writes rolled back, drafted to `brain/proof-drafts/`.

| # | Run | Look for |
|---|---|---|
| **CS1** | Read `brain/proof-drafts/activate-document.yaml`, then `python brain/heron_validate.py accept activate-document --by "Ajmal PS"` on the owner's word only | positive: aimed at `HeronNest_Car` with `HeronNest_Tyre` in front, `wasAlready false`, `activeAfter HeronNest_Car`; negative: a missing file, `activated false` |
| **CS2** | The same for `save-document` | positive: `HeronNest_Hub` saved (file 21:29:43 to 21:30:24) and `closed true`; negative: `HeronNest_Car`, the document Heron works in, refused and its file untouched |
| **CS3** | The same for `load-family` - **the owner's call whether to sign before Group CN**: the reload half of version 2 (CN1 to CN9) has not run | positive: a saved family not in the car loaded new; negative: `HeronNest_Hub` open with an unsaved form refused by name; both rolled back |
| **CS4** | The whole chain through the CHAT, in the owner's words, on a real job - a car with its tyres - with the host SAVED once | The part opens in its own window; Heron STOPS and shows it before anything goes into the host; at the end the host is in front, the part's window closed, and the host's count of that category equals the number placed |
| **CS5** | The same with a host NEVER SAVED - File > New > Family, nothing saved | Step e skipped and said so; the part saved; loaded and placed in the host BEHIND it; the part's window left open for the owner to close, and nothing lost when he does |
| **CS6** | [Row 5b-361](../fragment-issues/section-5b-rows-176-200.md): open a saved scratch family with `ACTIVATE_DOCUMENT` five times with the Heron Companion page closed, and five times with it open; read the add-in log after each | **RECORD how many opens lose their reply** (`unknown_outcome`, or a second run answering `wasAlready true`) and whether *"A newer connection took the session"* appears within seconds of each. Name the process that connects if it can be told |
| **CS7** | CS1 to CS3 on **Revit 2020** and **2027** | The same answers. All three compile on 2020, 2024 and 2027 (2026-10-07); the members they call read the same in the 2020, 2024 and 2027 reference XML; not run there |
| **CS8** | The second routes: the same part saved with Ctrl+S and closed with File > Close by hand | No "save changes?" question when closed after `SAVE_DOCUMENT` - the save took; `Manage > Load` of the same file into the host gives the same family and types |

**Scratch files left on the owner's PC:** `%TEMP%\heron-work\nest` holds the three families and Revit's
own backups (`.0001.rfa`, `.0002.rfa`). `HeronNest_Car` and `HeronNest_Tyre` were left open in session 9240;
both can be closed without saving and the folder deleted once CS1 to CS3 are signed.
