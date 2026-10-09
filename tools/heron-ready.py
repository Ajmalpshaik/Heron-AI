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


def main(argv):
    check_only = "--check" in argv
    if "-h" in argv or "--help" in argv:
        print(__doc__)
        return 0
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
