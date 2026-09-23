# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   1
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  test
# See docs/29-metadata-standard.md

"""
The cloud setup script - does it install what it says, on the image it meets?

    python tests/test_cloud_setup.py

tools/cloud-setup.sh is pasted into a Claude Code cloud environment's setup
box and runs once, as root, before any session. On 2026-09-22 it installed
NOTHING on that image - no .NET SDK, none of the five Python packages - and
still exited 0 with nothing said (FRAGMENT-ISSUES row 5b-167). Its own logs
recorded why, and both causes are modelled here:

  apt-get update  exited 100: two third-party PPAs the image carries answered
                  403, "no longer signed". The Ubuntu archive's own lists came
                  down fine - but the install was chained on with `&&`.
  pip             cannot uninstall a package Debian installed, because Debian
                  leaves no RECORD file. `--upgrade` asked for a newer PyYAML
                  than Debian's 6.0.1; mcp needs a newer PyJWT than Debian's
                  2.7.0. Either one stops the whole install.

This runs the REAL script, in a sandbox, with stand-ins for apt-get, pip,
dotnet and python that behave the way those logs recorded. Nothing is
installed and the real /tmp/heron-*.log files are not touched.

WHAT THIS PROVES
  1. A FAILED `apt-get update` DOES NOT STOP THE INSTALL: dotnet-sdk-10.0 and
     python-is-python3 are still asked for, and dotnet then answers.
  2. PIP NEVER HAS TO UNINSTALL A DISTRIBUTION'S PACKAGE: all five arrive on
     the image that refused them - and on an image with no such conflict the
     plain install is the only one made, so a re-run touches nothing.
  3. IT SAYS WHAT DID NOT ARRIVE. The exit code cannot carry a failure, so the
     log does: each package is imported and each one that fails is named,
     with the log that says why; a missing dotnet is named the same way.
  4. IT EXITS 0 WHATEVER HAPPENS - a non-zero exit fails the whole session
     (docs/38 section 5).
  5. It keeps its logs where HERON_SETUP_LOGS says, and the knowledge folder
     where HERON_KNOWLEDGE says, so a sandbox is a sandbox.

WHAT IT CANNOT PROVE
  That the next cloud image behaves like the last one. The stand-ins are the
  recorded behaviour of ONE image on ONE day; a new failure mode is found the
  way this one was, by reading the setup logs of a fresh session.

Needs bash. Without it this exits 3 - could not run - which is not a pass.
"""

import io
import os
import shutil
import stat
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SCRIPT = os.path.join(ROOT, "tools", "cloud-setup.sh")

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass

FAILURES = []

# The five the script installs, as pip names them and as Python imports them.
PACKAGES = {"pyyaml": "yaml", "mcp": "mcp", "model2vec": "model2vec",
            "sqlite-vec": "sqlite_vec", "pypdf": "pypdf"}

# THE STAND-INS. Each one appends what it was asked to $SANDBOX/calls, so the
# test reads what the script DID rather than what its text says.
APT_GET = r'''#!/bin/bash
echo "apt-get $*" >> "$SANDBOX/calls"
case "$1" in
  update)
    [ "$APT_UPDATE" = fail ] || exit 0
    echo "E: Failed to fetch https://ppa.launchpadcontent.net/deadsnakes/ppa/ubuntu/dists/noble/InRelease  403  Forbidden" >&2
    echo "E: The repository 'https://ppa.launchpadcontent.net/deadsnakes/ppa/ubuntu noble InRelease' is no longer signed." >&2
    exit 100 ;;
  install)
    if [ "$APT_INSTALL" = fail ]; then
      echo "E: Unable to locate package dotnet-sdk-10.0" >&2
      exit 100
    fi
    for a in "$@"; do
      [ "$a" = dotnet-sdk-10.0 ] && touch "$SANDBOX/has-dotnet"
    done
    exit 0 ;;
esac
exit 0
'''

# Debian's own copies: PyYAML imports without pip (it is pure Python once its
# C part is refused), PyJWT is too old for mcp. Neither can be uninstalled.
PIP = r'''#!/bin/bash
echo "pip $*" >> "$SANDBOX/calls"
ignore=0; upgrade=0; pkgs=()
for a in "$@"; do
  case "$a" in
    install) ;;
    --ignore-installed) ignore=1 ;;
    --upgrade|-U) upgrade=1 ;;
    -*) ;;
    *) pkgs+=("$a") ;;
  esac
done
if [ "$PIP_OFFLINE" = 1 ]; then
  echo "ERROR: Could not find a version that satisfies the requirement mcp" >&2
  exit 1
fi
if [ "$ignore" = 0 ] && [ "$DISTRO_CLEAN" != 1 ]; then
  if [ "$upgrade" = 1 ]; then
    echo "ERROR: Cannot uninstall PyYAML 6.0.1, RECORD file not found. Hint: The package was installed by debian." >&2
    exit 1
  fi
  for p in "${pkgs[@]}"; do
    if [ "$p" = mcp ]; then
      echo "ERROR: Cannot uninstall PyJWT 2.7.0, RECORD file not found. Hint: The package was installed by debian." >&2
      exit 1
    fi
  done
fi
for p in "${pkgs[@]}"; do echo "$p" >> "$SANDBOX/installed"; done
exit 0
'''

DOTNET = r'''#!/bin/bash
echo "dotnet $*" >> "$SANDBOX/calls"
if [ ! -f "$SANDBOX/has-dotnet" ]; then
  echo "dotnet: command not found" >&2
  exit 127
fi
[ "$1" = "--version" ] && echo "10.0.112"
exit 0
'''

# `python -c "import X"` answers from what pip recorded; yaml is Debian's and
# imports regardless. Anything else - the version, the store rebuild - is fine.
PYTHON = r'''#!/bin/bash
echo "python $*" >> "$SANDBOX/calls"
if [ "$1" = "--version" ]; then echo "Python 3.11.15"; exit 0; fi
if [ "$1" = "-c" ]; then
  mod=$(printf '%s' "$2" | sed -n 's/^import \([A-Za-z_0-9]*\).*/\1/p')
  [ -z "$mod" ] && exit 0
  [ "$mod" = yaml ] && exit 0
  case "$mod" in sqlite_vec) pkg=sqlite-vec ;; *) pkg="$mod" ;; esac
  grep -qx "$pkg" "$SANDBOX/installed" 2>/dev/null && exit 0
  echo "ModuleNotFoundError: No module named '$mod'" >&2
  exit 1
fi
exit 0
'''


def check(condition, what):
    print("  %-5s %s" % ("ok" if condition else "FAIL", what))
    if not condition:
        FAILURES.append(what)


def write_exe(path, text):
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    os.chmod(path, os.stat(path).st_mode | stat.S_IXUSR | stat.S_IXGRP
             | stat.S_IXOTH)


def read(path):
    try:
        with io.open(path, encoding="utf-8", errors="replace") as f:
            return f.read()
    except (IOError, OSError):
        return ""


def run(**world):
    """Run the real script in a fresh sandbox. (exit code, output, sandbox)."""
    box = tempfile.mkdtemp(prefix="heron-setup-")
    bindir = os.path.join(box, "bin")
    repo = os.path.join(box, "repo")
    os.makedirs(bindir)
    os.makedirs(os.path.join(repo, "brain"))
    os.makedirs(os.path.join(box, "logs"))
    io.open(os.path.join(repo, "HERON_CONSTITUTION.md"), "w").close()
    for name, text in (("apt-get", APT_GET), ("pip", PIP),
                       ("dotnet", DOTNET), ("python", PYTHON)):
        write_exe(os.path.join(bindir, name), text)
    env = dict(os.environ)
    env.update({
        "PATH": bindir + os.pathsep + env.get("PATH", ""),
        "SANDBOX": box,
        "HERON_SETUP_LOGS": os.path.join(box, "logs"),
        "HERON_KNOWLEDGE": os.path.join(box, "kb"),
    })
    env.update(world)
    done = subprocess.run(["bash", SCRIPT], cwd=repo, env=env,
                          capture_output=True, timeout=120)
    out = (done.stdout + done.stderr).decode("utf-8", "replace")
    return done.returncode, out, box


def calls(box, prefix):
    return [line for line in read(os.path.join(box, "calls")).splitlines()
            if line.startswith(prefix)]


def installed(box):
    return set(read(os.path.join(box, "installed")).split())


def main():
    if not shutil.which("bash"):
        print("NOT RUN - there is no bash here, and the setup script is bash.")
        return 3

    real_logs = {}
    for name in ("apt", "pip", "rebuild", "restore"):
        path = "/tmp/heron-%s.log" % name
        if os.path.exists(path):
            real_logs[path] = (os.path.getmtime(path), read(path))

    boxes = []
    try:
        print("The image of 2026-09-22 - update refused, Debian's packages in the way")
        code, out, box = run(APT_UPDATE="fail")
        boxes.append(box)
        check(code == 0, "it exits 0 (exit %s)" % code)
        installs = calls(box, "apt-get install")
        check(any("dotnet-sdk-10.0" in c and "python-is-python3" in c
                  for c in installs),
              "the install is still asked for after the update failed")
        check("[heron] dotnet 10.0.112" in out,
              "and dotnet answers afterwards")
        check(set(PACKAGES) <= installed(box),
              "all five Python packages arrive - got %s"
              % (sorted(installed(box)) or "none"))
        check(any("--ignore-installed" in c for c in calls(box, "pip")),
              "by installing beside Debian's copies, never removing one")
        check("MISSING" not in out and "NOT INSTALLED" not in out,
              "and nothing is reported missing")
        check(any(c.startswith("dotnet restore") for c in calls(box, "dotnet"))
              and "NuGet warm-up unfinished" not in out,
              "so the NuGet warm-up runs, and finishes")
        apt_log = read(os.path.join(box, "logs", "heron-apt.log"))
        check("403" in apt_log,
              "the refused update is kept in the log it wrote")

        print("")
        print("An image with nothing in the way")
        code, out, box = run(DISTRO_CLEAN="1")
        boxes.append(box)
        check(code == 0, "it exits 0 (exit %s)" % code)
        pip_calls = calls(box, "pip")
        check(len(pip_calls) == 1 and "--ignore-installed" not in pip_calls[0],
              "one plain pip install and no other, so a re-run touches "
              "nothing - %s" % pip_calls)
        check(set(PACKAGES) <= installed(box), "all five arrive")

        print("")
        print("An image where nothing can be installed")
        code, out, box = run(APT_UPDATE="fail", APT_INSTALL="fail",
                             PIP_OFFLINE="1")
        boxes.append(box)
        check(code == 0, "it STILL exits 0 - a non-zero exit fails the "
                         "session (exit %s)" % code)
        missing = [line for line in out.splitlines() if "MISSING" in line]
        check(len(missing) == 1, "one line names what did not arrive - %s"
              % missing)
        named = missing[0] if missing else ""
        check(all(mod in named for mod in
                  ("mcp", "model2vec", "sqlite_vec", "pypdf")),
              "it names every package that does not import")
        check("yaml" not in named.split("MISSING", 1)[-1].split(" - ")[0],
              "and not Debian's PyYAML, which does")
        check("heron-pip.log" in named, "and says which log explains it")
        check(any("dotnet" in line and "NOT INSTALLED" in line
                  and "heron-apt.log" in line for line in out.splitlines()),
              "a missing dotnet is named with its log too")

        print("")
        print("A sandbox is a sandbox")
        check(all(os.path.isdir(os.path.join(b, "kb")) for b in boxes),
              "the knowledge folder is the one HERON_KNOWLEDGE names")
        after = {}
        for path in real_logs:
            after[path] = (os.path.getmtime(path), read(path))
        check(after == real_logs,
              "the real /tmp/heron-*.log files are untouched (%d present)"
              % len(real_logs))
    finally:
        for b in boxes:
            shutil.rmtree(b, ignore_errors=True)

    print("")
    if FAILURES:
        print("FAILED - %d check(s):" % len(FAILURES))
        for what in FAILURES:
            print("  - " + what)
        return 1
    print("PASSED - the setup script installs past a refused update and past")
    print("Debian's own packages, names whatever still did not arrive, and")
    print("exits 0. It is proved against stand-ins recorded from one image;")
    print("the next image is read from its own setup logs.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
