[Heron AI](../../README.md) › [Documentation](../README.md) › **System requirements**

# System requirements

**So you can judge before you start - the whole cost, the optional pieces included.** Every figure is a
measurement with its date and where it came from; none is an estimate. How to install each piece is
[What to install](../WHAT-TO-INSTALL.md).

| You need | |
|---|---|
| **Windows and Revit** | Each Revit release `platform/heron-products.json` lists for the add-in - 2020 onward |
| **Python 3.11 to 3.14** | For your own user. The range is `LOCK_OLDEST_MINOR` and `LOCK_NEWEST_MINOR` in [`tools/heron-ready.py`](../../tools/heron-ready.py) |
| **Claude Code** | Where you type |
| **No administrator rights** | Everything installs for your own Windows user |

**What it takes on disk.** Measured 2026-11-20 in a Linux container unless it says otherwise - the
Windows figures are **not yet measured**, and Windows wheels differ in size:

| Piece | Needed? | On disk | Where the figure comes from |
|---|---|---|---|
| Heron's folder - the brain, the MCP server, the documents | Yes | about 45 MB | the files `tools/build-release-assets.py` packs, unpacked |
| The add-in, for each Revit release | Yes | 6 to 8 MB a release, as downloaded | the `heron-bridge-<release>.zip` files on the v0.2.0 release page; unpacked not measured |
| PyYAML and `mcp`, with what `mcp` pulls in | Yes | about 17 MB | the installed packages, counted file by file |
| The knowledge store, built from Heron's own library | Yes | about 5 MB | `global.db` after `python brain/heron_scope.py --rebuild` |
| **Minimum, all of the above** | | **about 70 MB, plus 6 to 8 MB for each Revit release** | the four lines above |
| The download itself, while you keep it | - | 120 MB | `HeronSetup-0.2.0-unsigned.exe`, the v0.2.0 release page; each release page gives its own Setup's size |
| Reading PDFs, `pypdf` | No | about 2 MB | `heron_ingest.PDF_READER_SIZE`, said when a PDF cannot be read without it |

**Memory:** each open chat's Heron holds its own copy. The one reading, on the owner's laptop, was
taken with the re-ranker and the trained search model installed, and both were removed on 2026-11-26
([D-201](../decisions/D-201.md), [D-203](../decisions/D-203.md)) -
[WHAT-TO-INSTALL §2](../WHAT-TO-INSTALL.md#2-search--built-in-no-add-on).
`python tools/check-dependencies.py` says, on your own PC, which pieces are there, what each one is for,
and whether each installed version is the right one.
