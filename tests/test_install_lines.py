#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Every pip line Heron prints for its own packages is the pinned lock, on Windows.

    python tests/test_install_lines.py

Rows 5b-944 and 5b-979. Unpinned, `pip install mcp` pulled a cryptography that
broke on import on 2026-09-16 (README.md), which is why requirements-lock.txt
exists: every version pinned, every file hashed, the file HeronInstaller.exe
installs from. One function says what to print - brain/heron_install.py's
install_line() - and this holds it and everything that prints such a line:

  1. heron_install.LOCK_LINE is the line requirements-lock.txt names at its
     own head, so the two cannot drift apart.
  2. install_line() gives that line on Windows, and the caller's unpinned one
     elsewhere - the lock pins pywin32, which has no wheel off Windows, so
     there it cannot install at all.
  3. In brain/, mcp/ and tools/, every string that names a pip line for a
     package the lock pins - PyYAML, mcp, cryptography, cffi, or
     requirements.txt - is handed to install_line(), never printed around
     it. Docstrings are prose and are left out; brain/fragments/ and tests/
     are not scanned.
  4. The MCP server, loaded with an MCP package that has neither SDK's
     server module, stops with the line install_line() gives here.
  5. A brain module, and a tool that does not put brain/ on its path, each
     with no PyYAML, print the line install_line() gives here.

WHAT IT CANNOT DO: see the Windows line printed on a real Windows. Sections 4
and 5 run on whatever runs this suite; the Windows answer is held by 2 and 3.
"""

import ast
import contextlib
import importlib.util
import io
import os
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
BRAIN = os.path.join(ROOT, "brain")
SERVER_DIR = os.path.join(ROOT, "mcp", "server")
HELPER = os.path.join(BRAIN, "heron_install.py")
LOCK = os.path.join(ROOT, "requirements-lock.txt")

sys.path.insert(0, os.path.join(ROOT, "tests", "support"))
import scratch_audit                                            # noqa: E402

# This run's audit lines go to a scratch folder, never the owner's real
# trail (FRAGMENT-ISSUES row 5b-1070).
scratch_audit.plant()

#: A pip line for a package the lock pins. Optional add-ons (model2vec and the
#: like) are not in the lock and stay their own lines.
LOCKED = re.compile(r"pip install[^\n]*?(\bpyyaml\b|\bmcp\b|\bcryptography\b|"
                    r"\bcffi\b|-r\s+requirements\.txt)", re.I)

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


def load(path, name):
    """A file as a module, or None when it is not there."""
    if not os.path.exists(path):
        return None
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def scanned():
    """The tracked Python files of brain/ (fragments aside), mcp/ and tools/."""
    done = subprocess.run(["git", "ls-files", "brain/*.py", "mcp/*.py", "tools/*.py"],
                          cwd=ROOT, stdout=subprocess.PIPE, encoding="utf-8")
    return [os.path.join(ROOT, f) for f in done.stdout.split()
            if not f.startswith("brain/fragments/")]


def loose_lines(path):
    """file:line of every string naming a locked package's pip line that is not
    the first argument of a call to install_line()."""
    tree = ast.parse(read(path), path)
    docs, handed = set(), set()
    for node in ast.walk(tree):
        if (isinstance(node, (ast.Module, ast.ClassDef, ast.FunctionDef,
                              ast.AsyncFunctionDef))
                and node.body and isinstance(node.body[0], ast.Expr)
                and isinstance(node.body[0].value, ast.Constant)):
            docs.add(id(node.body[0].value))
        if isinstance(node, ast.Call) and node.args:
            name = getattr(node.func, "id", None) or getattr(node.func, "attr", None)
            if name == "install_line":
                handed.add(id(node.args[0]))
    where = os.path.relpath(path, ROOT).replace(os.sep, "/")
    return ["%s:%d" % (where, node.lineno) for node in ast.walk(tree)
            if isinstance(node, ast.Constant) and isinstance(node.value, str)
            and id(node) not in docs and id(node) not in handed
            and LOCKED.search(node.value)]


def run_python(code, work, **env):
    """(exit code, everything printed) of `python -c code` in `work`."""
    environ = dict(os.environ, HERON_KNOWLEDGE=os.path.join(work, "store"),
                   PYTHONUTF8="1")
    environ.update(env)
    done = subprocess.run([sys.executable, "-c", code], cwd=work, env=environ,
                          stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                          timeout=300)
    return done.returncode, done.stdout.decode("utf-8", "replace")


def main():
    if not os.path.exists(LOCK):
        check(False, "requirements-lock.txt exists")
        return 1
    wanted = lock_line()
    helper = load(HELPER, "heron_install_under_test")

    print("1. The Windows line is the lock's own")
    check(wanted is not None,
          "requirements-lock.txt names its install line at its head: %s" % wanted)
    check(helper is not None, "brain/heron_install.py is there to say it")
    have = getattr(helper, "LOCK_LINE", None)
    check(have is not None and have == wanted,
          "and its LOCK_LINE is that line: %s" % have)

    print()
    print("2. install_line() - the lock on Windows, the caller's line elsewhere")
    line = getattr(helper, "install_line", None)
    check(line is not None, "heron_install defines install_line()")
    for unpinned in ("pip install --user pyyaml", "pip install --user mcp",
                     "pip install --user --upgrade mcp"):
        on = line(unpinned, windows=True) if line else None
        off = line(unpinned, windows=False) if line else None
        check(on == wanted,
              "on Windows, %r becomes the lock line: %s" % (unpinned, on))
        check(off == unpinned,
              "off Windows it stays %r, where the lock cannot install: %s"
              % (unpinned, off))

    print()
    print("3. No pip line for a locked package is printed around install_line()")
    files = scanned()
    check(len(files) > 0, "brain/, mcp/ and tools/ hold Python files: %d" % len(files))
    loose = []
    for path in files:
        loose.extend(loose_lines(path))
    check(not loose, "every one goes through install_line()%s"
          % ("" if not loose else " - not: %s" % ", ".join(loose)))

    work = tempfile.mkdtemp(prefix="heron-install-lines-")
    expected = wanted if os.name == "nt" else None      # None: the unpinned line
    try:
        print()
        print("4. With no SDK server module, the server stops naming this platform's line")
        fake = os.path.join(work, "fake")
        os.makedirs(os.path.join(fake, "mcp", "server"))
        for part in (("mcp",), ("mcp", "server")):
            io.open(os.path.join(fake, *(part + ("__init__.py",))), "w").close()
        code, said = run_python("import sys; sys.path[:0] = [%r, %r]; "
                                "import heron_mcp_server" % (fake, SERVER_DIR), work)
        mine = expected or "pip install --user mcp"
        check(code != 0 and "neither" in said,
              "it does not start, and says the SDK has neither module (exit %d)" % code)
        check(mine in said, "and names %s" % mine)

        print()
        print("5. With no PyYAML, a brain module and a tool name this platform's line")
        shadow = os.path.join(work, "shadow")
        os.makedirs(shadow)
        io.open(os.path.join(shadow, "yaml.py"), "w").write(
            "raise ImportError('PyYAML is not installed here')\n")
        mine = expected or "pip install --user pyyaml"
        code, said = run_python("import sys; sys.path[:0] = [%r, %r]; "
                                "import heron_agents" % (shadow, BRAIN), work)
        check(code != 0 and mine in said,
              "brain/heron_agents.py stops and names %s (exit %d)" % (mine, code))
        tool = os.path.join(ROOT, "tools", "resign-machine-proofs.py")
        code, said = run_python(
            "import importlib.util, sys; sys.path[:0] = [%r]; "
            "spec = importlib.util.spec_from_file_location('t', %r); "
            "t = importlib.util.module_from_spec(spec); spec.loader.exec_module(t); "
            "t._yaml()" % (shadow, tool), work)
        check(code != 0 and mine in said,
              "tools/resign-machine-proofs.py, which puts no brain/ on its path, "
              "names %s too (exit %d)" % (mine, code))
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for one in FAILURES:
            print("  - %s" % one)
        return 1
    print("PASSED - every pip line Heron prints for a locked package goes through")
    print("install_line(): requirements-lock.txt's own line on Windows, the")
    print("unpinned one elsewhere. Not seen printed on a real Windows.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
