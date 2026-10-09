# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Get a new PC ready for Heron before the first chat, and say what is still missing.

    python tools/heron-ready.py           check, then warm everything that can be warmed
    python tools/heron-ready.py --check   check only - build nothing, download nothing
    python tools/heron-ready.py --snapshot this-pc.json
                                          what this PC has, as facts to set beside
                                          another PC's - see write_snapshot()

Run it in the Heron folder with the plain command `python`: that is the command
Claude Code starts Heron with, so that is the one that has to work.

WHY IT EXISTS
-------------
On 2026-10-08 the owner put Heron on a new laptop. The first "create a family"
took more than seven minutes; on the office PC it takes one or two. Nothing in
Heron was slower. What the office PC had and the laptop did not:

  - a knowledge store already built, and the search model already downloaded -
    both were left for the first chat to do, while the modeller waited;
  - every package Heron's tools need - a missing one leaves Claude with no Heron
    tools, or with tools that answer "needs PyYAML", and Claude then works the
    job out by reading this folder's files;
  - Claude Code's own memory of Heron, and its approvals. Those stay on the old
    PC and cannot be copied from here; this says what to do about them.

So everything a first chat would otherwise do is done here, once, and every
gap that would send Claude reading files is named with the command that closes
it. Every PC that runs this starts from the same place.

WHAT IT CHANGES, AND WHAT IT NEVER DOES
---------------------------------------
It builds the knowledge store under %APPDATA%\\Heron, downloads the search
model once when `model2vec` is installed, and compiles Heron's Python. It
INSTALLS NOTHING - pulling a runtime onto a machine unasked is what a careful
user and a corporate laptop are both right to refuse (Q-39) - so a missing
package is named with its command. It never touches a Revit setting: Changes,
Admin and Publish are the owner's switches on the ribbon, and this only reads
them.

THE STORE IS BUILT ONLY WHERE A CHAT'S OWN START-UP WOULD BUILD IT - a private
store, an install with no git, or the main checkout on branch main
(heron_brain._store_warm_allowed, rows 131 and 136). From a branch or a
worktree it says so and leaves the shared store alone.

Exit 0 when everything Heron REQUIRES is here, 1 when something is missing.
An optional extra being absent is not a failure - Heron degrades and says so.
"""

import compileall
import glob
import importlib
import os
import shutil
import sys
import time
import warnings

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SERVER = os.path.join(ROOT, "mcp", "server")
CLIENT = os.path.join(ROOT, "mcp", "client")
BRAIN = os.path.join(ROOT, "brain")
for folder in (SERVER, CLIENT, BRAIN):
    if folder not in sys.path:
        sys.path.insert(0, folder)

OK, FIX, NOTE = "ok", "FIX", "--"

MISSING = []

#: Whether PyYAML imports - the knowledge store needs it and nothing else here.
HAVE_YAML = [False]


def say(mark, what, then=None):
    print("  %-4s %s" % (mark, what))
    if then:
        for line in then if isinstance(then, (list, tuple)) else [then]:
            print("         %s" % line)
    if mark == FIX:
        MISSING.append(what)


def head(title):
    print()
    print(title)


# ---------------------------------------------------------------------------
# Python and the packages
# ---------------------------------------------------------------------------

def python_itself():
    head("1. Python")
    say(OK, "Python %d.%d.%d at %s" % (sys.version_info[:3] + (sys.executable,)))
    plain = shutil.which("python")
    if plain is None:
        say(FIX, "the plain command `python` is not found",
            ["Claude Code starts Heron with `python`, so Heron cannot start.",
             "Install Python for your user: winget install Python.Python.3.12 "
             "--scope user, then open a NEW terminal."])
    elif "windowsapps" in plain.lower() and not _same_file(plain, sys.executable):
        say(FIX, "`python` is the Microsoft Store shortcut, not a Python (%s)"
            % plain,
            ["Claude Code would start that and Heron would never connect.",
             "Windows Settings > Apps > Advanced app settings > App execution "
             "aliases: turn OFF python.exe and python3.exe, then open a NEW "
             "terminal and run this again with `python`."])
    elif not _same_file(plain, sys.executable):
        say(NOTE, "`python` on PATH is %s, not the Python running this" % plain,
            "Run this again as `python tools/heron-ready.py` so it checks the "
            "Python Claude Code will use.")


def _same_file(a, b):
    try:
        return os.path.samefile(a, b)
    except OSError:
        return os.path.normcase(os.path.abspath(a)) == \
            os.path.normcase(os.path.abspath(b))


def packages():
    head("2. The Python packages Heron needs")
    try:
        import yaml                                     # noqa: F401
        HAVE_YAML[0] = True
        say(OK, "PyYAML - Heron reads every tool card with it")
    except ImportError:
        say(FIX, "PyYAML is missing",
            ["Without it every Heron lookup answers \"needs PyYAML\", and Claude",
             "reads the tool cards off disk instead. Install it:",
             "pip install --user -r requirements.txt"])

    sdk = None
    for module in ("mcp.server.mcpserver", "mcp.server.fastmcp"):
        try:
            importlib.import_module(module)
            sdk = module
            break
        except ImportError:
            continue
        except Exception as exc:                        # noqa: BLE001 - a broken install, named
            say(FIX, "the MCP package is installed but breaks on import - %s: %s"
                % (type(exc).__name__, exc),
                ["README.md records the known case. Repair it with:",
                 "pip install --user --upgrade mcp cryptography cffi"])
            return
    if sdk:
        say(OK, "the MCP package - the line between Claude Code and Heron (%s)"
            % sdk)
    else:
        say(FIX, "the MCP package is missing",
            ["Without it Heron cannot start, and Claude has no Heron tools at",
             "all. Install it: pip install --user mcp"])


def server_starts():
    head("3. Heron's MCP server")
    if MISSING:
        say(NOTE, "not tried - fix what is above first")
        return
    started = time.time()
    try:
        importlib.import_module("heron_mcp_server")
    except SystemExit as exc:
        say(FIX, "the server stopped while loading (exit %s)" % exc.code)
        return
    except Exception as exc:                            # noqa: BLE001 - every cause is named
        say(FIX, "the server does not load - %s: %s" % (type(exc).__name__, exc),
            "Run python tools/check-dependencies.py to see what is missing.")
        return
    say(OK, "it loads, with every tool registered, in %.1f s" % (time.time() - started))


# ---------------------------------------------------------------------------
# What a first chat would otherwise build while the modeller waits
# ---------------------------------------------------------------------------

def search_model(check_only):
    head("4. The search model")
    try:
        importlib.import_module("model2vec")
    except ImportError:
        say(NOTE, "not installed - Heron uses its basic search, which matches "
                  "words and not meaning",
            ["Optional. docs/WHAT-TO-INSTALL.md section 2 says what it buys.",
             "pip install --user model2vec, then run this again to download it."])
        return
    if check_only:
        say(NOTE, "model2vec is installed; --check does not load or download it")
        return
    import heron_embed as EMBED
    started = time.time()
    name, why = EMBED.backend()
    if name == EMBED.MODEL:
        say(OK, "loaded in %.1f s - %s" % (time.time() - started, EMBED.stamp()),
            "It is on this PC now, so no chat waits for a download.")
    else:
        say(NOTE, "model2vec is installed but the model did not load - %s" % why,
            "huggingface.co has to be reachable ONCE to fetch it. Heron works "
            "meanwhile with its basic search.")


def knowledge_store(check_only):
    head("5. Heron's knowledge store")
    if not HAVE_YAML[0]:
        say(NOTE, "not built - it needs PyYAML, above")
        return
    try:
        import heron_brain as BRAIN
        import heron_scope as SCOPE
    except Exception as exc:                            # noqa: BLE001 - say why, do not stop
        say(FIX, "the knowledge layer does not load - %s: %s"
            % (type(exc).__name__, exc))
        return
    where = SCOPE.knowledge_dir()
    if not where:
        say(FIX, "there is nowhere to keep it - no %APPDATA% and no HERON_KNOWLEDGE")
        return
    try:
        allowed = BRAIN._store_warm_allowed()
    except Exception as exc:                            # noqa: BLE001 - named, then left alone
        allowed = False
        say(NOTE, "could not tell whether this folder may build the shared "
                  "store - %s" % exc)
    if not allowed:
        say(NOTE, "left alone - this folder is a branch or a worktree, and the "
                  "store is shared by every chat on the PC",
            "Run this from the main Heron folder on branch main to build it.")
        return
    if check_only:
        say(NOTE, "kept in %s; --check does not build it" % where)
        return
    started = time.time()
    try:
        with BRAIN._Open() as store:
            count = store.count()
    except BRAIN.BrainUnavailable as why:
        say(FIX, "it could not be built", str(why).splitlines()[:4])
        return
    say(OK, "%d tool cards ready in %.1f s, in %s"
        % (count, time.time() - started, where),
        "A chat's first lookup finds it built instead of building it.")


def compiled(check_only):
    head("6. Heron's Python, compiled")
    if check_only:
        say(NOTE, "--check compiles nothing")
        return
    started = time.time()
    # A docstring's stray backslash is a SyntaxWarning printed mid-report, a
    # line a modeller cannot act on; Python prints the same once on first use.
    with warnings.catch_warnings():
        warnings.simplefilter("ignore", SyntaxWarning)
        good = all([compileall.compile_dir(folder, quiet=1, maxlevels=0)
                    for folder in (SERVER, CLIENT, BRAIN)])
    say(OK if good else NOTE,
        "compiled in %.1f s%s" % (time.time() - started,
                                  "" if good else " - some files did not compile"),
        None if good else "Not a fault for Heron: Python compiles them on use.")


# ---------------------------------------------------------------------------
# What this cannot change, only read or tell
# ---------------------------------------------------------------------------

def revit_side():
    head("7. Revit (read only - nothing here is changed)")
    appdata = os.environ.get("APPDATA")
    if not appdata:
        say(NOTE, "no %APPDATA% on this machine, so Revit is not here to check")
        return
    found = sorted(os.path.basename(os.path.dirname(p)) for p in glob.glob(
        os.path.join(appdata, "Autodesk", "Revit", "Addins", "*", "Heron.addin")))
    if found:
        say(OK, "the Heron add-in is installed for Revit %s" % ", ".join(found))
    else:
        say(FIX, "the Heron add-in is not installed for any Revit",
            "Close Revit, then run HeronInstaller.exe - or tools/setup.ps1 "
            "when building Heron yourself.")
    try:
        import heron_config as CONFIG
    except Exception:                                   # noqa: BLE001 - a report, not a gate
        return
    values = CONFIG.load()
    switches = [("Changes", "write.enabled", "any change to a model or family"),
                ("Admin", "admin.enabled", "a NEW family file"),
                ("Publish", "publish.enabled", "saving the family")]
    for label, key, needed in switches:
        on = CONFIG.truthy(values, key)
        say(NOTE, "%-8s is %-3s - needed for %s" % (label, "ON" if on else "off",
                                                     needed))
    print("         These are your switches on the Heron ribbon tab. They start "
          "off on a")
    print("         new PC; turn on what the job needs before you ask - a refusal "
          "costs a turn.")


def claude_side():
    head("8. Claude Code (it keeps these itself - check them once)")
    for line in (
            "Open Claude Code IN THIS FOLDER, answer YES to trusting it, and "
            "approve the heron server",
            "  when it asks - the old PC remembers both answers, a new one "
            "asks once.",
            "Type /mcp - `heron` must say connected. If it does not, Claude has "
            "no Heron tools,",
            "  and it will try to do the job by reading files: stop it and fix "
            "that first.",
            "Claude Code's memory of the old PC does not come across. Heron "
            "hands Claude a job's",
            "  whole method with heron_method, so a new PC no longer needs it."):
        print("  %-4s %s" % (NOTE if not line.startswith("  ") else "", line))


# ---------------------------------------------------------------------------
# --snapshot: what this PC has, as facts another PC's can be set beside
# ---------------------------------------------------------------------------

#: The packages whose presence or version can change how fast Heron answers.
SNAPSHOT_PACKAGES = ("PyYAML", "mcp", "model2vec", "sentence-transformers",
                     "torch", "numpy", "sqlite-vec", "pypdf", "huggingface_hub")

#: The only keys ever copied out of a Claude Code settings file.
SETTINGS_KEYS = ("enableAllProjectMcpServers", "enabledMcpjsonServers",
                 "disabledMcpjsonServers", "autoMemoryEnabled")

#: The only keys ever copied out of a project's entry in .claude.json.
PROJECT_KEYS = ("hasTrustDialogAccepted", "enabledMcpjsonServers",
                "disabledMcpjsonServers")


def _version(name):
    try:
        from importlib import metadata
        return metadata.version(name)
    except Exception:                                   # noqa: BLE001 - absent is an answer
        return None


def _files(folder, pattern="*"):
    """[{name, bytes, modified}] - sizes and dates, never contents."""
    found = []
    for path in sorted(glob.glob(os.path.join(folder, pattern))):
        try:
            stat = os.stat(path)
        except OSError:
            continue
        found.append({"name": os.path.basename(path), "bytes": stat.st_size,
                      "modified": time.strftime("%Y-%m-%d %H:%M",
                                                time.localtime(stat.st_mtime))})
    return found


def _tree_bytes(folder):
    total = 0
    for here, _dirs, names in os.walk(folder):
        for name in names:
            try:
                total += os.path.getsize(os.path.join(here, name))
            except OSError:
                continue
    return total


def _json_file(path):
    import json
    try:
        with open(path, "rb") as handle:
            return json.loads(handle.read().decode("utf-8"))
    except (OSError, ValueError):
        return None


def _settings_facts(path):
    """What a Claude Code settings file says about Heron - its approvals and
    the Heron tools it lets run unasked. Nothing else is copied out."""
    data = _json_file(path)
    if not isinstance(data, dict):
        return {"exists": os.path.exists(path)}
    allow = ((data.get("permissions") or {}).get("allow") or [])
    facts = {"exists": True,
             "heron_allow": sorted(str(a) for a in allow if "heron" in str(a).lower())}
    for key in SETTINGS_KEYS:
        if key in data:
            facts[key] = data[key]
    return facts


def _claude_side_facts():
    """Claude Code's own state for Heron: its version, approvals, and how much
    it REMEMBERS - counted in lines and files, never read out."""
    configured = os.environ.get("CLAUDE_CONFIG_DIR")
    home = configured or os.path.join(os.path.expanduser("~"), ".claude")
    state = (os.path.join(configured, ".claude.json") if configured
             else os.path.join(os.path.expanduser("~"), ".claude.json"))
    facts = {"config_folder": home, "version": None}
    try:
        import subprocess
        done = subprocess.run(["claude", "--version"], stdout=subprocess.PIPE,
                              stderr=subprocess.DEVNULL, timeout=15)
        facts["version"] = done.stdout.decode("utf-8", "replace").strip() or None
    except Exception:                                   # noqa: BLE001 - not on PATH is an answer
        pass
    facts["user_settings"] = _settings_facts(os.path.join(home, "settings.json"))
    facts["folder_settings_local"] = _settings_facts(
        os.path.join(ROOT, ".claude", "settings.local.json"))

    remembered = []
    for folder in sorted(glob.glob(os.path.join(home, "projects", "*"))):
        if "heron" not in os.path.basename(folder).lower():
            continue
        memory = os.path.join(folder, "memory", "MEMORY.md")
        lines = None
        if os.path.exists(memory):
            with open(memory, "rb") as handle:
                lines = handle.read().count(b"\n")
        remembered.append({
            "project": os.path.basename(folder),
            "memory_md_lines": lines,
            "memory_topic_files": len([n for n in glob.glob(
                os.path.join(folder, "memory", "*.md"))
                if os.path.basename(n) != "MEMORY.md"]),
            "chats_kept": len(glob.glob(os.path.join(folder, "*.jsonl")))})
    facts["projects"] = remembered

    approved = []
    data = _json_file(state)
    for path, entry in sorted(((data or {}).get("projects") or {}).items()):
        if "heron" not in str(path).lower() or not isinstance(entry, dict):
            continue
        kept = {"path": path}
        for key in PROJECT_KEYS:
            if key in entry:
                kept[key] = entry[key]
        kept["heron_allowed_tools"] = sorted(
            str(t) for t in (entry.get("allowedTools") or [])
            if "heron" in str(t).lower())
        approved.append(kept)
    facts["approvals"] = approved
    return facts


def _lookup_seconds():
    """Three lookups of one request, timed - the first may build the store.
    None where the store is shared and this folder may not build it."""
    import heron_brain as BRAIN
    try:
        if not BRAIN._store_warm_allowed():
            return None
    except Exception:                                   # noqa: BLE001 - not timed, and said so
        return None
    seconds = []
    for _ in range(3):
        started = time.time()
        BRAIN.lookup("create a family")
        seconds.append(round(time.time() - started, 3))
    return seconds


def write_snapshot(path):
    """Write what this PC has - facts only - to `path` as JSON.

    FOR SETTING TWO PCs SIDE BY SIDE. The owner offered to send the folders
    from his fast office PC; several hold his Claude login, and the memory
    files hold his own words. This writes NAMES, SIZES, VERSIONS and yes/no
    instead - run it on both PCs and the two files say what differs. It reads
    no file's contents except the few settings keys named above, and the
    line count of Claude's memory index.
    """
    import json
    print("Heron - writing what this PC has to %s" % path)
    appdata = os.environ.get("APPDATA")
    hub = os.path.join(os.environ.get("HF_HOME") or os.path.join(
        os.path.expanduser("~"), ".cache", "huggingface"), "hub")
    facts = {
        "what_this_is": "heron-ready --snapshot: names, sizes, versions and "
                        "yes/no only. No file contents, no keys, no tokens.",
        "written": time.strftime("%Y-%m-%d %H:%M:%S"),
        "heron_folder": {
            "path": ROOT,
            "under_onedrive": "onedrive" in ROOT.lower(),
            "git_head": None,
        },
        "python": {
            "version": "%d.%d.%d" % sys.version_info[:3],
            "executable": sys.executable,
            "plain_python": shutil.which("python"),
        },
        "packages": dict((name, _version(name)) for name in SNAPSHOT_PACKAGES),
        "search_model": [{"name": os.path.basename(m), "bytes": _tree_bytes(m)}
                         for m in sorted(glob.glob(os.path.join(hub, "models--*")))],
        "knowledge_store": None,
        "revit": {"addin_releases": [], "switches": {}},
        "claude_code": _claude_side_facts(),
    }
    head = os.path.join(ROOT, ".git", "HEAD")
    if os.path.isfile(head):
        with open(head) as handle:
            facts["heron_folder"]["git_head"] = handle.read().strip()
    try:
        import heron_scope as SCOPE
        where = SCOPE.knowledge_dir()
        facts["knowledge_store"] = {"folder": where,
                                    "files": _files(where) if where else []}
    except Exception as exc:                            # noqa: BLE001 - recorded as the fact
        facts["knowledge_store"] = {"error": "%s: %s" % (type(exc).__name__, exc)}
    if appdata:
        facts["revit"]["addin_releases"] = sorted(
            os.path.basename(os.path.dirname(p)) for p in glob.glob(os.path.join(
                appdata, "Autodesk", "Revit", "Addins", "*", "Heron.addin")))
    try:
        import heron_config as CONFIG
        values = CONFIG.load()
        for key in ("write.enabled", "admin.enabled", "publish.enabled",
                    "bridge.autoConnect", "fragments.warmUp"):
            facts["revit"]["switches"][key] = CONFIG.truthy(values, key)
    except Exception:                                   # noqa: BLE001 - a report, not a gate
        pass
    try:
        facts["lookup_seconds"] = _lookup_seconds()
    except Exception as exc:                            # noqa: BLE001 - recorded as the fact
        facts["lookup_seconds"] = "%s: %s" % (type(exc).__name__, exc)

    with open(path, "w") as handle:
        json.dump(facts, handle, indent=2, sort_keys=True, default=str)
    print("Written. Open it and read it before you send it to anyone: it holds")
    print("folder names and versions, and nothing from inside your files.")
    return 0


def main(argv):
    check_only = "--check" in argv
    if "-h" in argv or "--help" in argv:
        print(__doc__)
        return 0
    if "--snapshot" in argv:
        at = argv.index("--snapshot")
        if at + 1 >= len(argv):
            print("--snapshot needs a file name: python tools/heron-ready.py "
                  "--snapshot this-pc.json")
            return 2
        return write_snapshot(argv[at + 1])
    started = time.time()
    print("Heron - getting this PC ready%s" % (" (check only)" if check_only else ""))
    python_itself()
    packages()
    server_starts()
    search_model(check_only)
    knowledge_store(check_only)
    compiled(check_only)
    revit_side()
    claude_side()
    print()
    if MISSING:
        print("NOT READY - %d thing(s) to fix, each with its command above:"
              % len(MISSING))
        for what in MISSING:
            print("  - %s" % what)
        return 1
    print("READY in %.1f s. Everything Heron needs is here%s." % (
        time.time() - started,
        "" if check_only else ", and what a first chat would build is built"))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
