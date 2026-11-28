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
It builds the knowledge store under Heron's home (D-135) and compiles Heron's
Python. It
INSTALLS NOTHING - pulling a runtime onto a machine unasked is what a careful
user and a corporate laptop are both right to refuse (Q-39) - so a missing
package is named with its command. It never touches a Revit setting: Changes,
Admin and Publish are the owner's switches in Heron's Settings window, held in
Revit's memory and off whenever Revit starts (D-166), so this cannot read them
and says so; Ask to Keep it only reads from heron.config.

THE STORE IS BUILT ONLY WHERE A CHAT'S OWN START-UP WOULD BUILD IT - a private
store, an install with no git, or the main checkout on branch main
(heron_brain._store_warm_allowed, rows 131 and 136). From a branch or a
worktree it says so and leaves the shared store alone - and so does an install
on a PC whose developer note names a git checkout (D-167, amended 2026-11-16).
Where it may build it and the store does not hold this folder's cards - a card
with no row, a row with no card - it REBUILDS it, as `brain/heron_scope.py
--rebuild` does, and compares again (row 5b-1711): opening a store adds no card,
so "ready" was said over a store every lookup then warned about.

A MICROSOFT STORE CLAUDE KEEPS ITS OWN COPY OF APPDATA (row 5b-417). When the
Claude app is the Store (MSIX) build, Windows sends every write Claude's
children make under %APPDATA% and %LOCALAPPDATA% - Claude's own shell, and
Heron's server - into the app's LocalCache, and shows only them the two laid
over each other. A package Claude installed is then invisible to the person's
own terminal and to Revit: on the owner's laptop, 2026-10-10, this said READY
from Claude's shell and NOT READY from his terminal. Section 2a says so, with a
pip line pinned to the versions in the copy. It only WARNS: no exit code moves.

Exit 0 when everything Heron REQUIRES is here, 1 when something is missing.
An optional extra being absent is not a failure - Heron degrades and says so.
"""

import compileall
import glob
import importlib
import importlib.util
import os
import re
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

import heron_install as INSTALL                         # noqa: E402 - brain/, os alone

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


#: The line a person types on Windows for any of Heron's packages: every one
#: pinned and hashed, the file HeronInstaller.exe installs from - the line
#: requirements-lock.txt names at its own head. brain/heron_install.py owns it.
LOCK_LINE = INSTALL.LOCK_LINE

#: The Pythons the lock installs on: rpds-py, which jsonschema needs, starts at
#: 3.11, and 3.14 is the newest its Windows wheels were downloaded for - the
#: lock's header, and PythonSetup.OldestMinor and NewestMinor.
LOCK_OLDEST_MINOR = 11
LOCK_NEWEST_MINOR = 14


def install_lines(job, windows=None, minor=None):
    """The lines a section 2 FIX ends with, for job "yaml", "mcp" or "repair".

    ON WINDOWS EVERY ONE IS THE LOCK. Unpinned, `pip install mcp` pulled a
    cryptography that raised on import on 2026-09-16 (README.md), and the
    --upgrade repair went the same unpinned way. The lock puts back the
    versions measured to work: pip replaces any installed version that is not
    the pinned one. OFF WINDOWS THE LINES STAY UNPINNED, because the lock
    cannot install there at all: it pins pywin32, which has no wheel but
    Windows', and pip refuses the whole file for that one line - measured on
    Linux on 2026-10-17, "No matching distribution found for pywin32==312".
    """
    windows = (os.name == "nt") if windows is None else windows
    minor = sys.version_info[1] if minor is None else minor
    if job == "yaml":
        line = INSTALL.install_line("pip install --user -r requirements.txt", windows)
    elif job == "mcp":
        line = INSTALL.install_line("pip install --user mcp", windows)
    else:
        line = INSTALL.install_line("pip install --user --upgrade mcp cryptography cffi",
                                    windows)
    if windows and not LOCK_OLDEST_MINOR <= minor <= LOCK_NEWEST_MINOR:
        return ["The pinned packages are for Python 3.%d to 3.%d - install one with"
                % (LOCK_OLDEST_MINOR, LOCK_NEWEST_MINOR),
                "winget install Python.Python.3.12 --scope user, then in a NEW "
                "terminal, in the Heron folder:",
                line]
    return [line]


def packages(windows=None, minor=None):
    head("2. The Python packages Heron needs")
    try:
        import yaml                                     # noqa: F401
        HAVE_YAML[0] = True
        say(OK, "PyYAML - Heron reads every tool card with it")
    except ImportError:
        say(FIX, "PyYAML is missing",
            ["Without it every Heron lookup answers \"needs PyYAML\", and Claude",
             "reads the tool cards off disk instead. Install it:"]
            + install_lines("yaml", windows, minor))

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
                ["README.md records the known case. Repair it with:"]
                + install_lines("repair", windows, minor))
            return
    if sdk:
        say(OK, "the MCP package - the line between Claude Code and Heron (%s)"
            % sdk)
    else:
        say(FIX, "the MCP package is missing",
            ["Without it Heron cannot start, and Claude has no Heron tools at",
             "all. Install it:"] + install_lines("mcp", windows, minor))


# ---------------------------------------------------------------------------
# A Microsoft Store Claude and its private copy of AppData (row 5b-417)
# ---------------------------------------------------------------------------

#: The Claude desktop app's folder under %LOCALAPPDATA%\Packages when it is the
#: Microsoft Store (MSIX) build; the letters after the underscore are Windows'.
CLAUDE_PACKAGES = "AnthropicPBC.Claude_*"


def _key(name):
    """A package name the way pip compares them: case, '-', '_' and '.' alike."""
    return re.sub(r"[-_.]+", "-", name).lower()


def claude_app_copies(environ=None):
    """The LocalCache folder of every Store-built Claude app for this user -
    where Windows keeps what Claude's children wrote under AppData."""
    environ = os.environ if environ is None else environ
    local = environ.get("LOCALAPPDATA")
    if not local:
        return []
    return sorted(p for p in glob.glob(os.path.join(
        local, "Packages", CLAUDE_PACKAGES, "LocalCache")) if os.path.isdir(p))


def copy_packages(cache, wanted=None):
    """{name: version} of Heron's packages pip put in the copy for this Python
    (Python312 for 3.12), read off the .dist-info folder names - nothing runs."""
    keys = dict((_key(name), name) for name in (wanted or SNAPSHOT_PACKAGES))
    site = os.path.join(cache, "Roaming", "Python",
                        "Python%d%d" % sys.version_info[:2], "site-packages")
    found = {}
    for path in glob.glob(os.path.join(site, "*.dist-info")):
        name, _, version = os.path.basename(path)[:-len(".dist-info")].partition("-")
        if version and _key(name) in keys:
            found[keys[_key(name)]] = version
    return found


def inside_claude_app(cache, environ=None):
    """True when this run's %APPDATA% and %LOCALAPPDATA% ARE the app's copy.

    Read-only. Inside the app a folder in the copy and the same name under
    %APPDATA% open as ONE folder on disk; outside it they are two folders, or
    the second is not there. A packaged app's child has no package identity
    to ask for - GetCurrentPackageFullName answered 15700, no package, from
    inside Claude on the owner's laptop on 2026-10-10 - so this is the test.
    """
    environ = os.environ if environ is None else environ
    for side, variable in (("Roaming", "APPDATA"), ("Local", "LOCALAPPDATA")):
        real, mine = environ.get(variable), os.path.join(cache, side)
        if not real or not os.path.isdir(mine):
            continue
        for name in os.listdir(mine):
            try:
                if os.path.samefile(os.path.join(real, name),
                                    os.path.join(mine, name)):
                    return True
            except OSError:
                continue
    return False


def claude_app():
    """2a - said only on a PC whose Claude app is the Microsoft Store build.
    Every line is a NOTE or an ok: it warns, and moves no exit code."""
    copies = claude_app_copies()
    if not copies:
        return
    head("2a. The Claude app keeps its own copy of AppData (Microsoft Store build)")
    for cache in copies:
        held = copy_packages(cache)
        if inside_claude_app(cache):
            say(NOTE, "this run is INSIDE the Claude app, and sees its private "
                      "copy laid over your AppData",
                ["So READY here says nothing about your own terminal or about "
                 "Revit.",
                 "Run this again from your own terminal, opened outside Claude.",
                 "Never install packages from inside Claude: they land only in "
                 "its copy."])
            pinned = held
        else:
            say(NOTE, "the Claude app on this PC keeps its own copy of AppData, "
                      "in %s" % cache,
                ["Packages installed from inside Claude are only there - this "
                 "terminal and",
                 "Revit cannot see them. What you install here, Claude sees "
                 "too."])
            pinned = dict((name, version) for name, version in held.items()
                          if _version(name) != version)
            if held and not pinned:
                say(OK, "this Python has the same versions of Heron's packages "
                        "as Claude's copy")
        if pinned:
            say(NOTE, "install from your own terminal, pinned to the versions in "
                      "Claude's copy",
                ["A different version leaves two copies of one package mixed in",
                 "Claude's view. Use this in place of any pip line above:",
                 "pip install --user " + " ".join(
                     "%s==%s" % item for item in sorted(
                         pinned.items(), key=lambda item: item[0].lower())),
                 "Leave out any you do not want - only a DIFFERENT version is a",
                 "problem."])
        say(NOTE, "a Reset or uninstall of the Claude app deletes that copy",
            ["The packages Claude installed go with it. Heron's knowledge store,",
             "the projects' memory and the brain's audit no longer live there",
             "(D-135): they are in %s - docs/WHAT-TO-INSTALL.md section 1b."
             % (_heron_home() or "Heron's home folder")])


def _heron_home():
    """Where Heron's Python side keeps its data on this PC (D-135), or None.
    Only a name for a NOTE: data_home() carries nothing."""
    try:
        sys.path.insert(0, os.path.join(ROOT, "brain"))
        import heron_paths
        return heron_paths.data_home()
    except Exception:                               # noqa: BLE001 - a NOTE's wording only
        return None


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
    """Heron's search, and a leftover trained model the owner's PC may hold.

    ONE SEARCH SINCE D-203, built in, the same on every PC: nothing to load or
    download. A PC that installed model2vec before it was removed still has it
    on disk, and nothing in Heron uses it - so it is NAMED, with the command
    that frees the space, and never imported: asking whether it is there is
    find_spec's job (importing it is what needed PyYAML first, row 5b-1008,
    and is no business of this tool any more).
    """
    head("4. Search")
    say(OK, "built in - matches spelling and word order, not meaning; the same "
            "search on every PC (D-203)")
    leftover = [name for name in ("model2vec",)
                if importlib.util.find_spec(name) is not None]
    if leftover:
        say(NOTE, "%s is installed and nothing in Heron uses it any more"
                  % ", ".join(leftover),
            "Optional, to free the space: pip uninstall -y model2vec")


def knowledge_store(check_only):
    head("5. Heron's knowledge store")
    if not HAVE_YAML[0]:
        say(NOTE, "not built - it needs PyYAML, above")
        return
    # HOW VECTORS ARE COMPARED, said as the encoder is (PROPOSALS F7): always
    # in Python today, installed or not - row 5b-1171.
    import heron_embed as EMBED
    say(NOTE, "search: " + EMBED.comparison()[1])
    try:
        import heron_brain as BRAIN
        import heron_scope as SCOPE
    except Exception as exc:                            # noqa: BLE001 - say why, do not stop
        say(FIX, "the knowledge layer does not load - %s: %s"
            % (type(exc).__name__, exc))
        return
    where = SCOPE.knowledge_dir()
    if not where:
        say(FIX, "there is nowhere to keep it - no HERON_DATA, no Windows profile and no HERON_KNOWLEDGE")
        return
    try:
        allowed = BRAIN._store_warm_allowed()
    except Exception as exc:                            # noqa: BLE001 - named, then left alone
        allowed = False
        say(NOTE, "could not tell whether this folder may build the shared "
                  "store - %s" % exc)
    if not allowed:
        # AN INSTALLED HERON ON A DEVELOPER'S PC is neither a branch nor a
        # worktree, and is told so in its own words (D-167, amended 2026-11-16).
        developer = None
        try:
            developer = SCOPE.steps_aside_for()
        except Exception:                               # noqa: BLE001 - the general sentence then
            developer = None
        if developer:
            say(NOTE, "left alone - this PC has a Heron developer copy at %s, and "
                      "the store every chat reads follows that copy" % developer,
                "Nothing to do: a chat opened in that folder keeps it current.")
            return
        say(NOTE, "left alone - this folder is a branch or a worktree, and the "
                  "store is shared by every chat on the PC",
            "Run this from the main Heron folder on branch main to build it.")
        return
    if check_only:
        say(NOTE, "kept in %s; --check does not build it" % where)
        return
    # D-135: Heron's data into its home, once on this PC, before the store is
    # built there - from outside Claude this also reaches the files a Store
    # Claude's copy hides from the chats. Copied, never moved.
    home = BRAIN.carry_home()
    if home:
        say(NOTE, "Heron keeps its data in %s" % home,
            "What the old places held was copied there once; nothing was deleted.")
    started = time.time()
    try:
        with BRAIN._Open() as store:
            count = store.count()
            missing, extra = store_drift(SCOPE, store)
    except BRAIN.BrainUnavailable as why:
        say(FIX, "it could not be built", str(why).splitlines()[:4])
        return
    if missing or extra:
        # ROW 5b-1711, issue #853: a store that does not hold this folder's
        # cards warned on every lookup of a whole session, and this step said
        # "ready" over it - opening a store adds no card and drops no row.
        # This folder may build it (asked above), so it is rebuilt here, as
        # `brain/heron_scope.py --rebuild` does, and compared again.
        drifted = drift_words(missing, extra)
        code, lines = rebuilt(SCOPE)
        if code != 0:
            say(FIX, "it does not match this folder's cards (%s), and the rebuild "
                "did not run" % drifted, lines[:4])
            return
        try:
            with BRAIN._Open() as store:
                count = store.count()
                missing, extra = store_drift(SCOPE, store)
        except BRAIN.BrainUnavailable as why:
            say(FIX, "it was rebuilt and then could not be opened", str(why).splitlines()[:4])
            return
        skipped = [line for line in lines if line.startswith("skipped:")]
        if missing or extra:
            say(FIX, "rebuilt, and it STILL does not match this folder's cards (%s)"
                % drift_words(missing, extra),
                skipped[:4] or "A card that does not validate is never given a row: "
                "python brain/heron_validate.py names why.")
            return
        say(OK, "%d tool cards ready in %.1f s, in %s - rebuilt, because %s"
            % (count, time.time() - started, where, drifted),
            "Every lookup finds this folder's cards now, and no longer warns.")
        return
    say(OK, "%d tool cards ready in %.1f s, in %s"
        % (count, time.time() - started, where),
        "A chat's first lookup finds it built instead of building it.")


def store_drift(SCOPE, store):
    """(missing, extra) card folders between STORE and this folder's cards -
    heron_scope.drift - or two empty lists when the comparison cannot be made:
    a store that cannot be compared is never rebuilt on a guess."""
    try:
        return SCOPE.drift(store)
    except Exception:                                   # noqa: BLE001 - compared or left alone
        return [], []


def drift_words(missing, extra):
    """The drift said in a few words: how many of each, a few named."""
    parts = []
    if missing:
        parts.append("%d card(s) here not in it: %s" % (len(missing), ", ".join(missing[:3])))
    if extra:
        parts.append("%d row(s) with no card here: %s" % (len(extra), ", ".join(extra[:3])))
    return "; ".join(parts)


def rebuilt(SCOPE):
    """Run `heron_scope.py --rebuild` in this process: (exit code, its lines).

    The command itself, not a copy of it, so the rows, the search index and
    the embeddings are rebuilt together exactly as the documented recovery
    does - and its own refusal (exit 2, a shared store it may not build)
    stands. Its output is kept to report, not printed over this tool's."""
    import contextlib
    import io
    said = io.StringIO()
    try:
        with contextlib.redirect_stdout(said):
            code = SCOPE.main(["--rebuild"])
    except Exception as exc:                            # noqa: BLE001 - said, not raised
        return 1, ["%s: %s" % (type(exc).__name__, exc)]
    return code, [line.strip() for line in said.getvalue().splitlines() if line.strip()]


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

def _revit_addins_any_release():
    """Revit's per-user Addins folder with every release as `*`, for a glob.

    From platform/heron-folders.json through heron_bridge_client.heron_folder,
    the list's one Python reader; LookupError says why when it cannot be told.
    """
    try:
        import heron_bridge_client as BRIDGE
    except ImportError as exc:
        raise LookupError("mcp/client/heron_bridge_client.py, which reads the "
                          "list, did not load (%s)" % exc)
    return BRIDGE.heron_folder("RevitAddins", {"release": "*"})


def revit_side():
    head("7. Revit (read only - nothing here is changed)")
    if os.name != "nt":
        say(NOTE, "this is not Windows, so Revit is not here to check")
        return
    # THE FOLDER IS ASKED OF THE LIST HeronPaths WROTE, as the install scripts
    # ask it, never built here from APPDATA (row 5b-1746).
    try:
        addins = _revit_addins_any_release()
    except LookupError as exc:
        say(FIX, "where Revit reads add-ins could not be told - %s" % exc)
        return
    found = sorted(os.path.basename(os.path.dirname(p)) for p in glob.glob(
        os.path.join(addins, "Heron.addin")))
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
    # CHANGES, ADMIN AND PUBLISH ARE HELD IN REVIT SINCE D-166, off whenever it
    # starts and turned on only in its Settings window - so this file cannot
    # say which way they are, and this check does not pretend to.
    for label, needed in (("Changes", "any change to a model or family"),
                          ("Admin", "a NEW family file"),
                          ("Publish", "saving the family")):
        say(NOTE, "%-11s is off when Revit starts - needed for %s" % (label, needed))
    # ASK TO KEEP (D-138) lets nothing through: it only says whether Revit
    # asks Keep or Discard first. Off on a new PC, like the three above, and
    # still kept in heron.config.
    asks = CONFIG.truthy(values, "write.askBeforeKeeping")
    say(NOTE, "%-11s is %-3s - %s" % (
        "Ask to Keep", "ON" if asks else "off",
        "Revit asks Keep or Discard before a change is kept; the box can keep "
        "the rest of a job" if asks else
        "changes are kept without asking; Ctrl+Z still takes each one back"))
    print("         These are your switches in Heron's Settings (Heron tab). Changes, "
          "Admin and")
    print("         Publish start off every time Revit starts; turn on what the job "
          "needs before")
    print("         you ask - a refusal costs a turn.")


#: The oldest Claude Code that gives a chat all of Heron's rules. Claude Code's
#: MCP page, "Tool search for MCP server authors" (code.claude.com/docs/en/mcp):
#: "Before v2.1.296, the shorter limit was 2,048 characters" - so an older one
#: cuts the server's instructions in the middle of Article 9, and every Article
#: after it never reaches the chat (row 5b-2081).
CLAUDE_CODE_AT_LEAST = (2, 1, 296)


def _claude_version_text():
    """What `claude --version` prints, or None when it cannot be run here -
    the Claude app alone puts no `claude` on PATH."""
    try:
        import subprocess
        done = subprocess.run(["claude", "--version"], stdout=subprocess.PIPE,
                              stderr=subprocess.DEVNULL, timeout=15)
        return done.stdout.decode("utf-8", "replace").strip() or None
    except Exception:                                   # noqa: BLE001 - not on PATH is an answer
        return None


def claude_version(text):
    """(major, minor, patch) out of what `claude --version` printed - "2.1.330
    (Claude Code)" - or None when it holds no version."""
    found = re.search(r"(\d+)\.(\d+)\.(\d+)", text or "")
    return tuple(int(n) for n in found.groups()) if found else None


#: THE CLAUDE APP RUNS ITS OWN COPY. Claude Code's desktop page: the Code tab
#: "runs its own copy of Claude Code, which has its own version number ... it
#: can differ from the `claude` command in your terminal, and updating one
#: doesn't update the other". So whatever `claude --version` says, the app's
#: copy is read in the app - and this line says how, every time.
IN_THE_APP = ("The Claude app runs its own copy: type /status in a chat there "
              "and read the Claude Code row.")


def claude_code_version(text=None):
    """The one check of Claude Code itself: new enough to give a chat every rule.
    `text` is what `claude --version` printed; None runs it. That is the
    terminal's copy only, so the app's is named as /status every time, and a
    version that cannot be read is a note - the Claude app alone puts no
    `claude` on PATH."""
    said = _claude_version_text() if text is None else text
    have = claude_version(said)
    least = ".".join(str(n) for n in CLAUDE_CODE_AT_LEAST)
    if have is None:
        say(NOTE, "Claude Code's version could not be read here - it must be "
                  "%s or later" % least,
            ["An older one gives a chat only the first 2,048 characters of "
             "Heron's rules.", IN_THE_APP])
    elif have < CLAUDE_CODE_AT_LEAST:
        say(FIX, "Claude Code %s in this terminal is older than %s, so a chat is "
                 "given only the first 2,048 characters of Heron's rules"
                 % (".".join(str(n) for n in have), least),
            ["Update it with `claude update`, then start a new chat.",
             "The Claude app's own copy: Help > Check for Updates..., then a new "
             "chat."])
    else:
        say(OK, "Claude Code %s in this terminal - %s or later, so a chat is "
                "given all of Heron's rules"
                % (".".join(str(n) for n in have), least), IN_THE_APP)


def claude_side():
    head("8. Claude Code (it keeps these itself - check them once)")
    claude_code_version()
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
            "  method with heron_method, so a new PC no longer needs it."):
        print("  %-4s %s" % (NOTE if not line.startswith("  ") else "", line))


# ---------------------------------------------------------------------------
# --snapshot: what this PC has, as facts another PC's can be set beside
# ---------------------------------------------------------------------------

#: The packages whose presence or version can change how fast Heron answers.
#: sqlite-vec left this list on 2026-11-21: nothing in Heron uses it (D-194).
#: Two more left it on 2026-11-26, with the optional second reader (D-201),
#: and two more the same day with the trained search model (D-203).
SNAPSHOT_PACKAGES = ("PyYAML", "mcp", "numpy", "pypdf")

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
    facts = {"config_folder": home, "version": _claude_version_text()}
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
    if os.name == "nt":
        try:
            facts["revit"]["addin_releases"] = sorted(
                os.path.basename(os.path.dirname(p)) for p in glob.glob(os.path.join(
                    _revit_addins_any_release(), "Heron.addin")))
        except LookupError as exc:                      # recorded as the fact
            facts["revit"]["addin_releases"] = "LookupError: %s" % exc
    try:
        import heron_config as CONFIG
        values = CONFIG.load()
        # Changes, Admin and Publish are not in heron.config since D-166.
        for key in ("write.askBeforeKeeping", "bridge.autoConnect", "fragments.warmUp"):
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
    # Row 5b-2044: print safely on a cp1252 console. A stream
    # that cannot be reconfigured - a test's StringIO - is left as it is.
    for stream in (sys.stdout, sys.stderr):
        if hasattr(stream, "reconfigure"):
            stream.reconfigure(encoding="utf-8", errors="replace")

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
    claude_app()
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
