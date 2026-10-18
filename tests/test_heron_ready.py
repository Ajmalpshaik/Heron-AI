#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
tools/heron-ready.py - a new PC is made ready, or told exactly what it lacks.

    python tests/test_heron_ready.py

A new laptop's first "create a family" took over seven minutes where the
office PC took one or two (2026-10-08): the knowledge store and the search
model were left for the first chat to build, and a missing package left Claude
reading Heron's files instead of calling its tools. The tool exists so a new
PC starts where the old one is. This holds the three things it promises:

  1. A missing REQUIRED package exits 1 and names the command that installs
     it - and builds nothing it cannot build properly.
  2. Its exit code follows its own FIX lines: 0 exactly when none is printed.
  3. Without --check, the knowledge store is BUILT, so the first chat finds
     it built. With --check, nothing is.
  4. --snapshot writes what a PC has as facts - the Heron tools Claude Code
     lets run, how much it remembers, as counts - and NO secret: no memory
     text, no key, no address, no chat, no unrelated permission.
  5. A Microsoft Store Claude's private copy of AppData is found, its
     packages read off their folder names, a run inside it told from a run
     outside it, and a pip line pinned to its versions - as a WARNING that
     moves no exit code (row 5b-417). Faked in folders here: the real
     redirection is Windows', and only the owner's laptop has it.
  6. On Windows every FIX line for a package - PyYAML missing, the MCP
     package missing, the MCP package breaking on import - is the one line
     requirements-lock.txt names at its head, pinned and hashed, and no
     unpinned pip line is printed. Off Windows the lines stay what they were:
     the lock pins pywin32, which has no wheel but Windows'. Asked of the
     tool's section 2 in this process, with the platform given and the
     imports faked - so it runs here, and says nothing of a real Windows pip.

Every run points HERON_KNOWLEDGE at a scratch folder, so the shared store on
the machine running this is never touched (row 5b-233).

WHAT IT CANNOT DO: say anything about a real Windows PC - the Microsoft Store
shortcut, %APPDATA%, the add-in folders and Claude Code's own approvals are
not here to look at, and Windows' redirection of a Store app's AppData is
faked with a junction, not seen.
"""

import contextlib
import importlib.util
import io
import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOOL = os.path.join(ROOT, "tools", "heron-ready.py")
LOCK = os.path.join(ROOT, "requirements-lock.txt")

sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real
# trail (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant()

#: The line a Windows FIX must name - the one requirements-lock.txt names at
#: its own head, and HeronInstaller.exe runs.
LOCK_LINE = "python -m pip install --user --require-hashes -r requirements-lock.txt"

#: The lines it named everywhere until 2026-10-17: unpinned, so never again on
#: Windows, and still off it, where the lock cannot install.
OFF_WINDOWS = {"yaml": "pip install --user -r requirements.txt",
               "mcp": "pip install --user mcp",
               "repair": "pip install --user --upgrade mcp cryptography cffi"}

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def run(knowledge, *args, **env):
    """(exit code, output) of the tool with its store in `knowledge`."""
    environ = dict(os.environ, HERON_KNOWLEDGE=knowledge, PYTHONUTF8="1")
    environ.update(env)
    done = subprocess.run([sys.executable, TOOL] + list(args), cwd=ROOT,
                          env=environ, stdout=subprocess.PIPE,
                          stderr=subprocess.STDOUT, timeout=600)
    return done.returncode, done.stdout.decode("utf-8", "replace")


def load_tool():
    """The tool as a module, for the checks that call its parts directly."""
    spec = importlib.util.spec_from_file_location("heron_ready_under_test", TOOL)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def link_folder(link, target):
    """Make `link` open as `target` - the merged view a packaged app's child
    sees. A junction on Windows, which needs no administrator; a symlink
    elsewhere. False when neither can be made here."""
    try:
        if os.name == "nt":
            import _winapi
            _winapi.CreateJunction(target, link)
        else:
            os.symlink(target, link)
        return True
    except (OSError, ImportError, AttributeError):
        return False


def store_claude(work, plain_code):
    """Section 5 - a Microsoft Store Claude, faked in folders (row 5b-417)."""
    local = os.path.join(work, "local")
    roaming = os.path.join(work, "roaming")
    nothing = os.path.join(work, "local-without-claude")
    cache = os.path.join(local, "Packages", "AnthropicPBC.Claude_test1234",
                         "LocalCache")
    site = os.path.join(cache, "Roaming", "Python",
                        "Python%d%d" % sys.version_info[:2], "site-packages")
    for name in ("PyYAML-0.0.1.dist-info", "mcp-0.0.2.dist-info",
                 "sqlite_vec-0.0.3.dist-info", "requests-9.9.dist-info"):
        os.makedirs(os.path.join(site, name))
    os.makedirs(roaming)
    os.makedirs(nothing)

    tool = load_tool()
    found = getattr(tool, "claude_app_copies", None)
    held = getattr(tool, "copy_packages", None)
    inside = getattr(tool, "inside_claude_app", None)
    check(None not in (found, held, inside),
          "the tool can find the copy, read its packages, and tell a run "
          "inside the app from one outside it")
    if None not in (found, held, inside):
        check(found({"LOCALAPPDATA": local}) == [cache],
              "it finds the copy under %LOCALAPPDATA%/Packages")
        check(found({"LOCALAPPDATA": nothing}) == [] and found({}) == [],
              "and none where there is no Store Claude, or no %LOCALAPPDATA%")
        got = held(cache)
        check(got == {"PyYAML": "0.0.1", "mcp": "0.0.2", "sqlite-vec": "0.0.3"},
              "it reads Heron's packages off the copy's folder names, and only "
              "Heron's: %s" % got)
        environ = {"APPDATA": roaming, "LOCALAPPDATA": local}
        check(inside(cache, environ) is False,
              "a run whose %APPDATA% has no Python folder is OUTSIDE the app")
        own = os.path.join(roaming, "Python")
        os.makedirs(own)
        check(inside(cache, environ) is False,
              "and so is one whose %APPDATA% has a Python folder of its own")
        os.rmdir(own)
        if link_folder(own, os.path.join(cache, "Roaming", "Python")):
            try:
                check(inside(cache, environ) is True,
                      "a run whose %APPDATA% Python folder IS the copy's is "
                      "INSIDE the app")
            finally:
                if os.name == "nt":
                    os.rmdir(own)
                else:
                    os.unlink(own)
        else:
            print("  --    NOT RUN: no junction or symlink could be made here, "
                  "so the inside case is unchecked")

    store = os.path.join(work, "store-claude")
    os.makedirs(store)
    code, out = run(store, "--check", LOCALAPPDATA=local)
    part = out.split("2a.", 1)[-1].split("\n3. ", 1)[0] if "2a." in out else ""
    check(bool(part), "the whole tool says 2a when %LOCALAPPDATA% holds a "
                      "Store Claude's copy")
    check("pip install --user mcp==0.0.2 PyYAML==0.0.1 sqlite-vec==0.0.3" in part,
          "with a pip line pinned to the copy's versions")
    check("Reset or uninstall" in part,
          "and says a Reset or uninstall of the app deletes the copy")
    check("FIX " not in part and code == plain_code,
          "as a warning only: no FIX line, and the exit code is a plain "
          "--check's (%d, plain %d)" % (code, plain_code))
    code, out = run(store, "--check", LOCALAPPDATA=nothing)
    check("2a." not in out,
          "and says nothing of it where there is no Store Claude")


class _Breaks(object):
    """An import finder that makes the MCP package raise on import - the shape
    of 2026-09-16, a native binding that raised the moment `mcp` loaded."""

    def find_spec(self, name, path=None, target=None):
        if name == "mcp" or name.startswith("mcp."):
            raise RuntimeError("a native binding raised on import (faked)")
        return None


def section_two(tool, broken=False, **platform):
    """What the tool's section 2 prints with PyYAML and the MCP package
    missing - or, `broken`, with the MCP package raising on import - asked
    for the platform in `platform`. sys.modules is put back afterwards."""
    faked = ("yaml", "mcp.server.mcpserver", "mcp.server.fastmcp")
    saved = dict((name, module) for name, module in sys.modules.items()
                 if name in faked or name == "mcp" or name.startswith("mcp."))
    for name in saved:
        del sys.modules[name]
    finder = _Breaks()
    if broken:
        sys.meta_path.insert(0, finder)
    else:
        for name in faked:
            sys.modules[name] = None        # import raises ModuleNotFoundError
    printed = io.StringIO()
    try:
        with contextlib.redirect_stdout(printed):
            tool.packages(**platform)
    finally:
        if finder in sys.meta_path:
            sys.meta_path.remove(finder)
        for name in faked:
            if name in sys.modules and sys.modules[name] is None:
                del sys.modules[name]
        sys.modules.update(saved)
    return printed.getvalue()


def pip_lines(out):
    """Every printed line that is a pip command, stripped."""
    return [line.strip() for line in out.splitlines() if "pip " in line]


def lock_on_windows():
    """Section 6 - on Windows the FIX lines are the lock; off it, not."""
    lock = io.open(LOCK, encoding="utf-8").read() if os.path.exists(LOCK) else ""
    check(("#     %s\n" % LOCK_LINE) in lock.split("\n\n", 1)[0],
          "requirements-lock.txt names %r at its own head - the line the "
          "checks below hold the tool to" % LOCK_LINE)

    tool = load_tool()
    code = tool.packages.__code__
    asks = code.co_varnames[:code.co_argcount]
    can = "windows" in asks and "minor" in asks
    check(can, "section 2 can be asked for one platform's lines - "
               "packages(windows, minor); it takes %s" % (list(asks),))

    def section(broken=False, **platform):
        return section_two(tool, broken, **(platform if can else {}))

    out = section(windows=True, minor=12)
    pips = pip_lines(out)
    check(len(pips) == 2 and all(line == LOCK_LINE for line in pips),
          "on Windows, PyYAML missing and the MCP package missing each end in "
          "the lock line, and no other pip line is printed: %s" % pips)
    out = section(broken=True, windows=True, minor=12)
    pips = pip_lines(out)
    check("breaks on import" in out and pips == [LOCK_LINE],
          "a broken MCP import is repaired with the lock line too - the "
          "measured versions put back, never an unpinned --upgrade: %s" % pips)
    for minor in (10, 15):
        out = section(windows=True, minor=minor)
        pips = pip_lines(out)
        check("3.11 to 3.14" in out and len(pips) == 2
              and all(line == LOCK_LINE for line in pips),
              "on a Windows Python 3.%d it says the lock is for 3.11 to 3.14 - "
              "the lock's own header - and still names no unpinned line: %s"
              % (minor, pips))
    out = section(windows=True, minor=14)
    check("3.11 to 3.14" not in out,
          "and on 3.14, inside that range, it says nothing about the Python")

    out = section(windows=False, minor=12)
    check(OFF_WINDOWS["yaml"] in out and OFF_WINDOWS["mcp"] in out
          and "requirements-lock" not in out,
          "off Windows the lines stay what they were - the lock pins pywin32, "
          "which nothing but Windows installs: %s" % pip_lines(out))
    out = section(broken=True, windows=False, minor=12)
    check(OFF_WINDOWS["repair"] in out and "requirements-lock" not in out,
          "and so does the repair of a broken import: %s" % pip_lines(out))


def main():
    if not os.path.exists(TOOL):
        check(False, "tools/heron-ready.py exists")
        return 1
    work = tempfile.mkdtemp(prefix="heron-ready-")
    try:
        print("1. A missing required package is named, with its command")
        shadow = os.path.join(work, "shadow")
        os.makedirs(shadow)
        with open(os.path.join(shadow, "yaml.py"), "w") as handle:
            handle.write("raise ImportError('PyYAML is not installed here')\n")
        # model2vec present and PyYAML not, on EVERY machine: the search model
        # section imported heron_embed, which needs PyYAML, and died with a
        # traceback before the summary - only where model2vec was installed,
        # so CI never saw it (row 5b-1008).
        with open(os.path.join(shadow, "model2vec.py"), "w") as handle:
            handle.write("# stands in for model2vec being installed\n")
        store = os.path.join(work, "no-yaml")
        os.makedirs(store)
        code, out = run(store, PYTHONPATH=shadow)
        check(code == 1, "with no PyYAML it exits 1 (got %d)" % code)
        wanted = LOCK_LINE if os.name == "nt" else OFF_WINDOWS["yaml"]
        check(wanted in out,
              "and names the command that installs it on this platform: %s"
              % wanted)
        check(os.listdir(store) == [],
              "and builds no knowledge store without it")
        # THE INSTALLER READS THIS SUMMARY - PythonSetup.NotReadyItems takes
        # the "  - " lines under the line starting NOT READY. Reworded or
        # re-indented, the installer would only say "did not say READY", and
        # nothing else would go red (review of #558).
        lines = out.splitlines()
        heading = [i for i, line in enumerate(lines) if line.startswith("NOT READY")]
        check(len(heading) == 1,
              "the summary is ONE line starting NOT READY, which the installer looks for")
        items = [line[4:] for line in lines[heading[0] + 1:]
                 if line.startswith("  - ")] if heading else []
        check(any("PyYAML is missing" in item for item in items),
              "and each thing to fix is a '  - ' line under it, as the installer reads it: %s"
              % (items or "none"))

        print()
        print("2. The exit code follows the FIX lines")
        store = os.path.join(work, "check")
        os.makedirs(store)
        code, out = run(store, "--check")
        code_check = code
        fixes = [line for line in out.splitlines()
                 if line.strip().startswith("FIX ")]
        check((code == 0) == (not fixes),
              "exit %d with %d FIX line(s)" % (code, len(fixes)))
        check(os.listdir(store) == [],
              "--check builds nothing")

        print()
        print("3. Without --check the store is built before any chat asks")
        store = os.path.join(work, "full")
        os.makedirs(store)
        code, out = run(store)
        check("tool cards ready" in out,
              "it reports the tool cards ready")
        check(any(name.endswith(".db") for name in os.listdir(store)),
              "and the store is on disk in the folder it was given: %s"
              % ", ".join(sorted(os.listdir(store))))

        print()
        print("4. A snapshot says what a PC has - and carries no secret")
        claude = os.path.join(work, "claude")
        memory = os.path.join(claude, "projects", "D--Work-Heron-Ai", "memory")
        os.makedirs(memory)
        with open(os.path.join(memory, "MEMORY.md"), "w") as handle:
            handle.write("SECRET MEMORY TEXT\nsecond line\n")
        with open(os.path.join(claude, "settings.json"), "w") as handle:
            json.dump({"permissions": {"allow": ["mcp__heron__heron_lookup",
                                                 "Bash(git push:*)"]},
                       "env": {"ANTHROPIC_API_KEY": "sk-not-for-sharing"}},
                      handle)
        with open(os.path.join(claude, ".claude.json"), "w") as handle:
            json.dump({"primaryApiKey": "sk-not-for-sharing",
                       "oauthAccount": {"emailAddress": "someone@example.com"},
                       "projects": {"D:/Work/Heron-Ai": {
                           "hasTrustDialogAccepted": True,
                           "enabledMcpjsonServers": ["heron"],
                           "allowedTools": ["mcp__heron__revit_read"],
                           "history": [{"display": "SECRET CHAT TEXT"}]}}},
                      handle)
        out = os.path.join(work, "pc.json")
        code, said = run(os.path.join(work, "full"), "--snapshot", out,
                         CLAUDE_CONFIG_DIR=claude)
        check(code == 0 and os.path.exists(out),
              "it writes the file (exit %d)" % code)
        text = open(out).read() if os.path.exists(out) else ""
        got = json.loads(text) if text else {}
        side = got.get("claude_code") or {}
        check(side.get("user_settings", {}).get("heron_allow")
              == ["mcp__heron__heron_lookup"],
              "it names the Heron tools Claude Code lets run unasked")
        check([p.get("memory_md_lines") for p in side.get("projects", [])] == [2],
              "and how much Claude remembers of Heron, as a line count")
        check([a.get("heron_allowed_tools") for a in side.get("approvals", [])]
              == [["mcp__heron__revit_read"]],
              "and the approvals kept for the Heron folder")
        leaked = [s for s in ("SECRET MEMORY TEXT", "sk-not-for-sharing",
                              "someone@example.com", "SECRET CHAT TEXT",
                              "git push") if s in text]
        check(not leaked, "and nothing else - no memory text, key, address, "
                          "chat or other permission%s"
              % ("" if not leaked else " - LEAKED: " + ", ".join(leaked)))
        check(isinstance(got.get("lookup_seconds"), list),
              "it times three lookups where the store is its own: %s"
              % got.get("lookup_seconds"))

        print()
        print("5. A Microsoft Store Claude's own copy of AppData is named, as a warning")
        store_claude(work, plain_code=code_check)

        print()
        print("6. On Windows every package FIX is the pinned, hashed lock line")
        lock_on_windows()
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASSED - a missing package is named with its command, the exit code")
    print("follows what is printed, the store is built before a chat asks, and a")
    print("Store Claude's own copy of AppData is named as a warning, and on")
    print("Windows every package FIX is the pinned, hashed lock line.")
    print("It says nothing about a real Windows PC.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
