# -*- coding: utf-8 -*-
# Heron-Agent:  none
# Heron-Step:   17
# Heron-Status: DRAFT
# Heron-Since:  0.1.0
# Heron-Layer:  tool
# See docs/29-metadata-standard.md

"""
requirements-lock.txt - regenerated the one way its header says, and audited.

    python tools/lock-python.py                    regenerate, every version held
    python tools/lock-python.py --upgrade          regenerate at the newest versions
    python tools/lock-python.py --upgrade pyjwt    ... the newest of the named ones only
    python tools/lock-python.py --audit            known advisories on the pinned versions

WHY IT EXISTS
-------------
The lock is what HeronInstaller.exe installs and what heron-ready names on
Windows: every version exact, every file hashed. Its header says never to edit
a line by hand - a hand-edited version no longer matches its hashes, and pip
refuses the whole file - and gives the uv line that writes it. A recipe that
has to be retyped is a recipe that is retyped wrong, so this runs it.

REGENERATE. It runs the header's recipe: `uv pip compile` of what
requirements.txt requires plus mcp, constrained to the versions in the lock
now, for Windows (x86_64-pc-windows-msvc) and Python 3.12, with
--generate-hashes. The header is kept as it stands - it is the record of what
was measured - and the body is uv's. Every version held, it should write the
same bytes: measured 2026-10-17, it did. --upgrade lets the named packages, or
all of them, move to the newest that resolve. Whenever a version moves it says
which, and that the header's measurement no longer covers the file: re-measure
the way the header says, then correct it by hand.

AUDIT. `pip-audit` asks the PyPI advisory database about each pinned
name==version, with --disable-pip: nothing is installed, so the pywin32 line
that has no Linux wheel does not stop it. The plain `pip-audit -r` installs
the file first and fails on exactly that line - measured 2026-10-17.

EXIT CODES. 0 when it did what was asked - the audit included, whatever it
found: an advisory is a question for a person (does Heron reach that code?
move the pin?), not a failure. 2 when it could not run - no uv, no pip-audit,
no network - and a NOT RUN is never reported as clean.
"""

import argparse
import io
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOCK = os.path.join(ROOT, "requirements-lock.txt")
REQUIRED = os.path.join(ROOT, "requirements.txt")

#: What the lock is for, as its header's recipe says.
PLATFORM = "x86_64-pc-windows-msvc"
PYTHON = "3.12"

#: What the lock holds beyond requirements.txt - the line between Claude Code
#: and Heron, optional to Heron's own tools and required for a chat.
EXTRA = ("mcp",)

PIN = re.compile(r"^([A-Za-z0-9][A-Za-z0-9._-]*)==([^\s\\;]+)", re.M)

COULD_NOT_RUN = 2

try:
    sys.stdout.reconfigure(encoding="utf-8", errors="replace")
except AttributeError:
    pass


def key(name):
    """A package name the way pip compares them."""
    return re.sub(r"[-_.]+", "-", name).lower()


def read(path):
    with io.open(path, encoding="utf-8") as handle:
        return handle.read()


def split_lock(text):
    """(header, body): the comments and blanks before the first package, and the rest."""
    lines = text.splitlines(True)
    for number, line in enumerate(lines):
        if line.strip() and not line.startswith("#"):
            return "".join(lines[:number]), "".join(lines[number:])
    return text, ""


def pins(text):
    """{key: (name, version)} of every name==version line."""
    return dict((key(m.group(1)), (m.group(1), m.group(2))) for m in PIN.finditer(text))


def roots():
    """What the lock is compiled from: requirements.txt's packages, then mcp."""
    names = []
    for line in read(REQUIRED).splitlines():
        line = line.split("#", 1)[0].strip()
        if line:
            names.append(line)
    have = set(key(re.split(r"[<>=!~;\[\s]", n, maxsplit=1)[0]) for n in names)
    return names + [name for name in EXTRA if key(name) not in have]


def not_run(why, then=None):
    print("NOT RUN - %s" % why)
    if then:
        print("  %s" % then)
    return COULD_NOT_RUN


# ---------------------------------------------------------------------------
# Regenerate
# ---------------------------------------------------------------------------

def regenerate(upgrade):
    """Write requirements-lock.txt again. `upgrade`: None holds every version,
    [] frees every one, a list of names frees those only."""
    uv = shutil.which("uv")
    if uv is None:
        return not_run("uv is not installed, and the lock's recipe is uv's",
                       "pip install --user uv - or https://docs.astral.sh/uv/")
    if not os.path.exists(LOCK):
        return not_run("requirements-lock.txt is missing. Its header is the record "
                       "of what was measured, and this keeps it - it never writes one")
    before = read(LOCK)
    header, body = split_lock(before)
    held = pins(body)
    freed = set()
    if upgrade is not None:
        freed = set(held) if not upgrade else set(key(n) for n in upgrade)
        unknown = sorted(n for n in (upgrade or []) if key(n) not in held)
        if unknown:
            return not_run("not in the lock, so there is no version to free: %s"
                           % ", ".join(unknown))

    work = tempfile.mkdtemp(prefix="heron-lock-")
    try:
        wanted = os.path.join(work, "roots.in")
        limits = os.path.join(work, "measured.txt")
        out = os.path.join(work, "requirements-lock.txt")
        with io.open(wanted, "w", encoding="utf-8") as handle:
            handle.write("\n".join(roots()) + "\n")
        with io.open(limits, "w", encoding="utf-8") as handle:
            handle.write("".join("%s==%s\n" % pin for k, pin in sorted(held.items())
                                 if k not in freed))
        command = [uv, "pip", "compile", wanted, "-c", limits,
                   "--python-platform", PLATFORM, "--python-version", PYTHON,
                   "--generate-hashes", "--no-header", "--no-annotate",
                   "--quiet", "-o", out]
        done = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                              encoding="utf-8", errors="replace")
        if done.returncode != 0 or not os.path.exists(out):
            print(done.stdout.rstrip())
            return not_run("uv could not resolve the lock (exit %d) - nothing was "
                           "written" % done.returncode)
        fresh = read(out)
    finally:
        shutil.rmtree(work, ignore_errors=True)

    after = header + fresh
    now = pins(fresh)
    moved = sorted((held[k][0], held[k][1], now[k][1]) for k in held
                   if k in now and held[k][1] != now[k][1])
    added = sorted(now[k] for k in now if k not in held)
    gone = sorted(held[k] for k in held if k not in now)
    with io.open(LOCK, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(after)

    print("requirements-lock.txt regenerated - %d packages, for %s and Python %s"
          % (len(now), PLATFORM, PYTHON))
    print("  %s" % ("the same bytes as before" if after == before
                    else "the file changed - read `git diff requirements-lock.txt`"))
    for name, old, new in moved:
        print("  moved   %s %s -> %s" % (name, old, new))
    for name, version in added:
        print("  added   %s %s" % (name, version))
    for name, version in gone:
        print("  removed %s %s" % (name, version))
    if moved or added or gone:
        print()
        print("A VERSION MOVED, SO THE HEADER'S MEASUREMENT NO LONGER COVERS THIS FILE.")
        print("Measure it again the way the header says - a fresh virtual environment")
        print("with --require-hashes, tests/test_mcp_serves.py, and `pip download")
        print("--platform win_amd64` for Python 3.11 to 3.14 - then correct the header.")
    else:
        print("  no version moved - the header's measurement still stands")
    return 0


# ---------------------------------------------------------------------------
# Audit
# ---------------------------------------------------------------------------

def pip_audit():
    """The command that runs pip-audit here, or None."""
    found = shutil.which("pip-audit")
    if found:
        return [found]
    try:
        import importlib.util
        if importlib.util.find_spec("pip_audit") is not None:
            return [sys.executable, "-m", "pip_audit"]
    except (ImportError, ValueError):
        pass
    return None


def first_line(text, width=100):
    """The first line of an advisory's description that says something."""
    for line in (text or "").splitlines():
        line = re.sub(r"^(summary|description|impact)\b[:\s]*", "",
                      line.strip().lstrip("#").strip(), flags=re.I)
        if line:
            return line if len(line) <= width else line[:width - 3] + "..."
    return ""


def audit():
    if not os.path.exists(LOCK):
        return not_run("requirements-lock.txt is missing")
    command = pip_audit()
    if command is None:
        return not_run("pip-audit is not installed", "python -m pip install pip-audit")
    done = subprocess.run(command + ["-r", LOCK, "--require-hashes", "--disable-pip",
                                     "--progress-spinner", "off", "-f", "json"],
                          stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                          encoding="utf-8", errors="replace", cwd=ROOT)
    try:
        report = json.loads(done.stdout)
        found = report["dependencies"]
    except (ValueError, KeyError, TypeError):
        print("\n".join(done.stderr.strip().splitlines()[-6:]))
        return not_run("pip-audit gave no report (exit %d) - nothing was checked"
                       % done.returncode)

    actions = os.environ.get("GITHUB_ACTIONS") == "true"
    hits = [d for d in found if d.get("vulns")]
    skipped = [d for d in found if d.get("skip_reason")]
    count = sum(len(d["vulns"]) for d in hits)
    print("requirements-lock.txt - %d pinned packages asked of the PyPI advisory "
          "database, with pip-audit" % len(found))
    for dependency in hits:
        print()
        print("  %s %s" % (dependency["name"], dependency["version"]))
        for vuln in dependency["vulns"]:
            fixed = ", ".join(vuln.get("fix_versions") or []) or "no fixed version yet"
            aliases = ", ".join(vuln.get("aliases") or [])
            print("    %s%s - fixed in %s" % (vuln["id"],
                                             " (%s)" % aliases if aliases else "", fixed))
            said = first_line(vuln.get("description"))
            if said:
                print("      %s" % said)
            if actions:
                print("::warning title=Advisory on a pinned package::%s %s - %s, fixed in %s"
                      % (dependency["name"], dependency["version"], vuln["id"], fixed))
    for dependency in skipped:
        print("  NOT CHECKED %s - %s" % (dependency["name"], dependency["skip_reason"]))
    print()
    if not hits:
        print("No known advisory on any pinned version%s."
              % (" checked" if skipped else ""))
        return 0
    print("%d advisor%s on %d package%s. A FINDING IS A QUESTION, NOT A FAILURE:"
          % (count, "y" if count == 1 else "ies", len(hits), "" if len(hits) == 1 else "s"))
    print("does Heron reach that code? If the pin should move:")
    print("  python tools/lock-python.py --upgrade %s"
          % " ".join(sorted(d["name"] for d in hits)))
    print("then measure the lock again the way its header says.")
    return 0


def main(argv=None):
    parser = argparse.ArgumentParser(
        description="Regenerate requirements-lock.txt the way its header says, "
                    "or audit its pinned versions for known advisories.")
    parser.add_argument("--audit", action="store_true",
                        help="check the pinned versions with pip-audit; changes nothing")
    parser.add_argument("--upgrade", nargs="*", metavar="NAME",
                        help="let these packages - or, with no name, every one - "
                             "move to the newest version that resolves")
    args = parser.parse_args(argv)
    if args.audit and args.upgrade is not None:
        parser.error("--audit changes nothing; run --upgrade on its own")
    return audit() if args.audit else regenerate(args.upgrade)


if __name__ == "__main__":
    sys.exit(main())
