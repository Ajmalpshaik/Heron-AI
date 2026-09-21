# 38 — The cloud environment

> **Status:** set up and measured 2026-09-22. The four field values below are what is configured;
> everything stated about what runs there is measured on this repository, or taken from a file in it
> that recorded the measurement.

A Claude Code cloud session runs on **Ubuntu 24.04 in Anthropic's datacentre**, from a fresh clone of
this repository. It is not the owner's PC and cannot reach it. That makes it the right place for most
of Heron and the wrong place for the rest, and the line between them is not where people guess it is.

**The cloud is not a weaker machine. It is a machine with no Revit.** Every one of this repository's
CI gates already runs on `ubuntu-latest`, including the C# compiling for Revit 2020 through 2027
([30](30-compiling-away-from-windows.md)), so *"it needs Windows"* is true of far less of this project
than the registers assumed for months.

---

## 1. The four fields

Set in **Add cloud environment**, in the Claude desktop app or at claude.ai/code.

### Name

    Heron

### Network access — **Custom**, with the default list still ticked

Tick **"Also include default list of common package managers"**, then add:

    huggingface.co
    *.huggingface.co
    hf.co
    *.hf.co

### Environment variables

    HERON_KNOWLEDGE=/opt/heron-kb
    HERON_CLIENT_ID=ajmal-pc
    DOTNET_CLI_TELEMETRY_OPTOUT=1
    DOTNET_NOLOGO=1
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
    PYTHONUTF8=1
    PYTHONIOENCODING=utf-8

`HERON_KNOWLEDGE` is the one that is not optional. `heron_scope.knowledge_dir()` reads `%APPDATA%`
first and returns `None` on Linux without it, and every brain call then dies with *"No %APPDATA% and
no HERON_KNOWLEDGE, so there is nowhere to keep knowledge"*.
[`.github/workflows/gates.yml`](../.github/workflows/gates.yml) does the same thing for the same
reason, so this is the supported path rather than a workaround.

`/opt` rather than `$HOME`, because the setup script runs as root and the session may not.

`HERON_CLIENT_ID` matches [`.mcp.json`](../.mcp.json), so a command typed at a shell carries the same
chat id as the MCP server. No Revit is reachable from a cloud session, so it can never compete for a
lease — it is set so that a command copied out of a session prompt is correct wherever it is run.

### Setup script

[`tools/cloud-setup.sh`](../tools/cloud-setup.sh), pasted whole into the box. Section 3 says what it
does and, as importantly, what it refuses to do.

---

## 2. Why **Custom** and not **Trusted**

**Trusted already covers everything else.** `archive.ubuntu.com` and `security.ubuntu.com` for the
.NET SDK, `api.nuget.org` for the Revit API reference packages, `pypi.org`, `github.com`,
`packages.microsoft.com`. Nothing in section 3 needs a domain outside that list —

**except `huggingface.co`, which is not on it at all.**

That single absence is the difference between Heron understanding a question and matching its letters.
`pip install model2vec` succeeds — pypi.org answers 200 — and then `StaticModel.from_pretrained` has
to reach `huggingface.co` for the weights. [`brain/heron_embed.py`](../brain/heron_embed.py) has this
written down twice already, as a 403 recorded 2026-08-28 and re-checked 2026-09-11, and its fallback
reports itself as *"built-in character n-grams — tolerant of spelling and word order, but **NOT
meaning**"*.

**Four lines, not one, and the extra three are the point.** Hugging Face serves metadata from
`huggingface.co` and the weight files themselves from separate storage hosts under `hf.co`. One line
would have looked correct and failed at the download.

**The tick box is load-bearing.** Custom *without* "Also include default list" allows **only** what is
typed, so PyPI, NuGet and the Ubuntu archive would all be gone and nothing would install at all.

**Nothing here costs money and nothing needs an account.** `minishlab/potion-base-8M` is public and
ungated: measured on the owner's PC 2026-09-22, 59 MB in the Hugging Face cache with `HF_TOKEN` unset
and no stored login file. Trusted alone still works — it costs retrieval quality, and every answer
says so.

---

## 3. What the setup script does, and the three things it will not do

In order: the .NET 10 SDK and `python-is-python3` from apt; PyYAML, `mcp`, `model2vec`, `sqlite-vec`
and `pypdf` from pip; the knowledge directory; then a fragment index rebuild and a NuGet restore, run
in parallel. Every step is bounded with `timeout`, and every failure is swallowed into a named log
rather than raised.

**`python-is-python3` is not housekeeping.** [`.mcp.json`](../.mcp.json) runs `python`, and Ubuntu
24.04 ships `python3` only. Without it the Heron MCP server never starts, and nothing says why.

**It does not install `sentence-transformers`.** That pulls torch — 500 MB to 2 GB — to re-order a
shortlist it already has. [`requirements-optional.txt`](../requirements-optional.txt) gives the same
reasoning in the same words.

**It does not build the bridge test host**, although the host builds and runs here perfectly well —
32 checks, [30 §5](30-compiling-away-from-windows.md). The environment is snapshotted after the script
finishes, and a test host inside a snapshot is a **stale** test host, which reads exactly like a bridge
defect. Build it in-session instead, fresh:

```bash
dotnet build tests/Heron.Bridge.TestHost -p:RevitVersion=2024 \
  -p:HeronTfm=net8.0 -p:OutputPath=bin/x64/Debug-net8.0/
python tests/test_bridge_roundtrip.py
```

**It does not exit non-zero, ever.** A setup script that exits non-zero fails the session outright.

### The rebuild is warm, not stale

`python brain/heron_scope.py --rebuild` — measured 2026-09-22 at **19 s for 395 fragments** — is in the
script so that the first `heron_lookup` of a session answers, instead of reporting *"the knowledge
store is EMPTY."* It is safe to snapshot because `heron_search.library_digest()` hashes the fragment
files **as bytes, not by mtime**, for the reason that module states outright: *a git checkout moves
every file's mtime without changing a character.* A fresh clone therefore does not invalidate it, and a
real edit does.

---

## 4. What a cloud session can and cannot do

| | |
|---|---|
| All nine CI gates | yes |
| The C# compiles, Revit 2020–2027 | yes — `python tools/check-compile.py` |
| Every fragment's C#, against every release it claims | yes |
| Every test suite but the three below | yes |
| `test_bridge_roundtrip` — 32 checks | yes, after the build in §3 |
| `test_mcp_serves`, `test_served_claims` | yes — the script installs `mcp` |
| The brain: lookup, gaps, routing, retrieval | yes |
| **Proving a fragment or skill against a model** | **no. Needs Revit** |
| **Add-in load, upgrade, rollback, manifest discovery** | **no. Needs a real Windows install** |

So a cloud session goes **further than CI does**. `gates.yml` leaves exactly three suites unrun because
the MCP SDK and the .NET host drag in dependencies a hosted runner should not carry; this environment
installs one and builds the other on request, so **nothing in `tests/` is out of its reach**.

No total is written here on purpose. One was — *"196 of 199"* — and
[`check-docs.py`](../tools/check-docs.py) caught it on this page's first run: the source said **219**.
A count typed into a sentence is a count that goes stale, so derive it:

```bash
ls tests/test_*.py | wc -l
```

**The trap worth naming.** In a cloud session the `revit_*` MCP tools are still listed —
`revit_sheets`, `revit_rooms` and the rest. They are there because the MCP server is there. There is no
Revit behind them, so every call reports no session. **That is correct, and it looks like a fault the
first time it is seen.**

---

## 5. The machine, and the three constraints it imposes

4 vCPU, 16 GB RAM, 30 GB disk. Ubuntu 24.04 on x86-64. The setup script runs **as root**, before Claude
Code launches.

- **It must exit 0.** A non-zero exit fails the session, so every non-critical step is guarded.
- **It must finish in about five minutes**, or the snapshot is not built and every later session pays
  the install again. The two slow halves run in parallel with `&` and `wait` for that reason alone.
- **The filesystem is snapshotted; running processes are not.** Packages installed and files written
  carry over. Anything merely running does not. The script re-runs when its text changes, when the
  allowed domains change, or after roughly seven days.

---

## 6. What this does not prove

**It proves nothing about behaviour, on any release.** That is the sentence
[30 §3](30-compiling-away-from-windows.md) ends on and it has not weakened: code that compiles on eight
releases can still move a duct 200 feet instead of 200 millimetres. `D3` in
[`NEEDS-CHECKING.md`](NEEDS-CHECKING.md) — *move them, then measure one* — is what catches that, and no
environment substitutes for it.

**So the work a cloud session hands back is always the same shape**: it builds, it is gated, it is
tested, and the question left over is whether it does the right thing in a model. That question is
answered at the owner's PC, and `python tools/owner-queue.py` is the list of them.
