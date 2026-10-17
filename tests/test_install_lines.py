#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
What Heron's MCP server tells a person to install is the pinned lock, on Windows.

    python tests/test_install_lines.py

Row 5b-944. Unpinned, `pip install mcp` pulled a cryptography that broke on
import on 2026-09-16 (README.md), which is why requirements-lock.txt exists:
every version pinned, every file hashed, the file HeronInstaller.exe installs
from. tools/heron-ready.py and tools/HeronRevit.ps1 name it on Windows
(tests/test_heron_ready.py, tests/test_setup_script.py). The server names a
pip line in two places of its own - when it cannot load the MCP SDK at all,
and when revit_view_image meets an SDK with no image type - and this holds
those two:

  1. The line the server names on Windows is the one requirements-lock.txt
     names at its own head, so the two cannot drift apart.
  2. install_line() gives that line on Windows, and the unpinned one
     elsewhere - the lock pins pywin32, which has no wheel off Windows, so
     there it cannot install at all.
  3. Every string in the server that holds a pip line goes through
     install_line() - none is typed around it.
  4. Loaded with an MCP package that has neither SDK's server module, the
     server stops with the line install_line() gives on this platform.

Read from the server's source rather than by importing it, so it runs with no
MCP SDK installed - which is CI's case.

WHAT IT CANNOT DO: see the Windows line printed on a real Windows. Section 4
runs on whatever runs this suite; the Windows answer is held by 2 and 3.
"""

import ast
import io
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SERVER_DIR = os.path.join(ROOT, "mcp", "server")
SERVER = os.path.join(SERVER_DIR, "heron_mcp_server.py")
LOCK = os.path.join(ROOT, "requirements-lock.txt")

#: The two unpinned lines the server named before row 5b-944, and still names
#: off Windows.
UNPINNED = ("pip install --user mcp", "pip install --user --upgrade mcp")

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def read(path):
    with io.open(path, encoding="utf-8") as handle:
        return handle.read()


def lock_line():
    """The pip line requirements-lock.txt names at its own head, or None."""
    for line in read(LOCK).split("\n\n", 1)[0].splitlines():
        line = line.lstrip("#").strip()
        if line.startswith("python -m pip install") and "requirements-lock.txt" in line:
            return line
    return None


def server_parts(tree):
    """{'LOCK_LINE': value, 'install_line': function} lifted out of the
    server's source, run on their own - nothing else of the server is run."""
    nodes = [node for node in tree.body
             if (isinstance(node, ast.Assign)
                 and any(getattr(t, "id", None) == "LOCK_LINE" for t in node.targets))
             or (isinstance(node, ast.FunctionDef) and node.name == "install_line")]
    space = {"os": os}
    exec(compile(ast.Module(body=nodes, type_ignores=[]), SERVER, "exec"), space)
    return space


def pip_strings(tree):
    """Every string constant in the server that holds a pip line, with whether
    it is the first argument of a call to install_line()."""
    wrapped = set()
    for node in ast.walk(tree):
        if (isinstance(node, ast.Call) and getattr(node.func, "id", None) == "install_line"
                and node.args and isinstance(node.args[0], ast.Constant)):
            wrapped.add(id(node.args[0]))
    found = []
    for node in ast.walk(tree):
        if (isinstance(node, ast.Constant) and isinstance(node.value, str)
                and "pip install" in node.value):
            found.append((node.value, id(node) in wrapped))
    return found


def load_without_sdk(work):
    """The server's start-up with an MCP package that has neither SDK's server
    module: (exit code, what it printed)."""
    fake = os.path.join(work, "fake")
    os.makedirs(os.path.join(fake, "mcp", "server"))
    for part in (("mcp",), ("mcp", "server")):
        io.open(os.path.join(fake, *(part + ("__init__.py",))), "w").close()
    code = ("import sys; sys.path[:0] = [%r, %r]; import heron_mcp_server"
            % (fake, SERVER_DIR))
    environ = dict(os.environ, HERON_KNOWLEDGE=os.path.join(work, "store"),
                   PYTHONUTF8="1")
    done = subprocess.run([sys.executable, "-c", code], cwd=work, env=environ,
                          stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                          timeout=300)
    return done.returncode, done.stdout.decode("utf-8", "replace")


def main():
    for path in (SERVER, LOCK):
        if not os.path.exists(path):
            check(False, "%s exists" % os.path.relpath(path, ROOT))
            return 1
    tree = ast.parse(read(SERVER), SERVER)
    wanted = lock_line()

    print("1. The server's Windows line is the lock's own")
    check(wanted is not None,
          "requirements-lock.txt names its install line at its head: %s" % wanted)
    space = server_parts(tree)
    have = space.get("LOCK_LINE")
    check(have is not None and have == wanted,
          "the server's LOCK_LINE is that line: %s" % have)

    print()
    print("2. install_line() - the lock on Windows, the unpinned line elsewhere")
    line = space.get("install_line")
    check(line is not None, "the server defines install_line()")
    for unpinned in UNPINNED:
        on = line(unpinned, windows=True) if line else None
        off = line(unpinned, windows=False) if line else None
        check(on == wanted,
              "on Windows, %r becomes the lock line: %s" % (unpinned, on))
        check(off == unpinned,
              "off Windows it stays %r, where the lock cannot install: %s"
              % (unpinned, off))

    print()
    print("3. Every pip line the server prints goes through install_line()")
    found = pip_strings(tree)
    loose = sorted(set(text for text, wrapped in found
                       if not wrapped and text != wanted))
    check(not loose, "no pip line is typed around it%s"
          % ("" if not loose else " - typed: %s" % loose))
    passed = sorted(set(text for text, wrapped in found if wrapped))
    check(passed == sorted(UNPINNED),
          "and it is handed the two the server names - the SDK missing, and "
          "the SDK with no image type: %s" % passed)

    print()
    print("4. With no SDK server module, the server stops naming this platform's line")
    work = tempfile.mkdtemp(prefix="heron-install-lines-")
    try:
        code, said = load_without_sdk(work)
    finally:
        shutil.rmtree(work, ignore_errors=True)
    expected = wanted if os.name == "nt" else UNPINNED[0]
    check(code != 0 and "neither" in said,
          "it does not start, and says the SDK has neither module (exit %d)" % code)
    check(bool(expected) and expected in said,
          "and names %s - %s"
          % (expected, "the lock, on Windows" if os.name == "nt"
             else "the unpinned line, off Windows"))

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for one in FAILURES:
            print("  - %s" % one)
        return 1
    print("PASSED - on Windows the server names requirements-lock.txt's own line,")
    print("and off Windows the unpinned one. Not seen printed on a real Windows.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
