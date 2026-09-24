# What to install for Heron — and what is optional

> | | |
> |---|---|
> | **Type** | Permanent page, in plain words. **It copies no package list and no download size** — each line points to the file or command that owns it |
> | **For** | Anyone setting up a PC for Heron, and anyone asking *"why did Heron pick the wrong tool?"* |
> | **Authority** | None of its own. [`requirements.txt`](../requirements.txt) and [`requirements-optional.txt`](../requirements-optional.txt) are the lists, [Q-39](open-questions/answered.md) settled what a user must install, [07](07-installation-and-update.md) is how Heron is installed, and [`brain/retrieval-history.md`](../brain/retrieval-history.md) holds every measurement. Where this page disagrees with them, **they win** |
> | **Opened** | 2026-09-24, with the first measurement of the search add-ons on the owner's own questions |

> **Heron is not released yet.** No release has been published, and the [main README](../README.md) asks
> modellers not to install it until it is announced. This page is the list for that day — and for the
> owner's PC now.

## The short answer

| **Every PC needs** | **Optional** | **Only to build Heron yourself** |
|---|---|---|
| Revit · Claude Code · Python · two Python packages · Heron itself | the two search add-ons, and two more Python packages | Git · the .NET SDK |

**None of it needs administrator rights.** Everything installs for your own Windows user.

## 1. What every PC needs

| | What | Why Heron needs it | How to install it — for your user only | How to check it |
|---|---|---|---|---|
| 1 | **Revit** | Heron works inside it | You have it already. `heron-install --list` names the releases Heron finds on the PC | Revit opens |
| 2 | **Claude Code** | Where you type. Claude decides what you meant; Heron does the Revit work | From Anthropic. It is a program of its own and does not need Node ([Q-39](open-questions/answered.md)) | Open the Heron folder in it: `/mcp` lists `heron` |
| 3 | **Python** | Heron's brain, and its connection to Claude, are written in Python | `winget install Python.Python.3.12 --scope user`, or Python from the Microsoft Store | `python --version` in a new terminal. Claude Code starts Heron with the plain command `python`, so that command has to answer |
| 4 | **PyYAML** — Heron's only *required* Python package | Every Heron tool is described in a file Heron reads with it | In the Heron folder: `pip install --user -r requirements.txt` | `python tools/check-dependencies.py` |
| 5 | **The MCP package** (`mcp`) | It is the line between Claude Code and Heron | `pip install --user mcp` | the same command |
| 6 | **Heron itself** | The Heron tab in Revit | **Close Revit first**, then run `HeronInstaller.exe` from the Heron download | The Heron tab appears the next time Revit opens |

**Install both 4 and 5.** Installing `mcp` does not bring PyYAML with it (measured 2026-09-24), and
Heron's setup check looks only for `mcp` ([row 5b-201](FRAGMENT-ISSUES.md)). Without PyYAML, Heron
still talks to Revit, but every question to its knowledge answers *"needs PyYAML"* until it is
installed.

**Why `mcp` is on the optional list:** Heron's own tools and tests run without it. You need it the
moment you want to ask Heron something from Claude.

## 2. Optional — the two search add-ons

When you type *"select all ducts"*, Heron has to pick the right tool out of its library. **That pick is
the search.** Heron sends the best few to Claude and Claude chooses from those — so when the right tool
is not in that short list, Claude never sees it. **Heron works without either add-on:** the basic search
is built in, and every answer says which search ran.

| Level | What it does | Needs | What the first measurement says |
|---|---|---|---|
| **Basic** | Matches letters and words. It does not understand meaning, and its own code says so | Nothing — built in | It is what runs when nothing else is installed |
| **Trained search model** (`model2vec`) | Understands meaning: *"stop the air going the wrong way"* can reach the flow-direction check without sharing a word with it | A small download, once | **Finds more of your questions** — and sent more *"just asking"* questions to a tool that changes the model |
| **Re-ranker** (`sentence-transformers`) | A second, careful read of the top 20 picks, the question and each description together | The largest optional download in Heron | **Worse at picking tools.** Right on the one standards-document question measured |

**The measurement, 2026-09-24:** your 79 real questions, asked of all three levels at the same library
size. The trained model put the right tool in the **top three for 30** of 64 where basic search managed
25, and handed a model-changing tool to **9** questions that asked for no change, where basic search
handed one to 5. The re-ranker dropped **right-first from 19 to 12** and raised that 9 to **15**. Every
figure, and what was not measured, is in [`brain/retrieval-history.md`](../brain/retrieval-history.md)
under that date.

**Why a wrong pick is not a wrong edit:** with **Changes** off on the ribbon — the default — a tool that
changes the model cannot run. A wrong pick is a wrong suggestion.

**Are these "AI"?** Small trained helpers with **one job**: score how close your sentence is to each
tool's description. They cannot talk, decide, write code or touch Revit. **They run on your own PC and
send nothing anywhere.** Claude still decides what you meant; these only help the right tool reach it.

**To install the trained model:** `pip install --user model2vec`, then `python brain/heron_embed.py`
should answer `Backend: model`.

**The re-ranker — read before you agree.** `python brain/heron_rerank.py` says what it is and how big,
before anything downloads. The size it prints is a field reading, and a Linux machine downloaded more
than it says ([row 5b-200](FRAGMENT-ISSUES.md)); it has not been measured on Windows. If you still want
it: `pip install --user sentence-transformers`, then `python brain/heron_rerank.py --fetch --yes`.

**To take either one away:** `pip uninstall` the same package. Heron goes back to the level below and
says so on every answer.

## 3. Optional — two more Python packages

[`requirements-optional.txt`](../requirements-optional.txt) lists the rest — on the day this page was
written, a faster vector search and a PDF reader for standards documents. **Do not install that file in
one command:** it holds the re-ranker too. `python tools/check-dependencies.py` says what each one buys
and whether you have it.

## 4. Only to build Heron yourself

A modeller installing a release needs none of this.

| | What | Why |
|---|---|---|
| **Git** | To copy the source from GitHub | A released download needs no Git |
| **The .NET SDK — version 10, not 8** | To compile the Revit add-in for every release Heron supports | Revit 2027 runs on .NET 10, so only the 10 SDK builds every release — [30](30-compiling-away-from-windows.md) has the rest, and how on Linux |

A developer deploys the add-in with `tools/setup.ps1` rather than the installer, which also checks
Python and `mcp` first — [07](07-installation-and-update.md).

## 5. Check what you have

```bash
python tools/check-dependencies.py
```

It names every Python package Heron can use, whether this PC has it, what it buys, and what happens
without it. It fails only when a **required** one is missing.

## 6. Where everything lives on your PC

| | Where |
|---|---|
| **The Revit add-in** | `%AppData%\Autodesk\Revit\Addins\<release>\Heron` — per-user, no administrator rights |
| **Heron's settings and its knowledge store** | `%APPDATA%\Heron` |
| **Python packages** | Your own Python's user folder |
| **The search add-ons' model files** | `C:\Users\<you>\.cache\huggingface` |
| **The Heron folder** | The code and the tools. **None of the installs above** — so a Heron folder copied to another PC needs this page's list done again there |
| **GitHub** | The code and these lists. **Never** the model files, and never your settings |

After the first download, the search add-ons read their files from your own PC.

## 7. What is not decided yet

- **Nothing installs Python or its packages for you.** The installer puts Heron into Revit and installs
  no Python, and setup *tells* rather than installs — [Q-39](open-questions/answered.md)'s words:
  pulling a runtime onto somebody's machine unasked is what a careful user and a corporate laptop are
  both right to refuse. Whether a release should ever bundle Python is still open
  ([Q-38](open-questions/from-master-specification-part-2.md#q-38--what-is-the-exact-install-command-new)).
- **Whether the re-ranker should read standards documents only**, and not tool cards, is a code change
  nobody has made.
- **Why it misreads tools is not settled.** It is given a one-line description of each tool and not the
  tool's example phrases, and its model was trained on web searches. Neither has been tried another way.
