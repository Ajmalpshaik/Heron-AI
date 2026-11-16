# Heron-Agent:  none
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
Heron's folder list for the install scripts, written by the add-in's own path
code - Q-58, decided 2026-11-15.

    python tools/heron-folders.py                    check the committed list
    python tools/heron-folders.py --write            regenerate it from HeronPaths
    python tools/heron-folders.py --against FILE     check another copy instead
    python tools/heron-folders.py --write --to FILE  write another copy instead

WHY IT EXISTS
-------------
platform/README.md rule 3 says HeronPaths is the only thing that builds a
Heron path. The install scripts needed two - the Revit Addins folder and the
install backup - and a .ps1 cannot call a C# class, and runs before any Heron
assembly is on the PC. So the code writes the list and the scripts read it:
tests/Heron.Folders.TestHost compiles HeronPaths.cs and prints what
platform/heron-folders.json should say, this writes or compares it, and
tools/HeronRevit.ps1 reads it.

THE CHECK IS THE POINT. A list nobody compares with the code is a second
owner of the path, which is the defect Q-58 closes - so a hand edit fails
here, and tests/test_heron_folders.py runs this on every suite run.

Exit 0 the list is what the code says (or was written); 1 it differs, is
missing, or the host would not build or answer - each printed with what to
do; 3 NOT RUN, no .NET SDK and runtime meet on this machine, so the code was
not asked and nothing is claimed.
"""

import argparse
import difflib
import io
import os
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HOST = os.path.join(ROOT, "tests", "Heron.Folders.TestHost")
LIST = os.path.join(ROOT, "platform", "heron-folders.json")
COULD_NOT_RUN = 3


def _asked(what):
    """`dotnet <what>`, or None when dotnet cannot answer at all."""
    try:
        out = subprocess.check_output(["dotnet", what], stderr=subprocess.STDOUT)
    except (OSError, subprocess.CalledProcessError):
        return None
    return out.decode("utf-8", "replace")


def _majors(said, prefix=None):
    """The major version of every line dotnet listed, lowest first."""
    out = []
    for line in (said or "").splitlines():
        parts = line.split()
        if prefix:
            if not line.startswith(prefix):
                continue
            parts = parts[1:]
        if not parts:
            continue
        try:
            out.append(int(parts[0].split(".")[0]))
        except ValueError:
            continue
    return sorted(set(out))


def _tfm():
    """The newest framework an installed SDK can build AND a runtime can run.

    The same rule tests/test_kernel.py keeps: only an SDK builds and only a
    runtime runs, and asking either alone picks a target that cannot be both.
    None when none meet.
    """
    runtimes = _majors(_asked("--list-runtimes"), "Microsoft.NETCore.App")
    sdks = _majors(_asked("--list-sdks"))
    if not runtimes or not sdks:
        return None
    usable = [m for m in runtimes if m <= max(sdks)]
    return "net%d.0" % max(usable) if usable else None


def build_host(work):
    """(0, the host's dll) or (1 or 3, why). Built into `work`, never the tree."""
    tfm = _tfm()
    if tfm is None:
        return COULD_NOT_RUN, ("no .NET SDK and runtime meet on this machine, so "
                               "HeronPaths could not be asked. Linux: apt-get "
                               "install -y dotnet-sdk-10.0")
    out_dir = os.path.join(work, "bin") + os.sep
    built = subprocess.run(
        ["dotnet", "build", HOST, "-p:RevitVersion=2024", "-p:HeronTfm=%s" % tfm,
         "-p:OutputPath=%s" % out_dir, "--nologo"],
        stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    if built.returncode != 0:
        said = built.stdout.decode("utf-8", "replace").strip().splitlines()
        return 1, "the host did not build for %s:\n    %s" % (
            tfm, "\n    ".join(said[-20:]))
    dll = os.path.join(out_dir, "Heron.Folders.TestHost.dll")
    if not os.path.isfile(dll):
        return 1, "built, and %s is not there" % dll
    return 0, dll


def scratch_user(work, name="user"):
    """An environment in which .NET off Windows finds both special folders
    under a fresh home in `work`.

    OFF WINDOWS, .NET ANSWERS "" FOR A SPECIAL FOLDER THAT DOES NOT EXIST,
    and HeronPaths then hands back a RELATIVE path - measured 2026-11-16 with
    a home that had no .local/share. A CI runner's home is not promised to
    have one. So the host is asked as a user whose profile has both, which
    is every Windows profile; the answer is a template either way, and
    tests/test_heron_folders.py holds two such users to the same list.
    """
    home = os.path.join(work, name)
    for sub in (".config", os.path.join(".local", "share")):
        os.makedirs(os.path.join(home, sub), exist_ok=True)
    env = dict(os.environ, HOME=home)
    for var in ("XDG_CONFIG_HOME", "XDG_DATA_HOME"):
        env.pop(var, None)
    return env


def ask_host(dll, env=None):
    """(0, the list as HeronPaths says it) or (1, why).

    With no `env`, asked as this user on Windows and as a scratch user
    anywhere else (scratch_user, above).
    """
    work = None
    if env is None and os.name != "nt":
        work = tempfile.mkdtemp(prefix="heron-folders-user-")
        env = scratch_user(work)
    try:
        done = subprocess.run(["dotnet", dll], stdout=subprocess.PIPE,
                              stderr=subprocess.PIPE, env=env)
    finally:
        if work:
            shutil.rmtree(work, ignore_errors=True)
    if done.returncode != 0:
        return 1, (done.stderr.decode("utf-8", "replace").strip()
                   or "the host exited %d and said nothing" % done.returncode)
    return 0, done.stdout.decode("utf-8").replace("\r\n", "\n")


def generate():
    """(0, text), or (1 or 3, why) - built in a temporary folder and removed."""
    work = tempfile.mkdtemp(prefix="heron-folders-")
    try:
        code, dll = build_host(work)
        if code != 0:
            return code, dll
        return ask_host(dll)
    finally:
        shutil.rmtree(work, ignore_errors=True)


def differences(committed, generated):
    """The lines of a unified diff from the committed list to the code's, or []."""
    if committed.replace("\r\n", "\n") == generated:
        return []
    return list(difflib.unified_diff(
        committed.replace("\r\n", "\n").splitlines(), generated.splitlines(),
        "committed", "what HeronPaths says", lineterm=""))


def _rel(path):
    try:
        return os.path.relpath(path, ROOT).replace(os.sep, "/")
    except ValueError:
        return path


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n")[0])
    parser.add_argument("--write", action="store_true",
                        help="regenerate the list from HeronPaths")
    parser.add_argument("--to", help="with --write: write this file instead")
    parser.add_argument("--against", help="check this file instead of the committed list")
    args = parser.parse_args(argv)

    if args.to and not args.write:
        parser.error("--to goes with --write")
    if args.write and args.against:
        parser.error("--write and --against are two different runs")

    code, said = generate()
    if code == COULD_NOT_RUN:
        print("NOT RUN - %s" % said)
        print("Exit 3, which is NOT a pass: the list was not compared with the code.")
        return COULD_NOT_RUN
    if code != 0:
        print("FAILED - %s" % said)
        return 1

    if args.write:
        target = args.to or LIST
        with io.open(target, "w", encoding="ascii", newline="\n") as fh:
            fh.write(said)
        print("Wrote %s from HeronPaths." % _rel(target))
        return 0

    target = args.against or LIST
    if not os.path.isfile(target):
        print("FAILED - there is no folder list at %s, so the install scripts would"
              % _rel(target))
        print("         stop. Write it from the code:  python tools/heron-folders.py --write")
        return 1
    committed = io.open(target, encoding="utf-8", errors="replace").read()
    diff = differences(committed, said)
    if diff:
        print("FAILED - %s is not what HeronPaths says. It is written by the code" % _rel(target))
        print("         and never by hand (Q-58). The difference:")
        for line in diff:
            print("    %s" % line)
        print("If HeronPaths changed, regenerate it and commit the result:")
        print("    python tools/heron-folders.py --write")
        print("If the file was edited by hand, the edit belongs in")
        print("platform/Heron.Core/HeronPaths.cs, then regenerate.")
        return 1
    print("The folder list at %s is what HeronPaths says." % _rel(target))
    return 0


if __name__ == "__main__":
    sys.exit(main())
