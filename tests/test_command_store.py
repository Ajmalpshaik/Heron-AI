# -*- coding: utf-8 -*-
# Heron-Agent:  HERON-USR-SKL-003
# Heron-Step:   18
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The Heron panel reads what the chat saved, and passes the inputs whole.

    python tests/test_command_store.py

WHAT IT PROVES (docs/49)
  1. ONE FILE FORMAT, TWO LANGUAGES. A command saved by the PYTHON side
     (brain/heron_commands.py) is read by the panel's C#
     (HeronCommandStore.cs) with every field the panel shows - name,
     capability, change or read, description and each input's name, label
     and default. A file of another format, or not JSON, is read as nothing.
  2. AN INPUT ARRIVES WHOLE. The command line the panel builds for the
     runner, split the way Windows splits one, gives back exactly the
     arguments it was built from - spaces, quotes, trailing backslashes and
     an empty value included. A level called `Level "A"` that arrived as
     two arguments would run on the wrong level or not at all.

HOW IT RUNS WITHOUT REVIT: tests/Heron.CommandStore.TestHost links
HeronCommandStore.cs, Json.cs and HeronPaths.cs by source. It exits 3 when
no .NET SDK and runtime meet - NOT a pass.
"""

from __future__ import print_function

import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "brain"))
sys.path.insert(0, os.path.join(ROOT, "tests"))

import heron_commands as C                                    # noqa: E402
import test_binding_note as NOTE                              # noqa: E402

HOST = os.path.join(ROOT, "tests", "Heron.CommandStore.TestHost")
COULD_NOT_RUN = 3
FAILURES = []


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def windows_split(line):
    """How the Microsoft C runtime splits a command line into arguments -
    the oracle the panel's quoting is held to. Backslashes count only before
    a quote: 2n then a quote give n and toggle quoting, 2n+1 give n and a
    literal quote."""
    args, current, quoted, have, i = [], [], False, False, 0
    while i < len(line):
        c = line[i]
        if c == "\\":
            run = 0
            while i < len(line) and line[i] == "\\":
                run += 1
                i += 1
            if i < len(line) and line[i] == '"':
                current.append("\\" * (run // 2))
                if run % 2:
                    current.append('"')
                    i += 1
                have = True
            else:
                current.append("\\" * run)
                have = True
            continue
        if c == '"':
            quoted = not quoted
            have = True
        elif c in " \t" and not quoted:
            if have:
                args.append("".join(current))
                current, have = [], False
        else:
            current.append(c)
            have = True
        i += 1
    if have:
        args.append("".join(current))
    return args


def main():
    tfm = NOTE._tfm()
    if tfm is None:
        print("NOT RUN - no .NET SDK and runtime meet on this machine")
        return COULD_NOT_RUN
    work = tempfile.mkdtemp(prefix="heron-command-store-")
    try:
        out_dir = os.path.join(work, "bin") + os.sep
        built = subprocess.run(["dotnet", "build", HOST, "-p:RevitVersion=2024",
                                "-p:HeronTfm=%s" % tfm, "-p:OutputPath=%s" % out_dir],
                               stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
        if built.returncode != 0:
            print(built.stdout.decode("utf-8", "replace")[-3000:])
            print("FAILED  the test host did not build")
            return 1
        dll = os.path.join(out_dir, "Heron.CommandStore.TestHost.dll")

        def host(*args):
            done = subprocess.run(["dotnet", dll] + list(args),
                                  stdout=subprocess.PIPE, stderr=subprocess.PIPE)
            return done.stdout.decode("utf-8").splitlines()

        print("1. what the chat saves, the panel reads")
        home = os.path.join(work, "home")
        command = {"name": u"Room Sections é", "tool": "change", "capability": "CREATE_SECTION_VIEW",
                   "description": u"Sections for every room - \"quoted\"",
                   "values": [{"name": "levelName", "value": "{level}"},
                              {"name": "prefix", "value": "{prefix}"}],
                   "inputs": [{"name": "level", "label": "Level", "default": "Level 1"},
                              {"name": "prefix", "label": "Name start", "default": ""}]}
        needs = [{"name": "levelName", "source": "request"}, {"name": "prefix", "source": "request"}]
        path = C.save(command, needs, home=home)
        said = host("parse", path)
        check(said[:4] == [u"name=Room Sections é", "capability=CREATE_SECTION_VIEW", "changes=yes",
                           u"description=Sections for every room - \"quoted\""],
              "name, capability, change and description - accents and quotes included")
        check(said[4:] == ["input=level|Level|Level 1", "input=prefix|Name start|"],
              "each input's name, label and default, in order, an empty default kept")

        reader = dict(command, name="Count", tool="read", inputs=[], values=[])
        said = host("parse", C.save(reader, needs, home=home))
        check("changes=no" in said and not [l for l in said if l.startswith("input=")],
              "a read command is read as a read, with no inputs")
        other = os.path.join(work, "other.json")
        with open(other, "w") as fh:
            fh.write('{"heron-command": 2, "name": "x", "capability": "Y", "tool": "change"}')
        check(host("parse", other) == ["null"], "a file of another format is read as nothing")
        with open(other, "w") as fh:
            fh.write("{not json")
        check(host("parse", other) == ["null"], "a file that is not JSON is read as nothing")

        print("2. an input arrives whole")
        cases = [
            ["C:\\Heron\\mcp\\server\\heron_command_run.py", "--name", "Room Sections"],
            ["--set", 'level=Level "A"'],
            ["--set", "path=C:\\Some Folder\\"],
            ["--set", "trail=ends\\\\"],
            ["--set", "empty="],
            ["--set", ""],
            ["--set", 'mixed=a\\"b c\\\\"'],
            ["--set", "tab=a\tb"],
        ]
        for args in cases:
            line = host("quote", *args)
            got = windows_split(line[0]) if line else None
            check(got == args, "%r -> %s" % (args, line[0] if line else "nothing"))
    finally:
        shutil.rmtree(work, ignore_errors=True)

    print()
    if FAILURES:
        print("FAILED  %d check(s)" % len(FAILURES))
        for line in FAILURES:
            print("  - %s" % line)
        return 1
    print("PASS    the panel reads what the chat saved, and passes each input whole")
    return 0


if __name__ == "__main__":
    sys.exit(main())
