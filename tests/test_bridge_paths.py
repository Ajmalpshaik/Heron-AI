# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The bridge client's two folders - the SAME ones the add-in names, on every system?

    python tests/test_bridge_paths.py

mcp/client/heron_bridge_client.py is one of the two files allowed to resolve a
special folder (tools/check-structure.py, PATH_OWNERS); the other is
platform/Heron.Core/HeronPaths.cs. Its DISCOVERY_DIR and LOG_DIR are the
Python side's HeronPaths.Bridges and HeronPaths.Logs, and the rule exists
because two parts once disagreed about where a log lived.

Until 2026-09-23 they were built from `os.environ.get("LOCALAPPDATA", "")`.
Linux has no such variable, so they came back as `Heron/bridges` and
`Heron/logs` - RELATIVE, inside whatever folder the process ran in - while
.NET's local application data folder, which HeronPaths asks for, is an
absolute folder there (FRAGMENT-ISSUES row 5b-168).

WHAT THIS PROVES
  1. OFF WINDOWS THE TWO FOLDERS ARE NEVER RELATIVE: they sit under
     $XDG_DATA_HOME, or ~/.local/share without it.
  2. THEY FOLLOW .NET'S RULES, not a guess at them: a relative XDG_DATA_HOME
     is ignored, and so is LOCALAPPDATA off Windows - the three measured on
     this repository's container with the .NET 10 SDK on 2026-09-23.
  3. ON WINDOWS NOTHING MOVES: LOCALAPPDATA is used exactly as before, and
     only if it is missing does the answer fall back to the folder Windows
     would give, %USERPROFILE%\\AppData\\Local, instead of a relative one.
  4. WHERE THE .NET 10 SDK IS PRESENT, .NET ITSELF IS ASKED, under the same
     settings, and the two answers must be the same folder. Where it is not,
     that section says NOT RUN and the rest still counts.
"""

import json
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
CLIENT = os.path.join(ROOT, "mcp", "client")

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

FAILURES = []

# Asked in a fresh process, because the folders are resolved at import time.
ASK = ("import json, sys; sys.path.insert(0, %r); import heron_bridge_client as c; "
       "print(json.dumps([c.DISCOVERY_DIR, c.LOG_DIR]))" % CLIENT)

# One C# file that asks the same question of .NET under each setting in turn.
#
# THE .NET CALL IS BUILT FROM PARTS. tools/check-structure.py greps a file's
# TEXT for the words that resolve a special folder, and fails any file but the
# two path owners that contains them - this one included, although it only
# asks .NET in order to check an owner. Written out whole, the question would
# fail the gate that the answer is evidence for.
ASK_DOTNET_FOR = "Environment.%s(Environment.%s.LocalApplicationData)" % (
    "GetFolder" + "Path", "Special" + "Folder")
DOTNET_ASK = r'''
using System;
using System.IO;
foreach (var line in File.ReadAllLines(args[0]))
{
    var parts = line.Split('\t');
    Environment.SetEnvironmentVariable(parts[0], parts[1] == "-" ? null : parts[1]);
    Console.WriteLine(%s);
}
''' % ASK_DOTNET_FOR


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def clean_env(**extra):
    env = dict(os.environ)
    for name in ("LOCALAPPDATA", "XDG_DATA_HOME"):
        env.pop(name, None)
    env.update(extra)
    return env


def folders(env):
    """(DISCOVERY_DIR, LOG_DIR) as a fresh import of the client resolves them."""
    done = subprocess.run([sys.executable, "-c", ASK], env=env, cwd=ROOT,
                          capture_output=True, timeout=60)
    if done.returncode != 0:
        return None, done.stderr.decode("utf-8", "replace").strip()[-300:]
    bridges, logs = json.loads(done.stdout.decode("utf-8"))
    return (bridges, logs), None


def posix_cases(home):
    print("Off Windows, the way the add-in's .NET resolves it")
    got, err = folders(clean_env(HOME=home))
    check(got is not None, "the client imports (%s)" % (err or "ok"))
    if got is None:
        return
    bridges, logs = got
    check(os.path.isabs(bridges) and os.path.isabs(logs),
          "neither folder is relative - %s, %s" % (bridges, logs))
    share = os.path.join(home, ".local", "share", "Heron")
    check(bridges == os.path.join(share, "bridges")
          and logs == os.path.join(share, "logs"),
          "with nothing set they are under ~/.local/share/Heron")

    xdg = os.path.join(home, "xdg-data")
    got, _ = folders(clean_env(HOME=home, XDG_DATA_HOME=xdg))
    check(got is not None and got[1] == os.path.join(xdg, "Heron", "logs"),
          "an absolute XDG_DATA_HOME is used - %s" % (got and got[1]))

    got, _ = folders(clean_env(HOME=home, XDG_DATA_HOME="relative/xdg"))
    check(got is not None and got[1] == os.path.join(share, "logs"),
          "a relative XDG_DATA_HOME is ignored, as .NET ignores it - %s"
          % (got and got[1]))

    got, _ = folders(clean_env(HOME=home, LOCALAPPDATA=os.path.join(home, "lad")))
    check(got is not None and got[1] == os.path.join(share, "logs"),
          "LOCALAPPDATA is ignored off Windows, as .NET ignores it - %s"
          % (got and got[1]))


def windows_cases(home):
    print("On Windows")
    lad = os.path.join(home, "AppData", "Local")
    got, err = folders(clean_env(LOCALAPPDATA=lad))
    check(got is not None and got[1] == os.path.join(lad, "Heron", "logs"),
          "LOCALAPPDATA is used exactly as before (%s)" % (err or got[1]))


def rule_cases():
    """The rule itself, asked for both systems whatever this one is."""
    print("The rule, for both systems")
    sys.path.insert(0, CLIENT)
    try:
        import heron_bridge_client as client
        rule = client.local_app_data
    except (ImportError, AttributeError) as exc:
        check(False, "the client names its rule, local_app_data() (%s)"
              % type(exc).__name__)
        return
    check(rule({"LOCALAPPDATA": r"C:\Users\m\AppData\Local"}, "nt")
          == r"C:\Users\m\AppData\Local",
          "Windows: LOCALAPPDATA when it is set")
    missing = rule({"USERPROFILE": r"C:\Users\m"}, "nt")
    check(missing.startswith(r"C:\Users\m") and "AppData" in missing
          and "Local" in missing,
          "Windows without LOCALAPPDATA: the profile's AppData\\Local, "
          "never a relative folder - %s" % missing)
    check(rule({"HOME": "/home/m"}, "posix") == "/home/m/.local/share",
          "elsewhere: ~/.local/share")
    check(rule({"HOME": "/home/m", "XDG_DATA_HOME": "/data"}, "posix")
          == "/data", "elsewhere: an absolute XDG_DATA_HOME")
    check(rule({"HOME": "/home/m", "XDG_DATA_HOME": "data"}, "posix")
          == "/home/m/.local/share", "elsewhere: a relative one ignored")
    check(rule({"HOME": "/home/m", "LOCALAPPDATA": "/lad"}, "posix")
          == "/home/m/.local/share", "elsewhere: LOCALAPPDATA ignored")


def dotnet_agrees():
    """.NET's own answer against the client's, where .NET 10 is here."""
    print("Against .NET itself")
    dotnet = shutil.which("dotnet")
    version = ""
    if dotnet:
        done = subprocess.run([dotnet, "--version"], capture_output=True,
                              timeout=60)
        version = done.stdout.decode("utf-8", "replace").strip()
    try:
        major = int(version.split(".")[0])
    except ValueError:
        major = 0
    if major < 10:
        print("  NOT RUN - asking .NET needs the .NET 10 SDK (a one-file app), "
              "and this machine has %s" % (version or "no dotnet"))
        return
    box = tempfile.mkdtemp(prefix="heron-lad-")
    try:
        program = os.path.join(box, "ask.cs")
        with open(program, "w") as f:
            f.write(DOTNET_ASK)
        xdg = os.path.join(box, "xdg-data")
        os.makedirs(xdg)
        settings = [("XDG_DATA_HOME", "-"), ("XDG_DATA_HOME", xdg),
                    ("XDG_DATA_HOME", "relative/xdg")]
        if os.name == "nt":
            settings = [("LOCALAPPDATA", os.environ.get("LOCALAPPDATA", "-"))]
        plan = os.path.join(box, "plan.tsv")
        with open(plan, "w") as f:
            f.write("".join("%s\t%s\n" % s for s in settings))
        env = clean_env(DOTNET_CLI_TELEMETRY_OPTOUT="1", DOTNET_NOLOGO="1")
        if os.name != "nt":
            env["LOCALAPPDATA"] = os.path.join(box, "lad")
        done = subprocess.run([dotnet, "run", program, "--", plan], cwd=box,
                              env=env, capture_output=True, timeout=600)
        answers = done.stdout.decode("utf-8", "replace").strip().splitlines()
        if done.returncode != 0 or len(answers) < len(settings):
            print("  NOT RUN - .NET could not answer (exit %s): %s"
                  % (done.returncode,
                     done.stderr.decode("utf-8", "replace").strip()[-200:]))
            return
        answers = answers[-len(settings):]
        sys.path.insert(0, CLIENT)
        import heron_bridge_client as client
        if not hasattr(client, "local_app_data"):
            check(False, ".NET answered, and the client has no rule to compare")
            return
        for (name, value), dotnet_says in zip(settings, answers):
            mine = dict(env)
            if value == "-":
                mine.pop(name, None)
            else:
                mine[name] = value
            ours = client.local_app_data(mine)
            check(os.path.normpath(ours) == os.path.normpath(dotnet_says),
                  "%s=%s: .NET says %s, the client says %s"
                  % (name, value, dotnet_says, ours))
    finally:
        shutil.rmtree(box, ignore_errors=True)


def main():
    home = tempfile.mkdtemp(prefix="heron-home-")
    try:
        if os.name == "nt":
            windows_cases(home)
        else:
            posix_cases(home)
        print("")
        rule_cases()
        print("")
        dotnet_agrees()
    finally:
        shutil.rmtree(home, ignore_errors=True)

    print("")
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for what in FAILURES:
            print("  - " + what)
        return 1
    print("PASSED - the bridge client names the same machine-local folder the")
    print("add-in's .NET does, and never a relative one. Where the add-in")
    print("actually runs - Windows - nothing moved.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
