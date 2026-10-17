#!/usr/bin/env python3
# Heron-Agent:  none
# Heron-Step:   5
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The MCP server notices when its own Python changes under a chat.

    python tests/test_running_code.py

WHAT WENT WRONG
---------------
NEEDS-CHECKING B8, 2026-09-07. The write path was fixed - the approval
travels as `approvalToken` - and revit_apply_move kept answering "There is
nothing waiting to be approved", because the MCP server is a long-running
process and still ran the code from before the fix. Restarting Revit does not
reload it. Nothing in the chat said so; B8 was proved from a fresh process.

WHAT THIS PROVES, with no SDK and no Revit
  1. THE WATCH, over a scratch checkout of its own: silent at first look; a
     file whose bytes changed after it was loaded is named, its mtime moved
     with it; a touch that leaves the bytes alone is NOT a change, and the
     bytes decide it; bytes put back stop being a change; a deleted file is
     one; a module outside Heron's Python folders is not watched; a module
     loaded later is fingerprinted from when it is first seen, not reported.
     And it stays CHEAP: nothing is read again while no stat has moved.
  2. THE REAL CHECKOUT: a watch over it sees the modules the server imports
     and nothing from anywhere else.
  3. THE SERVER IS WIRED TO IT, read as text - the one place every tool
     passes through, and the Companion's side door. That a chat actually
     hears it through the SDK's dispatch, and a write is refused while a
     read answers, is tests/test_mcp_serves.py section 16, which needs the
     MCP SDK.

WHAT IT CANNOT DO: a real chat, on the owner's PC, after a real pull -
NEEDS-CHECKING group EU.
"""

import io
import os
import shutil
import sys
import tempfile
import types

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SERVER = os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py")
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

try:
    import heron_running_code as RC
except ImportError:
    RC = None

FAILURES = []


def check(condition, what):
    print("  %-4s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write(path, text):
    folder = os.path.dirname(path)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    with io.open(path, "w", encoding="utf-8", newline="") as fh:
        fh.write(text)


def later(path, seconds):
    """Move a file's mtime on by `seconds`, so a rewrite inside one tick of
    the clock cannot hide - the edit a pull makes is never that quick."""
    st = os.stat(path)
    when = st.st_mtime_ns + int(seconds * 1e9)
    os.utime(path, ns=(when, when))


def module_at(name, path):
    module = types.ModuleType(name)
    module.__file__ = path
    return module


def short(paths, root):
    return [os.path.relpath(p, root).replace(os.sep, "/") for p in paths]


def the_watch():
    print("1. The watch, over a scratch checkout of its own")
    if RC is None:
        check(False, "mcp/server/heron_running_code.py exists - the server has a watch "
                     "on its own code at all")
        return

    home = tempfile.mkdtemp(prefix="heron-running-code-")
    try:
        root = os.path.join(home, "Heron")
        one = os.path.join(root, "brain", "heron_one.py")
        two = os.path.join(root, "mcp", "server", "heron_two.py")
        late = os.path.join(root, "brain", "heron_late.py")
        outside = os.path.join(home, "site-packages", "other.py")
        compiled = os.path.join(root, "brain", "heron_compiled.pyc")
        write(one, "print('a')\n")
        write(two, "VALUE = 1\n")
        write(late, "LATE = 1\n")
        write(outside, "X = 1\n")
        write(compiled, "not source\n")
        modules = {"heron_one": module_at("heron_one", one),
                   "heron_two": module_at("heron_two", two),
                   "other": module_at("other", outside),
                   "heron_compiled": module_at("heron_compiled", compiled),
                   "builtin_like": types.ModuleType("builtin_like")}
        odd = types.ModuleType("odd")
        odd.__file__ = 42                          # not a path at all
        modules["odd"] = odd

        reads = []
        digest = RC._digest

        def counted(path):
            reads.append(path)
            return digest(path)

        RC._digest = counted
        try:
            watch = RC.RunningCode.of_checkout(root, modules)
            check(short(watch.watched(), root) == ["brain/heron_one.py", "mcp/server/heron_two.py"],
                  "it watches the two Heron modules, and not a module from elsewhere, "
                  "a compiled file, one with no file or one whose file is not a path - "
                  "watched %r"
                  % short(watch.watched(), root))
            check(watch.changed() == [], "silent at first look")

            del reads[:]
            for _ in range(50):
                watch.changed()
            check(watch.changed() == [] and reads == [],
                  "and cheap while nothing changed: fifty looks read no file again "
                  "- read %d" % len(reads))

            later(one, 5)
            del reads[:]
            check(watch.changed() == [],
                  "a touch that leaves the bytes alone is not a change - a checkout that "
                  "rewrites a file with the same content must not cry wolf")
            check(len(reads) == 1, "and the bytes decided it: one read, then none")
            watch.changed()
            check(len(reads) == 1, "  (no second read while that stat stays put)")

            write(one, "print('b')\n")             # the same size - the bytes, not the size
            later(one, 10)
            check(short(watch.changed(), root) == ["brain/heron_one.py"],
                  "a file whose bytes changed after it was loaded is named, even at the "
                  "same size - found %r" % short(watch.changed(), root))
            check(short(watch.changed(), root) == ["brain/heron_one.py"],
                  "and stays named on the next look")

            write(two, "VALUE = 22\n")
            later(two, 10)
            check(short(watch.changed(), root) == ["brain/heron_one.py", "mcp/server/heron_two.py"],
                  "a second changed file is named beside it")

            write(one, "print('a')\n")
            later(one, 15)
            check(short(watch.changed(), root) == ["mcp/server/heron_two.py"],
                  "bytes put back are the code that was loaded again - no longer named")

            os.remove(two)
            check(short(watch.changed(), root) == ["mcp/server/heron_two.py"],
                  "a file that is gone is a change - it is not the code that was loaded")

            write(two, "VALUE = 1\n")
            later(two, 20)
            check(watch.changed() == [],
                  "and put back as it was, it is not")

            modules["heron_late"] = module_at("heron_late", late)
            write(late, "LATE = 2\n")
            later(late, 5)
            check(watch.changed() == [],
                  "a module loaded after the first look is fingerprinted when it is first "
                  "seen - the moment its code was read - and is not called changed")
            check("brain/heron_late.py" in short(watch.watched(), root),
                  "and is watched from then on")
            write(late, "LATE = 3\n")
            later(late, 10)
            check(short(watch.changed(), root) == ["brain/heron_late.py"],
                  "so a change to it after that is named")
        finally:
            RC._digest = digest
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print()
    print("   what the modeller is told")
    said = getattr(RC, "SAID", "")
    refused = getattr(RC, "REFUSED", "")
    check("changed since this chat started" in said and "close and reopen the chat" in said,
          "the line says what happened and what to do")
    check(NEWLINE not in said and NEWLINE not in refused, "each is one line")
    check("Nothing has been sent to Revit." in refused and "Close and reopen the chat" in refused,
          "the refusal says nothing was sent, in as many words, and what to do")


NEWLINE = chr(10)


def the_checkout():
    print()
    print("2. A watch over the real checkout")
    if RC is None:
        check(False, "there is a watch to put over it")
        return
    import heron_write                                         # noqa: F401
    import heron_session                                       # noqa: F401
    import heron_bridge_client                                 # noqa: F401
    import heron_brain                                         # noqa: F401
    watch = RC.RunningCode.of_checkout(ROOT)
    seen = short(watch.watched(), ROOT)
    for wanted in ("mcp/server/heron_write.py", "mcp/server/heron_session.py",
                   "mcp/server/heron_brain.py", "mcp/server/heron_running_code.py",
                   "mcp/client/heron_bridge_client.py"):
        check(wanted in seen, "it watches %s" % wanted)
    check(seen and all(p.split("/", 1)[0] in RC.FOLDERS for p in seen),
          "and nothing outside Heron's Python folders %r - not this suite, not the "
          "standard library" % (RC.FOLDERS,))
    check(watch.changed() == [], "nothing in a checkout nobody is editing is called changed")


def the_wiring():
    print()
    print("3. The server is wired to it")
    try:
        text = io.open(SERVER, encoding="utf-8").read()
    except OSError as why:
        check(False, "could not read %s: %s" % (SERVER, why))
        return

    def body_of(name):
        start = text.find(NEWLINE + "def %s(" % name)
        if start < 0:
            return ""
        end = text.find(NEWLINE + "def ", start + 1)
        return text[start:end if end > 0 else len(text)]

    check("RunningCode.of_checkout(" in text,
          "the server keeps a watch over its own checkout from the moment it loads")
    recorded = body_of("_recorded")
    through = body_of("_through")
    check(recorded != "" and through != "",
          "_recorded and _through are still where this suite looks for them")
    # Every position is checked for presence before it is compared - heron-ship 2a.
    refuse = recorded.find("_refused_for_old_code(")
    run = recorded.find("fn(*a, **k)")
    check(refuse > 0 and run > 0 and refuse < run,
          "every tool passes the refusal before its own body runs")
    check("_said_old_code(" in recorded, "and every answer is handed to the line")
    refuse = through.find("_refused_for_old_code(")
    run = through.find("body(*a, **k)")
    check(refuse > 0 and run > 0 and refuse < run,
          "the Companion's side door asks the same before a body runs")
    refusal = body_of("_refused_for_old_code")
    check("_changes_model(" in refusal and "def _changes_model" in text
          and "tools.writes(" in body_of("_changes_model"),
          "and which tools are refused is heron_tools.writes, never a typed list")


def main():
    print("The MCP server notices when its own code changes under a chat")
    print("=" * 62)
    the_watch()
    the_checkout()
    the_wiring()
    print()
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASSED - the watch, over a scratch checkout and the real one, and the wiring "
          "as text. The behaviour through the SDK is test_mcp_serves.py section 16.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
