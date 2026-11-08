# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-USR-SKL-003
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
Saved commands (docs/49) - kept, checked, filled and run through the one door.

    python tests/test_commands.py

WHAT IT PROVES
  1. A COMMAND IS CHECKED AGAINST THE FRAGMENT'S OWN CONTRACT WHEN IT IS
     SAVED - a value the fragment does not take, an {input} nobody declared
     and an input no value uses are each refused by name, and nothing is kept.
  2. IT IS KEPT WHOLE, FOUND BY ITS NAME, LISTED, NEVER SILENTLY REPLACED,
     AND A BROKEN FILE IS NAMED RATHER THAN HIDDEN.
  3. FILLING puts the inputs in, falls back to a default, refuses a missing
     one and an unknown one, and quotes a value whose edges are spaces -
     revit_change's own rule.
  4. THE RUNNER NOTE names the Python, the Heron folder and the runner.
  5. THE RUNNER binds the Revit it was given BY CHOICE, runs the command's
     ONE capability through revit_change's or revit_read's own body, hands
     back what it held, and sends nothing when the Revit is gone or the
     command cannot be filled.
  The MCP tool itself is tests/test_commands_tool.py - it needs the MCP SDK,
  and this suite runs everywhere.
"""

from __future__ import print_function

import io
import json
import os
import shutil
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "server"))
sys.path.insert(0, os.path.join(ROOT, "mcp", "client"))

import heron_commands as C                                    # noqa: E402
import heron_command_run as RUN                               # noqa: E402

FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


NEEDS = [{"name": "doc", "type": "Document"},
         {"name": "levelName", "type": "string", "source": "request"},
         {"name": "prefix", "type": "string", "source": "request", "optional": "true"}]


def good():
    return {"name": "Room Sections", "tool": "change", "capability": "create_section_view",
            "values": [{"name": "levelName", "value": "{level}"},
                       {"name": "prefix", "value": "SEC-"}],
            "inputs": [{"name": "level", "label": "Level", "default": "Level 1"}],
            "words": "make sections for every room on level 1"}


def shape(home):
    print("1. checked against the contract when saved")
    check(C.problems(good(), NEEDS) == [], "a good command has no problems")
    bad = good()
    bad["values"].append({"name": "levelNmae", "value": "x"})
    said = " ".join(C.problems(bad, NEEDS))
    check("'levelNmae' is not a value" in said and "levelName" in said,
          "a value the fragment does not take is named, with what it does take")
    bad = good()
    bad["values"][0]["value"] = "{levle}"
    said = " ".join(C.problems(bad, NEEDS))
    check("'{levle}'" in said and "'level' is used by no value" in said,
          "an undeclared {input} and an unused input are both named")
    bad = good()
    bad["inputs"].append({"name": "level"})
    check(any("declared twice" in p for p in C.problems(bad, NEEDS)), "an input twice is refused")
    bad = good()
    bad["tool"] = "delete"
    check(any("'tool'" in p for p in C.problems(bad, NEEDS)), "a tool that is not change/read")
    bad = good()
    bad["name"] = "  "
    check(any("no name" in p for p in C.problems(bad, NEEDS)), "a command with no name")
    bad = good()
    bad["values"][1]["value"] = "two\nlines"
    check(any("more than one line" in p for p in C.problems(bad, NEEDS)), "a value of two lines")
    check(C.problems(good(), None)[-1].startswith("Heron could not read"),
          "an unreadable contract is refused, never passed")
    try:
        C.save(bad, NEEDS, home=home)
        check(False, "a bad command is refused")
    except C.CommandError as why:
        check(str(why).startswith("Not saved"), "a bad command is refused whole")
    check(C.all_commands(home) == ([], []), "and nothing was kept")


def keep(home):
    print("2. kept, found, listed, never silently replaced")
    path = C.save(good(), NEEDS, home=home, now="2026-11-08T10:00:00")
    check(path.endswith(os.path.join("commands", "personal", "room-sections.json")),
          "kept under the data home as its slug")
    with io.open(path, encoding="utf-8") as fh:
        kept = json.load(fh)
    check(kept["heron-command"] == 1 and kept["capability"] == "CREATE_SECTION_VIEW",
          "the format is marked and the capability upper-cased")
    check(not os.path.exists(path + ".part"), "no half-written file is left")
    check(C.find("room sections", home)["name"] == "Room Sections", "found by its name, any case")
    try:
        C.save(good(), NEEDS, home=home)
        check(False, "a second save under the same name is refused")
    except C.CommandError as why:
        check("already saved" in str(why), "a second save under the same name is refused")
    other = good()
    other["description"] = "replaced"
    C.save(other, NEEDS, home=home, replace=True)
    check(C.find("Room Sections", home)["description"] == "replaced", "replace=True replaces it")
    with io.open(os.path.join(C.folder(home), "broken.json"), "w", encoding="utf-8") as fh:
        fh.write("{not json")
    found, broken = C.all_commands(home)
    check([c["name"] for c in found] == ["Room Sections"] and broken == ["broken.json"],
          "a broken file is named, not hidden")
    os.remove(os.path.join(C.folder(home), "broken.json"))
    try:
        C.find("nothing here", home)
        check(False, "an unknown name is refused")
    except C.CommandError as why:
        check("Saved: Room Sections" in str(why), "an unknown name is refused, naming what is saved")
    check(C.slug("Room Sections (L1)") == "room-sections-l1", "the slug reads as the name")


def filling(home):
    print("3. filled")
    command = C.find("Room Sections", home)
    check(C.fill(command, {"level": "Level 2"}) == "levelName=Level 2\nprefix=SEC-",
          "an input given is put in")
    check(C.fill(command, {}) == "levelName=Level 1\nprefix=SEC-", "an input not given takes its default")
    check(C.fill(command, {"level": " L2 "}) == 'levelName=" L2 "\nprefix=SEC-',
          "a value whose edges are spaces is quoted to keep them")
    try:
        C.fill(command, {"levl": "x"})
        check(False, "an unknown input is refused")
    except C.CommandError as why:
        check("no input called levl" in str(why), "an unknown input is refused by name")
    nodefault = dict(command, inputs=[{"name": "level", "label": "Level", "default": ""}])
    try:
        C.fill(nodefault, {})
        check(False, "a missing input with no default is refused")
    except C.CommandError as why:
        check("needs a value for Level" in str(why), "a missing input with no default is refused")
    try:
        C.fill(command, {"level": "a\nb"})
        check(False, "a two-line input is refused")
    except C.CommandError:
        check(True, "a two-line input is refused")


def note(home):
    print("4. the runner note")
    path = C.write_runner_note("C:\\Python312\\python.exe", "C:\\Heron", home=home,
                               now="2026-11-08T10:00:00")
    with io.open(path, encoding="utf-8") as fh:
        kept = json.load(fh)
    check(kept["python"].endswith("python.exe") and kept["root"] == "C:\\Heron"
          and kept["runner"].endswith("heron_command_run.py"),
          "it names the Python, the Heron folder and the runner")
    blocked = os.path.join(home, "a-file")
    with io.open(blocked, "w", encoding="utf-8") as fh:
        fh.write(u"x")
    check(C.write_runner_note("p", "r", home=blocked) is None,
          "never fatal: a home it cannot write in answers None, it does not raise")
    os.remove(blocked)


class _FakeServer(object):
    """The MCP server module's names the runner uses, recording each call."""

    class NotBound(Exception):
        pass

    def __init__(self, home, live=("4242",)):
        self.calls, self.handed_back, self.chosen = [], 0, None
        outer = self

        class brain(object):
            @staticmethod
            def commands():
                class Store(object):
                    CommandError = C.CommandError

                    @staticmethod
                    def find(name):
                        return C.find(name, home)

                    fill = staticmethod(C.fill)
                return Store

        class binding(object):
            @staticmethod
            def choose(pid):
                if pid not in live:
                    raise outer.NotBound("There is no session %s." % pid)
                outer.chosen = pid

        class bridge(object):
            @staticmethod
            def hand_back_models():
                outer.handed_back += 1

        import threading
        self.brain, self.binding, self.bridge = brain, binding, bridge
        self._revit_lock = threading.RLock()

    def revit_change(self, capability, values, request=""):
        self.calls.append(("revit_change", capability, values, request))
        return "%s ran in Model." % capability

    def revit_read(self, capability, values, request=""):
        self.calls.append(("revit_read", capability, values, request))
        return "read"

    def _through(self, tool, origin="chat"):
        self.calls.append(("origin", origin))
        return tool


def runner(home):
    print("5. the runner")
    check(RUN.parse(["--name", "Room Sections", "--revit-pid", "4242", "--set", "level=Level 2"])
          == ("Room Sections", "4242", {"level": "Level 2"}), "the command line is read")
    for argv in (["--name", "x", "--revit-pid", "abc"],
                 ["--name", "x", "--revit-pid", "1", "--set", "novalue"]):
        try:
            RUN.parse(argv)
            check(False, "a bad command line is refused")
        except SystemExit as stop:
            check(stop.code == 2, "a bad command line exits 2: %s" % " ".join(argv[2:]))

    fake = _FakeServer(home)
    said = RUN.run("Room Sections", "4242", {"level": "Level 3"}, server=fake)
    check(fake.chosen == "4242", "the Revit given is chosen explicitly")
    check(("origin", "panel") in fake.calls, "through the tool's own body, labelled the panel's")
    check(("revit_change", "CREATE_SECTION_VIEW", "levelName=Level 3\nprefix=SEC-",
           "Saved command 'Room Sections' from the Heron panel") in fake.calls,
          "ONE call to revit_change with the filled values")
    check(said == "CREATE_SECTION_VIEW ran in Model." and fake.handed_back == 1,
          "the answer is revit_change's own, and the models are handed back")

    gone = _FakeServer(home, live=())
    said = RUN.run("Room Sections", "4242", {}, server=gone)
    check("not connected" in said and not [c for c in gone.calls if c[0].startswith("revit")],
          "a Revit that is gone: nothing is sent")
    bad = _FakeServer(home)
    said = RUN.run("Room Sections", "4242", {"nope": "1"}, server=bad)
    check("Nothing was sent" in said and bad.chosen is None,
          "an input it cannot fill: nothing is sent and no Revit is bound")

    reader = good()
    reader.update(name="Count Rooms", tool="read")
    C.save(reader, NEEDS, home=home)
    fake = _FakeServer(home)
    RUN.run("Count Rooms", "4242", {}, server=fake)
    check([c[0] for c in fake.calls if c[0].startswith("revit")] == ["revit_read"],
          "a read command goes through revit_read, never revit_change")
    check(RUN._client_id(ROOT) == _expected_client_id(),
          "the lease's person is .mcp.json's HERON_CLIENT_ID, the chat's own")


def _expected_client_id():
    try:
        with io.open(os.path.join(ROOT, ".mcp.json"), encoding="utf-8") as fh:
            return json.load(fh)["mcpServers"]["heron"]["env"]["HERON_CLIENT_ID"]
    except (OSError, ValueError, KeyError):
        return None


def main():
    home = tempfile.mkdtemp(prefix="heron-commands-")
    try:
        shape(home)
        keep(home)
        filling(home)
        note(home)
        runner(home)
    finally:
        shutil.rmtree(home, ignore_errors=True)
    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    saved commands are checked when kept and run through the one door")
    return 0


if __name__ == "__main__":
    sys.exit(main())
