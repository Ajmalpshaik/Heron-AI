# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   15
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

r"""
Heron's Python-side DATA lives where a Store Claude's private copy cannot reach it (D-135).

    python tests/test_data_home.py

WHY THIS EXISTS
---------------
On a PC whose Claude app is the Microsoft Store build, Windows keeps a private
copy of %APPDATA% and %LOCALAPPDATA% for the app and every process it starts -
Claude's shell and every heron MCP server - and a Reset or uninstall of the app
deletes it (rows 5b-416 to 5b-419). Measured on the owner's laptop on
2026-10-17: once a folder exists in BOTH the copy and the real disk, a new file,
a new folder, an edit to a file both hold, and a write-then-rename all land in
the COPY - and the copy already held Heron\knowledge, Heron\audit and
Heron\config. Only a folder outside AppData is out of its reach: the user's
profile and the temporary folder were seen real from both sides the same night.

WHAT IT PROVES
  1. THE HOME. `heron_paths.data_home()` is HERON_DATA when set; on Windows
     %USERPROFILE%\.heron - never under %APPDATA% or %LOCALAPPDATA%; anywhere
     else nothing, and a planted %APPDATA% alone no longer names one.
  2. THE KNOWLEDGE FOLLOWS IT. `heron_scope.knowledge_dir()` and `shared_dir()`
     are <home>\knowledge; HERON_KNOWLEDGE still wins for the first only.
  3. THE BRAIN'S AUDIT FOLLOWS IT, AND THE GAPS REPORT IS NOT BLINDED. The
     brain writes into <home>\audit; the add-in's trail is still read where
     the add-in writes it, %APPDATA%\Heron\audit; `heron_gaps.read()` returns
     both, and a line found in both places once. HERON_AUDIT still names one
     folder for both, which is what every other suite plants.
  4. WHAT WAS THERE IS CARRIED, NEVER MOVED. `carry_forward` copies the
     knowledge and the brain's audit from the old places into the home -
     the app's copy first, since that is what the chats were using - and
     deletes nothing: every source reads the same afterwards. A file the home
     already holds differently is left alone and named; an audit file is
     merged line by line, and a second carry adds nothing.
  5. IT HAPPENS ONCE, BY ITSELF, AND ONLY FOR THE DEFAULT HOME. The first
     `home()` on a PC carries and leaves a note saying what it did; the next
     does nothing; a HERON_DATA folder is somebody's own and is never filled.
  6. THE COMPANION'S NOTE IS WHERE REVIT LOOKS. The chat writes it under the
     temporary folder as .NET names it, and HeronPaths.cs builds
     HeronPaths.Companion from Path.GetTempPath() - not from %LOCALAPPDATA%,
     which the app copies (row 5b-416, D-109).
"""

import hashlib
import io
import json
import ntpath
import os
import re
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

import heron_paths as PATHS                                    # noqa: E402
import heron_scope as SCOPE                                    # noqa: E402
import heron_gaps as GAPS                                      # noqa: E402
import heron_audit as AUDIT                                    # noqa: E402
import heron_bridge_client as BRIDGE                           # noqa: E402

FAILURES = []
PACKAGE = "AnthropicPBC.Claude_fnn82j28hfe8t"
PATHS_CS = os.path.join(ROOT, "platform", "Heron.Core", "HeronPaths.cs")
COMPANION_PY = os.path.join(ROOT, "mcp", "companion")


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def ask(module, name, *args, **kwargs):
    """The function's answer, or the exception it raised - so a missing
    function is one FAIL rather than the end of the suite."""
    try:
        return getattr(module, name)(*args, **kwargs)
    except Exception as error:                     # noqa: BLE001 - reported as a FAIL
        return error


class planted(object):
    """os.environ with these keys set (None removes one), put back after."""

    KEYS = ("HERON_DATA", "HERON_KNOWLEDGE", "HERON_AUDIT", "APPDATA",
            "LOCALAPPDATA", "USERPROFILE")

    def __init__(self, **values):
        self.values = values

    def __enter__(self):
        self.saved = dict((key, os.environ.get(key)) for key in self.KEYS)
        for key in self.KEYS:
            os.environ.pop(key, None)
        for key, value in self.values.items():
            if value is not None:
                os.environ[key] = value
        return self

    def __exit__(self, *exc):
        for key, value in self.saved.items():
            if value is None:
                os.environ.pop(key, None)
            else:
                os.environ[key] = value


def write(path, text):
    folder = os.path.dirname(path)
    if not os.path.isdir(folder):
        os.makedirs(folder)
    with io.open(path, "w", encoding="utf-8", newline="") as handle:
        handle.write(text)


def read(path):
    with io.open(path, encoding="utf-8", newline="") as handle:
        return handle.read()


def snapshot(folder):
    """{relative path: sha256} of every file under a folder."""
    found = {}
    for here, _dirs, files in os.walk(folder):
        for name in files:
            path = os.path.join(here, name)
            with open(path, "rb") as handle:
                found[os.path.relpath(path, folder)] = hashlib.sha256(handle.read()).hexdigest()
    return found


def same(a, b):
    """One folder by two spellings. Anything that is not a path - None, or the
    exception a missing function raised - is never the same as one."""
    if not isinstance(a, str) or not isinstance(b, str) or not a or not b:
        return False
    return os.path.normcase(os.path.abspath(a)) == os.path.normcase(os.path.abspath(b))


def line(at, op, **fields):
    row = {"at": at, "workflow": "", "op": op, "ok": True}
    row.update(fields)
    return json.dumps(row, sort_keys=True) + "\n"


def plant_old_places(top):
    """A PC as the laptop was: the app's copy and the real AppData, each with
    a Heron folder. Returns (environ, the copy's Heron folder, the real one)."""
    roaming = os.path.join(top, "profile", "AppData", "Roaming")
    local = os.path.join(top, "profile", "AppData", "Local")
    copy = os.path.join(local, "Packages", PACKAGE, "LocalCache", "Roaming", "Heron")
    real = os.path.join(roaming, "Heron")
    # The copy: the store the chats used, a project's memory, the brain's
    # audit, and the add-in's settings it shadowed.
    write(os.path.join(copy, "knowledge", "global.db"), "store-from-the-copy")
    write(os.path.join(copy, "knowledge", "projects", "p1.hvac.json"), "{\"from\": \"copy\"}")
    write(os.path.join(copy, "knowledge", "projects", "p1.loads", "run-1.json"), "run one")
    write(os.path.join(copy, "audit", "audit-brain-202610.jsonl"),
          line("2026-10-01T10:00:00.000Z", "lookup") + line("2026-10-02T10:00:00.000Z", "resolve"))
    write(os.path.join(copy, "config", "heron.config"), "{\"copy\": true}")
    # The real folder: the installer's store, the add-in's own trail, a brain
    # line written from outside the app, and the add-in's settings.
    write(os.path.join(real, "knowledge", "global.db"), "store-from-the-installer")
    write(os.path.join(real, "knowledge", "projects", "p2.hvac.json"), "{\"from\": \"real\"}")
    write(os.path.join(real, "audit", "audit-202610.jsonl"),
          line("2026-10-03T10:00:00.000Z", "move_elements", session="2024/1"))
    write(os.path.join(real, "audit", "audit-brain-202610.jsonl"),
          line("2026-10-02T10:00:00.000Z", "resolve")      # the same line as the copy's
          + line("2026-10-04T10:00:00.000Z", "check"))
    write(os.path.join(real, "config", "heron.config"), "{\"real\": true}")
    environ = {"APPDATA": roaming, "LOCALAPPDATA": local,
               "USERPROFILE": os.path.join(top, "profile")}
    return environ, copy, real


def home_of(environ):
    """`PATHS.home(environ, "nt")` for a planted PC, with `data_home()` answering
    the planted profile's .heron for that one call (row 5b-1071).

    The planted profile is a folder of this machine, and off Windows it has no
    drive. From Python 3.13 `ntpath.isabs()` says such a path is not a whole
    one, so the real `data_home(env, "nt")` rightly answers None and `home()`
    had nothing to carry into - six FAILs on 3.13 that 3.11 and 3.12 never
    showed. `data_home()`'s own Windows rule is section 1's, on Windows
    spellings; here the subject is the carry."""
    was = PATHS.data_home
    PATHS.data_home = lambda env=None, system=None: os.path.join(
        environ["USERPROFILE"], getattr(PATHS, "HOME_FOLDER", ".heron"))
    try:
        return ask(PATHS, "home", environ, "nt")
    finally:
        PATHS.data_home = was


def main():
    yard = tempfile.mkdtemp(prefix="heron-data-home-")
    try:
        return run(yard)
    finally:
        shutil.rmtree(yard, ignore_errors=True)


def run(yard):
    print("1. Where the home is")
    got = ask(PATHS, "data_home", {"HERON_DATA": os.path.join(yard, "mine"),
                                   "USERPROFILE": "C:\\Users\\someone"}, "nt")
    check(same(got, os.path.join(yard, "mine")),
          "HERON_DATA names it, on any system (%r)" % (got,))
    windows = {"USERPROFILE": "C:\\Users\\someone",
               "APPDATA": "C:\\Users\\someone\\AppData\\Roaming",
               "LOCALAPPDATA": "C:\\Users\\someone\\AppData\\Local"}
    got = ask(PATHS, "data_home", windows, "nt")
    check(isinstance(got, str) and ntpath.normcase(got) == ntpath.normcase(
              "C:\\Users\\someone\\.heron"),
          "on Windows it is %%USERPROFILE%%\\.heron (%r)" % (got,))
    check(isinstance(got, str) and "appdata" not in got.lower(),
          "and never under %APPDATA% or %LOCALAPPDATA%, which a Store Claude copies")
    got = ask(PATHS, "data_home", {"APPDATA": os.path.join(yard, "appdata")}, "posix")
    check(got is None,
          "anywhere else there is none - a planted %%APPDATA%% alone no longer "
          "names one (%r)" % (got,))
    got = ask(PATHS, "data_home", {}, "nt")
    check(got is None, "and Windows with no profile has none rather than a guess (%r)" % (got,))
    got = ask(PATHS, "data_home", {"USERPROFILE": "not-absolute"}, "nt")
    check(got is None,
          "nor with a profile that is not a whole path, which would put the home "
          "inside whatever folder the process started in (%r)" % (got,))

    print("\n2. The knowledge folder follows it")
    home = os.path.join(yard, "home-2")
    with planted(HERON_DATA=home, APPDATA=os.path.join(yard, "appdata-2")):
        check(same(SCOPE.knowledge_dir(), os.path.join(home, "knowledge")),
              "knowledge_dir() is <home>\\knowledge (%r)" % (SCOPE.knowledge_dir(),))
        check(same(SCOPE.shared_dir(), os.path.join(home, "knowledge")),
              "and so is shared_dir(), the store every checkout shares (%r)"
              % (SCOPE.shared_dir(),))
        os.environ["HERON_KNOWLEDGE"] = os.path.join(yard, "private")
        check(same(SCOPE.knowledge_dir(), os.path.join(yard, "private"))
              and same(SCOPE.shared_dir(), os.path.join(home, "knowledge")),
              "HERON_KNOWLEDGE still names a private store, and the shared one stays put")
        check(not os.path.exists(os.path.join(home, PATHS_MARKER())),
              "a HERON_DATA folder is somebody's own: nothing was carried into it")
    with planted(APPDATA=os.path.join(yard, "appdata-2")):
        check(SCOPE.knowledge_dir() is None,
              "with no HERON_DATA and no profile, a planted %%APPDATA%% alone gives no "
              "knowledge folder (%r)" % (SCOPE.knowledge_dir(),))
        check(SCOPE.shared_dir() is None, "nor a shared one (%r)" % (SCOPE.shared_dir(),))

    print("\n3. The brain's audit follows it, and the gaps report still reads both")
    appdata = os.path.join(yard, "appdata-3")
    home = os.path.join(yard, "home-3")
    with planted(HERON_DATA=home, APPDATA=appdata):
        check(same(ask(GAPS, "brain_audit_dir"), os.path.join(home, "audit")),
              "brain_audit_dir() is <home>\\audit (%r)" % (ask(GAPS, "brain_audit_dir"),))
        check(same(GAPS.audit_dir(), os.path.join(appdata, "Heron", "audit")),
              "audit_dir() is still the add-in's trail, where the add-in writes it")
        target = AUDIT.current_file()
        check(bool(target) and same(os.path.dirname(target), os.path.join(home, "audit")),
              "the brain writes into the home (%r)" % (target,))
        check(AUDIT.record("lookup", True, numbers={"ms": 3}) is True,
              "and a line is written there")
        write(os.path.join(appdata, "Heron", "audit", "audit-202610.jsonl"),
              line("2026-10-05T10:00:00.000Z", "move_elements"))
        # A brain file left in the add-in's folder by an older Heron, one of
        # whose lines was carried into the home already.
        shared_line = line("2026-10-06T10:00:00.000Z", "resolve")
        write(os.path.join(appdata, "Heron", "audit", "audit-brain-202610.jsonl"),
              shared_line + line("2026-10-07T10:00:00.000Z", "check"))
        with io.open(target, "a", encoding="utf-8", newline="") as handle:
            handle.write(shared_line)
        entries, _skipped = GAPS.read()
        ops = sorted(entry.get("op") for entry in entries)
        check(ops == ["check", "lookup", "move_elements", "resolve"],
              "read() returns the add-in's trail AND the brain's, a line held in "
              "both places once (%s)" % ", ".join(str(op) for op in ops))
    with planted(HERON_DATA=home, APPDATA=appdata, HERON_AUDIT=os.path.join(yard, "one-folder")):
        check(same(ask(GAPS, "brain_audit_dir"), os.path.join(yard, "one-folder"))
              and same(GAPS.audit_dir(), os.path.join(yard, "one-folder")),
              "HERON_AUDIT still names ONE folder for both, as every other suite plants it")

    print("\n4. What was there is carried, never moved")
    environ, copy, real = plant_old_places(os.path.join(yard, "pc-4"))
    before_copy, before_real = snapshot(copy), snapshot(real)
    home = os.path.join(yard, "pc-4", "profile", ".heron")
    write(os.path.join(home, "knowledge", "projects", "p2.hvac.json"), "{\"from\": \"home\"}")
    sources = ask(PATHS, "old_homes", environ)
    check(isinstance(sources, list) and len(sources) == 2
          and same(sources[0], copy) and same(sources[1], real),
          "the old places are the app's copy first, then %%APPDATA%%\\Heron (%r)" % (sources,))
    report = ask(PATHS, "carry_forward", home, sources if isinstance(sources, list) else [copy, real])
    check(isinstance(report, dict), "carry_forward answers with what it did (%r)"
          % (report if not isinstance(report, dict) else "a report",))
    report = report if isinstance(report, dict) else {}
    after = snapshot(home)
    check(read(os.path.join(home, "knowledge", "global.db")) == "store-from-the-copy"
          if os.path.isfile(os.path.join(home, "knowledge", "global.db")) else False,
          "the store the chats used - the copy's - is the one carried")
    check(os.path.join("knowledge", "projects", "p1.loads", "run-1.json") in after
          and os.path.join("knowledge", "projects", "p1.hvac.json") in after,
          "a project's memory is carried, folders and all")
    check(read(os.path.join(home, "knowledge", "projects", "p2.hvac.json")) == "{\"from\": \"home\"}",
          "a file the home already holds differently is left as the home has it")
    left = " ".join(str(item) for item in report.get("left", []))
    check("p2.hvac.json" in left, "and the one not carried is named in the report (%s)" % left)
    brain = os.path.join(home, "audit", "audit-brain-202610.jsonl")
    held = read(brain).splitlines() if os.path.isfile(brain) else []
    check(sorted(json.loads(row)["op"] for row in held) == ["check", "lookup", "resolve"],
          "the brain's audit from both places is merged, each line once (%d lines)" % len(held))
    check(not os.path.exists(os.path.join(home, "audit", "audit-202610.jsonl")),
          "the add-in's own trail is not carried: it stays where the add-in writes and reads it")
    check(not os.path.exists(os.path.join(home, "config")),
          "nor the add-in's settings (Q-70)")
    check(snapshot(copy) == before_copy and snapshot(real) == before_real,
          "and NOTHING in either old place changed - carried means copied, never moved")
    again = ask(PATHS, "carry_forward", home, [copy, real])
    check(snapshot(home) == after,
          "a second carry adds nothing (%r)" % ({k: again.get(k) for k in ("copied", "lines")}
                                               if isinstance(again, dict) else again,))

    print("\n5. It happens once, by itself, and only for the default home")
    environ, copy, real = plant_old_places(os.path.join(yard, "pc-5"))
    home = os.path.join(yard, "pc-5", "profile", ".heron")
    got = home_of(environ)
    check(same(got, home), "home() is the default home (%r)" % (got,))
    note = os.path.join(home, PATHS_MARKER())
    check(os.path.isfile(note), "the first home() carried, and left a note saying so")
    check(os.path.isfile(os.path.join(home, "knowledge", "projects", "p1.hvac.json")),
          "with the copy's project memory in it")
    noted = json.loads(read(note)) if os.path.isfile(note) else {}
    check(bool(noted.get("runs")) and noted["runs"][0].get("sources"),
          "the note names the places it carried from")
    write(os.path.join(copy, "knowledge", "projects", "p9.hvac.json"), "written later")
    PATHS._carried_here.clear() if hasattr(PATHS, "_carried_here") else None
    home_of(environ)
    check(not os.path.exists(os.path.join(home, "knowledge", "projects", "p9.hvac.json")),
          "the next home() carries nothing more - the note says it was done")
    # TWO CHATS AT ONCE: the second waits for the first's carry, and a lock left
    # by a process that died is taken over rather than blocking every chat.
    environ, copy, real = plant_old_places(os.path.join(yard, "pc-5b"))
    home = os.path.join(yard, "pc-5b", "profile", ".heron")
    lock = os.path.join(home, getattr(PATHS, "CARRYING_LOCK", "carrying.lock"))
    write(lock, "4242")
    old = getattr(PATHS, "WAIT_SECONDS", None)
    PATHS.WAIT_SECONDS = 0.5
    PATHS._carried_here.clear() if hasattr(PATHS, "_carried_here") else None
    home_of(environ)
    check(not os.path.isfile(os.path.join(home, PATHS_MARKER())),
          "a carry another process holds is not run a second time beside it")
    import threading

    def finish():
        write(os.path.join(home, PATHS_MARKER()), json.dumps({"format": "1", "runs": [{"how": "the other"}]}))
        os.remove(lock)
    PATHS.WAIT_SECONDS = 10
    PATHS._carried_here.clear() if hasattr(PATHS, "_carried_here") else None
    other = threading.Timer(0.6, finish)
    other.start()
    home_of(environ)
    done_when_answered = os.path.isfile(os.path.join(home, PATHS_MARKER()))
    other.join()
    noted = json.loads(read(os.path.join(home, PATHS_MARKER())))
    check(done_when_answered,
          "the second chat WAITED: the first's carry was finished when it was answered")
    check(len(noted.get("runs", [])) == 1 and noted["runs"][0].get("how") == "the other"
          and not os.path.exists(os.path.join(home, "knowledge")),
          "and it did not carry again (%d run(s) noted)" % len(noted.get("runs", [])))
    os.remove(os.path.join(home, PATHS_MARKER()))
    write(lock, "4242")
    gone = time_ago(getattr(PATHS, "LOCK_SECONDS", 600) + 5)
    os.utime(lock, (gone, gone))
    PATHS._carried_here.clear() if hasattr(PATHS, "_carried_here") else None
    home_of(environ)
    check(os.path.isfile(os.path.join(home, PATHS_MARKER()))
          and os.path.isfile(os.path.join(home, "knowledge", "projects", "p1.hvac.json"))
          and not os.path.exists(lock),
          "a lock left by a process that died is taken over, and the carry runs")
    if old is not None:
        PATHS.WAIT_SECONDS = old

    # ASKING IS NOT CARRYING. A suite or a tool that only looks a folder up
    # must never start a copy on somebody's PC - only the doors do: the MCP
    # server at start, before it opens the store, and heron-ready.
    environ, copy, real = plant_old_places(os.path.join(yard, "pc-5c"))
    home = os.path.join(yard, "pc-5c", "profile", ".heron")
    was = PATHS.data_home
    PATHS.data_home = lambda environ=None, system=None: home
    try:
        with planted(APPDATA=environ["APPDATA"], LOCALAPPDATA=environ["LOCALAPPDATA"]):
            asked = (SCOPE.knowledge_dir(), SCOPE.shared_dir(), ask(GAPS, "brain_audit_dir"))
    finally:
        PATHS.data_home = was
    check(same(asked[0], os.path.join(home, "knowledge"))
          and same(asked[2], os.path.join(home, "audit")) and not os.path.exists(home),
          "knowledge_dir(), shared_dir() and brain_audit_dir() name the default home "
          "and carry nothing into it (%s)" % ("made" if os.path.exists(home) else "not made"))
    server = read(os.path.join(ROOT, "mcp", "server", "heron_mcp_server.py"))
    main_block = server[server.find('if __name__ == "__main__":'):]
    carry_at, warm_at = main_block.find("brain.carry_home()"), main_block.find("brain.warm_store()")
    check(carry_at != -1 and warm_at != -1 and carry_at < warm_at,
          "the MCP server carries at start, before it opens the store")
    check("BRAIN.carry_home()" in read(os.path.join(ROOT, "tools", "heron-ready.py")),
          "and heron-ready carries before it builds the store")

    explicit = dict(environ, HERON_DATA=os.path.join(yard, "pc-5", "chosen"))
    got = ask(PATHS, "home", explicit, "nt")
    check(same(got, os.path.join(yard, "pc-5", "chosen"))
          and not os.path.exists(os.path.join(yard, "pc-5", "chosen", "knowledge")),
          "a HERON_DATA folder is never filled from the old places")

    print("\n6. The Companion's note is where Revit looks")
    got = ask(BRIDGE, "temp_folder", {"TMP": "C:\\T1", "TEMP": "C:\\T2",
                                      "USERPROFILE": "C:\\Users\\someone"}, "nt")
    check(got == "C:\\T1", "temp_folder() asks TMP first on Windows, as GetTempPath does (%r)" % (got,))
    got = ask(BRIDGE, "temp_folder", {"TEMP": "C:\\T2", "USERPROFILE": "C:\\Users\\someone"}, "nt")
    check(got == "C:\\T2", "then TEMP (%r)" % (got,))
    got = ask(BRIDGE, "temp_folder", {"USERPROFILE": "C:\\Users\\someone"}, "nt")
    check(got == "C:\\Users\\someone", "then the profile (%r)" % (got,))
    got = ask(BRIDGE, "temp_folder", {"TMPDIR": "/var/tmp/x"}, "posix")
    check(got == "/var/tmp/x", "elsewhere TMPDIR, as .NET reads it (%r)" % (got,))
    got = ask(BRIDGE, "temp_folder", {}, "posix")
    check(got == "/tmp", "and /tmp without it (%r)" % (got,))
    sys.path.insert(0, COMPANION_PY)
    try:
        import heron_companion as COMPANION
        folder = COMPANION.companion_dir()
    except Exception as error:                     # noqa: BLE001 - reported
        folder = error
    check(isinstance(folder, str) and same(folder, os.path.join(
              BRIDGE.temp_folder() if hasattr(BRIDGE, "temp_folder") else "?",
              "Heron", "companion")),
          "the chat writes its note in <temp>\\Heron\\companion (%r)" % (folder,))
    check(isinstance(folder, str) and not folder.lower().startswith(
              BRIDGE.local_app_data().lower() + os.sep + "heron"),
          "not in %LOCALAPPDATA%\\Heron, which the app copies")
    text = read(PATHS_CS)
    body = re.search(r"public static string Companion\s*\{(.*?)\}\s*\n", text, re.S)
    body = body.group(1) if body else ""
    check("GetTempPath" in body and "Derived" not in body,
          "HeronPaths.Companion is built from Path.GetTempPath(), not from DERIVED (%s)"
          % " ".join(body.split())[:120])

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for what in FAILURES:
            print("  - %s" % what)
        return 1
    print("PASSED - the home, the two readers, the carry and the Companion's folder")
    return 0


def time_ago(seconds):
    import time
    return time.time() - seconds


def PATHS_MARKER():
    """The note's file name, read off the module so the suite follows it."""
    return getattr(PATHS, "CARRIED_NOTE", "carried.json")


if __name__ == "__main__":
    sys.exit(main())
